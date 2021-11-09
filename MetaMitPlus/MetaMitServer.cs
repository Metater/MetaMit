using BitManipulation;
using MetaMitPlus.Server;
using MetaMitPlus.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using static MetaMitPlus.Utils.DataUnpacker;

namespace MetaMitPlus
{
    public sealed class MetaMitServer : IDisposable
    {
        // Possible Ideas:
        // Could add data length receive limit policy in future
        // Hash data when sending and return the hash, on the sent data message give hash back?
        // Todo:
        // Make a better builder method maybe struct for start options
        // make constructor set things readonly

        public IPEndPoint LocalEP { get; private set; }
        public bool Listening { get; private set; } = false;
        public int Backlog { get; private set; } = 100;
        public int EncryptionInitiationTimeout { get; private set; } = 5000;

        public ReceivePolicy serverReceivePolicy = new ReceivePolicy(ReceivePolicy.EncryptionPolicy.Dual, ReceivePolicy.CompressionPolicy.Dual);
        public SendPolicy serverSendPolicy = new SendPolicy(true, 65536);

        private Socket listener;

        private bool IsLocalEpNull => LocalEP == null;
        private bool IsListenerNull => listener == null;

        private List<ClientConnection> connections = new List<ClientConnection>();
        private ConcurrentQueue<ServerEventArgs> queuedEvents = new ConcurrentQueue<ServerEventArgs>();
        private ConcurrentDictionary<Guid, ClientConnection> clientDictionary = new ConcurrentDictionary<Guid, ClientConnection>();
        internal ConcurrentQueue<ClientConnection> encryptingClients = new ConcurrentQueue<ClientConnection>();

        public event EventHandler<ClientConnectedEventArgs> ClientConnected;
        public event EventHandler<ClientEncryptedEventArgs> ClientEncrypted;
        public event EventHandler<ClientDisconnectedEventArgs> ClientDisconnected;
        public event EventHandler<DataReceivedEventArgs> DataReceived;
        public event EventHandler<DataSentEventArgs> DataSent;
        public event EventHandler<ServerStartedEventArgs> ServerStarted;
        public event EventHandler<ServerStoppedEventArgs> ServerStopped;

        private bool hasStarted = false;

        #region Construction
        internal MetaMitServer() { }
        /// <summary>
        /// Creates new instance of MetaMitServer
        /// </summary>
        /// <returns>New instance of MetaMitServer</returns>
        public static MetaMitServer NewServer()
        {
            return new MetaMitServer();
        }
        /// <summary>
        /// Forms the local endpoint for the server with the provided port and the privided local ip to use
        /// </summary>
        /// <param name="port">The port the listening socket will bind to</param>
        /// <param name="localIp">The local IP of the server</param>
        public MetaMitServer SetLocalEndpoint(int port, IPAddress localIp)
        {
            if (Listening) throw new Exception("Cannot set LocalEP, Server listening");
            LocalEP = NetworkUtils.GetEndPoint(localIp, port);
            return this;
        }
        /// <summary>
        /// Forms the local endpoint for the server with the provided port and parses the privided local ip to use
        /// </summary>
        /// <param name="port">The port the listening socket will bind to</param>
        /// <param name="localIp">The local IP of the server, is a string</param>
        public MetaMitServer SetLocalEndpoint(int port, string localIp)
        {
            if (IPAddress.TryParse(localIp, out IPAddress outLocalIp))
                SetLocalEndpoint(port, outLocalIp);
            else
                throw new Exception($"Unable to parse provided local IP: {localIp}");
            return this;
        }
        /// <summary>
        /// Forms the local endpoint for the server with the default local IPv4 and the provided port
        /// </summary>
        /// <param name="port">The port the listening socket will bind to</param>
        public MetaMitServer SetLocalEndpoint(int port)
        {
            SetLocalEndpoint(port, NetworkUtils.GetLocalIPv4());
            return this;
        }

        /// <summary>
        /// Sets the socket the server listens on
        /// </summary>
        /// <param name="listenerSocket">Socket the server listens on</param>
        public MetaMitServer SetListenerSocket(Socket listenerSocket)
        {
            if (Listening) throw new Exception("Cannot change listener socket, Server listening");
            listener = listenerSocket;
            return this;
        }

        /// <summary>
        /// Sets the defualt server receive policy
        /// </summary>
        /// <param name="serverReceivePolicy">Default server receive policy</param>
        public MetaMitServer SetReceivePolicy(ReceivePolicy serverReceivePolicy)
        {
            this.serverReceivePolicy = serverReceivePolicy;
            return this;
        }
        /// <summary>
        /// Sets the defualt server send policy
        /// </summary>
        /// <param name="serverSendPolicy">Default server send policy</param>
        public MetaMitServer SetSendPolicy(SendPolicy serverSendPolicy)
        {
            this.serverSendPolicy = serverSendPolicy;
            return this;
        }

        /// <summary>
        /// Sets the encryption initiation timeout, in milliseconds
        /// </summary>
        /// <param name="encryptionInitiationTimeout">Encryption initiation timeout in milliseconds</param>
        public MetaMitServer SetEncryptionInitiationTimeout(int encryptionInitiationTimeout)
        {
            if (Listening) throw new Exception("Server listening");
            EncryptionInitiationTimeout = encryptionInitiationTimeout;
            return this;
        }
        #endregion Construction

        #region PublicMethods
        /// <summary>
        /// Bind to the port provided and starts listening for connections
        /// </summary>
        public void Start()
        {
            if (hasStarted) throw new Exception("Tried to restart a MetaMitServer instance");
            if (IsLocalEpNull) throw new Exception("Local EP not provided with \"MetaMitServer.SetLocalEndpoint\"");
            if (IsListenerNull) listener = GetDefaultListenerSocket();
            hasStarted = true;
            Listening = true;
            try
            {
                listener.Bind(LocalEP);
                listener.Listen(Backlog);
                listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);
                QueueEvent(new ServerStartedEventArgs());
            }
            catch (Exception e)
            {
                QueueEvent(new ServerStoppedEventArgs(ServerStoppedReason.Exception, e.ToString()));
            }
        }
        /// <summary>
        /// Stops the server and disposes it
        /// </summary>
        public void Stop()
        {
            // disconnect all clients also
            Listening = false;
            listener.Close();
            Dispose();
        }

        /// <summary>
        /// Send data to a connected client with their Guid
        /// </summary>
        /// <param name="guid">Guid of client that will refer to a client connection</param>
        /// <param name="data">Data to send</param>
        /// <param name="sendOptions">Options for how data will be prepared to send</param>
        public void Send(Guid guid, byte[] data, SendOptions sendOptions)
        {
            if (TryGetClientConnection(guid, out ClientConnection clientConnection))
            {
                clientConnection.Send(data);
            }
        }

        /// <summary>
        /// Broadcast data to all connected clients with a supplied function allowing or denying a send
        /// </summary>
        /// <param name="data">Data to send to all clients that the function allows</param>
        /// <param name="shouldSend">A supplied function that decides if a client should be sent data, called once per client</param>
        /// <param name="sendOptions">Options for how data will be prepared to send</param>
        public void Broadcast(byte[] data, Func<ClientConnection, bool> shouldSend, SendOptions sendOptions)
        {
            List<ClientConnection> cachedConnections = new List<ClientConnection>(clientDictionary.Values);
            foreach (ClientConnection clientConnection in cachedConnections)
                if (shouldSend(clientConnection))
                    clientConnection.Send(data, sendOptions);
        }
        /// <summary>
        /// Broadcast data to all connected clients
        /// </summary>
        /// <param name="data">Data to send to all clients</param>
        /// <param name="sendOptions">Options for how data will be prepared to send</param>
        public void Broadcast(byte[] data, SendOptions sendOptions)
        {
            Broadcast(data, (_) => { return true; }, sendOptions);
        }

        /// <summary>
        /// Disconnect a connected client refered to by it's Guid
        /// </summary>
        /// <param name="guid">Guid of the client that will be disconnected</param>
        public void Disconnect(Guid guid)
        {
            if (TryGetClientConnection(guid, out ClientConnection clientConnection))
            {
                clientConnection.Disconnect();
            }
        }

        /// <summary>
        /// Disconnects all connected clients with a supplied function for allowing or denying
        /// </summary>
        /// <param name="shouldDisconnect">A supplied function that decides if a client should be disconnected, called once per client</param>
        public void DisconnectAll(Func<ClientConnection, bool> shouldDisconnect)
        {
            List<ClientConnection> cachedConnections = new List<ClientConnection>(clientDictionary.Values);
            foreach (ClientConnection clientConnection in cachedConnections)
                if (shouldDisconnect(clientConnection))
                    clientConnection.Disconnect();
        }
        /// <summary>
        /// Disconnects all connected clients
        /// </summary>
        public void DisconnectAll()
        {
            DisconnectAll((_) => { return true; } );
        }

        /// <summary>
        /// Polls all of the events in the event queue, call fairly frequently
        /// </summary>
        public void PollEvents()
        {
            int queuedEventsCount = queuedEvents.Count;
            for (int i = 0; i < queuedEventsCount; i++)
            {
                if (queuedEvents.TryDequeue(out ServerEventArgs serverEventArgs))
                    ProcessQueuedEvent(serverEventArgs);
                else
                    break;
            }
            int queuedEncryptingClients = encryptingClients.Count;
            for (int i = 0; i < queuedEncryptingClients; i++)
            {
                if (encryptingClients.TryPeek(out ClientConnection clientConnection))
                {
                    if (clientConnection.encryptionPhase != ClientConnection.EncryptionPhase.SentRSAPublicKey || !clientConnection.IsActive)
                    {
                        encryptingClients.TryDequeue(out _);
                        continue;
                    }
                    if (clientConnection.encryptionInitiationStopwatch.ElapsedMilliseconds >= EncryptionInitiationTimeout)
                    {
                        if (encryptingClients.TryDequeue(out _))
                            DisconnectClient(clientConnection, ClientDisconnectedReason.EncryptionTimeout, "Encryption initiation timeout");
                    }
                    else
                        break;
                }
                else
                    break;
            }
        }

        /// <summary>
        /// Disposes the listening socket and disposes the MetaMitServer instance
        /// </summary>
        public void Dispose()
        {
            listener.Dispose();
        }
        #endregion PublicMethods

        #region EventManagement
        private void QueueEvent(ServerEventArgs serverEventArgs)
        {
            queuedEvents.Enqueue(serverEventArgs);
        }
        private void ProcessQueuedEvent(ServerEventArgs serverEventArgs)
        {
            switch (serverEventArgs.eventType)
            {
                case ServerEventType.ClientConnected:
                    ClientConnected?.Invoke(this, (ClientConnectedEventArgs)serverEventArgs);
                    break;
                case ServerEventType.ClientEncrypted:
                    ClientEncrypted?.Invoke(this, (ClientEncryptedEventArgs)serverEventArgs);
                    break;
                case ServerEventType.ClientDisconnected:
                    ClientDisconnected?.Invoke(this, (ClientDisconnectedEventArgs)serverEventArgs);
                    break;
                case ServerEventType.DataReceived:
                    DataReceived?.Invoke(this, (DataReceivedEventArgs)serverEventArgs);
                    break;
                case ServerEventType.DataSent:
                    DataSent?.Invoke(this, (DataSentEventArgs)serverEventArgs);
                    break;
                case ServerEventType.ServerStarted:
                    ServerStarted?.Invoke(this, (ServerStartedEventArgs)serverEventArgs);
                    break;
                case ServerEventType.ServerStopped:
                    ServerStopped?.Invoke(this, (ServerStoppedEventArgs)serverEventArgs);
                    break;
            }
        }
        #endregion EventManagement

        #region Callbacks
        private void AcceptCallback(IAsyncResult ar)
        {
            if (!Listening)
            {
                QueueEvent(new ServerStoppedEventArgs(ServerStoppedReason.Requested, "The server has been stopped"));
                return;
            }
            ClientConnection clientConnection = new ClientConnection(this);
            try
            {
                clientConnection.socket = listener.EndAccept(ar);
                clientConnection.SetActive(true);
                lock (connections)
                {
                    connections.Add(clientConnection);
                }
                clientDictionary.TryAdd(clientConnection.guid, clientConnection);
                QueueEvent(new ClientConnectedEventArgs(clientConnection));
                clientConnection.socket.BeginReceive(clientConnection.buffer, 0, clientConnection.buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), clientConnection);
            }
            catch (Exception e)
            {
                DisconnectClient(clientConnection, ClientDisconnectedReason.Exception, $"Exception on connect: {e.StackTrace}");
            }
            listener.BeginAccept(new AsyncCallback(AcceptCallback), null);
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            ClientConnection clientConnection = (ClientConnection)ar.AsyncState;
            if (!clientConnection.IsActive) return;
            try
            {
                int bytesReceived = clientConnection.socket.EndReceive(ar);
                if (bytesReceived > 0)
                {
                    clientConnection.bytesReceived += bytesReceived;
                    if (clientConnection.dataUnpacker.TryUnpackData(bytesReceived, clientConnection.buffer, out List<UnpackedData> unpackedData))
                    {
                        foreach (UnpackedData data in unpackedData)
                        {
                            clientConnection.packetsReceived++;
                            HandleReceive(clientConnection, data);
                        }
                    }
                    clientConnection.socket.BeginReceive(clientConnection.buffer, 0, clientConnection.buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), clientConnection);
                }
                else
                {
                    DisconnectClient(clientConnection, ClientDisconnectedReason.Exception, "Exception on receive: Bytes received was less than or equal to 0");
                }
            }
            catch (SocketException e)
            {
                DisconnectClient(clientConnection, ClientDisconnectedReason.Exception, $"Exception on receive: {e.StackTrace}");
            }
        }
        private void HandleReceive(ClientConnection clientConnection, UnpackedData data)
        {
            if (data.hasMetadata)
            {
                if (data.metadata == 0 || data.metadata == 2)
                {
                    if (clientConnection.receivePolicy.encryptionPolicy == ReceivePolicy.EncryptionPolicy.No)
                    {
                        DisconnectClient(clientConnection, ClientDisconnectedReason.ReceivePolicyViolation, "Client tried to use encryption");
                        return;
                    }
                    try
                    {
                        data.data = CryptographyUtils.AESDecrypt(clientConnection.aesKey, data.data);
                    }
                    catch (Exception)
                    {
                        DisconnectClient(clientConnection, ClientDisconnectedReason.Exception, "Failed to decrypt incoming data");
                        return;
                    }
                }
                if (data.metadata == 1 || data.metadata == 2)
                {
                    if (clientConnection.receivePolicy.compressionPolicy == ReceivePolicy.CompressionPolicy.No)
                    {
                        DisconnectClient(clientConnection, ClientDisconnectedReason.ReceivePolicyViolation, "Client tried to use compression");
                        return;
                    }
                    try
                    {
                        data.data = CompressionUtils.Unzip(data.data);
                    }
                    catch (Exception)
                    {
                        DisconnectClient(clientConnection, ClientDisconnectedReason.Exception, "Failed to unzip incoming data");
                        return;
                    }
                }
                else if (data.metadata == 3) // Client giving AES key
                {
                    if (clientConnection.encryptionPhase != ClientConnection.EncryptionPhase.SentRSAPublicKey)
                    {
                        DisconnectClient(clientConnection, ClientDisconnectedReason.ProtocolViolation, $"Received AES key while in wrong state");
                        return;
                    }
                    data.data = CryptographyUtils.RSADecrypt(data.data, clientConnection.rsaPrivateKey);
                    clientConnection.aesKey = data.data;
                    clientConnection.encryptionPhase = ClientConnection.EncryptionPhase.Encrypted;
                    QueueEvent(new ClientEncryptedEventArgs(clientConnection));
                    return;
                }
                else
                {
                    DisconnectClient(clientConnection, ClientDisconnectedReason.ProtocolViolation, $"Client used unknown sessionFlag: {data.metadata}");
                    return;
                }
                
                if (data.metadata != 3)
                {
                    if (clientConnection.receivePolicy.encryptionPolicy == ReceivePolicy.EncryptionPolicy.Yes)
                    {
                        if (data.metadata != 0 || data.metadata != 2)
                        {
                            DisconnectClient(clientConnection, ClientDisconnectedReason.ReceivePolicyViolation, $"Client not using encryption, requested in receive policy");
                            return;
                        }
                    }
                    if (clientConnection.receivePolicy.compressionPolicy == ReceivePolicy.CompressionPolicy.Yes)
                    {
                        if (data.metadata != 1 || data.metadata != 2)
                        {
                            DisconnectClient(clientConnection, ClientDisconnectedReason.ReceivePolicyViolation, $"Client not using compression, requested in receive policy");
                            return;
                        }
                    }
                }
            }
            QueueEvent(new DataReceivedEventArgs(clientConnection, data.data));
        }

        internal void SendCallback(IAsyncResult ar)
        {
            ClientConnection clientConnection = (ClientConnection)ar.AsyncState;
            if (!clientConnection.IsActive) return;
            try
            {
                int bytesSent = clientConnection.socket.EndSend(ar);
                clientConnection.bytesSent += bytesSent;
                clientConnection.packetsSent++;
                QueueEvent(new DataSentEventArgs(clientConnection, bytesSent));
            }
            catch (SocketException e)
            {
                DisconnectClient(clientConnection, ClientDisconnectedReason.Exception, $"Exception on send: {e.StackTrace}");
            }
        }

        internal void DisconnectCallback(IAsyncResult ar)
        {
            ClientConnection clientConnection = (ClientConnection)ar.AsyncState;
            if (!clientConnection.IsActive) return;
            try
            {
                clientConnection.socket.EndDisconnect(ar);
            }
            catch (SocketException) { }
            DisconnectClient(clientConnection, ClientDisconnectedReason.Requested, "A client properly disconnected", false);
        }
        #endregion Callbacks

        #region ClientManagement
        internal void DisconnectClient(ClientConnection clientConnection, ClientDisconnectedReason reason, string message, bool closeSocket = true)
        {
            if (clientConnection.IsActive)
            {
                clientConnection.SetActive(false);
                if (closeSocket) clientConnection.socket.Close();
                lock (connections)
                {
                    if (connections.Remove(clientConnection))
                    {
                        clientDictionary.TryRemove(clientConnection.guid, out _);
                        QueueEvent(new ClientDisconnectedEventArgs(clientConnection, reason, message));
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
