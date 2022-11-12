using System;

namespace Fougerite
{
    public class ItemsBlocks : System.Collections.Generic.List<ItemDataBlock>
    {
        public ItemsBlocks(System.Collections.Generic.List<ItemDataBlock> items)
        {
            foreach (ItemDataBlock block in items)
            {
                Add(block);
            }
        }

        /// <summary>
        /// Finds the DataBlock by name.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public ItemDataBlock Find(string str)
        {
            foreach (ItemDataBlock block in this)
            {
                if (string.Equals(block.name, str, StringComparison.InvariantCultureIgnoreCase))
                {
                    return block;
                }
            }
            return null;
        }
    }
}