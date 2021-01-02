using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace MetaMit.Client.Base
{
    public class MetaMitClientBaseEventArgs
    {
        public class ServerConnectionPending
        {

        }
        public class ServerConnectionAccepted
        {

        }
        public class ServerConnectionFailed
        {

        }
        public class ServerConnectionEnded
        {

        }
        public class ServerConnectionLost
        {

        }
        public class DataReceived
        {
            public Socket socket;
            public string data;
        }
        public class DataSent
        {
            public int bytesSent;
        }
    }
}
