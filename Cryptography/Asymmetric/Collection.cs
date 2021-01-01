using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;

namespace MetaMit.Cryptography.Asymmetric
{
    public static class Collection
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
    }
}
