using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using MetaMitStandard.Utils;

namespace MetaMitStandard.Server
{
    public class ClientConnection
    {
        public Socket socket;
        public byte[] buffer;

        public long bytesReceived = 0;
        public const int BufferSize = 4096;

        public DataBuilder dataBuilder;

        public Guid client;

        public ClientConnection()
        {
            buffer = new byte[BufferSize];
            dataBuilder = new DataBuilder();
            client = Guid.NewGuid();
        }
    }
}
