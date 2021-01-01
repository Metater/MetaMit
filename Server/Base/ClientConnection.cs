using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Net.Sockets;

namespace MetaMit.Server.Base
{
    public class ClientConnection : IDisposable
    {
        public const int BufferSize = 1024;
        public byte[] buffer = new byte[BufferSize];
        public StringBuilder sb = new StringBuilder();
        public Socket socket = null;
        public Guid guid;
        public bool IsDisposed { get; private set; } = false;
        public void Dispose()
        {
            IsDisposed = true;
            buffer = null;
            sb = null;
            socket.Dispose();
            socket = null;
        }
    }
}
