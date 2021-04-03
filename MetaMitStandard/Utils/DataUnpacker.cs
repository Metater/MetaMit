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
        private ushort packetLength = 0;
        private ushort packetSessionFlags = 0;
        private int packetBytesReceived = 0;
        private const int OverheadBytes = 4;

        public event Action<ushort> SessionFlagsFound;

        public bool TryParseData(int bytesReceived, byte[] data, out byte[] builtData)
        {
            packetBytesReceived += bytesReceived;
            if (packetBytesReceived == bytesReceived) // Is first time?
            {
                packetLength = BitConverter.ToUInt16(data, 0);
                packetSessionFlags = BitConverter.ToUInt16(data, 2);
                if (packetSessionFlags > 0) // Any session flags?
                {
                    SessionFlagsFound?.Invoke(packetSessionFlags);
                }
                if (packetBytesReceived >= packetLength) // Is all data received?
                {
                    int bytesToKeep = packetLength + OverheadBytes;
                }
            }
            else // Is not first time?
            {

            }
        }

        public bool GetSessionFlag(SessionFlag sessionFlag)
        {
            return GetBit(packetSessionFlags, (int)sessionFlag);
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
