using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.IO;

namespace MetaMit.Cryptography
{
    public static class Decryption
    {
        public static string DecryptRSAString(string cipherText, string privateKey, int keySize)
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
        public static string DecryptAESStringOutString(string key, string cipherText)
        {
            // Key is hex 32 characters

            byte[] iv = new byte[16]; // Block size of algorithm
            byte[] buffer = Convert.FromBase64String(cipherText);

            using (Aes aes = Aes.Create())
            {
                aes.Key = Convert.FromBase64String(key);
                //aes.Key = Encoding.UTF8.GetBytes(key);
                aes.IV = iv;
                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (MemoryStream memoryStream = new MemoryStream(buffer)) {
                    using (CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, decryptor, CryptoStreamMode.Read)) {
                        using (StreamReader streamReader = new StreamReader((Stream)cryptoStream)) {
                            return streamReader.ReadToEnd();
                        }
                    }
                }
            }
        }
        public static byte[] DecryptAESStringOutBytes(string key, string cipherText)
        {
            // Key is hex 32 characters

            byte[] iv = new byte[16]; // Block size of algorithm
            byte[] buffer = Convert.FromBase64String(cipherText);

            using (Aes aes = Aes.Create())
            {
                aes.Key = Convert.FromBase64String(key);
                //aes.Key = Encoding.UTF8.GetBytes(key);
                aes.IV = iv;
                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (MemoryStream memoryStream = new MemoryStream(buffer))
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
