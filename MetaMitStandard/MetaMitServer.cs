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
        public event EventHandler<ClientDisconnectedEventArgs> ClientDisconnected; // Currently, the client disconnected event could go off multiple times per disconnect
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

        public void Send(ClientConnection clientConnection, byte[] data)
        {
            clientConnection.socket.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(SendCallback), clientConnection);
        }
        public void Send(Guid guid, byte[] data) // May want to keep a map of guids to client connections somewhere, this locks connections
        {
            if (TryGetClientConnection(guid, out ClientConnection clientConnection))
            {
                Send(clientConnection, data);
            }
        }

        public void Disconnect(ClientConnection clientConnection)
        {
            clientConnection.socket.BeginDisconnect(false, new AsyncCallback(DisconnectCallback), clientConnection);
        }
        public void Disconnect(Guid guid)
        {
            if (TryGetClientConnection(guid, out ClientConnection clientConnection))
            {
                Disconnect(clientConnection);
            }
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
            if (serverClosed)
            {
                QueueEvent(new ServerStoppedEventArgs(ServerStoppedReason.Requested, "The server has been stopped"));
                return;
            }

            ClientConnection clientConnection = new ClientConnection();
            try
            {
                clientConnection.socket = listener.EndAccept(ar);
                lock (connections)
                {
                    connections.Add(clientConnection);
                }
                clientConnection.socket.BeginReceive(clientConnection.buffer, 0, clientConnection.buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), clientConnection);
            }
            catch (SocketException e)
            {
                ForceDisconnectClient(clientConnection, ClientDisconnectedReason.ExceptionOnAccept, e.ToString());
            }
            catch (Exception e)
            {
                ForceDisconnectClient(clientConnection, ClientDisconnectedReason.ExceptionOnAccept, e.ToString());
            }
            listener.BeginAccept(new AsyncCallback(AcceptCallback), null);
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
                        QueueEvent(new DataReceivedEventArgs(clientConnection.guid, data));
                    }
                    clientConnection.socket.BeginReceive(clientConnection.buffer, 0, clientConnection.buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), clientConnection);
                }
                else
                {
                    ForceDisconnectClient(clientConnection, ClientDisconnectedReason.ExceptionOnReceive, "Bytes read was less than or equal to 0");
                }
            }
            catch (SocketException e)
            {
                ForceDisconnectClient(clientConnection, ClientDisconnectedReason.ExceptionOnReceive, e.ToString());
            }
        }
        private void SendCallback(IAsyncResult ar)
        {
            ClientConnection clientConnection = (ClientConnection)ar.AsyncState;
            try
            {
                clientConnection.socket.EndSend(ar);
            }
            catch (SocketException e)
            {
                ForceDisconnectClient(clientConnection, ClientDisconnectedReason.ExceptionOnSend, e.ToString());
            }
        }
        private void DisconnectCallback(IAsyncResult ar)
        {
            ClientConnection clientConnection = (ClientConnection)ar.AsyncState;
            try
            {
                clientConnection.socket.EndDisconnect(ar);
                QueueEvent(new ClientDisconnectedEventArgs(clientConnection.guid, ClientDisconnectedReason.Requested, "A client properly disconnected"));
            }
            catch (SocketException e)
            {
                QueueEvent(new ClientDisconnectedEventArgs(clientConnection.guid, ClientDisconnectedReason.ExceptionOnDisconnect, e.ToString()));
            }
        }
        #endregion Callbacks

        private void ForceDisconnectClient(ClientConnection clientConnection, ClientDisconnectedReason reason, string message)
        {
            if (clientConnection != null)
            {
                clientConnection.socket.Close();
                lock (connections)
                {
                    connections.Remove(clientConnection);
                }
            }
            QueueEvent(new ClientDisconnectedEventArgs(clientConnection.guid, reason, message));
        }

        private bool TryGetClientConnection(Guid guid, out ClientConnection clientConnection)
        {
            lock (connections)
            {
                foreach(ClientConnection client in connections)
                {
                    if (client.guid.Equals(guid))
                    {
                        clientConnection = client;
                        return true;
                    }
                }
            }
            clientConnection = null;
            return false;
        }


    }
}
