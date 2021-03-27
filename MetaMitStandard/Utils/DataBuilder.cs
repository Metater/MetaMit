using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Linq;

namespace MetaMitStandard.Utils
{
    public class DataBuilder
    {
        protected List<byte[]> dataSegments = new List<byte[]>();

        public void AddData(byte[] data)
        {
            dataSegments.Add(data);
        }

        public byte[] GetData()
        {
            byte[][] arrays = dataSegments.ToArray();
            byte[] rv = new byte[arrays.Sum(a => a.Length)];
            int offset = 0;
            foreach (byte[] array in arrays)
            {
                Buffer.BlockCopy(array, 0, rv, offset, array.Length);
                offset += array.Length;
            }
            return rv;
        }
    }
}
