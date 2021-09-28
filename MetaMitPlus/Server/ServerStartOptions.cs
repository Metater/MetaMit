using MetaMitPlus.Utils;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MetaMitPlus.Server
{
    public struct ServerStartOptions
    {
        internal Socket listenerSocket;
        internal IPEndPoint localEp;
        internal ReceivePolicy receivePolicy;
        internal SendPolicy sendPolicy;
        internal int encryptionInitiationTimeout;

        private int port;

        public ServerStartOptions(int port)
        {
            listenerSocket = GetDefaultListenerSocket();
            localEp = new IPEndPoint(NetworkUtils.GetLocalIPv4(), port);
            receivePolicy = new ReceivePolicy(ReceivePolicy.EncryptionPolicy.Dual, ReceivePolicy.CompressionPolicy.Dual);
            sendPolicy = new SendPolicy(true, 65536);
            encryptionInitiationTimeout = 5000;
        }

        /// <summary>
        /// Gives the default listening socket for the server, useful for setting your own socket options
        /// </summary>
        /// <returns>Default listening socket for the server</returns>
        public static Socket GetDefaultListenerSocket()
        {
            Socket listenerSocket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
            listenerSocket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
            return listenerSocket;
        }

        internal IPEndPoint GetLocalEP()
        {
            return new 
        }
    }
}
