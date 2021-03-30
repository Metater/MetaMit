using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Linq;

namespace MetaMitStandard.Utils
{
    public class DataBuilder
    {
        private List<byte[]> dataSegments = new List<byte[]>();
        private ushort dataLength = 0;
        private int bytesRead = 0;

        public bool TryBuildData(byte[] data, out byte[] builtData)
        {
            if (bytesRead == 0)
            {
                dataLength = BitConverter.ToUInt16(data, 0);
            }

            bytesRead += data.Length;

            bool allDataRead = bytesRead >= dataLength;

            if (allDataRead)
            {
                byte[] trimmedData = new byte[bytesRead - dataLength];
                Buffer.BlockCopy(data, 0, trimmedData, 0, bytesRead - dataLength);
                dataSegments.Add(trimmedData);
                builtData = GetData();
            }
            else
            {
                dataSegments.Add(data);
                builtData = null;
            }

            return allDataRead;
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
            return rv;
        }

        private void Reset()
        {
            dataSegments.Clear();
            bytesRead = 0;
            dataLength = 0;
        }
    }
}
