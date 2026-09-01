namespace Fougerite.Tools
{
    /// <summary>
    /// Contains Steam API related tools.
    /// </summary>
    public class SteamAPITools
    {
        /// <summary>
        /// AppID 252490
        /// </summary>
        public static readonly byte[] RustAppIdBytes = new byte[] { 0x4A, 0xDA, 0x03, 0x00 };
        /// <summary>
        /// AppID 480
        /// </summary>
        public static readonly byte[] SpacewarAppIdBytes = new byte[] { 0xE0, 0x01, 0x00, 0x00 };

        /// <summary>
        /// Searches for a specific byte sequence within a given byte array.
        /// </summary>
        /// <param name="data">The byte array in which to search for the sequence.</param>
        /// <param name="sequence">The byte sequence to locate within the data array.</param>
        /// <returns>
        /// Returns true if the specified sequence is found within the provided data array, otherwise, false.
        /// </returns>
        public static bool FindSequence(byte[] data, byte[] sequence)
        {
            if (data == null || sequence == null || data.Length < sequence.Length)
                return false;

            int max = data.Length - sequence.Length;
            for (int i = 0; i <= max; i++)
            {
                bool match = true;
                for (int j = 0; j < sequence.Length; j++)
                {
                    if (data[i + j] != sequence[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match) return true;
            }
            return false;
        }
    }
}