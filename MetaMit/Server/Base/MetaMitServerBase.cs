using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace MetaMit.Server.Base
{
    public sealed class MetaMitServerBase : IDisposable
    {
        #region Fields
        // Server Init Fields
        /// <summary>
        /// The max number of clients the server can have in the accept backlog
        /// </summary>
        public int Backlog { get; private set; }
        /// <summary>
        /// The server's internet protocol address
        /// </summary>
        public IPAddress Ip { get; private set; }
        /// <summary>
        /// Server's end point that it binds the listening socket to
        /// </summary>
        public IPEndPoint Ep { get; private set; }
        // Listening Fields
        /// <summary>
        /// The socket that binds to the local end point and listens on
        /// </summary>
        public Socket Listener { get; private set; }
        /// <summary>
        /// True if server is listening
        /// </summary>
        public bool IsListening { get; private set; } = false;
        /// <summary>
        /// Progress updates about the listener
        /// </summary>
        private Thread listeningThread;
        private static ManualResetEvent listenerDoneCycle = new ManualResetEvent(false);
        private CancellationTokenSource listenerCts = new CancellationTokenSource();
        // Server Events
        public event Action OnServerStartEvent;
        public event Action OnServerStopEvent;

        public event EventHandler<MetaMitServerBaseEventArgs.ConnectionPending> OnConnectionPendingEvent;
        public event EventHandler<MetaMitServerBaseEventArgs.ConnectionAccepted> OnConnectionAcceptedEvent;
        public event EventHandler<MetaMitServerBaseEventArgs.ConnectionEnded> OnConnectionEndedEvent;
        public event EventHandler<MetaMitServerBaseEventArgs.ConnectionLost> OnConnectionLostEvent;

        public event EventHandler<MetaMitServerBaseEventArgs.DataReceived> OnDataReceivedEvent;
        public event EventHandler<MetaMitServerBaseEventArgs.DataSent> OnDataSentEvent;
        // Loopback Events
        //public event EventHandler<MetaMitServerBaseEventArgs.LoopbackReceive> LoopbackReceive;
        #endregion Fields



        // Constructor
        public MetaMitServerBase(int port, int backlog)
        {
            Backlog = backlog;

            Ip = Generic.GetIP();
            Ep = Generic.GetEndPoint(Ip, port);

            Listener = new Socket(Ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        }


        #region GenericMethods
        public bool IsConnected(ClientConnection connection)
        {
            if (connection.IsDisposed)
            {
                return false;
            }
            if (!connection.socket.Connected)
            {
                connection.Dispose();
                return false;
            }
            return true;
        }
        #endregion GenericMethods



        #region Listening
        // Start listening on a different thread
        public void StartListening()
        {
            listeningThread = new Thread(new ThreadStart(() =>
            {
                try
                {
                    Listener.Bind(Ep);
                    Listener.Listen(Backlog);
                    IsListening = true;

                    OnServerStartEvent?.Invoke();

                    while (true)
                    {
                        listenerDoneCycle.Reset();
                        if (listenerCts.Token.IsCancellationRequested) break;
                        Listener.BeginAccept(new AsyncCallback(AcceptCallback), Listener);
                        if (listenerCts.Token.IsCancellationRequested) break;
                        listenerDoneCycle.WaitOne();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
                OnServerStopEvent?.Invoke();
                IsListening = false;
            }));
            listeningThread.Start();
        }
        // Stop listening
        public void StopListening()
        {
            IsListening = false;
            listenerCts.Cancel();
            listenerDoneCycle.Set();
            Listener.Close();
            Listener.Dispose();
        }
        #endregion Listening



        #region Receiving
        // Async callback for a client after BeginAccept
        private void AcceptCallback(IAsyncResult ar)
        {
            listenerDoneCycle.Set();

            if (listenerCts.Token.IsCancellationRequested) return;

            Socket listener = (Socket)ar.AsyncState;
            Socket client = listener.EndAccept(ar);

            // Could make recursive accepting

            ClientConnection connection = new ClientConnection();
            connection.socket = client;

            Task.Run(() =>
            {
                OnConnectionPendingEvent?.Invoke(this, new MetaMitServerBaseEventArgs.ConnectionPending
                {
                    socket = client
                });
            });
        }
        /// <summary>
        /// Starts the receive callback loop for a client
        /// </summary>
        public void AcceptClient(Guid guid, ClientConnection connection, int clientCount)
        {
            connection.guid = guid;
            // Could add time since last receive in StateObject if want to use keep alive and kick people off if no packets
            connection.socket.BeginReceive(connection.buffer, 0, ClientConnection.BufferSize, 0, new AsyncCallback(ReceiveCallback), connection);

            Task.Run(() =>
            {
                OnConnectionAcceptedEvent?.Invoke(this, new MetaMitServerBaseEventArgs.ConnectionAccepted
                {
                    connection = connection,
                    clientCount = clientCount
                });
            });
        }
        // Async callback for a client after BeginReceive
        private void ReceiveCallback(IAsyncResult ar)
        {
            ClientConnection connection = (ClientConnection)ar.AsyncState;

            if (connection.IsDisposed) return; // Connection was ended properly, stop receive callback loop

            Socket client = connection.socket;

            String content = String.Empty;
            int bytesRead = 0;
            try
            {
                bytesRead = client.EndReceive(ar);
            }
            catch (SocketException)
            {
                Guid guid = connection.guid;
                connection.Dispose();
                Task.Run(() =>
                {
                    OnConnectionLostEvent?.Invoke(this, new MetaMitServerBaseEventArgs.ConnectionLost
                    {
                        client = guid
                    });
                });
            }
            if (bytesRead > 0)
            {
                connection.sb.Append(Encoding.ASCII.GetString(connection.buffer, 0, bytesRead));
                content = connection.sb.ToString();
                if (content.IndexOf("<EOT>") > -1)
                {
                    connection.buffer = new byte[ClientConnection.BufferSize];
                    connection.sb = new StringBuilder();



                    // Data receive done content filled with data, add event later
                    Task.Run(() =>
                    {
                        OnDataReceivedEvent?.Invoke(this, new MetaMitServerBaseEventArgs.DataReceived
                        {
                            connection = connection,
                            data = content
                        });
                    });
                }
                connection.socket.BeginReceive(connection.buffer, 0, ClientConnection.BufferSize, 0, new AsyncCallback(ReceiveCallback), connection);
            }
        }
        #endregion Receiving



        #region Sending
        // Send a string to client with guid and data             MAY WANT TO MAKE ASYNC later
        public void SendString(ClientConnection connection, string data)
        {
            if (!IsConnected(connection)) return;
            byte[] byteData = Encoding.ASCII.GetBytes(data);
            connection.socket.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), connection);
        }
        private void SendCallback(IAsyncResult ar)
        {
            ClientConnection connection = (ClientConnection)ar.AsyncState;

            int bytesSent = 0;
            try
            {
                bytesSent = connection.socket.EndSend(ar);
                Task.Run(() =>
                {
                    OnDataSentEvent?.Invoke(this, new MetaMitServerBaseEventArgs.DataSent
                    {
                        connection = connection
                    });
                });
            }
            catch (SocketException)
            {
                Guid guid = connection.guid;
                connection.Dispose();
                Task.Run(() =>
                {
                    OnConnectionLostEvent?.Invoke(this, new MetaMitServerBaseEventArgs.ConnectionLost
                    {
                        client = guid
                    });
                });
            }
        }
        #endregion Sending



        #region SendingBlock
        public void SendStringBlock(ClientConnection connection, string data)
        {
            if (!IsConnected(connection)) return;
            byte[] byteData = Encoding.ASCII.GetBytes(data);
            SendStateObject sendStateObject = new SendStateObject(connection);
            sendStateObject.sendComplete.Set();
            connection.socket.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendStringBlockCallback), sendStateObject);
            sendStateObject.sendComplete.WaitOne();
        }
        private void SendStringBlockCallback(IAsyncResult ar)
        {
            SendStateObject sendStateObject = (SendStateObject)ar.AsyncState;

            ClientConnection connection = sendStateObject.connection;

            int bytesSent = 0;
            try
            {
                bytesSent = connection.socket.EndSend(ar);
                Task.Run(() =>
                {
                    OnDataSentEvent?.Invoke(this, new MetaMitServerBaseEventArgs.DataSent
                    {
                        connection = connection,
                    });
                });
            }
            catch (SocketException)
            {
                Guid guid = connection.guid;
                connection.Dispose();
                Task.Run(() =>
                {
                    OnConnectionLostEvent?.Invoke(this, new MetaMitServerBaseEventArgs.ConnectionLost
                    {
                        client = guid
                    });
                });
            }
            sendStateObject.sendComplete.Set();
        }
        #endregion SendingBlock



        // Pause heartbeat events for client when it is peacefully disconnecting



        #region Disconnecting
        // Disconnect a client with guid
        public void DisconnectClient(ClientConnection connection)
        {
            if (!connection.IsDisposed)
                connection.socket.BeginDisconnect(false, new AsyncCallback(DisconnectCallback), connection);
        }
        // Async callback for a client after BeginDisconnect
        private void DisconnectCallback(IAsyncResult ar)
        {
            ClientConnection connection = (ClientConnection)ar.AsyncState;
            Socket client = connection.socket;

            try
            {
                connection.socket.EndDisconnect(ar);
            }
            catch (ObjectDisposedException)
            {
                Console.WriteLine("Client disposed at wierd time during disconnect");
            }
            catch (SocketException)
            {

            }
            Guid guid = connection.guid;
            connection.Dispose();
            Task.Run(() =>
            {
                OnConnectionEndedEvent?.Invoke(this, new MetaMitServerBaseEventArgs.ConnectionEnded
                {
                    client = guid
                });
            });
        }
        #endregion Disconnecting



        public void Dispose()
        {
            StopListening();
        }
    }
}
