using MetaMitPlus.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;

namespace MetaMitPlus.Server
{
    public class ClientConnection : IDisposable
    {
        internal Socket socket;
        internal const int BufferSize = 65536;
        internal byte[] buffer = new byte[BufferSize];
        internal DataUnpacker dataUnpacker = new DataUnpacker();
        internal EncryptionPhase encryptionPhase = EncryptionPhase.None;
        internal Stopwatch encryptionInitiationStopwatch = new Stopwatch();
        internal RSAParameters rsaPrivateKey;
        internal byte[] aesKey;

        private MetaMitServer server;

        public bool IsActive { get; private set; } = false;
        public bool IsEncryptionActive => encryptionPhase == EncryptionPhase.Encrypted;
        public EndPoint RemoteEP => socket.RemoteEndPoint;

        public long bytesReceived = 0;
        public long bytesSent = 0;
        public long packetsReceived = 0;
        public long packetsSent = 0;

        public ReceivePolicy receivePolicy;
        public SendPolicy sendPolicy;

        public Guid guid = Guid.NewGuid();

        internal ClientConnection(MetaMitServer server)
        {
            this.server = server;
            receivePolicy = server.serverReceivePolicy;
            sendPolicy = server.serverSendPolicy;
        }

        internal void SetActive(bool value)
        {
            IsActive = value;
        }

        internal void Send(byte[] data, bool hasSessionFlags = false, byte sessionFlags = 0x00)
        {
            if (!IsActive) return;
            byte[] packedData = DataPacker.PackData(data, hasSessionFlags, sessionFlags);
            socket.BeginSend(packedData, 0, packedData.Length, SocketFlags.None, new AsyncCallback(server.SendCallback), this);
        }
        public void Send(byte[] data, SendOptions sendOptions)
        {
            bool hasSessionFlags = false;
            byte sessionFlags = 0x00;
            if (sendOptions == SendOptions.Compressed || sendOptions == SendOptions.EncryptedAndCompressed || (sendPolicy.hasDataLengthCompressionThreshhold && data.Length >= sendPolicy.dataLengthCompressionThreshold))
            {
                hasSessionFlags = true;
                data = CompressionUtils.Zip(data);
            }
            if (sendOptions == SendOptions.Encrypted || sendOptions == SendOptions.EncryptedAndCompressed)
            {
                hasSessionFlags = true;
                if (encryptionPhase != EncryptionPhase.Encrypted) throw new Exception("Cannot encrypt when it is not fully enabled");
                try
                {
                    data = CryptographyUtils.AESEncrypt(aesKey, data);
                }
                catch (Exception)
                {
                    server.DisconnectClient(this, ClientDisconnectedReason.Exception, "Exception on send: Could not encrypt data with key provided by client");
                    return;
                }
            }
            Send(data, hasSessionFlags, sessionFlags);
        }

        public void Disconnect()
        {
            if (!IsActive) return;
            socket.BeginDisconnect(false, new AsyncCallback(server.DisconnectCallback), this);
        }

        public void InitiateEncryption()
        {
            if (encryptionPhase != EncryptionPhase.None) throw new Exception("Encryption already enabled or was encrypting");
            encryptionInitiationStopwatch.Start();

            (RSAParameters, RSAParameters) keys = CryptographyUtils.GenRSAKeyPair(2048);
            rsaPrivateKey = keys.Item2;
            byte[] rsaPublicKeyBytes = Encoding.UTF8.GetBytes(CryptographyUtils.GetRSAPublicKeyString(keys.Item1));

            Send(rsaPublicKeyBytes, true, 0x00);
            encryptionPhase = EncryptionPhase.SentRSAPublicKey;
            server.encryptingClients.Enqueue(this);
        }

        public void Dispose()
        {
            socket.Dispose();
        }

        internal enum EncryptionPhase
        {
            None,
            SentRSAPublicKey,
            Encrypted
        }
    }
}
