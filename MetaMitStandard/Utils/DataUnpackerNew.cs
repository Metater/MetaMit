using System;
using System.Collections.Generic;
using System.Text;

namespace MetaMitStandard.Utils
{
    public class DataUnpackerNew
    {
        private List<byte[]> dataSegments = new List<byte[]>();
        private ushort dataLength = 0;
        private ushort sessionFlags = 0;

        public bool TryParseData(int bytesReceived, byte[] data, out List<byte[]> unpackedData, out ushort sessionFlags)
        {
            Console.WriteLine("Received Data: ");
            foreach(byte b in data)
            {
                Console.WriteLine("\t" + b);
            }
            unpackedData = new List<byte[]>();
            sessionFlags = this.sessionFlags;
            return false;
        }
    }
}
