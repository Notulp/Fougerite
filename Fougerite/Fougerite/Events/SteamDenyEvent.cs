
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
        private bool _forceallow = false;

        public SteamDenyEvent(ClientConnection cc, NetworkPlayerApproval approval, string strReason, NetError errornum)
        {
            _cc = cc;
            _approval = approval;
            _strReason = strReason;
            _errornum = errornum;
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
        /// Accept the player no matter the cost?
        /// </summary>
        public bool ForceAllow
        {
            get { return _forceallow; }
            set { _forceallow = value; }
        }
    }
}
