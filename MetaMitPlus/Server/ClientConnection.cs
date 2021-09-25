using MetaMitPlus.Utils;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace MetaMitPlus.Server
{
    public class ClientConnection : IDisposable
    {
        internal Socket socket;
        internal const int BufferSize = 4096;
        internal byte[] buffer = new byte[BufferSize];
        internal DataUnpacker dataUnpacker = new DataUnpacker();

        private MetaMitServer server;

        public bool isActive = false;

        public long bytesReceived = 0;
        public long bytesSent = 0;
        public long packetsReceived = 0;
        public long packetsSent = 0;

        public Guid guid = Guid.NewGuid();

        internal ClientConnection(MetaMitServer server)
        {
            this.server = server;
        }

        public void Send(byte[] data)
        {
            //server.Send()
            // send stuff through client only
            // make server messages by using negative length of packets to signal that
            // return a list of<(byte[] and bool for if server message included or not)>
        }

        public void Dispose()
        {
            socket.Dispose();
        }
    }
}
