namespace Fougerite.Events
{
    /// <summary>
    /// The unban type of the event.
    /// </summary>
    public enum UnbanType
    {
        Name,
        IP,
        ID
    }

    /// <summary>
    /// This class in used on the UnbanEvent hook.
    /// </summary>
    public class UnbanEvent
    {
        private readonly UnbanType _type;
        private readonly Player _sender;
        private readonly string _ip;
        private readonly string _id;
        private readonly string _name;
        private readonly string _unbanner;
        private bool _cancel = false;

        public UnbanEvent(string name, string UnBanner, Player Sender)
        {
            _type = UnbanType.Name;
            _name = name;
            _unbanner = UnBanner;
            _sender = Sender;
        }

        public UnbanEvent(string iporid, bool IsID)
        {
            if (IsID)
            {
                _type = UnbanType.ID;
                _id = iporid;
            }
            else
            {
                _type = UnbanType.IP;
                _ip = iporid;
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
        /// Returns the enum unban type.
        /// </summary>
        public UnbanType UnbanType
        {
            get { return _type; }
        }

        /// <summary>
        /// Returns the unban executor if its a player.
        /// </summary>
        public Player UnbanSender
        {
            get { return _sender; }
        }

        /// <summary>
        /// Gets the IP being unbanned. Can be null unless its an IP unban.
        /// </summary>
        public string IP
        {
            get { return _ip; }
        }

        /// <summary>
        /// Gets the ID being unbanned. Can be null unless its an ID unban.
        /// </summary>
        public string ID
        {
            get { return _id; }
        }

        /// <summary>
        /// Gets the name being unbanned. Can be null unless its a Name unban.
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// Gets the executors name.
        /// </summary>
        public string UnbannerName
        {
            get { return _unbanner; }
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
