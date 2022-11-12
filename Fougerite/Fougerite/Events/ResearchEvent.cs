

namespace Fougerite.Events
{
    /// <summary>
    /// This class is created when something is being researched.
    /// </summary>
    public class ResearchEvent
    {
        private readonly IInventoryItem _item;
        private readonly Player _player;
        private bool _cancelled;

        public ResearchEvent(IInventoryItem item)
        {
            _item = item;
            _player = Server.GetServer().FindPlayer(item.character.netUser.userID);
        }

        /// <summary>
        /// The player who does the research
        /// </summary>
        public Player Player
        {
            get { return _player; }
        }

        /// <summary>
        /// The item that is being researched. (IInventoryItem class)
        /// </summary>
        public IInventoryItem Item
        {
            get { return _item; }
        }

        /// <summary>
        /// The ItemDataBlock of the item.
        /// </summary>
        public ItemDataBlock ItemDataBlock
        {
            get { return _item.datablock; }
        }

        /// <summary>
        /// The item's name.
        /// </summary>
        public string ItemName 
        {
            get { return _item.datablock.name; }
        }
        
        /// <summary>
        /// Is the event cancelled?
        /// </summary>
        public bool Cancelled 
        {
            get { return _cancelled; }
        }

        /// <summary>
        /// Cancels the event.
        /// </summary>
        public void Cancel()
        {
            //PlayerInventory invent = Player.Inventory.InternalInventory as PlayerInventory;
            //if (invent != null) invent.GetBoundBPs().Remove(Util.GetUtil().BlueprintOfItem(ItemDataBlock));
            _cancelled = true;
        }
    }
}
