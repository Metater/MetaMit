﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using MetaMitStandard.Utils;

namespace MetaMitStandard.Server
{
    public class ClientConnection : IDisposable
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

        public Guid guid = Guid.NewGuid();

        public ClientConnection()
        {

        }

        public void Dispose()
        {
            socket.Dispose();
        }
    }
}
