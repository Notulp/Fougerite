namespace Fougerite.Events
{
    /// <summary>
    /// The ban type of the event.
    /// </summary>
    public enum BanType
    {
        Player,
        IDandIP,
        OnlyID,
        OnlyIP
    }

    /// <summary>
    /// This class in used on the BanEvent hook.
    /// </summary>
    public class BanEvent
    {
        private BanType _type;
        private Player _player;
        private Player _sender;
        private string _ip;
        private string _id;
        private string _name;
        private string _reason;
        private string _banner;
        private bool _cancel = false;

        public BanEvent(Player player, string Banner, string reason, Player Sender)
        {
            _type = BanType.Player;
            _player = player;
            _reason = reason;
            _ip = player.IP;
            _id = player.SteamID;
            _name = player.Name;
            _sender = Sender;
            _banner = Banner;
        }

        public BanEvent(string ip, string id, string name, string reason, string adminname)
        {
            _type = BanType.IDandIP;
            _reason = reason;
            _ip = ip;
            _id = id;
            _name = name;
            _banner = adminname;
        }

        public BanEvent(string iporid, string name, string reason, string adminname, bool IsID)
        {
            if (IsID)
            {
                _type = BanType.OnlyID;
                _reason = reason;
                _id = iporid;
                _name = name;
                _banner = adminname;
            }
            else
            {
                _type = BanType.OnlyIP;
                _reason = reason;
                _ip = iporid;
                _name = name;
                _banner = adminname;
            }
        }

        /// <summary>
        /// Cancels the event.
        /// </summary>
        public void Cancel()
        {
            _cancel = true;
        }

        /// <summary>
        /// Returns the enum ban type.
        /// </summary>
        public BanType BanType
        {
            get { return _type; }
        }

        /// <summary>
        /// Returns the banned user.
        /// </summary>
        public Player BannedUser
        {
            get { return _player; }
        }

        /// <summary>
        /// Returns the ban executor if its a player.
        /// </summary>
        public Player BanSender
        {
            get { return _sender; }
        }

        /// <summary>
        /// Gets the IP of the banned player. Can be null if its an ID ban.
        /// </summary>
        public string IP
        {
            get { return _ip; }
        }
        
        /// <summary>
        /// Gets the ID of the banned player. Can be null if its an IP ban.
        /// </summary>
        public string ID
        {
            get { return _id; }
        }
        
        /// <summary>
        /// Gets the name of the banned player. Can be null if its an IP / ID ban.
        /// </summary>
        public string Name
        {
            get { return _name; }
        }
        
        /// <summary>
        /// Gets the reason for the ban.
        /// </summary>
        public string Reason
        {
            get { return _reason; }
        }
        
        /// <summary>
        /// Gets the executors name.
        /// </summary>
        public string BannerName
        {
            get { return _banner; }
        }
        
        /// <summary>
        /// Gets if the event was cancelled.
        /// </summary>
        public bool Cancelled
        {
            get { return _cancel; }
        }
    }
}
