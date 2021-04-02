using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;
using System.Net.Sockets;
using System.Net;
using MetaMitStandard.Client;

namespace MetaMitStandard
{
    public sealed class MetaMitClient
    {
        private ServerConnection serverConnection = new ServerConnection();

        private ConcurrentQueue<ClientEvent> eventQueue = new ConcurrentQueue<ClientEvent>();

        public event EventHandler<ConnectedEventArgs> Connected;
        public event EventHandler<DisconnectedEventArgs> Disconnected;
        public event EventHandler<DataReceivedEventArgs> DataReceived;

        public MetaMitClient()
        {

        }

        public void Connect(IPEndPoint ep)
        {
            serverConnection.socket = new Socket(ep.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            serverConnection.socket.BeginConnect(ep, new AsyncCallback(ConnectCallback), null);
        }

        public void Disconnect()
        {

        }

        public void Send(byte[] data)
        {
            byte[] length = BitConverter.GetBytes((ushort)data.Length);
            byte[] sessionFlags = BitConverter.GetBytes((ushort)0);
            byte[] rv = new byte[length.Length + sessionFlags.Length + data.Length];
            Buffer.BlockCopy(length, 0, rv, 0, length.Length);
            Buffer.BlockCopy(sessionFlags, 0, rv, length.Length, sessionFlags.Length);
            Buffer.BlockCopy(data, 0, rv, length.Length + sessionFlags.Length, data.Length);
            serverConnection.socket.BeginSend(rv, 0, rv.Length, SocketFlags.None, new AsyncCallback(SendCallback), null);
        }

        public void PollEvents()
        {
            int queuedEventsCount = eventQueue.Count;
            for (int i = 0; i < queuedEventsCount; i++)
            {
                if (eventQueue.TryDequeue(out ClientEvent clientEvent))
                    ProcessQueuedEvent(clientEvent);
                else
                    break;
            }
        }

        private void QueueEvent(ClientEventArgs clientEventArgs)
        {
            eventQueue.Enqueue(new ClientEvent(clientEventArgs));
        }

        private void ProcessQueuedEvent(ClientEvent clientEvent)
        {
            switch (clientEvent.clientEventArgs.eventType)
            {
                case ClientEventType.Connected:
                    Connected?.Invoke(this, (ConnectedEventArgs)clientEvent.clientEventArgs);
                    break;
                case ClientEventType.Disconnected:
                    Disconnected?.Invoke(this, (DisconnectedEventArgs)clientEvent.clientEventArgs);
                    break;
                case ClientEventType.DataReceived:
                    DataReceived?.Invoke(this, (DataReceivedEventArgs)clientEvent.clientEventArgs);
                    break;
            }
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                serverConnection.socket.EndConnect(ar);
                serverConnection.socket.BeginReceive(serverConnection.buffer, 0, ServerConnection.BufferSize, SocketFlags.None, new AsyncCallback(ReceiveCallback), null);
                QueueEvent(new ConnectedEventArgs());
            }
            catch (SocketException e)
            {
                ForceDisconnectServer(DisconnectedReason.ExceptionOnConnect, e.ToString());
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                int bytesReceived = serverConnection.socket.EndReceive(ar);
                if (bytesReceived > 0)
                {
                    serverConnection.bytesReceived += bytesReceived;
                    if (serverConnection.dataParser.TryParseData(bytesReceived, serverConnection.buffer, out byte[] builtData))
                    {
                        QueueEvent(new DataReceivedEventArgs(builtData));
                    }
                }
            }
            catch (SocketException e)
            {
                ForceDisconnectServer(DisconnectedReason.ExceptionOnReceive, e.ToString());
            }
        }

        private void SendCallback(IAsyncResult ar) // Add events here later
        {
            try
            {
                int bytesSent = serverConnection.socket.EndSend(ar);
                serverConnection.bytesSent += bytesSent;
                Console.WriteLine("DASASsadsadaaaaaaaaaaaaaaaaaa");
            }
            catch (Exception e)
            {
                ForceDisconnectServer(DisconnectedReason.ExceptionOnSend, e.ToString());
            }
        }

        private void ForceDisconnectServer(DisconnectedReason reason, string message)
        {
            serverConnection.socket.Close();
            QueueEvent(new DisconnectedEventArgs(reason, message));
        }
    }
}
