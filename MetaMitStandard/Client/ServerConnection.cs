using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using MetaMitStandard.Utils;

namespace MetaMitStandard.Client
{
    public class ServerConnection : IDisposable
    {
        public Socket socket;
        public byte[] buffer = new byte[BufferSize];

        public bool isActive = false;

        public long bytesReceived = 0;
        public long bytesSent = 0;
        public long packetsReceived = 0;
        public long packetsSent = 0;

        public const int BufferSize = 4096;

        public DataUnpacker dataUnpacker = new DataUnpacker();

        public ServerConnection()
        {

        }

        public void Dispose()
        {
            socket.Dispose();
        }
    }
}
