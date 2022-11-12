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
        private readonly ResourceTarget res;
        private readonly ItemDataBlock dataBlock = null;
        private readonly ResourceGivePair resourceGivePair = null;

        public GatherEvent(ResourceTarget r, ItemDataBlock db, int qty)
        {
            res = r;
            _qty = qty;
            _item = db.name;
            _type = "Tree";
            dataBlock = db;
            Override = false;
        }

        public GatherEvent(ResourceTarget r, ResourceGivePair gp, int qty)
        {
            res = r;
            _qty = qty;
            _item = gp.ResourceItemDataBlock.name;
            _type = res.type.ToString();
            resourceGivePair = gp;
            Override = false;
        }

        /// <summary>
        /// Gets the amount of resources left in the object.
        /// </summary>
        public int AmountLeft
        {
            get
            {
                return res.GetTotalResLeft();
            }
        }

        /// <summary>
        /// Gets the name of item that we are receiving from the gather.
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
        /// Gets or Sets if we should minimize the amount of resources left in the resource.
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
        /// Gets the percent of the resources.
        /// </summary>
        public float PercentFull
        {
            get
            {
                return res.GetPercentFull();
            }
        }

        /// <summary>
        /// Gets the Quantity of the items we are gathering.
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
        /// Gets the type of resource we are hitting.
        /// </summary>
        public string Type
        {
            get
            {
                return _type;
            }
        }

        /// <summary>
        /// Gets the resource target that we are hitting.
        /// </summary>
        public ResourceTarget ResourceTarget
        {
            get
            {
                return res;
            }
        }

        /// <summary>
        /// Gets the itemdatablock that we are gathering.
        /// </summary>
        public ItemDataBlock ItemDataBlock
        {
            get
            {
                return dataBlock;
            }
        }

        /// <summary>
        /// Gets the original ResourceGivePair class.
        /// </summary>
        public ResourceGivePair ResourceGivePair
        {
            get
            {
                return resourceGivePair;
            }
        }
    }
}