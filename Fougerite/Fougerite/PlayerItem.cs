using System.Linq;

namespace Fougerite
{
    public class PlayerItem
    {
        private readonly Inventory internalInv;
        private readonly int internalSlot;

        public PlayerItem()
        {
        }

        public PlayerItem(ref Inventory inv, int slot)
        {
            internalInv = inv;
            internalSlot = slot;
        }

        /// <summary>
        /// Consumes the item if its not empty.
        /// </summary>
        /// <param name="qty"></param>
        public void Consume(int qty)
        {
            if (!IsEmpty())
            {
                RInventoryItem.Consume(ref qty);
            }
        }

        /// <summary>
        /// Drops the item.
        /// </summary>
        public void Drop()
        {
            DropHelper.DropItem(internalInv, Slot);
        }

        /// <summary>
        /// Returns the IInventoryItem by internal slot.
        /// </summary>
        /// <returns></returns>
        private IInventoryItem GetItemRef()
        {
            IInventoryItem item;
            internalInv.GetItem(internalSlot, out item);
            return item;
        }

        /// <summary>
        /// Checks if the current item on the slot exists or not.
        /// </summary>
        /// <returns></returns>
        public bool IsEmpty()
        {
            return (RInventoryItem == null);
        }

        /// <summary>
        /// Tries to combine this item with the specified one.
        /// </summary>
        /// <param name="pi"></param>
        /// <returns></returns>
        public bool TryCombine(PlayerItem pi)
        {
            if (IsEmpty() || pi.IsEmpty())
            {
                return false;
            }
            return (RInventoryItem.TryCombine(pi.RInventoryItem) != InventoryItem.MergeResult.Failed);
        }

        /// <summary>
        /// Tries to stack this item with the specified one.
        /// </summary>
        /// <param name="pi"></param>
        /// <returns></returns>
        public bool TryStack(PlayerItem pi)
        {
            if (IsEmpty() || pi.IsEmpty())
            {
                return false;
            }
            return (RInventoryItem.TryStack(pi.RInventoryItem) != InventoryItem.MergeResult.Failed);
        }

        /// <summary>
        /// Returns the inventory class of the item.
        /// </summary>
        public Inventory InternalInventory
        {
            get
            {
                return internalInv;
            }
        }

        /// <summary>
        /// Returns the slot of the item.
        /// </summary>
        public int InternalSlot
        {
            get
            {
                return internalSlot;
            }
        }

        /// <summary>
        /// Returns the original IInventoryItem class from Rust.
        /// </summary>
        public IInventoryItem RInventoryItem
        {
            get
            {
                return GetItemRef();
            }
            set
            {
                RInventoryItem = value;
            }
        }

        /// <summary>
        /// Gets the name of the item.
        /// </summary>
        public string Name
        {
            get
            {
                if (!IsEmpty())
                {
                    return RInventoryItem.datablock.name;
                }
                return null;
            }
            set
            {
                RInventoryItem.datablock.name = value;
            }
        }

        /// <summary>
        /// Returns the amount of the item
        /// </summary>
        public int Quantity
        {
            get
            {
                return Util.UStackable.Contains(Name) ? 1 : UsesLeft;
            }
            set
            {
                UsesLeft = value;
            }
        }

        /// <summary>
        /// Gets the current slot of the item. Returns -1 if the item is empty.
        /// </summary>
        public int Slot
        {
            get
            {
                if (!IsEmpty())
                {
                    return RInventoryItem.slot;
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
                if (!IsEmpty())
                {
                    return RInventoryItem.uses;
                }
                return -1;
            }
            set
            {
                RInventoryItem.SetUses(value);
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