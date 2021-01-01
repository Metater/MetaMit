using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;

namespace MetaMit.Cryptography.Asymmetric
{
    public static class Generation
    {
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
}
