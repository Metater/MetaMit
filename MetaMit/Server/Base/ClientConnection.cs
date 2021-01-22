using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Net.Sockets;

namespace MetaMit.Server.Base
{
    public class ClientConnection
    {
        public const int BufferSize = 1024;
        public byte[] buffer = new byte[BufferSize];
        public StringBuilder sb = new StringBuilder();
        public Socket socket = null;

        public ClientConnectionState state = ClientConnectionState.Connected;
        public DateTime creationTime;
        public Guid guid;

        public string clientRSAPublicKey;
        public string sessionAESKey;

        public bool IsClosed { get; private set; } = false;

        public ClientConnection() { }
        public ClientConnection(Guid guid, Socket socket) { this.guid = guid; this.socket = socket; }

        public void WipeBuffer()
        {
            buffer = new byte[BufferSize];
            sb = new StringBuilder();
        }
        public void Close()
        {
            IsClosed = true;

            socket.Dispose();
        }
    }
}
