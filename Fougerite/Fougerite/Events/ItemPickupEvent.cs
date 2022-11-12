namespace Fougerite.Events
{
    public enum PickupEventType
    {
        Before,
        After
    }
    
    /// <summary>
    /// This class is created when an item is picked up.
    /// </summary>
    public class ItemPickupEvent
    {
        private readonly Player _player;
        private readonly IInventoryItem _item;
        private readonly Inventory _inv;
        private bool _cancelled;
        private Inventory.AddExistingItemResult _result;
        private PickupEventType _type;

        public ItemPickupEvent(Controllable controllable, IInventoryItem item, Inventory local, Inventory.AddExistingItemResult result, PickupEventType type)
        {
            _player = Server.GetServer().FindPlayer(controllable.netUser.userID);
            _item = item;
            _inv = local;
            _result = result;
            _type = type;
        }

        /// <summary>
        /// The player who is picking the item up.
        /// </summary>
        public Player Player
        {
            get { return _player; }
        }

        /// <summary>
        /// The item we are picking up.
        /// </summary>
        public IInventoryItem Item
        {
            get { return _item; }
        }

        /// <summary>
        /// The inventory where the item is going to be placed.
        /// </summary>
        public Inventory Inventory
        {
            get { return _inv; }
        }

        /// <summary>
        /// Is the event cancelled?
        /// </summary>
        public bool Cancelled
        {
            get { return _cancelled; }
        }

        /// <summary>
        /// Inventory result. THIS DOESN'T WORK IF THE EVENT'S PickupEventType IS BEFORE.
        /// </summary>
        public Inventory.AddExistingItemResult Result
        {
            get { return _result; }
        }
        
        /// <summary>
        /// Gets if the event ran before the item got placed in the inventory, or after.
        /// Cancel doesn't work at the "After" event.
        /// </summary>
        public PickupEventType PickupEventType
        {
            get { return _type; }
        }

        /// <summary>
        /// Cancels the event.
        /// </summary>
        public void Cancel()
        {
            if (_cancelled) return;
            _cancelled = true;
        }
    }
}
