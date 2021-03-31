using System;
using System.Collections.Generic;
using System.Text;
using MetaMitStandard.Client;

namespace MetaMitStandard.Client
{
    public class ClientEvent
    {
        public ClientEventArgs clientEventArgs;

        public ClientEvent(ClientEventArgs clientEventArgs)
        {
            this.clientEventArgs = clientEventArgs;
        }
    }
}
