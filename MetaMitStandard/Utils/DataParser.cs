using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Linq;

namespace MetaMitStandard.Utils
{
    public class DataParser
    {
        private List<byte[]> dataSegments = new List<byte[]>();
        private ushort dataLength = 0;
        private ushort sessionFlags = 0;
        private int bytesRead = 0;

        public bool TryBuildData(int receivedDataLength, byte[] data, out byte[] builtData)
        {
            if (bytesRead == 0)
            {
                dataLength = BitConverter.ToUInt16(data, 0);
                sessionFlags = BitConverter.ToUInt16(data, 2);
            }

            bytesRead += receivedDataLength;

            bool allDataRead = bytesRead >= dataLength;

            if (allDataRead)
            {
                byte[] trimmedData = new byte[receivedDataLength-4];
                Console.WriteLine("Trimming" + trimmedData.Length);
                Buffer.BlockCopy(data, 0, trimmedData, 0, bytesRead - dataLength);
                dataSegments.Add(trimmedData);
                Console.WriteLine("Trimmed Data: " + trimmedData.Length);
                builtData = GetData();
                Console.WriteLine("Data Length: " + builtData.Length);
            }
            else
            {
                dataSegments.Add(data);
                builtData = null;
            }

            return allDataRead;
        }

        public bool GetSessionFlag(SessionFlag sessionFlag)
        {
            return GetBit(sessionFlags, (int)sessionFlag);
        }

        public static byte[] CombineArrays(byte[][] arrays)
        {
            byte[] rv = new byte[arrays.Sum(a => a.Length)];
            int offset = 0;
            foreach (byte[] array in arrays)
            {
                Buffer.BlockCopy(array, 0, rv, offset, array.Length);
                offset += array.Length;
            }
            return rv;
        }

        private bool GetBit(ushort sessionFlags, int index)
        {
            return ((sessionFlags >> index) & 1) != 0;
        }

        private byte[] GetData()
        {
            byte[][] arrays = dataSegments.ToArray();
            byte[] rv = new byte[arrays.Sum(a => a.Length)];
            int offset = 0;
            foreach (byte[] array in arrays)
            {
                Buffer.BlockCopy(array, 0, rv, offset, array.Length);
                offset += array.Length;
            }
            Reset();
            return rv.Skip(4).ToArray();
        }

        private void Reset()
        {
            dataSegments.Clear();
            bytesRead = 0;
            dataLength = 0;
        }
    }

    public enum SessionFlag
    {
        RequestDisconnect,
        OkayDisconnect
    }
}
