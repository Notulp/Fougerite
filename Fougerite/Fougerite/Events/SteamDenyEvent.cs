using Fougerite.Tools;
using uLink;

namespace Fougerite.Events
{
    /// <summary>
    /// Runs when the Steam API denies a player at connection.
    /// </summary>
    public class SteamDenyEvent
    {
        private readonly ClientConnection _cc;
        private readonly NetworkPlayerApproval _approval;
        private readonly string _strReason;
        private readonly NetError _errornum;
        private readonly bool _isValidSteamUser;
        private readonly SteamAuthMode _mode;
        private readonly SteamTicketInfo _ticket;
        private readonly SteamWebValidation _webValidation;
        private readonly bool _policyAllowed;
        private readonly string _policyReason;
        private bool _forceallow;

        public SteamDenyEvent(ClientConnection cc, NetworkPlayerApproval approval, string strReason, NetError errornum,
            bool isValidSteamUser)
            : this(cc, approval, strReason, errornum, isValidSteamUser, SteamAuthMode.Legacy, null, null, false,
                "Legacy: decided by plugins")
        {
        }

        public SteamDenyEvent(ClientConnection cc, NetworkPlayerApproval approval, string strReason, NetError errornum,
            bool isValidSteamUser, SteamAuthMode mode, SteamTicketInfo ticket, SteamWebValidation webValidation,
            bool policyAllowed, string policyReason)
        {
            _cc = cc;
            _approval = approval;
            _strReason = strReason;
            _errornum = errornum;
            _isValidSteamUser = isValidSteamUser;
            _mode = mode;
            _ticket = ticket;
            _webValidation = webValidation;
            _policyAllowed = policyAllowed;
            _policyReason = policyReason;
            // Outside Legacy the policy verdict is the starting point, plugins may only tighten it.
            _forceallow = mode != SteamAuthMode.Legacy && policyAllowed;
        }

        /// <summary>
        /// The netuser of the player.
        /// </summary>
        public NetUser NetUser
        {
            get { return _cc.netUser; }
        }

        /// <summary>
        /// The ClientConnection class that is created at connection.
        /// </summary>
        public ClientConnection ClientConnection
        {
            get { return _cc; }
        }

        /// <summary>
        /// Returns the NetworkPlayerApproval class
        /// </summary>
        public NetworkPlayerApproval Approval
        {
            get { return _approval; }
        }

        /// <summary>
        /// Reason of the deny.
        /// </summary>
        public string Reason
        {
            get { return _strReason; }
        }

        /// <summary>
        /// Returns the NetError number.
        /// </summary>
        public NetError ErrorNumber
        {
            get { return _errornum; }
        }

        /// <summary>
        /// Gets or sets whether the player is admitted despite the Steam rejection.
        /// In <see cref="SteamAuthMode.Legacy"/> the value starts as false and setting it to true admits the player,
        /// exactly like the AuthAllow plugin always did. In every other mode the value starts as
        /// <see cref="PolicyAllowed"/>. Plugins may set it to false to reject a player the mode would admit, but
        /// setting it to true never admits a player the mode rejects.
        /// </summary>
        public bool ForceAllow
        {
            get { return _forceallow; }
            set { _forceallow = value; }
        }

        /// <summary>
        /// Gets a value indicating whether the player is considered a valid Steam user.
        /// In <see cref="SteamAuthMode.Legacy"/> this only means the ticket contains the Rust or Spacewar AppID bytes,
        /// which proves nothing. In every other mode it is true only when the Steam Web API confirmed that the ticket
        /// belongs to this account, so it stays false in <see cref="SteamAuthMode.SteamAccountsUnverified"/>.
        /// The raw ticket is available through ClientConnection.SteamTicket.
        /// </summary>
        public bool IsValidSteamUser
        {
            get { return _isValidSteamUser; }
        }

        /// <summary>
        /// Gets the SteamAuthMode that was in effect for this connection.
        /// </summary>
        public SteamAuthMode Mode
        {
            get { return _mode; }
        }

        /// <summary>
        /// Gets the unverified claims of the ticket, or null in <see cref="SteamAuthMode.Legacy"/> and for malformed tickets.
        /// </summary>
        public SteamTicketInfo Ticket
        {
            get { return _ticket; }
        }

        /// <summary>
        /// Gets the AppID the ticket claims to be issued for, or zero when it is unknown.
        /// </summary>
        public uint TicketAppId
        {
            get { return _ticket != null ? _ticket.AppId : 0; }
        }

        /// <summary>
        /// Gets the Steam Web API validation of the ticket, or null when no validation was performed.
        /// </summary>
        public SteamWebValidation WebValidation
        {
            get { return _webValidation; }
        }

        /// <summary>
        /// Gets the verdict of the SteamAuthMode, which is always false in <see cref="SteamAuthMode.Legacy"/>.
        /// </summary>
        public bool PolicyAllowed
        {
            get { return _policyAllowed; }
        }

        /// <summary>
        /// Gets a description of why the SteamAuthMode admitted or rejected the player.
        /// </summary>
        public string PolicyReason
        {
            get { return _policyReason; }
        }
    }
}