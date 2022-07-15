using System.Linq;

namespace Fougerite
{
    /// <summary>
    /// Represents an Entity inventory.
    /// </summary>
    public class EntityInv
    {
        private readonly Fougerite.Entity entity;
        private readonly EntityItem[] _items;
        private readonly Inventory _inv;

        public EntityInv(Inventory inv, Entity ent)
        {
            this.entity = ent;
            this._inv = inv;

            this._items = new EntityItem[inv.slotCount];
            for (var i = 0; i < inv.slotCount; i++)
                this._items[i] = new EntityItem(this._inv, i);
        }

        /// <summary>
        /// Adds the Item by It's name if correctly specified.
        /// </summary>
        /// <param name="name">Name of the item.</param>
        public void AddItem(string name)
        {
            this.AddItem(name, 1);
        }

        /// <summary>
        /// Adds the Item by It's name if correctly specified with the amount.
        /// </summary>
        /// <param name="name">Name of the item.</param>
        /// <param name="amount">Amount of the item.</param>
        public void AddItem(string name, int amount)
        {
            ItemDataBlock item = DatablockDictionary.GetByName(name);
            this._inv.AddItemAmount (item, amount);
        }

        /// <summary>
        /// Adds the Item by It's name if correctly specified to the specified slot number.
        /// </summary>
        /// <param name="name">Name of the item.</param>
        /// <param name="slot">The slot number of the Chest / Stash. Large: 1-35, Medium: 1-11, Stash: 1-3</param>
        public void AddItemTo(string name, int slot)
        {
            this.AddItemTo(name, slot, 1);
        }

        /// <summary>
        /// Adds the Item by It's name if correctly specified to the specified slot number.
        /// </summary>
        /// <param name="name">Name of the item.</param>
        /// <param name="amount">Amount of the item.</param>
        /// <param name="slot">The slot number of the Chest / Stash. Large: 1-35, Medium: 1-11, Stash: 1-3</param>
        public void AddItemTo(string name, int slot, int amount)
        {
            ItemDataBlock byName = DatablockDictionary.GetByName(name);
            if (byName != null)
            {
                Inventory.Slot.Kind place = Inventory.Slot.Kind.Default;
                this._inv.AddItemSomehow(byName, new Inventory.Slot.Kind?(place), slot, amount);
            }
        }

        /// <summary>
        /// Deletes all items from the Inventory.
        /// </summary>
        public void ClearAll()
        {
            this._inv.Clear();
        }
        
        /// <summary>
        /// Gets entity by the Inventory.
        /// </summary>
        public Fougerite.Entity Entity
        {
            get
            {
                return this.entity;
            }
        }

        private int GetFreeSlots ()
        {
            int num = 0;
            for (int i = 0; i < this._inv.slotCount; i++)
            {
                if (this._inv.IsSlotFree(i))
                {
                    num++;
                }
            }
            return num;
        }

        /// <summary>
        /// Checks if the inventory contains an item.
        /// </summary>
        /// <param name="name">Name of the item</param>
        /// <param name="amount">Amount of the item atleast, default is 1.</param>
        /// <returns>Returns true if the item is found in the inventory.</returns>
        public bool HasItem(string name, int amount = 1)
        {
            int num = 0;
            foreach (EntityItem item in this.Items)
            {
                if (item.Name == name)
                {
                    if (Util.UStackable.Contains(name))
                    {
                        num += 1;
                        continue;
                    }
                    num += item.UsesLeft;
                }
            }
            return (num >= amount);
        }

        /// <summary>
        /// Moves item on the s1 slot to s2.
        /// </summary>
        /// <param name="s1"></param>
        /// <param name="s2"></param>
        public void MoveItem(int s1, int s2)
        {
            this._inv.MoveItemAtSlotToEmptySlot(this._inv, s1, s2);
        }

        /// <summary>
        /// Removes the specified amount of item. By default It's 1.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="amount"></param>
        public void RemoveItem (string name, int amount = 1)
        {
            foreach (EntityItem item in this.Items)
            {
                if (item.Name == name)
                {
                    if (item.UsesLeft > amount)
                    {
                        this._inv.RemoveItem(item.RInventoryItem);
                        this.AddItem(item.Name, (item.UsesLeft - amount));
                        return;
                    }
                    else if (item.UsesLeft == amount)
                    {
                        this._inv.RemoveItem(item.RInventoryItem);
                        return;
                    }
                    else
                    {
                        this._inv.RemoveItem(item.RInventoryItem);
                        amount -= item.UsesLeft;
                    }
                }    
            }
        }

        /// <summary>
        /// Removes a specified amount of item on the specified slot.
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="amount"></param>
        public void RemoveItem (int slot, int amount = 1)
        {
            EntityItem item = this.Items [slot];
            if (item == null)
                return;
            if (item.UsesLeft > amount)
            {
                this._inv.RemoveItem (item.RInventoryItem);
                this.AddItem (item.Name, (item.UsesLeft - amount));
                return;
            }
            this._inv.RemoveItem (item.RInventoryItem);
        }

        /// <summary>
        /// Counts the available slots in the inventory.
        /// </summary>
        public int FreeSlots
        {
            get
            {
                return this.GetFreeSlots();
            }
        }

        /// <summary>
        /// Tells you the maximum slot in the inventory.
        /// </summary>
        public int SlotCount
        {
            get
            {
                return this._inv.slotCount;
            }
        }

        /// <summary>
        /// Gets the Rust inventory class.
        /// </summary>
        public Inventory InternalInventory
        {
            get
            {
                return this._inv;
            }
        }

        /// <summary>
        /// Gets you all Items in an array.
        /// </summary>
        public EntityItem[] Items
        {
            get
            {
                return this._items;
            }
        }
    }
}
