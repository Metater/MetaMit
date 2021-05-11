using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net;
using MetaMitStandard.Server;
using MetaMitStandard.Utils;

namespace MetaMitStandard
{
    public sealed class MetaMitServer : IDisposable
    {
        public IPEndPoint Ep { get; private set; }
        public int Backlog { get; private set; }
        public bool ServerOpen { get; private set; } = false;

        private Socket listener;

        private List<ClientConnection> connections = new List<ClientConnection>();
        private ConcurrentQueue<ServerEvent> eventQueue = new ConcurrentQueue<ServerEvent>();
        private ConcurrentDictionary<Guid, ClientConnection> clientDictionary = new ConcurrentDictionary<Guid, ClientConnection>();

        public event EventHandler<ClientConnectedEventArgs> ClientConnected;
        public event EventHandler<ClientDisconnectedEventArgs> ClientDisconnected;
        public event EventHandler<DataReceivedEventArgs> DataReceived;
        public event EventHandler<DataSentEventArgs> DataSent;
        public event EventHandler<ServerStartedEventArgs> ServerStarted;
        public event EventHandler<ServerStoppedEventArgs> ServerStopped;

        public MetaMitServer(int port, int backlog)
        {
            Backlog = backlog;
            Ep = NetworkUtils.GetEndPoint(NetworkUtils.GetLocalIPv4(), port);
            listener = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
            listener.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
        }

        #region PublicMethods
        public void Start()
        {
            try
            {
                listener.Bind(Ep);
                listener.Listen(Backlog);
                ServerOpen = true;
                listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);
                QueueEvent(new ServerStartedEventArgs());
            }
            catch (Exception e)
            {
                QueueEvent(new ServerStoppedEventArgs(ServerStoppedReason.Exception, e.ToString()));
            }
        }

        public void Stop()
        {
            ServerOpen = false;
            listener.Close();
        }

        #region Sending
        public void Send(ClientConnection clientConnection, byte[] data, bool includeOverhead = true)
        {
            byte[] packedData;
            if (includeOverhead) packedData = DataPacker.PackData(data, 0);
            else packedData = data;
            clientConnection.socket.BeginSend(packedData, 0, packedData.Length, SocketFlags.None, new AsyncCallback(SendCallback), clientConnection);
        }
        public void Send(Guid guid, byte[] data, bool includeOverhead = true)
        {
            if (TryGetClientConnection(guid, out ClientConnection clientConnection))
            {
                Send(clientConnection, data, includeOverhead);
            }
        }

        public void Broadcast(byte[] data)
        {
            List<ClientConnection> cachedConnections = (List<ClientConnection>)clientDictionary.Keys;
            foreach (ClientConnection connection in cachedConnections)
                Send(connection, data);
        }
        public void Broadcast(byte[] data, Func<ClientConnection, bool> shouldSend)
        {
            List<ClientConnection> cachedConnections = (List<ClientConnection>)clientDictionary.Keys;
            foreach (ClientConnection connection in cachedConnections)
                if (shouldSend(connection))
                    Send(connection, data);
        }
        public void BroadcastToBut(ClientConnection skipConnection, byte[] data)
        {
            Broadcast(data, (connection) => { return connection != skipConnection; });
        }
        public void BroadcastToBut(Guid guid, byte[] data)
        {
            if (TryGetClientConnection(guid, out ClientConnection skipConnection))
            {
                BroadcastToBut(skipConnection, data);
            }
        }
        #endregion Sending

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
        #endregion PublicMethods

        #region EventManagement
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
                case ServerEventType.DataSent:
                    DataSent?.Invoke(this, (DataSentEventArgs)serverEvent.serverEventArgs);
                    break;
                case ServerEventType.ServerStarted:
                    ServerStarted?.Invoke(this, (ServerStartedEventArgs)serverEvent.serverEventArgs);
                    break;
                case ServerEventType.ServerStopped:
                    ServerStopped?.Invoke(this, (ServerStoppedEventArgs)serverEvent.serverEventArgs);
                    break;
            }
        }
        #endregion EventManagement

        #region Callbacks
        private void AcceptCallback(IAsyncResult ar)
        {
            if (!ServerOpen)
            {
                QueueEvent(new ServerStoppedEventArgs(ServerStoppedReason.Requested, "The server has been stopped"));
                return;
            }
            ClientConnection clientConnection = new ClientConnection();
            try
            {
                clientConnection.socket = listener.EndAccept(ar);
                clientConnection.isActive = true;
                lock (connections)
                {
                    connections.Add(clientConnection);
                }
                clientDictionary.TryAdd(clientConnection.guid, clientConnection);
                QueueEvent(new ClientConnectedEventArgs(clientConnection.guid, clientConnection.socket.RemoteEndPoint));
                clientConnection.socket.BeginReceive(clientConnection.buffer, 0, clientConnection.buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), clientConnection);
            }
            catch (Exception e)
            {
                DisconnectClient(clientConnection, ClientDisconnectedReason.ExceptionOnAccept, e.ToString(), true);
            }
            listener.BeginAccept(new AsyncCallback(AcceptCallback), null);
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            ClientConnection clientConnection = (ClientConnection)ar.AsyncState;
            if (!clientConnection.isActive) return;
            try
            {
                int bytesReceived = clientConnection.socket.EndReceive(ar);
                if (bytesReceived > 0)
                {
                    clientConnection.bytesReceived += bytesReceived;
                    if (clientConnection.dataUnpacker.TryUnpackData(bytesReceived, clientConnection.buffer, out List<byte[]> unpackedData, out List<ushort> sessionFlags))
                    {
                        foreach(byte[] data in unpackedData)
                        {
                            clientConnection.packetsReceived++;
                            QueueEvent(new DataReceivedEventArgs(clientConnection, data));
                        }
                    }
                    clientConnection.socket.BeginReceive(clientConnection.buffer, 0, clientConnection.buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), clientConnection);
                }
                else
                {
                    DisconnectClient(clientConnection, ClientDisconnectedReason.ExceptionOnReceive, "Bytes received was less than or equal to 0", true);
                }
            }
            catch (SocketException e)
            {
                DisconnectClient(clientConnection, ClientDisconnectedReason.ExceptionOnReceive, e.ToString(), true);
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            ClientConnection clientConnection = (ClientConnection)ar.AsyncState;
            if (!clientConnection.isActive) return;
            try
            {
                int bytesSent = clientConnection.socket.EndSend(ar);
                clientConnection.bytesSent += bytesSent;
                clientConnection.packetsSent++;
                QueueEvent(new DataSentEventArgs(clientConnection, bytesSent));
            }
            catch (SocketException e)
            {
                DisconnectClient(clientConnection, ClientDisconnectedReason.ExceptionOnSend, e.ToString(), true);
            }
        }

        private void DisconnectCallback(IAsyncResult ar)
        {
            ClientConnection clientConnection = (ClientConnection)ar.AsyncState;
            if (!clientConnection.isActive) return;
            try
            {
                clientConnection.socket.EndDisconnect(ar);
            }
            catch (SocketException)
            {

            }
            DisconnectClient(clientConnection, ClientDisconnectedReason.Requested, "A client properly disconnected", false);
        }
        #endregion Callbacks

        #region ClientManagement
        private void DisconnectClient(ClientConnection clientConnection, ClientDisconnectedReason reason, string message, bool closeSocket)
        {
            if (clientConnection.isActive)
            {
                clientConnection.isActive = false;
                if (closeSocket) clientConnection.socket.Close();
                lock (connections)
                {
                    if (connections.Remove(clientConnection))
                    {
                        clientDictionary.TryRemove(clientConnection.guid, out _);
                        QueueEvent(new ClientDisconnectedEventArgs(clientConnection.guid, reason, message));
                    }
                }
                clientConnection.Dispose();
            }
        }

        private bool TryGetClientConnection(Guid guid, out ClientConnection clientConnection)
        {
            return clientDictionary.TryGetValue(guid, out clientConnection);
        }
        #endregion ClientManagement
    }
}
