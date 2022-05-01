using System.Linq;
using Fougerite.Events;
using InventoryExtensions;
using UnityEngine;

namespace Fougerite
{
	/// <summary>
	/// Represents an Item on a slot.
	/// </summary>
    public class EntityItem
    {
        private readonly Inventory internalInv;
		private readonly int internalSlot;
		//internal const string PrefabName = ";drop_lootsack"; Dynamic cannot be used with this.

        public EntityItem(Inventory inv, int slot)
		{
			this.internalInv = inv;
			this.internalSlot = slot;
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
				Vector3 position = internalInv.transform.localPosition;
				// Try making the positions random, instead of letting the objects stuck into together.
				position.x += UnityEngine.Random.Range(0f, 0.85f);
				position.y += UnityEngine.Random.Range(0.75f, 1f);
				position.z += UnityEngine.Random.Range(0f, 0.85f);
				
				Vector3 arg = new Vector3(UnityEngine.Random.Range(0.75f, 1.3f), UnityEngine.Random.Range(0.75f, 1.3f), UnityEngine.Random.Range(0.75f, 1.3f));
				Quaternion rotation = new Quaternion(0f, 0f, 0f, 1f);
				GameObject go = NetCull.InstantiateDynamicWithArgs<Vector3>(trait.prefab, position, rotation, arg);
				dropped = go.GetComponent<ItemPickup>();
				if (!dropped.SetPickupItem(item))
				{
					//Debug.LogError($"Could not make item pickup for {item}", inventory);
					NetCull.Destroy(go);
					//internalInv.RemoveItem(item);
					//internalInv.MarkSlotDirty(Slot);
					return null;
				}

				internalInv.RemoveItem(item);
				//internalInv.MarkSlotDirty(Slot);
				return dropped;
				//DropHelper.DropItem(this.internalInv, this.Slot);
			}

			return null;
		}

		private IInventoryItem GetItemRef()
		{
			IInventoryItem item;
			this.internalInv.GetItem(this.internalSlot, out item);
			return item;
		}

		/// <summary>
		/// Gets the internal inventory.
		/// </summary>
		public Inventory Inventory
		{
			get { return this.internalInv; }
		}

	    /// <summary>
	    /// Checks if the Item Slot is empty.
	    /// </summary>
	    /// <returns></returns>
		public bool IsEmpty()
		{
			return (this.RInventoryItem == null);
		}

	    /// <summary>
	    /// Gets the original IInventoryItem of this item from the rust api.
	    /// </summary>
		public IInventoryItem RInventoryItem
		{
			get
			{
				return this.GetItemRef();
			}
		}

	    /// <summary>
	    /// Gets / Sets the name of this item.
	    /// </summary>
		public string Name
		{
			get
			{
				if (!this.IsEmpty())
				{
					return this.RInventoryItem.datablock.name;
				}
				return "Empty slot";
			}
			set
			{
				this.RInventoryItem.datablock.name = value;
			}
		}

	    /// <summary>
	    /// Gets the amount of the item in this slot.
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
	    /// Gets the slot of the item.
	    /// </summary>
		public int Slot
		{
			get
			{
				if (!this.IsEmpty())
				{
					return this.RInventoryItem.slot;
				}
				return this.internalSlot;
			}
		}

	    /// <summary>
	    /// Gets the uses remaining of the item. (Ammo, Research kit, etc.)
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
    }
}
