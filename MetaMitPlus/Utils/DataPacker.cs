using System;
using System.Collections.Generic;
using System.Text;

namespace MetaMitPlus.Utils
{
    internal class DataPacker
    {
        public static byte[] PackData(byte[] data)
        {
            byte[] length = BitConverter.GetBytes(data.Length);
            byte[] packedData = new byte[length.Length + data.Length];
            Buffer.BlockCopy(length, 0, packedData, 0, length.Length);
            Buffer.BlockCopy(data, 0, packedData, length.Length, data.Length);
            return packedData;
        }
    }
}
