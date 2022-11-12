
namespace Fougerite.Events
{
    using uLink;

    /// <summary>
    /// This class is created when the player is authenticating with the server.
    /// </summary>
    public class PlayerApprovalEvent
    {
        private readonly ConnectionAcceptor _ca;
        private readonly NetworkPlayerApproval _approval;
        private readonly ClientConnection _cc;
        private readonly bool _deny;
        private bool _ForceAccept = false;
        private readonly ulong _steamid;
        private readonly string _name;
        private readonly string _ip;

        public PlayerApprovalEvent(ConnectionAcceptor ca, NetworkPlayerApproval approval, ClientConnection cc, 
            bool AboutToDeny, ulong steamid, string ip, string name)
        {
            _ca = ca;
            _cc = cc;
            _approval = approval;
            _deny = AboutToDeny;
            _steamid = steamid;
            _ip = ip;
            _name = name;
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
            get { return _ForceAccept; }
            set { _ForceAccept = value; }
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
    }
}
