using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

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
        public EndPoint ep;

        public ClientConnectedEventArgs(Guid guid, EndPoint ep)
        {
            eventType = ServerEventType.ClientConnected;
            this.guid = guid;
            this.ep = ep;
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
        public ClientConnection clientConnection;
        public byte[] data;

        public DataReceivedEventArgs(ClientConnection clientConnection, byte[] data)
        {
            eventType = ServerEventType.DataReceived;
            this.clientConnection = clientConnection;
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
