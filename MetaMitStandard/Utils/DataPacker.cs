using System;
using System.Collections.Generic;
using System.Text;

namespace MetaMitStandard.Utils
{
    public class DataPacker
    {
        public static byte[] PackData(byte[] data, ushort dataSessionFlags)
        {
            byte[] length = BitConverter.GetBytes((ushort)data.Length);
            byte[] sessionFlags = BitConverter.GetBytes(dataSessionFlags);
            byte[] packedData = new byte[length.Length + sessionFlags.Length + data.Length];
            Buffer.BlockCopy(length, 0, packedData, 0, length.Length);
            Buffer.BlockCopy(sessionFlags, 0, packedData, length.Length, sessionFlags.Length);
            Buffer.BlockCopy(data, 0, packedData, length.Length + sessionFlags.Length, data.Length);
            return packedData;
        }
    }
}
