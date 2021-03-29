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
        private Thread listeningThread;
        private static ManualResetEvent listenerDoneCycle = new ManualResetEvent(false);
        private CancellationTokenSource listenerCts = new CancellationTokenSource();

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
            //Action listener = new Action(Listen);
            //listeningTask = Task.Run(listener);
            Thread listeningThread = new Thread(new ThreadStart(() =>
            {
                Listen();
            }));
            listeningThread.Start();
        }

        public void Stop()
        {
            listenerCts.Cancel();
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
            listenerDoneCycle.Dispose();
            listenerCts.Dispose();
        }

        private void Listen()
        {
            ServerStoppedEventArgs serverStopped;
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
                serverStopped = new ServerStoppedEventArgs(ServerStoppedReason.Commanded, "The server has stopped listening");
            }
            catch (Exception e)
            {
                serverStopped = new ServerStoppedEventArgs(ServerStoppedReason.Exception, e.ToString());
            }
            QueueEvent(serverStopped);
        }

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

        private void AcceptCallback(IAsyncResult ar)
        {
            if (listenerCts.Token.IsCancellationRequested) return;

            Socket client = listener.EndAccept(ar);
        }

        private void
    }
}
