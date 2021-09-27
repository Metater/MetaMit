using System;
using System.Collections.Generic;
using System.Text;

namespace MetaMitPlus
{
    public class MetaMitClient : IDisposable
    {
        //private SessionOptions sessionOptions, maybe not now, just have option, with authoritative server no need
        // move send options to seaparte class
        // Client event for failed to connect
        // Could have multiple, client, no, too much complexity


        public void Dispose()
        {
            
        }
    }
}
