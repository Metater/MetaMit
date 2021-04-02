using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Linq;

namespace MetaMitStandard.Utils
{
    public class DataUnpacker
    {
        private List<byte[]> dataSegments = new List<byte[]>();
        private ushort dataLength = 0;
        private ushort sessionFlags = 0;
        private int bytesRead = 0;
        private const int OverheadBytes = 4;

        public event Action<ushort> SessionFlagsFound;

        public bool TryParseData(int bytesReceived, byte[] data, out byte[] builtData)
        {
            if (bytesRead == 0)
            {
                dataLength = BitConverter.ToUInt16(data, 0);
                sessionFlags = BitConverter.ToUInt16(data, 2);
                if (sessionFlags > 0)
                {
                    SessionFlagsFound?.Invoke(sessionFlags);
                }
            }

            bytesRead += data.Length;

            bool allDataRead = bytesRead >= dataLength;

            if (allDataRead)
            {
                bool needsToBeTrimmed = data.Length * (dataSegments.Count + 1) != dataLength;
                if (needsToBeTrimmed)
                {
                    int overheadOffset = 0;
                    if (bytesRead == data.Length) overheadOffset = OverheadBytes;
                    int howMuchDataRemains = (dataLength - (data.Length * dataSegments.Count)) + overheadOffset;
                    byte[] trimmedData = new byte[howMuchDataRemains];
                    Buffer.BlockCopy(data, 0, trimmedData, 0, howMuchDataRemains);
                    dataSegments.Add(trimmedData);
                    foreach (byte b in trimmedData)
                    {
                        Console.WriteLine(b);
                    }
                }
                else
                {
                    dataSegments.Add(data);
                }
                builtData = GetData();
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

        private bool GetBit(ushort sessionFlags, int index)
        {
            return ((sessionFlags >> index) & 1) != 0;
        }

        private byte[] GetData()
        {
            byte[][] arrays = dataSegments.ToArray();
            byte[] combinedArray = new byte[arrays.Sum(a => a.Length)];
            int offset = 0;
            foreach (byte[] array in arrays)
            {
                Buffer.BlockCopy(array, 0, combinedArray, offset, array.Length);
                offset += array.Length;
            }
            Reset();
            return combinedArray.Skip(OverheadBytes).ToArray();
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
