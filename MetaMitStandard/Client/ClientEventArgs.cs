using System;
using System.Collections.Generic;
using System.Text;

namespace MetaMitStandard.Client
{
    public class ConnectedEventArgs : EventArgs
    {

    }
    public class DisconnectedEventArgs : EventArgs
    {
        public DisconnectedReason reason;
        public string message;

        public DisconnectedEventArgs(DisconnectedReason reason, string message)
        {
            this.reason = reason;
            this.message = message;
        }
    }
    public class DataReceivedEventArgs : EventArgs
    {
        public byte[] data;

        public DataReceivedEventArgs(byte[] data)
        {
            this.data = data;
        }
    }
    public enum DisconnectedReason
    {
        Requested,
        Exception
    }
}
