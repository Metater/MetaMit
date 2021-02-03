using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace MetaMitUnity
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
