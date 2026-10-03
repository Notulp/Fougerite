using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace Fougerite.Tools
{
    /// <summary>
    /// Determines who is allowed to join when native Steam authentication rejects a connection,
    /// which happens for RustBuster clients on Spacewar, for cracked clients and for Rust tickets that fail.
    /// The value is read from the SteamAuthMode option in Fougerite.cfg.
    /// </summary>
    public enum SteamAuthMode
    {
        /// <summary>
        /// Keeps the original behaviour. Nothing is enforced and plugins such as AuthAllow decide through
        /// <see cref="Fougerite.Events.SteamDenyEvent.ForceAllow"/>.
        /// </summary>
        Legacy = 0,

        /// <summary>
        /// Admits only players whose ticket passes native Steam authentication for Rust. Every connection that
        /// reaches SteamDeny is rejected.
        /// </summary>
        RustOnly = 1,

        /// <summary>
        /// Admits Rust players as well as Spacewar players whose ticket is confirmed by the Steam Web API and whose
        /// account owns Rust. Requires a Web API key and public game details on the player's profile.
        /// </summary>
        RustOwners = 2,

        /// <summary>
        /// Admits Rust players as well as any genuine Steam account using a Spacewar ticket confirmed by the
        /// Steam Web API. Forged and emulated tickets are rejected. Requires a Web API key.
        /// </summary>
        SteamAccounts = 3,

        /// <summary>
        /// Admits Rust players as well as Spacewar players whose ticket passes every offline check in
        /// <see cref="SteamAPITools.IsPlausibleTicket"/>. No Web API key is used and no request leaves the server,
        /// which makes this the fastest Steam only mode. Broken emulators and careless forgeries are rejected,
        /// but a carefully forged ticket is accepted and may claim any SteamID.
        /// </summary>
        SteamAccountsUnverified = 4,

        /// <summary>
        /// Admits everybody, including players that do not run Steam at all.
        /// </summary>
        AllowAll = 5
    }

    /// <summary>
    /// Describes the state of a Steam Web API ticket validation.
    /// </summary>
    public enum SteamWebValidationStatus
    {
        /// <summary>The request is still running.</summary>
        Pending,
        /// <summary>The ticket is genuine and the account owns Rust whenever ownership was required.</summary>
        Verified,
        /// <summary>Steam rejected the ticket because it is forged, expired, cancelled or issued for another app.</summary>
        InvalidTicket,
        /// <summary>The ticket is genuine but belongs to a different SteamID than the one the client claimed.</summary>
        SteamIdMismatch,
        /// <summary>The ticket is genuine but the account does not own Rust.</summary>
        NotRustOwner,
        /// <summary>The ticket is genuine but ownership cannot be read because the profile hides its game details.</summary>
        OwnershipPrivate,
        /// <summary>Steam could not be asked because of a transport error, a rate limit, an outage or an unreadable response.</summary>
        ApiError,
        /// <summary>Steam refused the configured key with HTTP 401 or 403. This is never covered by SteamWebAPIFailOpen.</summary>
        ApiKeyRejected,
        /// <summary>Steam did not answer within the configured timeout.</summary>
        TimedOut
    }

    /// <summary>
    /// Holds the result of validating the ticket of a single connection against the Steam Web API.
    /// The result is written by a ThreadPool thread and read on the main thread, every member is thread safe.
    /// </summary>
    public class SteamWebValidation
    {
        private readonly object _lock = new object();
        private SteamWebValidationStatus _status = SteamWebValidationStatus.Pending;
        private string _detail = string.Empty;
        private int _attempts;
        private bool _sawInvalidTicket;
        private System.Threading.Timer _retryTimer;

        internal SteamWebValidation(ulong claimedSteamId, uint appId, bool requireRustOwnership, float maxWaitSeconds)
        {
            ClaimedSteamId = claimedSteamId;
            AppId = appId;
            RequireRustOwnership = requireRustOwnership;
            MaxWaitSeconds = maxWaitSeconds;
            CreatedUtc = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets the SteamID the client claimed in its connection data.
        /// </summary>
        public ulong ClaimedSteamId { get; private set; }

        /// <summary>
        /// Gets the AppID the ticket is validated against.
        /// </summary>
        public uint AppId { get; private set; }

        /// <summary>
        /// Gets a value indicating whether Rust ownership is checked in addition to the ticket, as done by <see cref="SteamAuthMode.RustOwners"/>.
        /// </summary>
        public bool RequireRustOwnership { get; private set; }

        /// <summary>
        /// Gets the time the validation started.
        /// </summary>
        public DateTime CreatedUtc { get; private set; }

        internal float MaxWaitSeconds { get; private set; }

        internal string RequestTicketHex { get; private set; }

        internal string RequestIdentity { get; private set; }

        /// <summary>
        /// Gets the number of padding bytes that were removed from the client ticket before it was sent to Steam.
        /// </summary>
        public int TrimmedBytes { get; private set; }

        /// <summary>
        /// Gets the number of AuthenticateUserTicket requests that were sent for this connection.
        /// </summary>
        public int Attempts
        {
            get { lock (_lock) return _attempts; }
        }

        /// <summary>
        /// Gets the SteamID Steam returned for the ticket, or zero when Steam did not accept it.
        /// </summary>
        public ulong VerifiedSteamId { get; private set; }

        /// <summary>
        /// Gets the owner of the license the ticket was issued for, which differs from <see cref="VerifiedSteamId"/> when the game is family shared.
        /// </summary>
        public ulong OwnerSteamId { get; private set; }

        /// <summary>
        /// Gets the VAC ban flag Steam returned for the app of the ticket.
        /// </summary>
        public bool VacBanned { get; private set; }

        /// <summary>
        /// Gets the publisher ban flag Steam returned for the app of the ticket.
        /// </summary>
        public bool PublisherBanned { get; private set; }

        /// <summary>
        /// Gets whether the account owns Rust, or null when ownership was not checked or could not be determined.
        /// </summary>
        public bool? OwnsRust { get; private set; }

        /// <summary>
        /// Gets the HTTP status code of the most recent request.
        /// </summary>
        public int LastHttpStatus { get; private set; }

        /// <summary>
        /// Gets the current status of the validation.
        /// </summary>
        public SteamWebValidationStatus Status
        {
            get { lock (_lock) return _status; }
        }

        /// <summary>
        /// Gets a human readable description of the result.
        /// </summary>
        public string Detail
        {
            get { lock (_lock) return _detail; }
        }

        /// <summary>
        /// Gets a value indicating whether a final status has been reached.
        /// </summary>
        public bool IsCompleted
        {
            get { return Status != SteamWebValidationStatus.Pending; }
        }

        /// <summary>
        /// Gets a value indicating whether Steam confirmed that the ticket belongs to the claimed account, regardless of Rust ownership.
        /// </summary>
        public bool IsGenuineSteamAccount
        {
            get
            {
                SteamWebValidationStatus s = Status;
                return s == SteamWebValidationStatus.Verified
                       || s == SteamWebValidationStatus.NotRustOwner
                       || s == SteamWebValidationStatus.OwnershipPrivate;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the outcome is unknown because Steam could not be reached in time.
        /// A rejected API key is deliberately not counted as a failure of this kind.
        /// </summary>
        public bool IsApiFailure
        {
            get
            {
                SteamWebValidationStatus s = Status;
                return s == SteamWebValidationStatus.ApiError || s == SteamWebValidationStatus.TimedOut;
            }
        }

        internal void SetTicketData(ulong steamId, ulong ownerSteamId, bool vacBanned, bool publisherBanned)
        {
            lock (_lock)
            {
                VerifiedSteamId = steamId;
                OwnerSteamId = ownerSteamId;
                VacBanned = vacBanned;
                PublisherBanned = publisherBanned;
            }
        }

        internal void SetRequest(string ticketHex, string identity, int trimmedBytes)
        {
            RequestTicketHex = ticketHex;
            RequestIdentity = identity;
            TrimmedBytes = trimmedBytes;
        }

        internal int BeginAttempt()
        {
            lock (_lock)
            {
                return ++_attempts;
            }
        }

        internal void MarkInvalidTicketSeen()
        {
            lock (_lock)
            {
                _sawInvalidTicket = true;
            }
        }

        internal void SetRetryTimer(System.Threading.Timer timer)
        {
            lock (_lock)
            {
                if (_status != SteamWebValidationStatus.Pending)
                {
                    timer.Dispose();
                    return;
                }

                if (_retryTimer != null)
                {
                    _retryTimer.Dispose();
                }
                _retryTimer = timer;
            }
        }

        internal void ReleaseRetryTimer()
        {
            lock (_lock)
            {
                if (_retryTimer != null)
                {
                    _retryTimer.Dispose();
                    _retryTimer = null;
                }
            }
        }

        /// <summary>
        /// Finalizes a validation whose deadline passed. When Steam already called the ticket invalid the result
        /// stays a rejection, so that a forged ticket can never slip through SteamWebAPIFailOpen just because its
        /// retries ran out of time.
        /// </summary>
        internal void CompleteOnDeadline()
        {
            bool sawInvalid;
            lock (_lock)
            {
                sawInvalid = _sawInvalidTicket;
            }

            if (sawInvalid)
            {
                TryComplete(SteamWebValidationStatus.InvalidTicket,
                    $"101 Invalid ticket, deadline reached after {Attempts} attempt(s)");
            }
            else
            {
                TryComplete(SteamWebValidationStatus.TimedOut, $"No answer within {MaxWaitSeconds:0.#}s");
            }
        }

        internal void SetHttpStatus(int code)
        {
            lock (_lock)
            {
                LastHttpStatus = code;
            }
        }

        internal bool TryComplete(SteamWebValidationStatus status, string detail, bool? ownsRust = null)
        {
            lock (_lock)
            {
                // First final answer wins, a late HTTP response can't overwrite a timeout.
                if (_status != SteamWebValidationStatus.Pending)
                {
                    return false;
                }

                _status = status;
                _detail = detail ?? string.Empty;
                if (_retryTimer != null)
                {
                    _retryTimer.Dispose();
                    _retryTimer = null;
                }
                if (ownsRust.HasValue)
                {
                    OwnsRust = ownsRust;
                }
                return true;
            }
        }

        public override string ToString()
        {
            return $"{Status} ({Detail}) AppId={AppId} Claimed={ClaimedSteamId} Verified={VerifiedSteamId} " +
                   $"OwnsRust={OwnsRust} Attempts={Attempts} Trimmed={TrimmedBytes}";
        }
    }

    /// <summary>
    /// Validates Spacewar tickets through the Steam Web API and applies the configured <see cref="SteamAuthMode"/>
    /// to connections that native Steam authentication rejected.
    /// </summary>
    public static class SteamTicketValidator
    {
        /// <summary>
        /// The identity string the client passes to ISteamUser.GetAuthTicketForWebApi and the server passes to
        /// AuthenticateUserTicket. Steam rejects the ticket when the two values differ, so a ticket requested for
        /// another service cannot be used here.
        /// </summary>
        public const string WebApiTicketIdentity = "Fougerite";

        /// <summary>
        /// The marker a client writes after its session ticket to announce a Steam Web API ticket.
        /// The value spells FGWT in little endian ASCII.
        /// </summary>
        public const int WebApiTicketMagic = 0x54574746;

        /// <summary>
        /// The version of the Web API ticket extension in the connection data.
        /// </summary>
        public const byte WebApiTicketVersion = 1;

        /// <summary>
        /// The largest Web API ticket that is accepted. Steam tickets are at most 2560 bytes long.
        /// </summary>
        public const int MaxWebApiTicketLength = 2560;

        private static readonly Dictionary<ClientConnection, SteamWebValidation> Validations =
            new Dictionary<ClientConnection, SteamWebValidation>();
        private static readonly object ValidationsLock = new object();
        private static readonly TimeSpan MaxAge = TimeSpan.FromMinutes(5);

        // Steam only accepts a session ticket after the client received GetAuthSessionTicketResponse_t,
        // which can be later than the moment the client connects. Error 101 is retried with these delays.
        private static readonly int[] InvalidTicketRetryDelaysMs = { 750, 1500, 2500 };
        private const int SteamErrorInvalidTicket = 101;

        /// <summary>
        /// Gets a value indicating whether a Steam Web API key is configured.
        /// </summary>
        public static bool IsWebApiConfigured
        {
            get { return !string.IsNullOrEmpty(Bootstrap.SteamWebAPIKey); }
        }

        /// <summary>
        /// Determines whether Spacewar tickets are sent to the Steam Web API in the given mode.
        /// <see cref="SteamAuthMode.AllowAll"/> still validates when a key is configured so that genuine Steam users
        /// are registered correctly. <see cref="SteamAuthMode.SteamAccountsUnverified"/> never contacts Steam.
        /// </summary>
        public static bool ShouldValidate(SteamAuthMode mode)
        {
            return IsWebApiConfigured
                   && (mode == SteamAuthMode.RustOwners
                       || mode == SteamAuthMode.SteamAccounts
                       || mode == SteamAuthMode.AllowAll);
        }

        /// <summary>
        /// Reads an optional ticket from ISteamUser.GetAuthTicketForWebApi that a client may append to its connection
        /// data. Clients are not required to send one, the session ticket is validated when it is absent.
        /// The stream is only touched for Spacewar clients in a mode that uses the Web API, which keeps vanilla
        /// clients from ever reading past the end of their connection data.
        /// Must be called directly after ClientConnection.ReadConnectionData.
        /// </summary>
        /// <param name="cc">The connection whose data was just read.</param>
        /// <param name="loginData">The connection data stream, positioned right after the session ticket.</param>
        /// <param name="mode">The active SteamAuthMode.</param>
        /// <returns>The Web API ticket, or null when it is absent, malformed or not needed.</returns>
        public static byte[] ReadWebApiTicket(ClientConnection cc, uLink.BitStream loginData, SteamAuthMode mode)
        {
            if (cc == null || loginData == null || !ShouldValidate(mode))
            {
                return null;
            }

            SteamTicketInfo info;
            if (!SteamAPITools.TryParseTicket(cc.SteamTicket, out info) || info.AppId != SteamAPITools.SpacewarAppId)
            {
                return null;
            }

            try
            {
                if (loginData.ReadInt32() != WebApiTicketMagic || loginData.ReadByte() != WebApiTicketVersion)
                {
                    return null;
                }

                byte[] ticket = loginData.ReadBytes();
                if (ticket == null || ticket.Length == 0 || ticket.Length > MaxWebApiTicketLength)
                {
                    return null;
                }

                return ticket;
            }
            catch (Exception)
            {
                // Clients without the extension simply end their data after the session ticket.
                return null;
            }
        }

        /// <summary>
        /// Returns the validation that belongs to a connection, or null when none was started.
        /// </summary>
        public static SteamWebValidation Get(ClientConnection cc)
        {
            if (cc == null) return null;
            lock (ValidationsLock)
            {
                SteamWebValidation v;
                return Validations.TryGetValue(cc, out v) ? v : null;
            }
        }

        internal static void Forget(ClientConnection cc)
        {
            if (cc == null) return;
            lock (ValidationsLock)
            {
                Validations.Remove(cc);
            }
        }

        /// <summary>
        /// Starts validating the ticket of a connection. The call returns immediately and the requests run on
        /// the ThreadPool, poll <see cref="SteamWebValidation.IsCompleted"/> from the main thread to learn the outcome.
        /// </summary>
        /// <param name="cc">The connection being validated.</param>
        /// <param name="ticketInfo">The parsed session ticket of the connection.</param>
        /// <param name="webApiTicket">An optional ticket from ISteamUser.GetAuthTicketForWebApi, null to validate the session ticket.</param>
        /// <param name="mode">The active SteamAuthMode.</param>
        /// <returns>The validation, which completes asynchronously.</returns>
        internal static SteamWebValidation Begin(ClientConnection cc, SteamTicketInfo ticketInfo, byte[] webApiTicket,
            SteamAuthMode mode)
        {
            bool requireOwnership = mode == SteamAuthMode.RustOwners;
            float timeout = Bootstrap.SteamWebAPITimeout;
            float retryWindow = InvalidTicketRetryDelaysMs.Sum() / 1000f;
            // Ticket request, its retries, the ownership request and a little slack.
            float maxWait = timeout * (requireOwnership ? 2 : 1) + retryWindow + 1f;

            SteamWebValidation validation = new SteamWebValidation(cc.UserID, ticketInfo.AppId, requireOwnership,
                maxWait);

            if (webApiTicket != null)
            {
                validation.SetRequest(SteamAPITools.ToHex(webApiTicket), WebApiTicketIdentity, 0);
            }
            else
            {
                byte[] issued = SteamAPITools.GetIssuedTicket(cc.SteamTicket, ticketInfo);
                validation.SetRequest(SteamAPITools.ToHex(issued), null, cc.SteamTicket.Length - issued.Length);
            }

            lock (ValidationsLock)
            {
                PurgeOld();
                Validations[cc] = validation;
            }

            SendTicketRequest(validation);
            return validation;
        }

        private static void SendTicketRequest(SteamWebValidation v)
        {
            if (v.IsCompleted)
            {
                return;
            }

            v.BeginAttempt();

            // Never log this URL, it contains the key.
            string url = $"{SteamAPITools.SteamWebApiBaseUrl}ISteamUserAuth/AuthenticateUserTicket/v1/" +
                         $"?key={Uri.EscapeDataString(Bootstrap.SteamWebAPIKey)}" +
                         $"&appid={v.AppId}" +
                         $"&ticket={v.RequestTicketHex}";
            if (v.RequestIdentity != null)
            {
                url += $"&identity={Uri.EscapeDataString(v.RequestIdentity)}";
            }

            try
            {
                WinHttpClient.GetInstance().MakeRequest(url,
                    (code, body) => OnTicketResponse(v, code, body), "GET", null, null, null,
                    Bootstrap.SteamWebAPITimeout);
            }
            catch (Exception ex)
            {
                v.TryComplete(SteamWebValidationStatus.ApiError, $"Request failed to start: {ex.Message}");
            }
        }

        private static void ScheduleTicketRetry(SteamWebValidation v, int delayMs)
        {
            System.Threading.Timer timer = new System.Threading.Timer(_ =>
            {
                v.ReleaseRetryTimer();
                SendTicketRequest(v);
            }, null, delayMs, Timeout.Infinite);
            v.SetRetryTimer(timer);
        }

        /// <summary>
        /// Applies a mode to a connection that native Steam authentication rejected.
        /// </summary>
        /// <param name="mode">The mode to apply.</param>
        /// <param name="cc">The rejected connection.</param>
        /// <param name="ticket">The parsed ticket of the connection, null when it could not be parsed.</param>
        /// <param name="web">The Web API validation of the connection, null when none was performed.</param>
        /// <param name="reason">A description of why the player is admitted or rejected.</param>
        /// <returns>True if the player may join, otherwise false.</returns>
        public static bool Evaluate(SteamAuthMode mode, ClientConnection cc, SteamTicketInfo ticket,
            SteamWebValidation web, out string reason)
        {
            switch (mode)
            {
                case SteamAuthMode.Legacy:
                    reason = "Legacy: decided by plugins";
                    return false;

                case SteamAuthMode.AllowAll:
                    reason = web != null && web.IsGenuineSteamAccount
                        ? "AllowAll: genuine Steam account"
                        : "AllowAll: everyone is allowed";
                    return true;

                case SteamAuthMode.RustOnly:
                    reason = "RustOnly: only tickets that pass native Steam auth for Rust are accepted";
                    return false;

                case SteamAuthMode.RustOwners:
                case SteamAuthMode.SteamAccounts:
                case SteamAuthMode.SteamAccountsUnverified:
                    break;

                default:
                    reason = $"Unknown mode {mode}";
                    return false;
            }

            if (cc == null)
            {
                reason = "No connection";
                return false;
            }

            if (ticket == null)
            {
                reason = "Ticket is missing or malformed (no Steam)";
                return false;
            }

            if (ticket.SteamId != cc.UserID)
            {
                reason = $"Ticket SteamID {ticket.SteamId} does not match claimed {cc.UserID}";
                return false;
            }

            if (ticket.AppId == SteamAPITools.RustAppId)
            {
                reason = "Rust ticket failed native Steam auth";
                return false;
            }

            if (ticket.AppId != SteamAPITools.SpacewarAppId)
            {
                reason = $"Ticket AppID {ticket.AppId} is not accepted";
                return false;
            }

            if (mode == SteamAuthMode.SteamAccountsUnverified)
            {
                string offlineReason;
                bool plausible = SteamAPITools.IsPlausibleTicket(ticket, cc.UserID, SteamAPITools.SpacewarAppId,
                    out offlineReason);
                reason = $"SteamAccountsUnverified: {offlineReason}";
                return plausible;
            }

            if (!IsWebApiConfigured)
            {
                reason = "SteamWebAPIKey is not configured, Spacewar tickets can't be verified";
                return false;
            }

            if (web == null)
            {
                reason = "Ticket was not validated with the Steam Web API";
                return false;
            }

            switch (web.Status)
            {
                case SteamWebValidationStatus.Verified:
                    reason = mode == SteamAuthMode.RustOwners
                        ? "Steam Web API: genuine Spacewar ticket, account owns Rust"
                        : "Steam Web API: genuine Spacewar ticket";
                    return true;

                case SteamWebValidationStatus.ApiError:
                case SteamWebValidationStatus.TimedOut:
                case SteamWebValidationStatus.Pending:
                    if (Bootstrap.SteamWebAPIFailOpen)
                    {
                        reason = $"Steam Web API unavailable ({web.Status}: {web.Detail}), allowed by SteamWebAPIFailOpen";
                        return true;
                    }
                    reason = $"Steam Web API unavailable ({web.Status}: {web.Detail})";
                    return false;

                default:
                    reason = $"Steam Web API: {web.Status} ({web.Detail})";
                    return false;
            }
        }

        private static void OnTicketResponse(SteamWebValidation v, int code, string body)
        {
            try
            {
                v.SetHttpStatus(code);
                if (v.IsCompleted) return;
                if (!CheckHttp(v, code, body)) return;

                JToken response = JObject.Parse(body)["response"];
                if (response == null)
                {
                    v.TryComplete(SteamWebValidationStatus.ApiError, "Response has no 'response' object");
                    return;
                }

                JToken error = response["error"];
                if (error != null)
                {
                    int errorCode = (int?)error["errorcode"] ?? 0;
                    string errorDesc = (string)error["errordesc"];

                    // A genuine ticket reads as invalid until Steam registered it, so 101 is retried a few times.
                    if (errorCode == SteamErrorInvalidTicket)
                    {
                        v.MarkInvalidTicketSeen();
                        int attempt = v.Attempts;
                        if (attempt <= InvalidTicketRetryDelaysMs.Length)
                        {
                            ScheduleTicketRetry(v, InvalidTicketRetryDelaysMs[attempt - 1]);
                            return;
                        }
                    }

                    // Every other error is about the ticket itself (102 other app, 103 expired and so on).
                    v.TryComplete(SteamWebValidationStatus.InvalidTicket,
                        $"{errorCode} {errorDesc} after {v.Attempts} attempt(s)");
                    return;
                }

                JToken p = response["params"];
                if (p == null || !string.Equals((string)p["result"], "OK", StringComparison.OrdinalIgnoreCase))
                {
                    v.TryComplete(SteamWebValidationStatus.InvalidTicket,
                        p == null ? "No params in response" : $"result={(string)p["result"]}");
                    return;
                }

                ulong steamId;
                ulong ownerSteamId;
                ulong.TryParse((string)p["steamid"], out steamId);
                ulong.TryParse((string)p["ownersteamid"], out ownerSteamId);
                v.SetTicketData(steamId, ownerSteamId,
                    p["vacbanned"] != null && (bool)p["vacbanned"],
                    p["publisherbanned"] != null && (bool)p["publisherbanned"]);

                // The important one. A genuine ticket of account A must not let someone join as account B.
                if (steamId == 0 || steamId != v.ClaimedSteamId)
                {
                    v.TryComplete(SteamWebValidationStatus.SteamIdMismatch,
                        $"Ticket belongs to {steamId}, client claimed {v.ClaimedSteamId}");
                    return;
                }

                if (!v.RequireRustOwnership)
                {
                    v.TryComplete(SteamWebValidationStatus.Verified, "Ticket OK");
                    return;
                }

                string url = $"{SteamAPITools.SteamWebApiBaseUrl}IPlayerService/GetOwnedGames/v1/" +
                             $"?key={Uri.EscapeDataString(Bootstrap.SteamWebAPIKey)}" +
                             $"&steamid={steamId}" +
                             "&include_played_free_games=1" +
                             $"&appids_filter%5B0%5D={SteamAPITools.RustAppId}" +
                             "&format=json";

                WinHttpClient.GetInstance().MakeRequest(url,
                    (code2, body2) => OnOwnershipResponse(v, code2, body2), "GET", null, null, null,
                    Bootstrap.SteamWebAPITimeout);
            }
            catch (Exception ex)
            {
                v.TryComplete(SteamWebValidationStatus.ApiError, $"Bad ticket response: {ex.Message}");
            }
        }

        private static void OnOwnershipResponse(SteamWebValidation v, int code, string body)
        {
            try
            {
                v.SetHttpStatus(code);
                if (v.IsCompleted) return;
                if (!CheckHttp(v, code, body)) return;

                JToken response = JObject.Parse(body)["response"];
                if (response == null)
                {
                    v.TryComplete(SteamWebValidationStatus.ApiError, "Response has no 'response' object");
                    return;
                }

                // Private game details come back as an empty response object.
                if (response["game_count"] == null)
                {
                    v.TryComplete(SteamWebValidationStatus.OwnershipPrivate,
                        "Game details are private, can't confirm Rust ownership");
                    return;
                }

                JArray games = response["games"] as JArray;
                bool owns = games != null && games.Any(g =>
                    g["appid"] != null && (uint)g["appid"] == SteamAPITools.RustAppId);

                if (owns)
                {
                    v.TryComplete(SteamWebValidationStatus.Verified, "Ticket OK, owns Rust", true);
                }
                else
                {
                    v.TryComplete(SteamWebValidationStatus.NotRustOwner, "Account does not own Rust", false);
                }
            }
            catch (Exception ex)
            {
                v.TryComplete(SteamWebValidationStatus.ApiError, $"Bad ownership response: {ex.Message}");
            }
        }

        /// <summary>
        /// Translates transport and HTTP errors into a final status and reports whether the body should be parsed.
        /// </summary>
        private static bool CheckHttp(SteamWebValidation v, int code, string body)
        {
            if (code == 0)
            {
                // WinHttpClient reports transport failures with code 0 and a short tag as the body.
                v.TryComplete(SteamWebValidationStatus.ApiError, $"Transport: {body}");
                return false;
            }

            if (code == 401 || code == 403)
            {
                v.TryComplete(SteamWebValidationStatus.ApiKeyRejected, $"HTTP {code}, check SteamWebAPIKey");
                return false;
            }

            if (code == 429)
            {
                v.TryComplete(SteamWebValidationStatus.ApiError, "HTTP 429, rate limited");
                return false;
            }

            // AuthenticateUserTicket answers bad tickets with a JSON error body, sometimes with a 4xx.
            if (code >= 400 && code < 500 && body != null && body.TrimStart().StartsWith("{"))
            {
                return true;
            }

            if (code < 200 || code >= 300)
            {
                v.TryComplete(SteamWebValidationStatus.ApiError, $"HTTP {code}");
                return false;
            }

            return true;
        }

        private static void PurgeOld()
        {
            DateTime now = DateTime.UtcNow;
            List<ClientConnection> old = null;
            foreach (KeyValuePair<ClientConnection, SteamWebValidation> pair in Validations)
            {
                if (now - pair.Value.CreatedUtc > MaxAge)
                {
                    if (old == null) old = new List<ClientConnection>();
                    old.Add(pair.Key);
                }
            }

            if (old == null) return;
            foreach (ClientConnection cc in old)
            {
                Validations.Remove(cc);
            }
        }
    }
}