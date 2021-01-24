using System;
using System.Collections.Generic;
using System.Text;

namespace MetaMit.DataManipulation
{
    public static class Generic
    {
        public static string ForSending(string key, string data)
        {
            byte[] compressedData = Compression.Generic.ZipStringOutBytes(data);
            string encryptedData = Cryptography.Encryption.EncryptAESBytesOutString(key, compressedData);
            return encryptedData;
        }
        public static string ForReceiving(string key, string data)
        {
            byte[] decryptedData = Cryptography.Decryption.DecryptAESStringOutBytes(key, data);
            string decompressedData = Compression.Generic.UnzipBytesOutString(decryptedData);
            return decompressedData;
        }
    }
}
