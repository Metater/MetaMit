using System;
using System.Collections.Generic;
using System.Text;

namespace MetaMitPlus.Client
{
    public abstract class ClientEventArgs : EventArgs
    {
        internal ClientEventType eventType;
    }

    internal enum ClientEventType
    {
        Connected,
        Encrypted,
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
    public class EncryptedEventArgs : ClientEventArgs
    {
        public EncryptedEventArgs()
        {
            eventType = ClientEventType.Encrypted;
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
        Exception
    }
}
