using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace MetaMit.Client.Base
{
    public class MetaMitClientBaseEventArgs
    {
        public class ServerConnectionPending : EventArgs
        {

        }
        public class ServerConnectionAccepted : EventArgs
        {

        }
        public class ServerConnectionFailed : EventArgs
        {

        }
        public class ServerConnectionEnded : EventArgs
        {

        }
        public class ServerConnectionLost : EventArgs
        {

        }
        public class DataReceived : EventArgs
        {
            public Socket socket;
            public string data;
        }
        public class DataSent : EventArgs
        {
            public int bytesSent;
        }
    }
}
