
namespace Fougerite.Events
{
    /// <summary>
    /// This class is created when a player is crafting an item.
    /// </summary>
    public class CraftingEvent
    {
        private readonly CraftingInventory _inv;
        private readonly BlueprintDataBlock _block;
        private readonly int _amount;
        private readonly ulong _startTime;
        private readonly Player _player;
        private readonly bool _legit = true;
        private readonly NetUser _user;
        private bool _cancelled = false;

        public CraftingEvent(CraftingInventory inv, BlueprintDataBlock blueprint, int amount, ulong startTime)
        {
            _inv = inv;
            _block = blueprint;
            _amount = amount;
            _startTime = startTime;
            var netUser = inv.GetComponent<Character>().netUser;
            _player = Server.GetServer().FindPlayer(netUser.userID);
            _user = netUser;
            if (!_player.HasBlueprint(blueprint))
            {
                _legit = false;
                Cancel();
                Logger.LogWarning(
                    $"[CraftingHack] Detected: {_player.Name} | {_player.SteamID} | {_player.IP} | {blueprint.name}");
                Server.GetServer().Broadcast($"CraftingHack Detected: {_player.Name}");
                if (Bootstrap.AutoBanCraft)
                {
                    Server.GetServer().BanPlayer(_player, "Console", "CraftingHack");
                }
            }
        }

        /// <summary>
        /// Gets if hacking wasnt involved in the crafting.
        /// </summary>
        public bool IsLegit
        {
            get { return _legit; }
        }

        /// <summary>
        /// Returns the player who is crafting.
        /// </summary>
        public Player Player
        {
            get { return _player; }
        }

        /// <summary>
        /// Returns the netuser of the crafting player.
        /// </summary>
        public NetUser NetUser
        {
            get { return _user; }
        }

        /// <summary>
        /// Gets the start time of the crafting.
        /// </summary>
        public ulong StartTime
        {
            get { return _startTime; }
        }

        /// <summary>
        /// Cancels the crafting event.
        /// </summary>
        public void Cancel()
        {
            if (_cancelled) return;
            _cancelled = true;
            _inv.CancelCrafting();
        }

        /// <summary>
        /// Returns the craftinginventory class.
        /// </summary>
        public CraftingInventory CraftingInventory
        {
            get { return _inv; }
        }

        /// <summary>
        /// Gets the time when the player was near a workbench.
        /// </summary>
        public float LastWorkBenchTime
        {
            get { return _inv._lastWorkBenchTime; }
        }

        /// <summary>
        /// Gets the blueprint datablock of the item.
        /// </summary>
        public BlueprintDataBlock BlueprintDataBlock
        {
            get { return _block; }
        }
        
        /// <summary>
        /// Gets the itemname of the crafting item.
        /// </summary>
        public string ItemName
        {
            get { return _block.name; }
        }

        /// <summary>
        /// Returns the ingredients of the crafting item.
        /// </summary>
        public BlueprintDataBlock.IngredientEntry[] Ingredients
        {
            get { return _block.ingredients.ToArray(); }
        }

        /// <summary>
        /// Returns if the item is requiring a workbench.
        /// </summary>
        public bool RequireWorkbench
        {
            get { return _block.RequireWorkbench; }
        }

        /// <summary>
        /// Gets the ResultItem of the datablock.
        /// </summary>
        public ItemDataBlock ResultItem
        {
            get { return _block.resultItem; }
        }

        /// <summary>
        /// Gets the amount of the result item.
        /// </summary>
        public int ResultItemNumber
        {
            get { return _block.numResultItem; }
        }

        /// <summary>
        /// Gets the amount of the item that we are crafting.
        /// </summary>
        public int Amount
        {
            get { return _amount; }
        }

    }
}
