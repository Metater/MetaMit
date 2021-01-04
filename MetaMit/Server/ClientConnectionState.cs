using System;
using System.Collections.Generic;
using System.Text;

namespace MetaMit.Server
{
    public enum ClientConnectionState
    {
        Connected,
        ClientGaveRSAPublicKey, // If the client doesnt give after a certain amount of time, they will be kicked
        ServerGaveAESKey,
        ConnectedAndEncrypted
    }
}
