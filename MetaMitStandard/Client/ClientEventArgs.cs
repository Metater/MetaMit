using System;
using System.Collections.Generic;
using System.Text;

namespace MetaMitStandard.Client
{
    public abstract class ClientEventArgs : EventArgs
    {
        public ClientEventType eventType;
    }
    public enum ClientEventType
    {
        Connected,
        Disconnected,
        DataReceived,
        DataSent
    }
    public class ConnectedEventArgs : ClientEventArgs
    {
        public ConnectedEventArgs()
        {
            eventType = ClientEventType.Connected;
        }
    }
    public class DisconnectedEventArgs : ClientEventArgs
    {
        public DisconnectedReason reason;
        public string message;

        public DisconnectedEventArgs(DisconnectedReason reason, string message)
        {
            eventType = ClientEventType.Disconnected;
            this.reason = reason;
            this.message = message;
        }
    }
    public class DataReceivedEventArgs : ClientEventArgs
    {
        public byte[] data;

        public DataReceivedEventArgs(byte[] data)
        {
            eventType = ClientEventType.DataReceived;
            this.data = data;
        }
    }
    public class DataSentEventArgs : ClientEventArgs
    {
        public int bytesSent;

        public DataSentEventArgs(int bytesSent)
        {
            eventType = ClientEventType.DataSent;
            this.bytesSent = bytesSent;
        }
    }
    public enum DisconnectedReason
    {
        Requested,
        ExceptionOnConnect,
        ExceptionOnReceive,
        ExceptionOnSend,
        ExceptionOnDisconnect
    }
}
