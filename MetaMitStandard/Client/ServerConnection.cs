using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using MetaMitStandard.Client;

namespace MetaMitStandard.Client
{
    public class ServerConnection
    {
        public Socket socket;
        private const int BufferSize = 1024;
        private byte[] buffer = new byte[BufferSize];
        private StringBuilder sb = new StringBuilder();
    }
}
