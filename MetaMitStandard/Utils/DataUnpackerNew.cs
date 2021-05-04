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
        private ushort sessionFlags = 0;
        private int unreadData = -1;
        private byte[] overheadCarryover = new byte[4];

        public bool TryUnpackData(int bytesReceived, byte[] data, out List<byte[]> unpackedData, out ushort sessionFlags)
        {
            int unreadBufferData = bytesReceived;
            unpackedData = new List<byte[]>();

            while (unreadBufferData > 0) // Done reading entire buffer
            {
                if (unreadData == -1) // Ready to start reading a new packet
                {
                    dataLength = BitConverter.ToUInt16(data, bytesReceived - unreadBufferData);
                    unreadBufferData -= 2;
                    sessionFlags = BitConverter.ToUInt16(data, bytesReceived - unreadBufferData);
                    unreadBufferData -= 2;

                    unreadData = dataLength;
                }
                else // Continue reading data from a packet
                {

                }
                int readableData = Math.Min(unreadData, unreadBufferData);
                byte[] dataSegment = new byte[readableData];
                Buffer.BlockCopy(data, bytesReceived - unreadBufferData, dataSegment, 0, readableData);
                dataSegments.Add(dataSegment);
                unreadBufferData -= readableData;
                unreadData -= readableData;
                Console.WriteLine(unreadBufferData);
                Console.WriteLine(unreadData);
                if (unreadData == 0) // Done reading a packet
                {
                    unpackedData.Add(CombineSegments());
                    dataSegments.Clear();
                    unreadData = -1;
                }
            }





            if (data.Length != bytesReceived) // The buffer only contains a packet or packets that are not partial
            {

            }
            else // Buffer contains packets and a partial packet, or a partial packet
            {

            }
            sessionFlags = this.sessionFlags;
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
