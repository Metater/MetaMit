using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace MetaMitStandard.Utils
{
    public class DataUnpackerNew
    {
        private List<byte[]> dataSegments = new List<byte[]>();
        private ushort dataLength = 0;
        private int unreadData = -1;
        private byte[] overheadCarryover = new byte[3];
        private int overheadBytes = 0;

        public bool TryUnpackData(int bytesReceived, byte[] data, out List<byte[]> unpackedData, out List<ushort> sessionFlags)
        {
            if (overheadBytes > 0)
            {
                byte[] concatData = new byte[data.Length + overheadBytes];
                Buffer.BlockCopy(overheadCarryover, 0, concatData, 0, overheadBytes);
                Buffer.BlockCopy(data, 0, concatData, overheadBytes, data.Length);
                data = concatData;
                bytesReceived += overheadBytes;
                overheadBytes = 0;
            }

            int unreadBufferData = bytesReceived;
            unpackedData = new List<byte[]>();
            sessionFlags = new List<ushort>();

            while (unreadBufferData > 0) // Done reading entire buffer
            {
                if (unreadData == -1) // Ready to start reading a new packet
                {
                    if (unreadBufferData > 3)
                    {
                        dataLength = BitConverter.ToUInt16(data, bytesReceived - unreadBufferData);
                        unreadBufferData -= 2;
                        ushort dataSessionFlags = BitConverter.ToUInt16(data, bytesReceived - unreadBufferData);
                        unreadBufferData -= 2;
                        if (dataSessionFlags > 0) sessionFlags.Add(dataSessionFlags);

                        unreadData = dataLength;
                    }
                    else
                    {
                        overheadBytes = unreadBufferData;
                        Buffer.BlockCopy(data, bytesReceived - unreadBufferData, overheadCarryover, 0, overheadBytes);
                        break;
                    }
                }

                int readableData = Math.Min(unreadData, unreadBufferData);
                byte[] dataSegment = new byte[readableData];
                Buffer.BlockCopy(data, bytesReceived - unreadBufferData, dataSegment, 0, readableData);
                dataSegments.Add(dataSegment);
                unreadBufferData -= readableData;
                unreadData -= readableData;

                if (unreadData == 0) // Done reading a packet
                {
                    unpackedData.Add(CombineSegments());
                    dataSegments.Clear();
                    unreadData = -1;
                }
            }

            return unpackedData.Count != 0;
        }

        private byte[] CombineSegments()
        {
            byte[][] arrays = dataSegments.ToArray();
            byte[] combinedArray = new byte[arrays.Sum(a => a.Length)];
            int offset = 0;
            foreach (byte[] array in arrays)
            {
                Buffer.BlockCopy(array, 0, combinedArray, offset, array.Length);
                offset += array.Length;
            }
            return combinedArray.ToArray();
        }
    }
}
