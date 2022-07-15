using System.Linq;

namespace Fougerite
{
    public class PlayerItem
    {
        private Inventory internalInv;
        private int internalSlot;

        public PlayerItem()
        {
        }

        public PlayerItem(ref Inventory inv, int slot)
        {
            this.internalInv = inv;
            this.internalSlot = slot;
        }

        /// <summary>
        /// Consumes the item if its not empty.
        /// </summary>
        /// <param name="qty"></param>
        public void Consume(int qty)
        {
            if (!this.IsEmpty())
            {
                this.RInventoryItem.Consume(ref qty);
            }
        }

        /// <summary>
        /// Drops the item.
        /// </summary>
        public void Drop()
        {
            DropHelper.DropItem(this.internalInv, this.Slot);
        }

        private IInventoryItem GetItemRef()
        {
            IInventoryItem item;
            this.internalInv.GetItem(this.internalSlot, out item);
            return item;
        }

        /// <summary>
        /// Checks if the current item on the slot exists or not.
        /// </summary>
        /// <returns></returns>
        public bool IsEmpty()
        {
            return (this.RInventoryItem == null);
        }

        /// <summary>
        /// Tries to combine this item with the specified one.
        /// </summary>
        /// <param name="pi"></param>
        /// <returns></returns>
        public bool TryCombine(PlayerItem pi)
        {
            if (this.IsEmpty() || pi.IsEmpty())
            {
                return false;
            }
            return (this.RInventoryItem.TryCombine(pi.RInventoryItem) != InventoryItem.MergeResult.Failed);
        }

        /// <summary>
        /// Tries to stack this item with the specified one.
        /// </summary>
        /// <param name="pi"></param>
        /// <returns></returns>
        public bool TryStack(PlayerItem pi)
        {
            if (this.IsEmpty() || pi.IsEmpty())
            {
                return false;
            }
            return (this.RInventoryItem.TryStack(pi.RInventoryItem) != InventoryItem.MergeResult.Failed);
        }

        /// <summary>
        /// Returns the original IInventoryItem class from Rust.
        /// </summary>
        public IInventoryItem RInventoryItem
        {
            get
            {
                return this.GetItemRef();
            }
            set
            {
                this.RInventoryItem = value;
            }
        }

        /// <summary>
        /// Gets the name of the item.
        /// </summary>
        public string Name
        {
            get
            {
                if (!this.IsEmpty())
                {
                    return this.RInventoryItem.datablock.name;
                }
                return null;
            }
            set
            {
                this.RInventoryItem.datablock.name = value;
            }
        }

        /// <summary>
        /// Returns the amount of the item
        /// </summary>
        public int Quantity
        {
            get
            {
                return Util.UStackable.Contains(Name) ? 1 : this.UsesLeft;
            }
            set
            {
                this.UsesLeft = value;
            }
        }

        /// <summary>
        /// Gets the current slot of the item. Returns -1 if the item is empty.
        /// </summary>
        public int Slot
        {
            get
            {
                if (!this.IsEmpty())
                {
                    return this.RInventoryItem.slot;
                }
                return -1;
            }
        }

        /// <summary>
        /// Gets the uses left of this item.
        /// </summary>
        public int UsesLeft
        {
            get
            {
                if (!this.IsEmpty())
                {
                    return this.RInventoryItem.uses;
                }
                return -1;
            }
            set
            {
                this.RInventoryItem.SetUses(value);
            }
        }

        /*public class Mods
        {
            public BulletWeaponDataBlock Weapon;
            public bool _IsWeapon;
            public Mods(IInventoryItem iitem)
            {
                Weapon = iitem.datablock as BulletWeaponDataBlock;
                if (Weapon == null)
                {
                    _IsWeapon = false;
                    return;
                }
                //Weapon.ConstructItem().;
            }

            public bool IsWeapon
            {
                get { return _IsWeapon; }
            }
        }*/
    }
}