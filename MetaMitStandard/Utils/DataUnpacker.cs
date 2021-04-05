using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Linq;

namespace MetaMitStandard.Utils
{
    public class DataUnpacker
    {
        private List<byte[]> packetSegments = new List<byte[]>();
        private int packetSegmentsCount => packetSegments.Count;
        private ushort packetLength = 0;
        private ushort packetSessionFlags = 0;
        private int packetBytesReceived = 0;

        private const int OverheadBytes = 4;
        private const int SessionFlagsCount = 2;

        public event Action<List<SessionFlag>> SessionFlagsFound;

        public bool TryParseData(int bytesReceived, byte[] data, out List<byte[]> parsedData)
        {
            parsedData = new List<byte[]>();
            bool allDataParsed = false;
            int dataStartIndex = 0;
            while (!allDataParsed)
            {
                bool isNewPacket = packetSegmentsCount == 0;
                if (isNewPacket) // New packet
                {
                    packetLength = BitConverter.ToUInt16(data, dataStartIndex);
                    packetSessionFlags = BitConverter.ToUInt16(data, dataStartIndex + 2);
                    if (packetSessionFlags > 0)
                    {
                        List<SessionFlag> foundSessionFlags = new List<SessionFlag>();
                        for (int i = 0; i < SessionFlagsCount; i++)
                        {
                            if (GetBit(packetSessionFlags, i))
                            {
                                SessionFlag foundSessionFlag = (SessionFlag)i;
                            }
                        }
                        SessionFlagsFound?.Invoke(foundSessionFlags);
                    }
                    int accessibleDataCount = Math.Min(packetLength, (data.Length - dataStartIndex) - OverheadBytes);
                    byte[] accessibleData = new byte[accessibleDataCount];
                    Buffer.BlockCopy(data, dataStartIndex + OverheadBytes, accessibleData, 0, accessibleDataCount);
                    packetBytesReceived += accessibleDataCount;
                    packetSegments.Add(accessibleData);
                    dataStartIndex += packetBytesReceived + OverheadBytes;
                }
                else // Old packet
                {
                    int accessibleDataCount = Math.Min(packetLength - (data.Length * packetSegmentsCount), data.Length - dataStartIndex);
                    byte[] accessibleData = new byte[accessibleDataCount];
                    Buffer.BlockCopy(data, dataStartIndex, accessibleData, 0, accessibleDataCount);
                    packetBytesReceived += accessibleDataCount;
                    packetSegments.Add(accessibleData);
                    dataStartIndex += packetBytesReceived;
                }
                if ((packetBytesReceived == packetLength && isNewPacket) || (packetBytesReceived + OverheadBytes == packetLength && !isNewPacket))
                {   
                    parsedData.Add(CombineSegments());
                }
                if (dataStartIndex >= bytesReceived)
                {
                    allDataParsed = true;
                }
            }
            return parsedData.Count != 0;
        }

        public bool GetSessionFlag(SessionFlag sessionFlag)
        {
            return GetBit(packetSessionFlags, (int)sessionFlag);
        }

        private bool GetBit(ushort sessionFlags, int index)
        {
            return ((sessionFlags >> index) & 1) != 0;
        }

        private byte[] CombineSegments()
        {
            byte[][] arrays = packetSegments.ToArray();
            byte[] combinedArray = new byte[arrays.Sum(a => a.Length)];
            int offset = 0;
            foreach (byte[] array in arrays)
            {
                Buffer.BlockCopy(array, 0, combinedArray, offset, array.Length);
                offset += array.Length;
            }
            Reset();
            return combinedArray.ToArray();
        }

        private void Reset()
        {
            packetSegments.Clear();
            packetBytesReceived = 0;
            packetLength = 0;
        }
    }

    public enum SessionFlag
    {
        RequestDisconnect,
        OkayDisconnect
    }
}
