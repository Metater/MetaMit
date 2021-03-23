using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace MetaMitStandard
{
    public sealed class MetaMitServer : IDisposable
    {
        private IPEndPoint ep;
        private int backlog;

        private Socket listener;
        private Thread listeningThread;
        private static ManualResetEvent listenerDoneCycle = new ManualResetEvent(false);
        private CancellationTokenSource listenerCts = new CancellationTokenSource();

        public event EventHandler<ClientConnectedEventArgs> ClientConnected;
        public event EventHandler<ClientDisconnectedEventArgs> ClientDisconnected;
        public event EventHandler<DataReceivedEventArgs> DataReceived;
        public event EventHandler<ServerStoppedEventArgs> ServerStopped;

        public MetaMitServer(int port, int backlog)
        {
            this.backlog = backlog;
            ep = Utils.NetUtils.GetEndPoint(Utils.NetUtils.GetLocalIPv4(), port);
            listener = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
            listener.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
        }

        public void Start()
        {
            listeningThread = new Thread(new ThreadStart(() =>
            {
                ServerStoppedEventArgs serverStopped = new ServerStoppedEventArgs();
                try
                {
                    listener.Bind(ep);
                    listener.Listen(backlog);
                    while (true)
                    {
                        listenerDoneCycle.Reset();
                        listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);
                        listenerDoneCycle.WaitOne();
                        listenerCts.Token.ThrowIfCancellationRequested();
                    }
                }
                catch (OperationCanceledException)
                {
                    serverStopped.reason = ServerStoppedReason.Commanded;
                    serverStopped.message = "The server has stopped listening";
                }
                catch (Exception e)
                {
                    serverStopped.reason = ServerStoppedReason.Crashed;
                    serverStopped.message = e.ToString();
                }
                ServerStopped?.Invoke(this, serverStopped);
            }));
            listeningThread.Start();
        }

        public void Stop()
        {
            listenerCts.Cancel();
            listener.Close();
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            listenerDoneCycle.Set();
            if (listenerCts.Token.IsCancellationRequested) return;

            Socket client = listener.EndAccept(ar);
        }

        public void Dispose()
        {
            listener.Dispose();
            listenerDoneCycle.Dispose();
            listenerCts.Dispose();
        }
    }
}
