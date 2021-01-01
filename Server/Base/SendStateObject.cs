using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace MetaMit.Server.Base
{
    public class SendStateObject
    {
        public ClientConnection connection;
        public ManualResetEvent sendComplete = new ManualResetEvent(false);
        public SendStateObject(ClientConnection connection)
        {
            this.connection = connection;
        }
    }
}
