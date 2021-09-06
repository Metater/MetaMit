using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;
using System.Net.Sockets;
using System.Net;
using MetaMitStandard.Client;
using MetaMitStandard.Utils;

namespace MetaMitStandard
{
    public sealed class MetaMitClient
    {
        private ServerConnection serverConnection;

        private ConcurrentQueue<ClientEvent> eventQueue = new ConcurrentQueue<ClientEvent>();

        public event EventHandler<ConnectedEventArgs> Connected;
        public event EventHandler<DisconnectedEventArgs> Disconnected;
        public event EventHandler<DataReceivedEventArgs> DataReceived;
        public event EventHandler<DataSentEventArgs> DataSent;

        public MetaMitClient()
        {
            // Think about how to better handler socket options that may want to be conntrolled
            //https://docs.microsoft.com/en-us/dotnet/api/system.net.sockets.socket.nodelay?view=net-5.0
            //https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/xmldoc/recommended-tags-for-documentation-comments
        }

        /// <summary>
        /// Connect to a remote endpoint formed by the provided ip and port
        /// </summary>
        /// <remarks>
        /// If an endpoint cannot be formed, a disconnect event will be invoked without a connect event
        /// </remarks>
        /// <param name="ip">The ip of the remote endpoint</param>
        /// <param name="port">The port of the remote endpoint</param>
        public void Connect(string ip, int port, Func<Socket> setSocketOptions = null)
        {
            if (serverConnection != null) if (serverConnection.isActive) return;
            try
            {
                IPEndPoint ep = NetworkUtils.GetEndPoint(ip, port);
                Connect(ep);
            }
            catch (Exception)
            {
                QueueEvent(new DisconnectedEventArgs(DisconnectedReason.ExceptionOnConnect, $"Failed to form endpoint from ip: {ip} and port: {port} that were given"));
            }
        }
        /// <summary>
        /// Connect to a remote endpoint
        /// </summary>
        /// <remarks>
        /// If a connection fails, a disconnect event will be invoked without a connect event
        /// </remarks>
        /// <param name="ep">The remote endpoint to connect to</param>
        public void Connect(IPEndPoint ep, List<(SocketOptionLevel, SocketOptionName, bool)> setSocketOptions = null)
        {
            if (serverConnection != null) if (serverConnection.isActive) return;
            serverConnection = new ServerConnection
            {
                socket = new Socket(ep.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
            };
            if (setSocketOptions != null)
            {
                foreach((SocketOptionLevel, SocketOptionName, bool) setSocketOption in setSocketOptions)
                {
                    serverConnection.socket.SetSocketOption(setSocketOption.Item1, setSocketOption.Item2, setSocketOption.Item3);
                }
            }
            try
            {
                serverConnection.socket.BeginConnect(ep, new AsyncCallback(ConnectCallback), null);
            }
            catch (Exception e)
            {
                DisconnectServer(DisconnectedReason.ExceptionOnConnect, e.ToString(), false, true);
            }
        }

        public void Disconnect()
        {
            if (serverConnection.isActive)
                serverConnection.socket.BeginDisconnect(false, new AsyncCallback(DisconnectCallback), null);
        }

        public void Send(byte[] data, bool includeOverhead = true)
        {
            if (!serverConnection.isActive) return;
            byte[] packedData;
            if (includeOverhead) packedData = DataPacker.PackData(data, 0);
            else packedData = data;
            serverConnection.socket.BeginSend(packedData, 0, packedData.Length, SocketFlags.None, new AsyncCallback(SendCallback), null);
        }

        /// <summary>
        /// Polls all of the events in the event queue, call frequently, at least every 15ms
        /// </summary>
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
                case ClientEventType.DataSent:
                    DataSent?.Invoke(this, (DataSentEventArgs)clientEvent.clientEventArgs);
                    break;
            }
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                serverConnection.socket.EndConnect(ar);
                serverConnection.isActive = true;
                serverConnection.socket.BeginReceive(serverConnection.buffer, 0, ServerConnection.BufferSize, SocketFlags.None, new AsyncCallback(ReceiveCallback), null);
                QueueEvent(new ConnectedEventArgs());
            }
            catch (SocketException e)
            {
                DisconnectServer(DisconnectedReason.ExceptionOnConnect, e.ToString());
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            if (!serverConnection.isActive) return;
            try
            {
                int bytesReceived = serverConnection.socket.EndReceive(ar);
                if (bytesReceived > 0)
                {
                    serverConnection.bytesReceived += bytesReceived;
                    if (serverConnection.dataUnpacker.TryUnpackData(bytesReceived, serverConnection.buffer, out List<byte[]> parsedData, out List<ushort> sessionFlags))
                    {
                        foreach (ushort dataSessionFlags in sessionFlags)
                        {

                        }
                        foreach (byte[] data in parsedData)
                        {
                            serverConnection.packetsReceived++;
                            QueueEvent(new DataReceivedEventArgs(data));
                        }
                    }
                    serverConnection.socket.BeginReceive(serverConnection.buffer, 0, ServerConnection.BufferSize, SocketFlags.None, new AsyncCallback(ReceiveCallback), null);
                }
                else
                {
                    DisconnectServer(DisconnectedReason.ExceptionOnReceive, "Bytes received was less than or equal to 0");
                }
            }
            catch (SocketException e)
            {
                DisconnectServer(DisconnectedReason.ExceptionOnReceive, e.ToString());
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            if (!serverConnection.isActive) return;
            try
            {
                int bytesSent = serverConnection.socket.EndSend(ar);
                serverConnection.bytesSent += bytesSent;
                serverConnection.packetsSent++;
                QueueEvent(new DataSentEventArgs(bytesSent));
            }
            catch (Exception e)
            {
                DisconnectServer(DisconnectedReason.ExceptionOnSend, e.ToString());
            }
        }

        private void DisconnectCallback(IAsyncResult ar)
        {
            if (!serverConnection.isActive) return;
            try
            {
                serverConnection.socket.EndDisconnect(ar);
            }
            catch (SocketException)
            {

            }
            DisconnectServer(DisconnectedReason.Requested, "A client properly disconnected", false);
        }

        private void DisconnectServer(DisconnectedReason reason, string message, bool closeSocket = true, bool ignoreIsActive = false)
        {
            if (serverConnection.isActive || ignoreIsActive)
            {
                serverConnection.isActive = false;
                if (closeSocket) serverConnection.socket.Close();
                QueueEvent(new DisconnectedEventArgs(reason, message));
                serverConnection.Dispose();
            }
        }
    }
}
