using System;
using System.Net;
using System.Net.Sockets;

namespace MetaMitPlus
{
    public sealed class MetaMitServer : IDisposable
    {
        public IPEndPoint LocalEP { get; private set; }
        public bool Listening { get; private set; } = false;

        private int backlog;
        private Socket listener;

        public void Dispose()
        {

        }
    }
}
