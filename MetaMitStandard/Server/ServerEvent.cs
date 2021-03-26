using System;
using System.Collections.Generic;
using System.Text;

namespace MetaMitStandard.Server
{
    public class ServerEvent
    {
        public ServerEventArgs serverEventArgs;

        public ServerEvent(ServerEventArgs serverEventArgs)
        {
            this.serverEventArgs = serverEventArgs;
        }
    }
}
