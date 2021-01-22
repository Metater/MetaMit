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
        #endregion Fields

        // Constructor
        public MetaMitServerBase(int port, int backlog)
        {
            Backlog = backlog;

            Ep = Utils.NetUtils.GetEndPoint(Utils.NetUtils.GetLocalIPv4(), port);

            Listener = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
            Listener.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
        }

        #region Listening
        public void StartListening()
        {
            listeningThread = new Thread(new ThreadStart(() =>
            {
                try
                {
                    Listener.Bind(Ep);
                    Listener.Listen(Backlog);

                    ServerStart();

                    while (true)
                    {
                        listenerDoneCycle.Reset();
                        if (listenerCts.Token.IsCancellationRequested) break;
                        Listener.BeginAccept(new AsyncCallback(AcceptCallback), Listener);
                        if (listenerCts.Token.IsCancellationRequested) break;
                        listenerDoneCycle.WaitOne();
                    }

                    ServerStop();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }));
            listeningThread.Start();
        }
        public void StopListening()
        {
            IsListening = false;
            listenerCts.Cancel();
            listenerDoneCycle.Set();
            Listener.Close();
            Listener.Dispose();
        }
        #endregion Listening

        #region Accepting
        public void AcceptClient(ClientConnection connection, int clientCount)
        {
            // Could add time since last receive in StateObject if want to use keep alive and kick people off if no packets
            connection.socket.BeginReceive(connection.buffer, 0, ClientConnection.BufferSize, 0, new AsyncCallback(ReceiveCallback), connection);

            // REMOVE CLIENT COUNT LATER WHEN OTHER CODE IS TRANSFERRED INTO SERVERBASE
            ConnectionAccepted(connection, clientCount);
        }
        private void AcceptCallback(IAsyncResult ar)
        {
            listenerDoneCycle.Set();

            if (listenerCts.Token.IsCancellationRequested) return;

            Socket listener = (Socket)ar.AsyncState;
            Socket client = listener.EndAccept(ar);
            ConnectionPending(client);

            /*
             * 
             * Replace with code for accepting a client through each state
             * 
             * 
             * 
            */
        }
        #endregion Accepting

        #region Receiving
        private void ReceiveCallback(IAsyncResult ar)
        {
            ClientConnection connection = (ClientConnection)ar.AsyncState;

            // Do more thinking about what causes this, do you need separate closes for properly closed, and forcibly
            // Currently the same method ClientConnection.Close() is used for both connections being ended and lost
            if (connection.IsClosed) return; // Connection was ended properly, stop receive callback loop

            int bytesRead = 0;
            try
            {
                bytesRead = connection.socket.EndReceive(ar);
            }
            catch (SocketException)
            {
                ConnectionLost(connection);
                return;
            }
            if (bytesRead > 0)
            {
                connection.sb.Append(Encoding.ASCII.GetString(connection.buffer, 0, bytesRead));
                string content = string.Empty;
                content = connection.sb.ToString();
                if (content.IndexOf("<EOT>") > -1)
                {
                    connection.WipeBuffer();
                    HandleData(connection, content);
                }
                connection.socket.BeginReceive(connection.buffer, 0, ClientConnection.BufferSize, 0, new AsyncCallback(ReceiveCallback), connection);
                return;
            }
            else
                ConnectionEnded(connection);
        }
        private void HandleData(ClientConnection connection, string data)
        {
            if (connection.state == ClientConnectionState.ConnectedAndEncrypted)
            {
                DataReceived(connection, data);
                return; // Normal data recieved, handle with data recieved event
            }
            switch (connection.state)
            {
                case ClientConnectionState.Connected:
                    // May want to handle higher up, when accepted do stuff this would do,
                    //and some of these steps may be non server controlled nessessary or unness ------------------------------- Left off on
                    // May want to Make ConnectedEncryptedAndCompressing?!?!?
                    break;
                case ClientConnectionState.ClientGaveRSAPublicKey:
                    break;
                case ClientConnectionState.ServerGaveAESKey:
                    break;
                default: return;
            }
        }
        #endregion Receiving

        #region Sending
        public void SendString(ClientConnection connection, string data)
        {
            byte[] byteData = Encoding.ASCII.GetBytes(data);
            connection.socket.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), connection);
        }
        public void SendStringEOT(ClientConnection connection, string data)
        {
            data += "<EOT>";
            byte[] byteData = Encoding.ASCII.GetBytes(data);
            connection.socket.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), connection);
        }
        private void SendCallback(IAsyncResult ar)
        {
            ClientConnection connection = (ClientConnection)ar.AsyncState;

            try
            {
                connection.socket.EndSend(ar);
            }
            catch (SocketException)
            {
                ConnectionLost(connection);
            }
        }
        #endregion Sending

        #region Disconnecting
        public void DisconnectClient(ClientConnection connection)
        {
            connection.socket.BeginDisconnect(false, new AsyncCallback(DisconnectCallback), connection);
        }
        private void DisconnectCallback(IAsyncResult ar)
        {
            ClientConnection connection = (ClientConnection)ar.AsyncState;

            try
            {
                connection.socket.EndDisconnect(ar);
            }
            catch (SocketException)
            {
                // If this, connection was already closed unexpectedly by client, treat as ended
            }
            ConnectionEnded(connection);
        }
        #endregion Disconnecting

        #region Events
        private void ConnectionPending(Socket client)
        {
            Task.Run(() =>
            {
                OnConnectionPendingEvent?.Invoke(this, new MetaMitServerBaseEventArgs.ConnectionPending
                {
                    socket = client
                });
            });
        }
        private void ConnectionAccepted(ClientConnection connection, int clientCount)
        {
            Task.Run(() =>
            {
                OnConnectionAcceptedEvent?.Invoke(this, new MetaMitServerBaseEventArgs.ConnectionAccepted
                {
                    connection = connection,
                    clientCount = clientCount
                });
            });
        }
        private void ConnectionEnded(ClientConnection connection)
        {
            Task.Run(() =>
            {
                OnConnectionEndedEvent?.Invoke(this, new MetaMitServerBaseEventArgs.ConnectionEnded
                {
                    client = connection.guid
                });
            });
            connection.Close();
        }
        private void ConnectionLost(ClientConnection connection)
        {
            Task.Run(() =>
            {
                OnConnectionLostEvent?.Invoke(this, new MetaMitServerBaseEventArgs.ConnectionLost
                {
                    client = connection.guid
                });
            });
            connection.Close();
        }
        
        private void DataReceived(ClientConnection connection, string data)
        {
            Task.Run(() =>
            {
                OnDataReceivedEvent?.Invoke(this, new MetaMitServerBaseEventArgs.DataReceived
                {
                    connection = connection,
                    data = data
                });
            });
        }

        private void ServerStart()
        {
            IsListening = true;
            Task.Run(() =>
            {
                OnServerStartEvent?.Invoke();
            });
        }
        private void ServerStop()
        {
            Task.Run(() =>
            {
                OnServerStopEvent?.Invoke();
            });
        }
        #endregion Events

        public void Dispose()
        {
            StopListening();
        }
    }
}
