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

        public Socket socket;
        // Events
        public event Action OnServerStartEvent;
        public event Action OnServerStopEvent;

        public event EventHandler<MetaMitClientBaseEventArgs.ServerConnectionPending> OnServerConnectionPendingEvent;
        public event EventHandler<MetaMitClientBaseEventArgs.ServerConnectionAccepted> OnServerConnectionAcceptedEvent;
        public event EventHandler<MetaMitClientBaseEventArgs.ServerConnectionEnded> OnServerConnectionEndedEvent;
        public event EventHandler<MetaMitClientBaseEventArgs.ServerConnectionLost> OnServerConnectionLostEvent;

        public event EventHandler<MetaMitClientBaseEventArgs.DataReceived> OnDataReceivedEvent;
        public event EventHandler<MetaMitClientBaseEventArgs.DataSent> OnDataSentEvent;
        #endregion Fields



        public MetaMitClientBase(IPEndPoint serverEp)
        {
            ServerEp = serverEp;
        }
        public void ConnectToServer()
        {
            socket = new Socket(ServerEp.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.BeginConnect(ServerEp)
        }
        public void ConnectCallback(IAsyncResult ar)
        {

        }
        public void StartReceiveLoop()
        {

        }
    }
}
