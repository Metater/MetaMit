using System;
using System.Collections.Generic;
using System.Text;

namespace MetaMitPlus.Utils
{
    internal class DataPacker
    {
        public static byte[] PackData(byte[] data, bool hasSessionFlags = false, byte dataSessionFlags = 0x00)
        {
            int dataLength = hasSessionFlags ? -data.Length : data.Length;
            byte[] length = BitConverter.GetBytes(dataLength);
            byte[] sessionFlags = new byte[] { dataSessionFlags };
            byte[] packedData = new byte[length.Length + sessionFlags.Length + data.Length];
            Buffer.BlockCopy(length, 0, packedData, 0, length.Length);
            Buffer.BlockCopy(sessionFlags, 0, packedData, length.Length, sessionFlags.Length);
            Buffer.BlockCopy(data, 0, packedData, length.Length + sessionFlags.Length, data.Length);
            return packedData;
        }
    }
}
