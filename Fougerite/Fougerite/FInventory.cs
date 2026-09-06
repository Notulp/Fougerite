namespace Fougerite
{
    /// <summary>
    /// This class is created when an Item is added or removed to/from an inventory.
    /// </summary>
    /// <remarks>
    /// NOTE: During 'OnItemMoved' hooks, FInventory is typically instantiated twice:
    /// one for the 'Source' and one for the 'Destination'. The wrappers in the 
    /// _items array ensure that as the item's Slot changes during the move, 
    /// the EntityItem correctly identifies its new parent container.
    /// </remarks>
    public class FInventory
    {
        private readonly Inventory _inv;
        private readonly EntityItem[] _items;

        /// <summary>
        /// Initializes a new instance of the <see cref="FInventory"/> class by wrapping a raw Rust <see cref="Inventory"/>.
        /// </summary>
        /// <param name="inv">The underlying Rust engine <see cref="Inventory"/> to be wrapped.</param>
        /// <remarks>
        /// This constructor performs a "greedy" initialization. It creates a local array of <see cref="EntityItem"/> 
        /// objects matching the <see cref="Inventory.slotCount"/> of the source. 
        /// Each <see cref="EntityItem"/> is initialized with a reference back to this <see cref="FInventory"/> 
        /// instance, enabling complex operations like mod-swapping and inter-slot item movement.
        /// </remarks>
        /// <summary>
        /// Initializes a new instance of the <see cref="FInventory"/> class wrapping the specified game inventory.
        /// </summary>
        /// <param name="inv">The underlying <see cref="Inventory"/> to wrap.</param>
        public FInventory(Inventory inv)
        {
            _inv = inv;
            _items = new EntityItem[inv.slotCount];
            for (var i = 0; i < inv.slotCount; i++)
                _items[i] = new EntityItem(_inv, i);
        }

        /// <summary>
        /// Adds a single item of the specified type to the inventory.
        /// </summary>
        /// <param name="name">The name of the item to add.</param>
        public void AddItem(string name)
        {
            AddItem(name, 1);
        }

        /// <summary>
        /// Adds a specified amount of an item type to the inventory.
        /// </summary>
        /// <param name="name">The name of the item to add.</param>
        /// <param name="amount">The quantity of the item to add.</param>
        public void AddItem(string name, int amount)
        {
            ItemDataBlock item = DatablockDictionary.GetByName(name);
            _inv.AddItemAmount(item, amount);
        }

        /// <summary>
        /// Adds a single item to a specific slot in the inventory.
        /// </summary>
        /// <param name="name">The name of the item to add.</param>
        /// <param name="slot">The target inventory slot index.</param>
        public void AddItemTo(string name, int slot)
        {
            AddItemTo(name, slot, 1);
        }

        /// <summary>
        /// Adds a specified amount of an item to a specific slot in the inventory.
        /// </summary>
        /// <param name="name">The name of the item to add.</param>
        /// <param name="slot">The target inventory slot index.</param>
        /// <param name="amount">The quantity of the item to add.</param>
        public void AddItemTo(string name, int slot, int amount)
        {
            ItemDataBlock byName = DatablockDictionary.GetByName(name);
            if (byName != null)
            {
                Inventory.Slot.Kind place = Inventory.Slot.Kind.Default;
                _inv.AddItemSomehow(byName, new Inventory.Slot.Kind?(place), slot, amount);
            }
        }

        /// <summary>
        /// Deletes all items from the inventory.
        /// </summary>
        public void ClearAll()
        {
            _inv.Clear();
        }

        private int GetFreeSlots()
        {
            int num = 0;
            for (int i = 0; i < _inv.slotCount; i++)
            {
                if (_inv.IsSlotFree(i))
                {
                    num++;
                }
            }
            return num;
        }

        /// <summary>
        /// Checks if the inventory contains at least the specified amount of an item.
        /// </summary>
        /// <param name="name">The name of the item to check for.</param>
        /// <param name="amount">The minimum quantity required. Defaults to 1.</param>
        /// <returns>True if the item and amount are found, otherwise false.</returns>
        public bool HasItem(string name, int amount = 1)
        {
            int num = 0;
            foreach (EntityItem item in Items)
            {
                if (!item.IsEmpty() && item.Name == name)
                    num += item.UsesLeft;
            }
            return (num >= amount);
        }

        /// <summary>
        /// Moves an item from one slot to another within the inventory.
        /// </summary>
        /// <param name="s1"> The source slot index.</param>
        /// <param name="s2">The destination slot index.</param>
        public void MoveItem(int s1, int s2)
        {
            _inv.MoveItemAtSlotToEmptySlot(_inv, s1, s2);
        }

        /// <summary>
        /// Removes a specified amount of an item from the inventory by its name.
        /// </summary>
        /// <param name="name">The name of the item to remove.</param>
        /// <param name="amount">The quantity to remove. Defaults to 1.</param>
        public void RemoveItem(string name, int amount = 1)
        {
            foreach (EntityItem item in Items)
            {
                if (!item.IsEmpty() && item.Name == name)
                {
                    if (item.UsesLeft > amount)
                    {
                        _inv.RemoveItem(item.RInventoryItem);
                        AddItem(item.Name, (item.UsesLeft - amount));
                    }
                    else if (item.UsesLeft == amount)
                    {
                        _inv.RemoveItem(item.RInventoryItem);
                    }
                    else
                    {
                        _inv.RemoveItem(item.RInventoryItem);
                        amount -= item.UsesLeft;
                    }
                }
            }
        }

        /// <summary>
        /// Removes a specified amount of an item from a specific inventory slot.
        /// </summary>
        /// <param name="slot">The index of the slot to remove from.</param>
        /// <param name="amount">The quantity to remove. Defaults to 1.</param>
        public void RemoveItem(int slot, int amount = 1)
        {
            EntityItem item = Items[slot];
            if (item == null)
                return;
            if (item.UsesLeft > amount)
            {
                _inv.RemoveItem(item.RInventoryItem);
                AddItem(item.Name, (item.UsesLeft - amount));
                return;
            }
            _inv.RemoveItem(item.RInventoryItem);
        }

        /// <summary>
        /// Gets the current number of free (empty) slots in the inventory.
        /// </summary>
        public int FreeSlots
        {
            get
            {
                return GetFreeSlots();
            }
        }

        /// <summary>
        /// Gets the total number of slots available in this inventory.
        /// </summary>
        public int SlotCount
        {
            get
            {
                return _inv.slotCount;
            }
        }

        /// <summary>
        /// Gets the items from the inventory.
        /// </summary>
        public EntityItem[] Items
        {
            get
            {
                return _items;
            }
        }
    }
}
