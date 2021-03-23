using System;
using System.Collections.Generic;
using System.Text;

namespace MetaMitStandard
{
    public class ClientConnectedEventArgs : EventArgs
    {
        public Guid client;
    }
    public class ClientDisconnectedEventArgs : EventArgs
    {
        public Guid client;
    }
    public class DataReceivedEventArgs : EventArgs
    {
        public Guid client;
        public byte[] data;
    }
    public class ServerStoppedEventArgs : EventArgs
    {
        public ServerStoppedReason reason;
        public string message;
    }
    public enum ServerStoppedReason
    {
        Commanded,
        Crashed
    }
}
