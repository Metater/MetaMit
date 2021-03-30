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
        public Guid guid;

        public ClientConnectedEventArgs(Guid guid)
        {
            eventType = ServerEventType.ClientConnected;
            this.guid = guid;
        }
    }
    public class ClientDisconnectedEventArgs : ServerEventArgs
    {
        public Guid guid;
        public ClientDisconnectedReason reason;
        public string message;

        public ClientDisconnectedEventArgs(Guid guid, ClientDisconnectedReason reason, string message)
        {
            eventType = ServerEventType.ClientDisconnected;
            this.guid = guid;
            this.reason = reason;
            this.message = message;
        }
    }
    public class DataReceivedEventArgs : ServerEventArgs
    {
        public Guid guid;
        public byte[] data;

        public DataReceivedEventArgs(Guid guid, byte[] data)
        {
            eventType = ServerEventType.DataReceived;
            this.guid = guid;
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
    public enum ClientDisconnectedReason
    {
        Requested,
        ExceptionOnAccept,
        ExceptionOnReceive,
        ExceptionOnSend,
        ExceptionOnDisconnect
    }
    public enum ServerStoppedReason
    {
        Requested,
        Exception
    }
}
