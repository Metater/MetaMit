using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;

namespace MetaMit.Cryptography
{
    public static class Generic
    {
        public static class RSAUtils
        {
            public static string GetPublicKey(RSAParameters RSAParameters)
            {
                using (RSA rsa = RSA.Create(RSAParameters))
                {
                    return rsa.ToXmlString(false);
                }
            }
            public static string GetPrivateKey(RSAParameters RSAParameters)
            {
                using (RSA rsa = RSA.Create(RSAParameters))
                {
                    return rsa.ToXmlString(true);
                }
            }
            public static RSAParameters GenRSAParams(int keySize)
            {
                using (var rsa = new RSACryptoServiceProvider(keySize))
                {
                    try
                    {
                        return rsa.ExportParameters(true);
                    }
                    finally
                    {
                        rsa.PersistKeyInCsp = false;
                    }
                }
            }
        }
        public static class AESUtils
        {
            public static string GenKey()
            {
                string key = "";
                using (Aes aes = Aes.Create())
                {
                    aes.GenerateIV();
                    aes.GenerateKey();
                    key = Convert.ToBase64String(aes.Key);
                    aes.Dispose();
                }
                return key;
            }
        }
    }
}
