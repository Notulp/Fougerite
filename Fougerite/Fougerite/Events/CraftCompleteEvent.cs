namespace Fougerite.Events
{
    /// <summary>
    /// Represents the state after a crafting operation has been completed, when the crafted item is given to the player.
    /// </summary>
    public enum CraftCompleteEventType
    {
        /// <summary>
        /// Represents the state of a crafting operation before it is completed
        /// and the crafted item is given to the player.
        /// </summary>
        Before,
        /// <summary>
        /// Represents the state after a crafting operation is completed
        /// and the crafted item has been given to the player.
        /// </summary>
        After
    }

    /// <summary>
    /// This class is created exactly when a crafting operation completes.
    /// </summary>
    public class CraftCompleteEvent
    {
        private readonly CraftingInventory _inv;
        private readonly Player _player;
        private readonly NetUser _user;
        private readonly CraftCompleteEventType _type;

        public CraftCompleteEvent(CraftingInventory inv, CraftCompleteEventType type)
        {
            _inv = inv;
            _type = type;
            var character = inv.GetComponent<Character>();
            if (character != null && character.netUser != null)
            {
                _user = character.netUser;
                _player = Server.GetServer().FindPlayer(_user.userID);
            }
        }

        /// <summary>
        /// Returns the player whose craft just completed.
        /// </summary>
        public Player Player
        {
            get { return _player; }
        }

        /// <summary>
        /// Returns the netuser of the player.
        /// </summary>
        public NetUser NetUser
        {
            get { return _user; }
        }

        /// <summary>
        /// Returns the crafting inventory class.
        /// </summary>
        public CraftingInventory CraftingInventory
        {
            get { return _inv; }
        }

        /// <summary>
        /// Gets the event type indicating whether the craft completion occurred before or after the item was given to the player.
        /// </summary>
        public CraftCompleteEventType EventType
        {
            get { return _type; }
        }
    }
}