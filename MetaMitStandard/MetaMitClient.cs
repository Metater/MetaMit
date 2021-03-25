using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using MetaMitStandard.Client;

namespace MetaMitStandard
{
    public sealed class MetaMitClient
    {
        private Socket socket;
        private const int BufferSize = 1024;
        private byte[] buffer = new byte[BufferSize];
        private StringBuilder sb = new StringBuilder();

        public event EventHandler<ConnectedEventArgs> Connected;
        public event EventHandler<DisconnectedEventArgs> Disconnected;
        public event EventHandler<DataReceivedEventArgs> DataReceived;

        public MetaMitClient()
        {

        }

        public void Connect(IPEndPoint ep)
        {
            socket = new Socket(ep.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.BeginConnect(ep, new AsyncCallback(ConnectCallback), null);
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                socket.EndConnect(ar);
                socket.BeginReceive(buffer, 0, BufferSize, SocketFlags.None, new AsyncCallback(ReceiveCallback), null);
                Connected?.Invoke(this, new ConnectedEventArgs());
            }
            catch (SocketException e)
            {
                Disconnected?.Invoke(this, new DisconnectedEventArgs(DisconnectedReason.SocketException, e.ToString()));
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {

        }
    }
}
