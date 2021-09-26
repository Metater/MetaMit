using System;
using System.Collections.Generic;
using System.Text;

namespace MetaMitPlus.Server
{
    public struct ReceivePolicy
    {
        public EncryptionPolicy encryptionPolicy;
        public CompressionPolicy compressionPolicy;

        public ReceivePolicy(EncryptionPolicy encryptionPolicy, CompressionPolicy compressionPolicy)
        {
            this.encryptionPolicy = encryptionPolicy;
            this.compressionPolicy = compressionPolicy;
        }

        public enum EncryptionPolicy
        {
            No,
            Yes,
            Dual
        }
        public enum CompressionPolicy
        {
            No,
            Yes,
            Dual
        }
    }
}
