using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace MetaMit.Server.Base
{
    public class MetaMitServerBaseEventArgs
    {
        public class ConnectionPending
        {
            public Socket socket;
        }
        public class ConnectionAccepted
        {
            public ClientConnection connection;
            public int clientCount;
        }
        public class ConnectionEnded
        {
            public Guid client;
        }
        public class ConnectionLost
        {
            public Guid client;
        }
        public class DataReceived
        {
            public ClientConnection connection;
            public string data;
        }
        public class DataSent
        {
            public ClientConnection connection;
            public int bytesSent;
        }
        /*
        public class LoopbackReceive
        {
            public ClientConnection connection;
        }
        */
    }
}
