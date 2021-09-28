using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MetaMitPlus.Utils
{
    internal class DataUnpacker
    {
        internal const int OverheadCarryoverSize = 3;

        private List<byte[]> dataSegments = new List<byte[]>();
        private int unreadData = -1;
        private UnpackedData currentData = new UnpackedData();
        private byte[] overheadCarryover = new byte[OverheadCarryoverSize];
        private int overheadBytes = 0;

        public bool TryUnpackData(int bytesReceived, byte[] data, out List<UnpackedData> unpackedData)
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
            unpackedData = new List<UnpackedData>();

            while (unreadBufferData > 0)
            {
                if (unreadData == -1)
                {
                    if (unreadBufferData > OverheadCarryoverSize)
                    {
                        unreadData = BitConverter.ToInt32(data, bytesReceived - unreadBufferData);
                        unreadBufferData -= 4;
                        if (unreadData < 0)
                        {
                            unreadData = -unreadData;
                            currentData.hasMetadata = true;
                            currentData.metadata = data[bytesReceived - unreadBufferData];
                            unreadBufferData--;
                        }
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

                if (unreadData == 0)
                {
                    currentData.data = CombineSegments();
                    unpackedData.Add(currentData);
                    dataSegments.Clear();
                    unreadData = -1;
                    currentData = new UnpackedData();
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

        public struct UnpackedData
        {
            public bool hasMetadata;
            public byte metadata;
            public byte[] data;
        }
    }
}
