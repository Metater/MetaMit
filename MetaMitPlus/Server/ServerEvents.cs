using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace MetaMitPlus.Server
{
    public abstract class ServerEventArgs : EventArgs
    {
        internal ServerEventType eventType;
    }

    internal enum ServerEventType
    {
        ClientConnected,
        ClientEncrypted,
        ClientDisconnected,
        DataReceived,
        DataSent,
        ServerStarted,
        ServerStopped
    }

    public class ClientConnectedEventArgs : ServerEventArgs
    {
        public ClientConnection clientConnection;

        public ClientConnectedEventArgs(ClientConnection clientConnection)
        {
            eventType = ServerEventType.ClientConnected;
            this.clientConnection = clientConnection;
        }
    }
    public class ClientEncryptedEventArgs : ServerEventArgs
    {
        public ClientConnection clientConnection;

        public ClientEncryptedEventArgs(ClientConnection clientConnection)
        {
            eventType = ServerEventType.ClientEncrypted;
            this.clientConnection = clientConnection;
        }
    }
    public class ClientDisconnectedEventArgs : ServerEventArgs
    {
        public ClientConnection clientConnection;
        public ClientDisconnectedReason reason;
        public string message;

        public ClientDisconnectedEventArgs(ClientConnection clientConnection, ClientDisconnectedReason reason, string message)
        {
            eventType = ServerEventType.ClientDisconnected;
            this.clientConnection = clientConnection;
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
    public class DataSentEventArgs : ServerEventArgs
    {
        public ClientConnection clientConnection;
        public int bytesSent;

        public DataSentEventArgs(ClientConnection clientConnection, int bytesSent)
        {
            eventType = ServerEventType.DataSent;
            this.clientConnection = clientConnection;
            this.bytesSent = bytesSent;
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
        EncryptionTimeout,
        ReceivePolicyViolation,
        ProtocolViolation,
        Exception
    }

    public enum ServerStoppedReason
    {
        Requested,
        Exception
    }
}
