namespace Fougerite.Events
{
    /// <summary>
    /// This class is created when a player attempts to cancel a crafting operation.
    /// </summary>
    public class CraftCancelEvent
    {
        private readonly CraftingInventory _inv;
        private readonly Player _player;
        private readonly NetUser _user;
        private bool _cancelled = false;

        public CraftCancelEvent(CraftingInventory inv)
        {
            _inv = inv;
            var character = inv.GetComponent<Character>();
            if (character != null && character.netUser != null)
            {
                _user = character.netUser;
                _player = Server.GetServer().FindPlayer(_user.userID);
            }
        }

        /// <summary>
        /// Returns the player who is cancelling the craft.
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
        /// Gets whether the cancellation has been blocked by a plugin.
        /// </summary>
        public bool Cancelled
        {
            get { return _cancelled; }
        }

        /// <summary>
        /// Cancels the cancellation event. (The crafting will continue).
        /// </summary>
        public void Cancel()
        {
            _cancelled = true;
        }
    }
}