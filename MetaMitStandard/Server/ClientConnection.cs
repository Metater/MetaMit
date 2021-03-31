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
        public byte[] buffer = new byte[BufferSize];

        public long bytesReceived = 0;
        public const int BufferSize = 4096;

        public DataParser dataParser = new DataParser();

        public Guid guid = Guid.NewGuid();

        public ClientConnection()
        {

        }
    }
}
