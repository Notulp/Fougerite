using System;
using System.Runtime.InteropServices;

namespace Fougerite
{
    public static class SuperFastHashUInt16Hack
    {
        [StructLayout(LayoutKind.Explicit)]
        // no guarantee this will remain working
        public struct BytetoUInt16Converter
        {
            [FieldOffset(0)] public Byte[] Bytes;

            [FieldOffset(0)] public UInt16[] UInts;
        }

        public static UInt32 Hash(Byte[] dataToHash)
        {
            Int32 dataLength = dataToHash.Length;
            if (dataLength == 0)
                return 0;
            UInt32 hash = (UInt32) dataLength;
            Int32 remainingBytes = dataLength & 3; // mod 4
            Int32 numberOfLoops = dataLength >> 2; // div 4
            Int32 currentIndex = 0;
            UInt16[] arrayHack = new BytetoUInt16Converter {Bytes = dataToHash}.UInts;
            while (numberOfLoops > 0)
            {
                hash += arrayHack[currentIndex++];
                UInt32 tmp = (UInt32) (arrayHack[currentIndex++] << 11) ^ hash;
                hash = (hash << 16) ^ tmp;
                hash += hash >> 11;
                numberOfLoops--;
            }

            currentIndex *= 2; // fix the length
            switch (remainingBytes)
            {
                case 3:
                    hash += (UInt16) (dataToHash[currentIndex++] | dataToHash[currentIndex++] << 8);
                    hash ^= hash << 16;
                    hash ^= ((UInt32) dataToHash[currentIndex]) << 18;
                    hash += hash >> 11;
                    break;
                case 2:
                    hash += (UInt16) (dataToHash[currentIndex++] | dataToHash[currentIndex] << 8);
                    hash ^= hash << 11;
                    hash += hash >> 17;
                    break;
                case 1:
                    hash += dataToHash[currentIndex];
                    hash ^= hash << 10;
                    hash += hash >> 1;
                    break;
                default:
                    break;
            }

            /* Force "avalanching" of final 127 bits */
            hash ^= hash << 3;
            hash += hash >> 5;
            hash ^= hash << 4;
            hash += hash >> 17;
            hash ^= hash << 25;
            hash += hash >> 6;

            return hash;
        }
    }
}