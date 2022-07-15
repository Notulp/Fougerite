namespace Fougerite.Events
{
    /// <summary>
    /// This class is created when a Repair is done.
    /// </summary>
    public class RepairEvent
    {
        private readonly Inventory _inv;
        private readonly RepairBench _rb;
        private readonly Fougerite.Player _pl;
        internal bool _cancel;

        public RepairEvent(RepairBench rb, Inventory inv)
        {
            _rb = rb;
            _inv = inv;
            var netUser = _inv.GetComponent<Character>().netUser;
            if (netUser != null)
            {
                _pl = Fougerite.Server.GetServer().FindPlayer(netUser.userID);
            }
        }

        /// <summary>
        /// Player who is performing the repair.
        /// </summary>
        public Fougerite.Player Player
        {
            get
            {
                return _pl;
            }
        }

        /// <summary>
        /// Returns the original Inventory class
        /// </summary>
        public Inventory Inv
        {
            get
            {
                return _inv;
            }
        }

        /// <summary>
        /// Returns the RepairBench class.
        /// </summary>
        public RepairBench RepairBench
        {
            get
            {
                return _rb;
            }
        }

        /// <summary>
        /// Gets if the event was cancelled.
        /// </summary>
        public bool Cancelled
        {
            get { return _cancel; }
        }

        /// <summary>
        /// Cancels the event.
        /// </summary>
        public void Cancel()
        {
            if (_cancel)
            {
                return;
            }
            _cancel = true;
        }
    }
}
