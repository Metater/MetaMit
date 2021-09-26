using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;

namespace MetaMitPlus.Utils
{
    internal static class CryptographyUtils
    {
        internal static string GetRSAPublicKeyString(RSAParameters rsaPublicKey)
        {
            StringWriter sw = new StringWriter();
            XmlSerializer xs = new XmlSerializer(typeof(RSAParameters));
            xs.Serialize(sw, rsaPublicKey);
            return sw.ToString();
        }
        internal static RSAParameters GetRSAPublicKey(string rsaPublicKeyString)
        {
            StringReader sr = new StringReader(rsaPublicKeyString);
            XmlSerializer xs = new XmlSerializer(typeof(RSAParameters));
            return (RSAParameters)xs.Deserialize(sr);
        }
        internal static (RSAParameters, RSAParameters) GenRSAKeyPair(int keySize)
        {
            using (var csp = new RSACryptoServiceProvider(keySize))
            {
                try
                {
                    return (csp.ExportParameters(false), csp.ExportParameters(true));
                }
                finally
                {
                    csp.PersistKeyInCsp = false;
                }
            }
        }
        internal static byte[] RSAEncrypt(byte[] data, RSAParameters rsaPublicKey)
        {
            RSACryptoServiceProvider csp = new RSACryptoServiceProvider();
            csp.ImportParameters(rsaPublicKey);
            return csp.Encrypt(data, true);
        }
        internal static byte[] RSADecrypt(byte[] data, RSAParameters rsaPrivateKey)
        {
            RSACryptoServiceProvider csp = new RSACryptoServiceProvider();
            csp.ImportParameters(rsaPrivateKey);
            return csp.Decrypt(data, true);
        }

        internal static byte[] GenAESKey()
        {
            using (Aes aes = Aes.Create())
            {
                aes.GenerateIV();
                aes.GenerateKey();
                return aes.Key;
            }
        }
        internal static byte[] AESEncrypt(byte[] key, byte[] data)
        {
            byte[] iv = new byte[16];

            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, encryptor, CryptoStreamMode.Write))
                    {
                        using (BinaryWriter streamWriter = new BinaryWriter((Stream)cryptoStream))
                        {
                            streamWriter.Write(data);
                        }
                        return memoryStream.ToArray();
                    }
                }
            }
        }
        internal static byte[] AESDecrypt(byte[] key, byte[] data)
        {
            byte[] iv = new byte[16];
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (MemoryStream memoryStream = new MemoryStream(data))
                {
                    using (CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, decryptor, CryptoStreamMode.Read))
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            ((Stream)cryptoStream).CopyTo(ms);
                            return ms.ToArray();
                        }
                    }
                }
            }
        }
    }
}
