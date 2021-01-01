using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net;
using System.Threading;


namespace MetaMit.Client.Base
{
    public class MetaMitClientBase
    {
        #region Fields
        // Client Fields
        public IPEndPoint ServerEp { get; private set; }
        public bool IsConnected { get; private set; } = false;
        public Socket socket;
        // Events
        public event Action OnServerStart;
        public event Action OnServerStop;

        public event Action OnServerConnectionRejected;

        public event EventHandler<MetaMitClientBaseEventArgs.ServerConnectionPending> OnServerConnectionPending;
        public event EventHandler<MetaMitClientBaseEventArgs.ServerConnectionAccepted> OnServerConnectionAccepted;
        public event EventHandler<MetaMitClientBaseEventArgs.ServerConnectionEnded> OnServerConnectionEnded;
        public event EventHandler<MetaMitClientBaseEventArgs.ServerConnectionLost> OnServerConnectionLost;

        public event EventHandler<MetaMitClientBaseEventArgs.DataReceived> OnDataReceived;
        public event EventHandler<MetaMitClientBaseEventArgs.DataSent> OnDataSent;
        #endregion Fields



        public MetaMitClientBase(IPEndPoint serverEp)
        {
            ServerEp = serverEp;
        }
        public void ConnectToServer()
        {
            socket = new Socket(ServerEp.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.BeginConnect(ServerEp, new AsyncCallback(ConnectCallback), null);

            Task.Run(() =>
            {
                OnServerConnectionPending?.Invoke(this, new MetaMitClientBaseEventArgs.ServerConnectionPending
                {

                });
            });
        }
        public void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                socket.EndConnect(ar);
                IsConnected = true;
                Task.Run(() =>
                {
                    OnServerConnectionAccepted?.Invoke(this, new MetaMitClientBaseEventArgs.ServerConnectionAccepted
                    {
                        
                    });
                });
            }
            catch(SocketException)
            {
                Task.Run(() =>
                {
                    OnServerConnectionLost?.Invoke(this, new MetaMitClientBaseEventArgs.ServerConnectionLost
                    {

                    });
                });
            }
        }
        public void StartReceiveLoop()
        {

        }
    }
}
