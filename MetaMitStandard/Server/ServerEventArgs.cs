using System;
using System.Collections.Generic;
using System.Text;

namespace MetaMitStandard.Server
{
    public abstract class ServerEventArgs : EventArgs
    {
        public ServerEventType eventType;
    }
    public enum ServerEventType
    {
        ClientConnected,
        ClientDisconnected,
        DataReceived,
        ServerStarted,
        ServerStopped
    }
    public class ClientConnectedEventArgs : ServerEventArgs
    {
        public Guid client;

        public ClientConnectedEventArgs(Guid client)
        {
            eventType = ServerEventType.ClientConnected;
            this.client = client;
        }
    }
    public class ClientDisconnectedEventArgs : ServerEventArgs
    {
        public Guid client;

        public ClientDisconnectedEventArgs(Guid client)
        {
            eventType = ServerEventType.ClientDisconnected;
            this.client = client;
        }
    }
    public class DataReceivedEventArgs : ServerEventArgs
    {
        public Guid client;
        public byte[] data;

        public DataReceivedEventArgs(Guid client, byte[] data)
        {
            eventType = ServerEventType.DataReceived;
            this.client = client;
            this.data = data;
        }
    }
    public class ServerStartedEventArgs : ServerEventArgs
    {
        public ServerStartedEventArgs()
        {
            eventType = ServerEventType.ServerStarted;
        }
    }
    public class ServerStoppedEventArgs : ServerEventArgs
    {
        public ServerStoppedReason reason;
        public string message;

        public ServerStoppedEventArgs(ServerStoppedReason reason, string message)
        {
            eventType = ServerEventType.ServerStopped;
            this.reason = reason;
            this.message = message;
        }
    }
    public enum ServerStoppedReason
    {
        Commanded,
        Exception
    }
}
