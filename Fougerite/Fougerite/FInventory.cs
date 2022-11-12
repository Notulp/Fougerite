namespace Fougerite
{
    /// <summary>
    /// This class is created when an Item is added or removed to/from an inventory.
    /// </summary>
    public class FInventory
    {
        private Inventory _inv;
        private EntityItem[] _items;

        public FInventory(Inventory inv)
        {
            _inv = inv;
            _items = new EntityItem[inv.slotCount];
            for (var i = 0; i < inv.slotCount; i++)
                _items[i] = new EntityItem(_inv, i);
        }

        /// <summary>
        /// Adds one item to the inventory.
        /// </summary>
        /// <param name="name"></param>
        public void AddItem(string name)
        {
            AddItem(name, 1);
        }

        /// <summary>
        /// Adds an Item with the given amount to the inventory.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="amount"></param>
        public void AddItem(string name, int amount)
        {
            ItemDataBlock item = DatablockDictionary.GetByName(name);
            _inv.AddItemAmount(item, amount);
        }

        /// <summary>
        /// Adds an item to the specified slot.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="slot"></param>
        public void AddItemTo(string name, int slot)
        {
            AddItemTo(name, slot, 1);
        }

        /// <summary>
        /// Adds an item to the specified slot with the given amount.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="slot"></param>
        /// <param name="amount"></param>
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
        /// Checks if the inventory has the specified item.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="amount"></param>
        /// <returns></returns>
        public bool HasItem(string name, int amount = 1)
        {
            int num = 0;
            foreach (EntityItem item in Items)
            {
                if (item.Name == name)
                    num += item.UsesLeft;
            }
            return (num >= amount);
        }

        /// <summary>
        /// Moves the item from s1 slot to s2 slot.
        /// </summary>
        /// <param name="s1"></param>
        /// <param name="s2"></param>
        public void MoveItem(int s1, int s2)
        {
            _inv.MoveItemAtSlotToEmptySlot(_inv, s1, s2);
        }

        /// <summary>
        /// Removes the specific item with the given amount.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="amount"></param>
        public void RemoveItem(string name, int amount = 1)
        {
            foreach (EntityItem item in Items)
            {
                if (item.Name == name)
                {
                    if (item.UsesLeft > amount)
                    {
                        _inv.RemoveItem(item.RInventoryItem);
                        AddItem(item.Name, (item.UsesLeft - amount));
                        return;
                    }
                    else if (item.UsesLeft == amount)
                    {
                        _inv.RemoveItem(item.RInventoryItem);
                        return;
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
        /// Removes an item from the specified slot with the given amount.
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="amount"></param>
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
        /// Counts the freeslots in the inventory.
        /// </summary>
        public int FreeSlots
        {
            get
            {
                return GetFreeSlots();
            }
        }

        /// <summary>
        /// Gets the maximum slot amount rom the inventory.
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
