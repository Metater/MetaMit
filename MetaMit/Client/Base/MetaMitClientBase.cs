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
        // Receiving data
        public const int BufferSize = 1024;
        public byte[] buffer = new byte[BufferSize];
        public StringBuilder sb = new StringBuilder();
        // Events
        public event Action OnServerStart;
        public event Action OnServerStop;

        public event Action OnServerConnectionRejected;

        public event EventHandler<MetaMitClientBaseEventArgs.ServerConnectionPending> OnServerConnectionPending;
        public event EventHandler<MetaMitClientBaseEventArgs.ServerConnectionAccepted> OnServerConnectionAccepted;
        public event EventHandler<MetaMitClientBaseEventArgs.ServerConnectionFailed> OnServerConnectionFailed;
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
        private void ConnectCallback(IAsyncResult ar)
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
                    // May want to move StartReceiveLoop in here to ensure all accept processing is done before receiving is started
                });

                StartReceiveLoop();
            }
            catch(SocketException)
            {
                Task.Run(() =>
                {
                    OnServerConnectionFailed?.Invoke(this, new MetaMitClientBaseEventArgs.ServerConnectionFailed
                    {

                    });
                });
            }
        }
        private void StartReceiveLoop()
        {
            try
            {
                socket.BeginReceive(buffer, 0, BufferSize, 0, new AsyncCallback(ReceiveCallback), null);
            }
            catch (Exception e)
            {
                // Figure out what error this would be
                Console.WriteLine(e.ToString());
            }
        }
        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                int bytesRead = 0;
                bytesRead = socket.EndReceive(ar);
                string content = string.Empty;
                if (bytesRead > 0)
                {
                    sb.Append(Encoding.ASCII.GetString(buffer, 0, bytesRead));
                    content = sb.ToString();
                    if (content.IndexOf("<EOT>") > -1)
                    {
                        buffer = new byte[BufferSize];
                        sb = new StringBuilder();
                        // Data receive done content filled with data, add event later
                        Task.Run(() =>
                        {
                            OnDataReceived?.Invoke(this, new MetaMitClientBaseEventArgs.DataReceived
                            {
                                socket = socket,
                                data = content
                            });
                        });
                    }
                }
                socket.BeginReceive(buffer, 0, BufferSize, 0, new AsyncCallback(ReceiveCallback), null);
            }
            catch (SocketException)
            {
                Task.Run(() =>
                {
                    OnServerConnectionLost?.Invoke(this, new MetaMitClientBaseEventArgs.ServerConnectionLost
                    {

                    });
                });
            }

        }
        public void SendString(string data)
        {
            byte[] byteData = Encoding.ASCII.GetBytes(data);
            socket.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), null);
        }
        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                int bytesSent = 0;
                bytesSent = socket.EndSend(ar);
                Task.Run(() =>
                {
                    OnDataSent?.Invoke(this, new MetaMitClientBaseEventArgs.DataSent
                    {
                        bytesSent = bytesSent
                    });
                });
            }
            // Replace with SocketExeption when error known
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
