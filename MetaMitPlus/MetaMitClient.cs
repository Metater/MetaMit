using MetaMitPlus.Client;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MetaMitPlus
{
    public class MetaMitClient : IDisposable
    {
        // Possible Ideas:
        // private SessionOptions sessionOptions, maybe not now, just have option, with authoritative server no need
        // Client event for failed to connect
        // Could have multiple, client, no, too much complexity
        // auto reconnect
        // Todo:
        // move send options to seaparte class
        // Make a better builder method maybe struct for start options
        // make constructor set things readonly

        private ServerConnection serverConnection;

        private ConcurrentQueue<ClientEventArgs> queuedEvents = new ConcurrentQueue<ClientEventArgs>();

        public event EventHandler<ConnectedEventArgs> Connected;
        public event EventHandler<EncryptedEventArgs> Encrypted;
        public event EventHandler<DisconnectedEventArgs> Disconnected;
        public event EventHandler<DataReceivedEventArgs> DataReceived;
        public event EventHandler<DataSentEventArgs> DataSent;

        #region Construction
        internal MetaMitClient() {}
        /// <summary>
        /// Creates new instance of MetaMitClient
        /// </summary>
        /// <returns>New instance of MetaMitClient</returns>
        public static MetaMitClient NewClient()
        {
            return new MetaMitClient();
        }
        public static Socket GetSocketFromEndPoint(IPEndPoint ep)
        {
            
        }
        #endregion Construction


        public void Dispose()
        {
            
        }
    }
}
