using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace CDR.DataHolder.API.Infrastructure.IdPermanence
{
    public class AesEncryptor
    {
        public static byte[] EncryptString(string key, string plainText)
        {
            byte[] iv = new byte[16];
            byte[] array;

            using (var aes = Aes.Create())
            {
                var keyHash = new MD5CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(key));
                aes.Key = keyHash.Take(24).ToArray();
                aes.IV = iv;

                using (var encryptedStream = new MemoryStream())
                {
                    using (ICryptoTransform encryptor = aes.CreateEncryptor())
                    {
                        using (CryptoStream cryptoStream = new CryptoStream(encryptedStream, encryptor, CryptoStreamMode.Write))
                        {
                            using (var originalByteStream = new MemoryStream(Encoding.UTF8.GetBytes(plainText).Compress()))
                            {
                                int data;
                                while ((data = originalByteStream.ReadByte()) != -1)
                                    cryptoStream.WriteByte((byte)data);
                            }
                        }
                    }

                    array = encryptedStream.ToArray();
                }
            }

            return array;
        }

        public static string DecryptString(string key, byte[] cipherText)
        {
            byte[] iv = new byte[16];
            byte[] buffer = cipherText; //Convert.FromBase64String(cipherText);

            using (var encryptedStream = new MemoryStream(buffer))
            {
                //stream where decrypted contents will be stored
                using (var decryptedStream = new MemoryStream())
                {
                    using (var aes = Aes.Create())
                    {
                        var keyHash = new MD5CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(key));
                        aes.Key = keyHash.Take(24).ToArray();
                        aes.IV = iv;

                        using (var decryptor = aes.CreateDecryptor())
                        {
                            //decrypt stream and write it to parent stream
                            using (var cryptoStream = new CryptoStream(encryptedStream, decryptor, CryptoStreamMode.Read))
                            {
                                int data;

                                while ((data = cryptoStream.ReadByte()) != -1)
                                    decryptedStream.WriteByte((byte)data);
                            }
                        }
                    }

                    //reset position in prep for reading
                    decryptedStream.Position = 0;
                    var payloadBytes = decryptedStream.ToArray();

                    return Encoding.UTF8.GetString(payloadBytes.Decompress());
                }
            }
        }
    }
}
