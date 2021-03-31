using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using MetaMitStandard.Utils;

namespace MetaMitStandard.Client
{
    public class ServerConnection
    {
        public Socket socket;
        public byte[] buffer = new byte[BufferSize];

        public long bytesReceived = 0;
        public long bytesSent = 0;
        public const int BufferSize = 4096;

        public DataParser dataParser = new DataParser();

        public ServerConnection()
        {

        }
    }
}
