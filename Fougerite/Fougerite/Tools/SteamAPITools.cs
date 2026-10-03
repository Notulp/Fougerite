using System;
using System.Text;

namespace Fougerite.Tools
{
    /// <summary>
    /// Provides helpers for inspecting Steam authentication session tickets and for talking to the Steam Web API.
    /// </summary>
    public class SteamAPITools
    {
        /// <summary>
        /// The Steam AppID of Rust.
        /// </summary>
        public const uint RustAppId = 252490;

        /// <summary>
        /// The Steam AppID of Spacewar, the free Valve test application that RustBuster clients authenticate with.
        /// </summary>
        public const uint SpacewarAppId = 480;

        /// <summary>
        /// The base address of the public Steam Web API.
        /// </summary>
        public const string SteamWebApiBaseUrl = "https://api.steampowered.com/";

        /// <summary>
        /// The little endian byte representation of AppID 252490.
        /// </summary>
        public static readonly byte[] RustAppIdBytes = new byte[] { 0x4A, 0xDA, 0x03, 0x00 };

        /// <summary>
        /// The little endian byte representation of AppID 480.
        /// </summary>
        public static readonly byte[] SpacewarAppIdBytes = new byte[] { 0xE0, 0x01, 0x00, 0x00 };

        private const uint GcTokenSectionLength = 20;
        private const uint SessionHeaderLength = 24;
        private const uint OwnershipTicketVersion = 4;
        private const int SignatureLength = 128;
        private const int GcSectionTotalLength = 56;
        private const int OwnershipFixedLength = 40;

        private const ulong SteamIdUniversePublic = 1;
        private const ulong SteamIdTypeIndividual = 1;
        private const ulong SteamIdInstanceDesktop = 1;

        private static readonly TimeSpan ClockSkew = TimeSpan.FromDays(1);
        private static readonly TimeSpan MaxSessionTicketAge = TimeSpan.FromDays(30);
        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Searches for a specific byte sequence within a given byte array.
        /// This is kept for <see cref="SteamAuthMode.Legacy"/> compatibility only. The sequence may match anywhere,
        /// including inside the signature, so a match proves nothing about the ticket. Use
        /// <see cref="TryParseTicket"/> for anything that matters.
        /// </summary>
        /// <param name="data">The byte array in which to search for the sequence.</param>
        /// <param name="sequence">The byte sequence to locate within the data array.</param>
        /// <returns>True if the sequence occurs within the data array, otherwise false.</returns>
        public static bool FindSequence(byte[] data, byte[] sequence)
        {
            if (data == null || sequence == null || data.Length < sequence.Length)
                return false;

            int max = data.Length - sequence.Length;
            for (int i = 0; i <= max; i++)
            {
                bool match = true;
                for (int j = 0; j < sequence.Length; j++)
                {
                    if (data[i + j] != sequence[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match) return true;
            }
            return false;
        }

        /// <summary>
        /// Parses the layout of a Steam authentication session ticket produced by ISteamUser.GetAuthSessionTicket.
        /// Parsing only extracts what the ticket claims about itself. The signature is never verified, so a forged
        /// ticket parses successfully as long as its layout is sane. Authenticity can only be established by native
        /// Steam authentication or by the Steam Web API, see <see cref="SteamTicketValidator"/>.
        /// </summary>
        /// <param name="ticket">The raw ticket bytes sent by the client.</param>
        /// <param name="info">The parsed claims of the ticket, or null if the ticket could not be parsed.</param>
        /// <returns>True if the ticket has a recognisable layout, otherwise false.</returns>
        public static bool TryParseTicket(byte[] ticket, out SteamTicketInfo info)
        {
            // Little endian layout.
            // Optional GC section, 56 bytes in total
            //   u32 section length (always 20), u64 GC token, u64 SteamID, u32 generation time
            //   u32 session header length (always 24) followed by the 24 byte session header
            //   u32 length of everything that follows (ownership ticket plus signature)
            // Ownership ticket
            //   u32 length including itself, u32 version, u64 SteamID, u32 AppID
            //   u32 external IP, u32 internal IP, u32 flags, u32 issue time, u32 expiry time
            //   u16 license count followed by u32 license ids
            //   u16 DLC count followed by (u32 AppID, u16 license count, u32 license ids) per DLC
            //   u16 reserved
            // 128 byte RSA signature
            info = null;
            if (ticket == null || ticket.Length < 20)
            {
                return false;
            }

            int offset = 0;
            bool hasGcSection = false;
            bool layoutExact = true;
            ulong gcToken = 0;
            ulong outerSteamId = 0;
            uint sessionGenerated = 0;
            int declaredEnd = 0;

            if (ReadUInt32(ticket, 0) == GcTokenSectionLength)
            {
                if (ticket.Length < GcSectionTotalLength)
                {
                    return false;
                }

                gcToken = ReadUInt64(ticket, 4);
                outerSteamId = ReadUInt64(ticket, 12);
                sessionGenerated = ReadUInt32(ticket, 20);
                if (ReadUInt32(ticket, 24) != SessionHeaderLength)
                {
                    return false;
                }

                uint remaining = ReadUInt32(ticket, 52);
                if (remaining > (uint)(ticket.Length - GcSectionTotalLength))
                {
                    return false;
                }

                declaredEnd = GcSectionTotalLength + (int)remaining;
                offset = GcSectionTotalLength;
                hasGcSection = true;
            }

            if (ticket.Length - offset < 20)
            {
                return false;
            }

            uint ownershipLength = ReadUInt32(ticket, offset);
            if (ownershipLength < 20 || ownershipLength > (uint)(ticket.Length - offset))
            {
                return false;
            }

            uint version = ReadUInt32(ticket, offset + 4);
            ulong steamId = ReadUInt64(ticket, offset + 8);
            uint appId = ReadUInt32(ticket, offset + 16);

            // Both SteamIDs inside a single ticket have to agree.
            if (hasGcSection && outerSteamId != steamId)
            {
                return false;
            }

            int ownershipEnd = offset + (int)ownershipLength;
            uint issued = 0;
            uint expires = 0;

            if (ownershipLength >= OwnershipFixedLength)
            {
                issued = ReadUInt32(ticket, offset + 32);
                expires = ReadUInt32(ticket, offset + 36);
                layoutExact &= WalkLicenses(ticket, offset + OwnershipFixedLength, ownershipEnd);
            }
            else
            {
                layoutExact = false;
            }

            bool hasSignature = ticket.Length - ownershipEnd >= SignatureLength;
            int ticketLength = hasSignature ? ownershipEnd + SignatureLength : ticket.Length;

            // The GC section states how long the rest of the ticket is. Anything the client sent beyond that
            // is buffer padding and is cut off, it is not part of the ticket Steam issued.
            if (hasGcSection)
            {
                layoutExact &= hasSignature && declaredEnd == ticketLength;
            }
            else
            {
                layoutExact &= hasSignature;
            }

            bool signatureBlank = hasSignature && IsUniform(ticket, ownershipEnd, SignatureLength);

            info = new SteamTicketInfo(steamId, appId, version, hasGcSection, hasSignature, gcToken,
                FromUnix(sessionGenerated), FromUnix(issued), FromUnix(expires), layoutExact, signatureBlank,
                ticketLength, ticket.Length - ticketLength);
            return true;
        }

        /// <summary>
        /// Performs every check that can be done on a Spacewar ticket without contacting Steam.
        /// The ticket has to match the exact layout Steam produces, be issued for the expected AppID and for the
        /// SteamID the client claims, carry a non blank signature and have plausible issue and expiry times.
        /// This reliably rejects broken emulators and lazy forgeries. It cannot stop a carefully crafted forgery,
        /// because the signature is not verified, and such a forgery may claim any SteamID including an admin's.
        /// </summary>
        /// <param name="info">The parsed ticket, may be null.</param>
        /// <param name="claimedSteamId">The SteamID the client sent in its connection data.</param>
        /// <param name="expectedAppId">The AppID the ticket is expected to be issued for.</param>
        /// <param name="reason">A description of the first failed check, or of the success.</param>
        /// <returns>True if the ticket passed every offline check, otherwise false.</returns>
        public static bool IsPlausibleTicket(SteamTicketInfo info, ulong claimedSteamId, uint expectedAppId,
            out string reason)
        {
            if (info == null)
            {
                reason = "Ticket is missing or malformed";
                return false;
            }

            if (info.SteamId != claimedSteamId)
            {
                reason = $"Ticket SteamID {info.SteamId} does not match claimed {claimedSteamId}";
                return false;
            }

            if (!IsIndividualSteamId(info.SteamId))
            {
                reason = $"SteamID {info.SteamId} is not a public individual account";
                return false;
            }

            if (info.AppId != expectedAppId)
            {
                reason = $"Ticket AppID {info.AppId} is not {expectedAppId}";
                return false;
            }

            if (!info.HasSessionHeader || info.GcToken == 0)
            {
                reason = "Ticket has no session header or GC token";
                return false;
            }

            if (info.Version != OwnershipTicketVersion)
            {
                reason = $"Unexpected ownership ticket version {info.Version}";
                return false;
            }

            if (!info.IsLayoutExact)
            {
                reason = "Ticket sections do not add up to the received length";
                return false;
            }

            if (info.IsSignatureBlank)
            {
                reason = "Ticket signature is blank";
                return false;
            }

            DateTime now = DateTime.UtcNow;
            if (info.OwnershipIssuedUtc == UnixEpoch || info.OwnershipExpiresUtc <= info.OwnershipIssuedUtc)
            {
                reason = "Ownership ticket has an invalid lifetime";
                return false;
            }

            if (info.OwnershipIssuedUtc > now + ClockSkew || info.OwnershipExpiresUtc < now - ClockSkew)
            {
                reason = $"Ownership ticket is not valid now (issued {info.OwnershipIssuedUtc:u}, " +
                         $"expires {info.OwnershipExpiresUtc:u})";
                return false;
            }

            if (info.SessionGeneratedUtc > now + ClockSkew || info.SessionGeneratedUtc < now - MaxSessionTicketAge)
            {
                reason = $"Session ticket generation time {info.SessionGeneratedUtc:u} is implausible";
                return false;
            }

            reason = "Ticket passed offline checks (signature not verified)";
            return true;
        }

        /// <summary>
        /// Determines whether a SteamID64 belongs to a regular user account in the public universe.
        /// </summary>
        /// <param name="steamId">The SteamID64 to inspect.</param>
        /// <returns>True for a public individual desktop account with a non zero account number.</returns>
        public static bool IsIndividualSteamId(ulong steamId)
        {
            ulong universe = steamId >> 56;
            ulong type = (steamId >> 52) & 0xF;
            ulong instance = (steamId >> 32) & 0xFFFFF;
            ulong account = steamId & 0xFFFFFFFF;
            return universe == SteamIdUniversePublic
                   && type == SteamIdTypeIndividual
                   && instance == SteamIdInstanceDesktop
                   && account != 0;
        }

        /// <summary>
        /// Returns the ticket exactly as Steam issued it, without any padding the client sent after it.
        /// Steam rejects tickets that carry extra bytes, so this is what has to be sent to the Web API.
        /// </summary>
        /// <param name="ticket">The raw ticket bytes sent by the client.</param>
        /// <param name="info">The parsed ticket.</param>
        /// <returns>The trimmed ticket, or the original array when nothing has to be removed.</returns>
        public static byte[] GetIssuedTicket(byte[] ticket, SteamTicketInfo info)
        {
            if (ticket == null || info == null || info.TrailingBytes <= 0 || info.TicketLength > ticket.Length)
            {
                return ticket;
            }

            byte[] trimmed = new byte[info.TicketLength];
            Buffer.BlockCopy(ticket, 0, trimmed, 0, info.TicketLength);
            return trimmed;
        }

        /// <summary>
        /// Encodes bytes as an uppercase hexadecimal string, which is the format the Steam Web API expects for tickets.
        /// </summary>
        /// <param name="data">The bytes to encode.</param>
        /// <returns>The hexadecimal representation, or an empty string when data is null.</returns>
        public static string ToHex(byte[] data)
        {
            if (data == null)
            {
                return string.Empty;
            }

            const string alphabet = "0123456789ABCDEF";
            StringBuilder sb = new StringBuilder(data.Length * 2);
            foreach (byte b in data)
            {
                sb.Append(alphabet[b >> 4]);
                sb.Append(alphabet[b & 0x0F]);
            }
            return sb.ToString();
        }

        private static bool WalkLicenses(byte[] data, int pos, int end)
        {
            if (!TrySkipLicenseList(data, ref pos, end))
            {
                return false;
            }

            if (pos + 2 > end)
            {
                return false;
            }

            int dlcCount = ReadUInt16(data, pos);
            pos += 2;
            for (int i = 0; i < dlcCount; i++)
            {
                if (pos + 4 > end)
                {
                    return false;
                }

                pos += 4;
                if (!TrySkipLicenseList(data, ref pos, end))
                {
                    return false;
                }
            }

            // Reserved field, after which the ownership ticket must end exactly.
            pos += 2;
            return pos == end;
        }

        private static bool TrySkipLicenseList(byte[] data, ref int pos, int end)
        {
            if (pos + 2 > end)
            {
                return false;
            }

            int count = ReadUInt16(data, pos);
            pos += 2 + count * 4;
            return pos <= end;
        }

        private static bool IsUniform(byte[] data, int offset, int length)
        {
            byte first = data[offset];
            for (int i = 1; i < length; i++)
            {
                if (data[offset + i] != first)
                {
                    return false;
                }
            }
            return true;
        }

        private static DateTime FromUnix(uint seconds)
        {
            return UnixEpoch.AddSeconds(seconds);
        }

        private static int ReadUInt16(byte[] data, int offset)
        {
            return data[offset] | (data[offset + 1] << 8);
        }

        private static uint ReadUInt32(byte[] data, int offset)
        {
            return (uint)(data[offset]
                          | (data[offset + 1] << 8)
                          | (data[offset + 2] << 16)
                          | (data[offset + 3] << 24));
        }

        private static ulong ReadUInt64(byte[] data, int offset)
        {
            return ReadUInt32(data, offset) | ((ulong)ReadUInt32(data, offset + 4) << 32);
        }
    }

    /// <summary>
    /// Describes what a Steam session ticket claims about itself. None of these values are verified.
    /// </summary>
    public class SteamTicketInfo
    {
        internal SteamTicketInfo(ulong steamId, uint appId, uint version, bool hasSessionHeader, bool hasSignature,
            ulong gcToken, DateTime sessionGeneratedUtc, DateTime ownershipIssuedUtc, DateTime ownershipExpiresUtc,
            bool isLayoutExact, bool isSignatureBlank, int ticketLength, int trailingBytes)
        {
            SteamId = steamId;
            AppId = appId;
            Version = version;
            HasSessionHeader = hasSessionHeader;
            HasSignature = hasSignature;
            GcToken = gcToken;
            SessionGeneratedUtc = sessionGeneratedUtc;
            OwnershipIssuedUtc = ownershipIssuedUtc;
            OwnershipExpiresUtc = ownershipExpiresUtc;
            IsLayoutExact = isLayoutExact;
            IsSignatureBlank = isSignatureBlank;
            TicketLength = ticketLength;
            TrailingBytes = trailingBytes;
        }

        /// <summary>
        /// Gets the SteamID64 written in the ownership ticket.
        /// </summary>
        public ulong SteamId { get; private set; }

        /// <summary>
        /// Gets the AppID the ticket claims to be issued for.
        /// </summary>
        public uint AppId { get; private set; }

        /// <summary>
        /// Gets the version of the ownership ticket. Steam currently issues version 4.
        /// </summary>
        public uint Version { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the ticket contains the GC token and session header section.
        /// </summary>
        public bool HasSessionHeader { get; private set; }

        /// <summary>
        /// Gets a value indicating whether at least 128 bytes of signature follow the ownership ticket.
        /// </summary>
        public bool HasSignature { get; private set; }

        /// <summary>
        /// Gets the GC token of the session section, or zero when the section is absent.
        /// </summary>
        public ulong GcToken { get; private set; }

        /// <summary>
        /// Gets the time the session ticket claims to have been generated.
        /// </summary>
        public DateTime SessionGeneratedUtc { get; private set; }

        /// <summary>
        /// Gets the time the ownership ticket claims to have been issued.
        /// </summary>
        public DateTime OwnershipIssuedUtc { get; private set; }

        /// <summary>
        /// Gets the time the ownership ticket claims to expire.
        /// </summary>
        public DateTime OwnershipExpiresUtc { get; private set; }

        /// <summary>
        /// Gets a value indicating whether every length field inside the ticket is consistent,
        /// which is always the case for tickets produced by Steam. Trailing padding does not affect this value.
        /// </summary>
        public bool IsLayoutExact { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the signature consists of a single repeated byte,
        /// which is typical for emulators that do not bother faking one.
        /// </summary>
        public bool IsSignatureBlank { get; private set; }

        /// <summary>
        /// Gets the length of the ticket as Steam issued it.
        /// </summary>
        public int TicketLength { get; private set; }

        /// <summary>
        /// Gets the number of bytes the client sent after the end of the ticket, typically unused buffer space.
        /// </summary>
        public int TrailingBytes { get; private set; }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"SteamId={SteamId} AppId={AppId} Version={Version} Header={HasSessionHeader} " +
                   $"Signature={HasSignature} Exact={IsLayoutExact} Length={TicketLength} " +
                   $"Trailing={TrailingBytes} Expires={OwnershipExpiresUtc:u}";
        }
    }
}