using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;

namespace MetaMit.Cryptography.Asymmetric
{
    public static class Decryption
    {
        public static string Decrypt(string cipherText, string privateKey, int keySize)
        {
            byte[] data = Encoding.UTF8.GetBytes(cipherText);

            using (var rsa = new RSACryptoServiceProvider(keySize))
            {
                try
                {
                    // server decrypting data with private key                    
                    rsa.FromXmlString(privateKey);

                    byte[] encryptedData = Convert.FromBase64String(cipherText);
                    byte[] decryptedData = rsa.Decrypt(encryptedData, true);
                    string plainText = Encoding.UTF8.GetString(decryptedData);
                    return decryptedData.ToString();
                }
                finally
                {
                    rsa.PersistKeyInCsp = false;
                }
            }
        }
    }
}
