using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace MetaMit.Server.Base
{
    public class MetaMitServerBaseEventArgs
    {
        public class ConnectionPending : EventArgs
        {
            public Socket socket;
        }
        public class ConnectionAccepted : EventArgs
        {
            public ClientConnection connection;
            public int clientCount;
        }
        public class ConnectionEnded : EventArgs
        {
            public Guid client;
        }
        public class ConnectionLost : EventArgs
        {
            public Guid client;
        }
        public class DataReceived : EventArgs
        {
            public ClientConnection connection;
            public string data;
        }
        public class DataSent : EventArgs
        {
            public ClientConnection connection;
            public int bytesSent;
        }
    }
}
