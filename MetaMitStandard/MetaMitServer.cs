using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using MetaMitStandard.Server;

namespace MetaMitStandard
{
    public sealed class MetaMitServer : IDisposable
    {
        private IPEndPoint ep;
        private int backlog;
        private Socket listener;
        private bool serverClosed = true;

        private List<ClientConnection> connections = new List<ClientConnection>();
        private ConcurrentQueue<ServerEvent> eventQueue = new ConcurrentQueue<ServerEvent>();

        public event EventHandler<ClientConnectedEventArgs> ClientConnected;
        public event EventHandler<ClientDisconnectedEventArgs> ClientDisconnected;
        public event EventHandler<DataReceivedEventArgs> DataReceived;
        public event EventHandler<ServerStartedEventArgs> ServerStarted;
        public event EventHandler<ServerStoppedEventArgs> ServerStopped;

        public MetaMitServer(int port, int backlog)
        {
            this.backlog = backlog;
            ep = Utils.NetworkUtils.GetEndPoint(Utils.NetworkUtils.GetLocalIPv4(), port);
            listener = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
            listener.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
        }

        public void Start()
        {
            try
            {
                listener.Bind(ep);
                listener.Listen(backlog);
                serverClosed = false;
                listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);
            }
            catch (Exception e)
            {
                QueueEvent(new ServerStoppedEventArgs(ServerStoppedReason.Exception, e.ToString()));
            }
        }

        public void Stop()
        {
            serverClosed = true;
            listener.Close();
        }

        public void PollEvents()
        {
            int queuedEventsCount = eventQueue.Count;
            for (int i = 0; i < queuedEventsCount; i++)
            {
                if (eventQueue.TryDequeue(out ServerEvent serverEvent))
                    ProcessQueuedEvent(serverEvent);
                else
                    break;
            }
        }

        public void Dispose()
        {
            listener.Dispose();
        }

        #region Events
        private void QueueEvent(ServerEventArgs serverEventArgs)
        {
            eventQueue.Enqueue(new ServerEvent(serverEventArgs));
        }

        private void ProcessQueuedEvent(ServerEvent serverEvent)
        {
            switch (serverEvent.serverEventArgs.eventType)
            {
                case ServerEventType.ClientConnected:
                    ClientConnected?.Invoke(this, (ClientConnectedEventArgs)serverEvent.serverEventArgs);
                    break;
                case ServerEventType.ClientDisconnected:
                    ClientDisconnected?.Invoke(this, (ClientDisconnectedEventArgs)serverEvent.serverEventArgs);
                    break;
                case ServerEventType.DataReceived:
                    DataReceived?.Invoke(this, (DataReceivedEventArgs)serverEvent.serverEventArgs);
                    break;
                case ServerEventType.ServerStarted:
                    ServerStarted?.Invoke(this, (ServerStartedEventArgs)serverEvent.serverEventArgs);
                    break;
                case ServerEventType.ServerStopped:
                    ServerStopped?.Invoke(this, (ServerStoppedEventArgs)serverEvent.serverEventArgs);
                    break;
            }
        }
        #endregion Events


        #region Callbacks
        private void AcceptCallback(IAsyncResult ar)
        {
            ClientConnection clientConnection = new ClientConnection();
            try
            {
                Socket listenerAr = (Socket)ar.AsyncState;
                clientConnection.socket = listenerAr.EndAccept(ar);
                lock (connections)
                {
                    connections.Add(clientConnection);
                }
                clientConnection.socket.BeginReceive(clientConnection.buffer, 0, clientConnection.buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), clientConnection);
                listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);
            }
            catch (SocketException e)
            {
                ForceDisconnectClient(clientConnection);
                listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);
            }
            catch (Exception e)
            {
                ForceDisconnectClient(clientConnection);
                listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);
            }
        }
        private void ReceiveCallback(IAsyncResult ar)
        {
            ClientConnection clientConnection = (ClientConnection)ar.AsyncState;
            try
            {
                int bytesRead = clientConnection.socket.EndReceive(ar);
                if (bytesRead > 0)
                {
                    if (clientConnection.dataBuilder.TryBuildData(clientConnection.buffer, out byte[] data))
                    {
                        QueueEvent(new DataReceivedEventArgs(clientConnection.client, data));
                    }
                    clientConnection.socket.BeginReceive(clientConnection.buffer, 0, clientConnection.buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), clientConnection);
                }
                else
                {
                    ForceDisconnectClient(clientConnection);
                }
            }
            catch (SocketException e)
            {
                ForceDisconnectClient(clientConnection);
            }
        }
        #endregion Callbacks

        private void ForceDisconnectClient(ClientConnection clientConnection)
        {
            if (clientConnection != null)
            {
                clientConnection.socket.Close();
                lock (connections)
                {
                    connections.Remove(clientConnection);
                }
            }
        }


    }
}
