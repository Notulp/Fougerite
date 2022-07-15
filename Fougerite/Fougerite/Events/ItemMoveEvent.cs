namespace Fougerite.Events
{
    /// <summary>
    /// This class is created when an item is being moved to a slot.
    /// </summary>
    public class ItemMoveEvent
    {
        private readonly Inventory _from;
        private readonly Inventory _to;
        private readonly int _fslot;
        private readonly int _tslot;
        private readonly Inventory.SlotOperationsInfo _iinfo;
        private readonly Fougerite.Player _pl;

        public ItemMoveEvent(Inventory inst, int fromSlot, Inventory toInventory, int toSlot, Inventory.SlotOperationsInfo info)
        {
            _from = inst;
            _to = toInventory;
            _fslot = fromSlot;
            _tslot = toSlot;
            _iinfo = info;
            if (toInventory._netListeners != null) // This is null when Rust is placing items.
            {
                foreach (uLink.NetworkPlayer netplayer in toInventory._netListeners)
                {
                    try
                    {
                        NetUser user = netplayer.GetLocalData() as NetUser;
                        if (user != null)
                        {
                            _pl = Fougerite.Server.GetServer().FindPlayer(user.userID);
                            break;
                        }
                    }
                    catch
                    {
                        //ignore
                    }
                }
            }
        }

        /// <summary>
        /// Gets the player of the event if possible.
        /// </summary>
        public Fougerite.Player Player
        {
            get { return _pl; }
        }

        /// <summary>
        /// Gets the source inventory.
        /// </summary>
        public Inventory FromInventory
        {
            get{ return _from; }
        }

        /// <summary>
        /// Gets the target inventory.
        /// </summary>
        public Inventory ToInventory
        {
            get { return _to; }
        }

        /// <summary>
        /// Gets the slot where the item is being moved from
        /// </summary>
        public int FromSlot
        {
            get { return _fslot; }
        }

        /// <summary>
        /// Gets the slot where the item is being moved to.
        /// </summary>
        public int ToSlot
        {
            get { return _tslot; }
        }

        /// <summary>
        /// Gets the slot operation info.
        /// </summary>
        public Inventory.SlotOperationsInfo SlotOperation
        {
            get { return _iinfo; }
        }
    }
}
