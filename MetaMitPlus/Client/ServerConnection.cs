using MetaMitPlus.Utils;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MetaMitPlus.Client
{
    public class ServerConnection : IDisposable
    {
        internal Socket socket;
        internal const int BufferSize = 65536;
        public byte[] buffer = new byte[BufferSize];
        internal DataUnpacker dataUnpacker = new DataUnpacker();
        internal EncryptionPhase encryptionPhase = EncryptionPhase.None;
        internal byte[] aesKey;

        private readonly MetaMitClient client;

        public bool IsActive { get; private set; } = false;
        public bool IsEncryptionActive => encryptionPhase == EncryptionPhase.Encrypted;
        public EndPoint RemoteEP => socket.RemoteEndPoint;

        public long bytesReceived = 0;
        public long bytesSent = 0;
        public long packetsReceived = 0;
        public long packetsSent = 0;

        internal ServerConnection(MetaMitClient client)
        {
            this.client = client;
        }

        internal void SetActive(bool value)
        {
            IsActive = value;
        }

        public void Dispose()
        {
            socket.Dispose();
        }

        internal enum EncryptionPhase
        {
            None,
            Encrypted
        }
    }
}
