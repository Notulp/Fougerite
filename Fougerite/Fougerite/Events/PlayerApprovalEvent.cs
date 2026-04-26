using uLink;

namespace Fougerite.Events
{
    /// <summary>
    /// This class is created when the player is authenticating with the server.
    /// </summary>
    public class PlayerApprovalEvent
    {
        private readonly ConnectionAcceptor _ca;
        private readonly NetworkPlayerApproval _approval;
        private readonly ClientConnection _cc;
        private bool _deny;
        private uLink.NetworkConnectionError _reason;
        private bool _forceAccept = false;
        private readonly ulong _steamid;
        private readonly string _name;
        private readonly string _ip;

        /// <summary>
        /// Represents an event initiated during the player's authentication process,
        /// providing mechanisms to approve or deny a player based on their connection
        /// details and authentication status.
        /// </summary>
        /// <param name="ca">The <see cref="ConnectionAcceptor"/> responsible for managing incoming connection requests.</param>
        /// <param name="approval">The <see cref="NetworkPlayerApproval"/> instance containing the player's authentication approval details.</param>
        /// <param name="cc">The <see cref="ClientConnection"/> representing the client's connection information.</param>
        /// <param name="AboutToDeny">Indicates whether the player is about to be denied authentication.</param>
        /// <param name="steamid">The SteamID of the player attempting to authenticate.</param>
        /// <param name="ip">The IP address of the client attempting to connect.</param>
        /// <param name="name">The name of the player attempting to connect.</param>
        /// <param name="connectionError">
        /// (Optional) The reason for the player's connection failure, represented as a 
        /// <see cref="uLink.NetworkConnectionError"/>. Defaults to <see cref="uLink.NetworkConnectionError.NoError"/>.
        /// </param>

        public PlayerApprovalEvent(ConnectionAcceptor ca, NetworkPlayerApproval approval, ClientConnection cc,
            bool AboutToDeny, ulong steamid, string ip, string name,
            uLink.NetworkConnectionError connectionError = uLink.NetworkConnectionError.NoError)
        {
            _ca = ca;
            _cc = cc;
            _approval = approval;
            _deny = AboutToDeny;
            _steamid = steamid;
            _ip = ip;
            _name = name;
            _reason = connectionError;
        }

        /// <summary>
        /// Gets the ConnectionAcceptor class
        /// </summary>
        public ConnectionAcceptor ConnectionAcceptor
        {
            get { return _ca; }
        }

        /// <summary>
        /// Gets the ClientConnection class
        /// </summary>
        public ClientConnection ClientConnection
        {
            get { return _cc; }
        }

        /// <summary>
        /// Gets the NetworkPlayerApproval class.
        /// </summary>
        public NetworkPlayerApproval NetworkPlayerApproval
        {
            get { return _approval; }
        }

        /// <summary>
        /// Is the player going to be denied?
        /// </summary>
        public bool AboutToDeny
        {
            get { return _deny; }
        }

        /// <summary>
        /// Accept the player no matter the cost?
        /// </summary>
        public bool ForceAccept
        {
            get { return _forceAccept; }
            set { _forceAccept = value; }
        }

        /// <summary>
        /// This just checks if the player's steamid is already found in the online list.
        /// </summary>
        public bool ServerHasPlayer
        {
            get
            {
                Player pl = Server.GetServer().FindPlayer(_cc.UserID);
                if (pl != null)
                {
                    return pl.IsOnline;
                }
                return false;
            }
        }

        /// <summary>
        /// Returns the UID.
        /// </summary>
        public ulong SteamID
        {
            get { return _steamid; }
        }

        /// <summary>
        /// Returns the playername.
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// Returns the IP Address.
        /// </summary>
        public string IP
        {
            get { return _ip; }
        }

        /// <summary>
        /// Gets the reason for denying a player's connection to the server.
        /// </summary>
        public uLink.NetworkConnectionError DenyReason
        {
            get
            {
                return _reason;
            }
        }

        /// <summary>
        /// Denies the player's authentication attempt with a specific reason.
        /// </summary>
        /// <param name="reason">The reason for denying the player's authentication attempt, represented as a NetworkConnectionError.</param>
        public void Deny(uLink.NetworkConnectionError reason)
        {
            _deny = true;
            _reason = reason;
        }
    }
}
