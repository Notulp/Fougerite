using System.Linq;

namespace Fougerite
{
    public class PlayerInv
    {
        private PlayerItem[] _armorItems;
        private PlayerItem[] _barItems;
        private Inventory _inv;
        private PlayerItem[] _items;
        private Fougerite.Player player;

        public PlayerInv(Fougerite.Player player)
        {
            this.player = player;
            this._inv = player.PlayerClient.controllable.GetComponent<Inventory>();
            this.InitItems();
        }

        /// <summary>
        /// Returns the Current item that is in the player's hand. Can return null or empty item.
        /// </summary>
        public PlayerItem ActiveItem
        {
            get
            {
                return new PlayerItem(ref _inv, InternalInventory.activeItem.slot);
            }
        }

        /// <summary>
        /// Adds 1 Item to the player inventory.
        /// </summary>
        /// <param name="name"></param>
        public void AddItem(string name)
        {
            this.AddItem(name, 1);
        }

        /// <summary>
        /// Adds an Item with the specified amount.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="amount"></param>
        public void AddItem(string name, int amount)
        {
            string[] strArray = new string[] { name, amount.ToString() };
            ConsoleSystem.Arg arg = new ConsoleSystem.Arg("");
            arg.Args = strArray;
            arg.SetUser(this.player.PlayerClient.netUser);
            inv.give(ref arg);
        }

        /// <summary>
        /// Adds an Item to the inventory.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="i"></param>
        public void AddItem(PlayerItem item, int i = 1)
        {
            _inv.AddItemAmount(item.RInventoryItem.datablock, i);
        }

        /// <summary>
        /// Adds one item to the specified slot.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="slot"></param>
        public void AddItemTo(string name, int slot)
        {
            this.AddItemTo(name, slot, 1);
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
                Inventory.Slot.Kind belt = Inventory.Slot.Kind.Default;
                if ((slot > 0x1d) && (slot < 0x24))
                {
                    belt = Inventory.Slot.Kind.Belt;
                }
                else if ((slot >= 0x24) && (slot < 40))
                {
                    belt = Inventory.Slot.Kind.Armor;
                }
                this._inv.AddItemSomehow(byName, new Inventory.Slot.Kind?(belt), slot, amount);
            }
        }

        /// <summary>
        /// Clears the inventory.
        /// </summary>
        public void Clear()
        {
            foreach (PlayerItem item in this.Items)
            {
                this._inv.RemoveItem(item.RInventoryItem);
            }
            foreach (PlayerItem item2 in this.BarItems)
            {
                this._inv.RemoveItem(item2.RInventoryItem);
            }
        }

        /// <summary>
        /// Clears everything from the inventory.
        /// </summary>
        public void ClearAll()
        {
            this._inv.Clear();
        }
        
        /// <summary>
        /// Clears the armor slots-
        /// </summary>
        public void ClearArmor()
        {
            foreach (PlayerItem item in this.ArmorItems)
            {
                this._inv.RemoveItem(item.RInventoryItem);
            }
        }

        /// <summary>
        /// Clears the bar slots.
        /// </summary>
        public void ClearBar()
        {
            foreach (PlayerItem item in this.BarItems)
            {
                this._inv.RemoveItem(item.RInventoryItem);
            }
        }

        /// <summary>
        /// Drops all items.
        /// </summary>
        public void DropAll()
        {
            DropHelper.DropInventoryContents(this.InternalInventory);
        }
        
        /// <summary>
        /// Drops the specific item.
        /// </summary>
        /// <param name="pi"></param>
        public void DropItem(PlayerItem pi)
        {
            DropHelper.DropItem(this.InternalInventory, pi.Slot);
        }

        /// <summary>
        /// Drops the item that is on the specified slot.
        /// </summary>
        /// <param name="slot"></param>
        public void DropItem(int slot)
        {
            DropHelper.DropItem(this.InternalInventory, slot);
        }

        private int GetFreeSlots()
        {
            int num = 0;
            for (int i = 0; i < (this._inv.slotCount - 4); i++)
            {
                if (this._inv.IsSlotFree(i))
                {
                    num++;
                }
            }
            return num;
        }

        /// <summary>
        /// Checks if the player has the specific item.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool HasItem(string name)
        {
            return this.HasItem(name, 1);
        }

        /// <summary>
        /// Checks if the player has a specific amount of item.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="number"></param>
        /// <returns></returns>
        public bool HasItem(string name, int number)
        {
            int num = 0;
            foreach (PlayerItem item in this.Items)
            {
                if (item.Name == name)
                {
                    if (Util.UStackable.Contains(name))
                    {
                        num += 1;
                        continue;
                    }
                    if (item.UsesLeft >= number)
                    {
                        return true;
                    }
                    num += item.UsesLeft;
                }
            }
            foreach (PlayerItem item2 in this.BarItems)
            {
                if (item2.Name == name)
                {
                    if (Util.UStackable.Contains(name))
                    {
                        num += 1;
                        continue;
                    }
                    if (item2.UsesLeft >= number)
                    {
                        return true;
                    }
                    num += item2.UsesLeft;
                }
            }
            foreach (PlayerItem item3 in this.ArmorItems)
            {
                if (item3.Name == name)
                {
                    if (Util.UStackable.Contains(name))
                    {
                        num += 1;
                        continue;
                    }
                    if (item3.UsesLeft >= number)
                    {
                        return true;
                    }
                    num += item3.UsesLeft;
                }
            }
            return (num >= number);
        }

        private void InitItems()
        {
            this.Items = new PlayerItem[30];
            this.ArmorItems = new PlayerItem[4];
            this.BarItems = new PlayerItem[6];
            for (int i = 0; i < this._inv.slotCount; i++)
            {
                if (i < 30)
                {
                    this.Items[i] = new PlayerItem(ref this._inv, i);
                }
                else if (i < 0x24)
                {
                    this.BarItems[i - 30] = new PlayerItem(ref this._inv, i);
                }
                else if (i < 40)
                {
                    this.ArmorItems[i - 0x24] = new PlayerItem(ref this._inv, i);
                }
            }
        }

        /// <summary>
        /// Moves item from s1 slot to s2 slot.
        /// </summary>
        /// <param name="s1"></param>
        /// <param name="s2"></param>
        public void MoveItem(int s1, int s2)
        {
            this._inv.MoveItemAtSlotToEmptySlot(this._inv, s1, s2);
        }

        /// <summary>
        /// Removes player item.
        /// </summary>
        /// <param name="pi"></param>
        public void RemoveItem(PlayerItem pi)
        {
            foreach (PlayerItem item in this.Items)
            {
                if (item == pi)
                {
                    this._inv.RemoveItem(pi.RInventoryItem);
                    return;
                }
            }
            foreach (PlayerItem item2 in this.ArmorItems)
            {
                if (item2 == pi)
                {
                    this._inv.RemoveItem(pi.RInventoryItem);
                    return;
                }
            }
            foreach (PlayerItem item3 in this.BarItems)
            {
                if (item3 == pi)
                {
                    this._inv.RemoveItem(pi.RInventoryItem);
                    break;
                }
            }
        }

        /// <summary>
        /// Removes item from slot.
        /// </summary>
        /// <param name="slot"></param>
        public void RemoveItem(int slot)
        {
            this._inv.RemoveItem(slot);
        }

        /// <summary>
        /// Removes an amount of item on the specific slot.
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="number"></param>
        public void RemoveItem(int slot, int number)
        {
            int qty = number;
            foreach (PlayerItem item in this.Items)
            {
                if (item.Slot == slot)
                {
                    if (item.UsesLeft > qty)
                    {
                        item.Consume(qty);
                        qty = 0;
                        break;
                    }
                    qty -= item.UsesLeft;
                    if (qty < 0)
                    {
                        qty = 0;
                    }
                    this._inv.RemoveItem(item.Slot);
                    if (qty == 0)
                    {
                        break;
                    }
                }
            }
            if (qty != 0)
            {
                foreach (PlayerItem item2 in this.ArmorItems)
                {
                    if (item2.Slot == slot)
                    {
                        if (item2.UsesLeft > qty)
                        {
                            item2.Consume(qty);
                            qty = 0;
                            break;
                        }
                        qty -= item2.UsesLeft;
                        if (qty < 0)
                        {
                            qty = 0;
                        }
                        this._inv.RemoveItem(item2.Slot);
                        if (qty == 0)
                        {
                            break;
                        }
                    }
                }
                if (qty != 0)
                {
                    foreach (PlayerItem item3 in this.BarItems)
                    {
                        if (item3.Slot == slot)
                        {
                            if (item3.UsesLeft > qty)
                            {
                                item3.Consume(qty);
                                qty = 0;
                                return;
                            }
                            qty -= item3.UsesLeft;
                            if (qty < 0)
                            {
                                qty = 0;
                            }
                            this._inv.RemoveItem(item3.Slot);
                            if (qty == 0)
                            {
                                return;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Removes a specific item name with an amount.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="number"></param>
        public void RemoveItem(string name, int number = 1)
        {
            int qty = number;
            foreach (PlayerItem item in this.Items)
            {
                if (item.Name == name)
                {
                    if (item.UsesLeft > qty)
                    {
                        item.Consume(qty);
                        qty = 0;
                        break;
                    }
                    qty -= item.UsesLeft;
                    if (qty < 0)
                    {
                        qty = 0;
                    }
                    this._inv.RemoveItem(item.Slot);
                    if (qty == 0)
                    {
                        break;
                    }
                }
            }
            if (qty != 0)
            {
                foreach (PlayerItem item2 in this.ArmorItems)
                {
                    if (item2.Name == name)
                    {
                        if (item2.UsesLeft > qty)
                        {
                            item2.Consume(qty);
                            qty = 0;
                            break;
                        }
                        qty -= item2.UsesLeft;
                        if (qty < 0)
                        {
                            qty = 0;
                        }
                        this._inv.RemoveItem(item2.Slot);
                        if (qty == 0)
                        {
                            break;
                        }
                    }
                }
                if (qty != 0)
                {
                    foreach (PlayerItem item3 in this.BarItems)
                    {
                        if (item3.Name == name)
                        {
                            if (item3.UsesLeft > qty)
                            {
                                item3.Consume(qty);
                                qty = 0;
                                return;
                            }
                            qty -= item3.UsesLeft;
                            if (qty < 0)
                            {
                                qty = 0;
                            }
                            this._inv.RemoveItem(item3.Slot);
                            if (qty == 0)
                            {
                                return;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Removes the specific item from the inventory.
        /// </summary>
        /// <param name="name"></param>
        public void RemoveItemAll(string name)
        {
            this.RemoveItem(name, 0x1869f);
        }

        /// <summary>
        /// Gets all armor items of the player.
        /// </summary>
        public PlayerItem[] ArmorItems
        {
            get
            {
                return this._armorItems;
            }
            set
            {
                this._armorItems = value;
            }
        }

        /// <summary>
        /// Gets all bar items of the player.
        /// </summary>
        public PlayerItem[] BarItems
        {
            get
            {
                return this._barItems;
            }
            set
            {
                this._barItems = value;
            }
        }

        /// <summary>
        /// Gets the freeslots of the player's inventory.
        /// </summary>
        public int FreeSlots
        {
            get
            {
                return this.GetFreeSlots();
            }
        }

        /// <summary>
        /// Gets the original rust inventory class.
        /// </summary>
        public Inventory InternalInventory
        {
            get
            {
                return this._inv;
            }
            set
            {
                this._inv = value;
            }
        }

        /// <summary>
        /// Gets all items of the player's inventory.
        /// </summary>
        public PlayerItem[] Items
        {
            get
            {
                return this._items;
            }
            set
            {
                this._items = value;
            }
        }
    }
}