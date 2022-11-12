namespace Fougerite.Events
{
    public class BeltUseEvent
    {
        private readonly InventoryHolder _holder;
        private readonly int _belt;
        private readonly Player _player;
        private bool _bypass = false;
        private bool _cancelled = false;
        
        public BeltUseEvent(InventoryHolder holder, int belt)
        {
            _holder = holder;
            _belt = belt;
            if (holder.netUser != null)
            {
                _player = Server.GetServer().FindPlayer(holder.netUser.userID);
            }
        }

        /// <summary>
        /// Returns the slot number of the belt from 0-6
        /// </summary>
        public int SelectedBelt
        {
            get { return _belt; }
        }

        public InventoryHolder InventoryHolder
        {
            get { return _holder; }
        }

        public Player Player
        {
            get { return _player; }
        }

        public bool Bypassed
        {
            get { return _bypass; }
        }

        public bool Cancelled
        {
            get { return _cancelled; }
        }

        public void Cancel()
        {
            _cancelled = true;
        }

        /// <summary>
        /// Bypasses the cooldown check, so there is no time limit when selecting different items.
        /// </summary>
        public void BypassBeltCooldown()
        {
            _bypass = true;
        }
    }
}