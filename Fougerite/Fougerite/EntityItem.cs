using System.Linq;
using UnityEngine;

namespace Fougerite
{
	/// <summary>
	/// Represents an Item on a slot.
	/// </summary>
    public class EntityItem
    {
        private readonly Inventory _internalInv;
		private readonly int _internalSlot;
		//internal const string PrefabName = ";drop_lootsack"; Dynamic cannot be used with this.

        public EntityItem(Inventory inv, int slot)
		{
			_internalInv = inv;
			_internalSlot = slot;
		}

	    /// <summary>
	    /// Drops this item from the inventory.
	    /// </summary>
		public ItemPickup Drop()
		{
			if (!IsEmpty())
			{
				IInventoryItem item = GetItemRef();
				if (item == null)
				{
					return null;
				}
				
				CharacterItemDropPrefabTrait trait = new Character().GetTrait<CharacterItemDropPrefabTrait>();
				
				ItemPickup dropped = null;
				Vector3 position = _internalInv.transform.localPosition;
				// Try making the positions random, instead of letting the objects stuck into together.
				position.x += Random.Range(0f, 0.85f);
				position.y += Random.Range(0.75f, 1f);
				position.z += Random.Range(0f, 0.85f);
				
				Vector3 arg = new Vector3(Random.Range(0.75f, 1.3f), Random.Range(0.75f, 1.3f), Random.Range(0.75f, 1.3f));
				Quaternion rotation = new Quaternion(0f, 0f, 0f, 1f);
				GameObject go = NetCull.InstantiateDynamicWithArgs(trait.prefab, position, rotation, arg);
				dropped = go.GetComponent<ItemPickup>();
				if (!dropped.SetPickupItem(item))
				{
					//Debug.LogError($"Could not make item pickup for {item}", inventory);
					NetCull.Destroy(go);
					//internalInv.RemoveItem(item);
					//internalInv.MarkSlotDirty(Slot);
					return null;
				}

				_internalInv.RemoveItem(item);
				//internalInv.MarkSlotDirty(Slot);
				return dropped;
				//DropHelper.DropItem(this.internalInv, this.Slot);
			}

			return null;
		}

		private IInventoryItem GetItemRef()
		{
			IInventoryItem item;
			_internalInv.GetItem(_internalSlot, out item);
			return item;
		}

		/// <summary>
		/// Gets the internal inventory.
		/// </summary>
		public Inventory Inventory
		{
			get { return _internalInv; }
		}

	    /// <summary>
	    /// Checks if the Item Slot is empty.
	    /// </summary>
	    /// <returns></returns>
		public bool IsEmpty()
		{
			return (RInventoryItem == null);
		}

	    /// <summary>
	    /// Gets the original IInventoryItem of this item from the rust api.
	    /// </summary>
		public IInventoryItem RInventoryItem
		{
			get
			{
				return GetItemRef();
			}
		}

	    /// <summary>
	    /// Gets / Sets the name of this item.
	    /// </summary>
		public string Name
		{
			get
			{
				if (!IsEmpty())
				{
					return RInventoryItem.datablock.name;
				}
				return "Empty slot";
			}
			set
			{
				RInventoryItem.datablock.name = value;
			}
		}

	    /// <summary>
	    /// Gets the amount of the item in this slot.
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
	    /// Gets the slot of the item.
	    /// </summary>
		public int Slot
		{
			get
			{
				if (!IsEmpty())
				{
					return RInventoryItem.slot;
				}
				return _internalSlot;
			}
		}

	    /// <summary>
	    /// Gets the uses remaining of the item. (Ammo, Research kit, etc.)
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
    }
}
