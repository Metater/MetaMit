using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;

namespace MetaMit.Cryptography.Asymmetric
{
    public static class Encryption
    {
        public static string Encrypt(string plainText, string publicKey, int keySize)
        {
            byte[] data = Encoding.UTF8.GetBytes(plainText);

            using (var rsa = new RSACryptoServiceProvider(keySize))
            {
                try
                {
                    // client encrypting data with public key issued by server                    
                    rsa.FromXmlString(publicKey.ToString());

                    byte[] encryptedData = rsa.Encrypt(data, true);

                    string cipherText = Convert.ToBase64String(encryptedData);

                    return cipherText;
                }
                finally
                {
                    rsa.PersistKeyInCsp = false;
                }
            }
        }
    }
}
