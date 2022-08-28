namespace Fougerite.Events
{
    /// <summary>
    /// This class is used on Blueprint Use Hook.
    /// </summary>
    public class BPUseEvent
    {
        private readonly BlueprintDataBlock _bdb;
        private bool _cancel;
        private readonly IBlueprintItem _item;

        public BPUseEvent(BlueprintDataBlock bdb, IBlueprintItem item)
        {
            _bdb = bdb;
            Cancel = false;
            _item = item;
        }

        /// <summary>
        /// Gets if the event is cancelled or can be set to cancelled.
        /// </summary>
        public bool Cancel
        {
            get
            {
                return _cancel;
            }
            set
            {
                _cancel = value;
            }
        }

        /// <summary>
        /// Gets the blueprint's datablock.
        /// </summary>
        public BlueprintDataBlock DataBlock
        {
            get
            {
                return _bdb;
            }
        }

        /// <summary>
        /// Gets the actual blueprint item.
        /// </summary>
        public IBlueprintItem Item
        {
            get
            {
                return _item;
            }
        }

        /// <summary>
        /// Gets the name of the blueprint item.
        /// </summary>
        public string ItemName
        {
            get
            {
                return _bdb.resultItem.name;
            }
        }
    }
}