using UnityEngine;

namespace Fougerite.Events
{
    /// <summary>
    /// This class is created when a player is gathering from an animal or from a resource.
    /// </summary>
    public class GatherEvent
    {
        private string _item;
        private bool _over;
        private int _qty;
        private readonly string _type;
        private readonly ResourceTarget _res;
        private readonly ItemDataBlock _dataBlock;
        private readonly ResourceGivePair _resourceGivePair;
        private readonly WoodBlockerTemp _wbt;
        private readonly GameObject _treeGameObject;
        private readonly ResourceTarget.ResourceTargetType _resType;

        /// <summary>
        /// Initializes a new instance of the <see cref="GatherEvent"/> class for Tree gathering.
        /// </summary>
        /// <param name="r">The resource target being hit.</param>
        /// <param name="db">The datablock of the item being gathered.</param>
        /// <param name="qty">The initial quantity to be gathered.</param>
        /// <param name="wbt">The WoodBlockerTemp instance associated with this tree gathering event.</param>
        /// <param name="treeGameObject">The collider gameobject of the tree being farmed.</param>
        public GatherEvent(ResourceTarget r, ItemDataBlock db, int qty, WoodBlockerTemp wbt, GameObject treeGameObject)
        {
            _res = r;
            _wbt = wbt;
            _treeGameObject = treeGameObject;
            _qty = qty;
            _item = db.name;
            _type = "Tree";
            _dataBlock = db;
            Override = false;
            _resType = ResourceTarget.ResourceTargetType.StaticTree;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GatherEvent"/> class for general resource gathering.
        /// </summary>
        /// <param name="r">The resource target being hit.</param>
        /// <param name="gp">The resource give pair containing the item data.</param>
        /// <param name="qty">The initial quantity to be gathered.</param>
        public GatherEvent(ResourceTarget r, ResourceGivePair gp, int qty)
        {
            _res = r;
            _qty = qty;
            _item = gp.ResourceItemDataBlock.name;
            _dataBlock = gp.ResourceItemDataBlock;
            _type = _res.type.ToString();
            _resourceGivePair = gp;
            Override = false;
            _resType = r.type;
        }

        /// <summary>
        /// Gets the total amount of resources remaining in the target object.
        /// </summary>
        public int AmountLeft
        {
            get
            {
                if (_wbt != null)
                    return (int) _wbt.GetWoodLeft();
                if (_res != null)
                    return _res.GetTotalResLeft();

                return 0;
            }
        }

        /// <summary>
        /// Gets or sets the name of the item that the player will receive.
        /// Changing this allows a plugin to swap the resource gathered (Like M4, and so on).
        /// </summary>
        public string Item
        {
            get
            {
                return _item;
            }
            set
            {
                _item = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the gathering logic should bypass resource limits.
        /// If false, the quantity is capped at the amount actually remaining in the resource.
        /// If true, the player can receive the full <see cref="Quantity"/> regardless of the object's remaining health.
        /// </summary>
        public bool Override
        {
            get
            {
                return _over;
            }
            set
            {
                _over = value;
            }
        }

        /// <summary>
        /// Gets the current percentage of resources remaining in the target.
        /// </summary>
        public float PercentFull
        {
            get
            {
                if (_wbt != null)
                    return _wbt.GetWoodLeft() / _wbt.maxWood;
                if (_res != null)
                    return _res.GetPercentFull();

                return 0f;
            }
        }

        /// <summary>
        /// Gets or sets the quantity of items to be gathered. 
        /// IMPORTANT: Setting this value to 0 will effectively cancel the gathering event, 
        /// resulting in the player receiving no items and the object receiving no damage.
        /// </summary>
        public int Quantity
        {
            get
            {
                return _qty;
            }
            set
            {
                _qty = value;
            }
        }

        /// <summary>
        /// Gets the name of type of resource we are hitting.
        /// </summary>
        public string Type
        {
            get
            {
                return _type;
            }
        }

        /// <summary>
        /// Gets the original <see cref="ResourceTarget"/> object being interacted with.
        /// This is NULL when you farm a tree by the logic of rust legacy.
        /// </summary>
        public ResourceTarget ResourceTarget
        {
            get
            {
                return _res;
            }
        }

        /// <summary>
        /// Gets the <see cref="ItemDataBlock"/> representing the gathered resource.
        /// </summary>
        public ItemDataBlock ItemDataBlock
        {
            get
            {
                return _dataBlock;
            }
        }

        /// <summary>
        /// Gets the original <see cref="ResourceGivePair"/> associated with this gather.
        /// This is NULL when you farm a tree by the logic of rust legacy.
        /// </summary>
        public ResourceGivePair ResourceGivePair
        {
            get
            {
                return _resourceGivePair;
            }
        }

        /// <summary>
        /// Gets the <see cref="WoodBlockerTemp"/> instance associated with this gather event, if applicable.
        /// Only non-null for tree gathering events. Originally Rust legacy server doesn't know where trees are
        /// so the client side decides which tree you are gathering. The gather is then broadcasted to all players
        /// server side for local WoodBlockerTemp instance, so multiple players cannot farm the same tree at the same time.
        /// By default the trees reload after 5 minutes.
        /// Fougerite adds server side check by using WoodBlockerTemp that Facepunch implemented so It is also
        /// verified, and tree farming cheats do not work anymore.
        /// Plugins can now also access the content of the tree being farmed.
        /// </summary>
        public WoodBlockerTemp WoodBlockerTemp
        {
            get
            {
                return _wbt;
            }
        }

        /// <summary>
        /// The collider GameObject of the tree being farmed.
        /// This is NULL when you farm a resource that is not a tree.
        /// </summary>
        public GameObject TreeGameObject
        {
            get
            {
                return _treeGameObject;
            }
        }

        /// <summary>
        /// Gets the enum type of the resource we are hitting.
        /// </summary>
        public ResourceTarget.ResourceTargetType ResourceTargetType
        {
            get
            {
                return _resType;
            }
        }
    }
}