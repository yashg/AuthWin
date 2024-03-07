using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AuthWin
{
    internal class EncDec
    {
        static public String Encrypt(string plainText, string password)
        {
            /*For AES-256 
             * key size must be 256 bits or 32 bytes.
             * The IV must always be 16 bytes 
             * as AES is a 128 bit block cipher
             * */

            var encoding = new UTF8Encoding();
            //var Key = encoding.GetBytes(password);
            var Key = StringToByteArray(password); //password is 64 char hex string
            string md5 = MD5(password);
            var IV = StringToByteArray(md5);
            byte[] encrypted;

            using (var rj = new RijndaelManaged())
            {
                try
                {
                    rj.Padding = PaddingMode.PKCS7;
                    rj.Mode = CipherMode.CBC;
                    rj.BlockSize = 128;
                    rj.Key = Key;
                    rj.IV = IV;

                    var ms = new MemoryStream();

                    using (var cs = new CryptoStream(ms, rj.CreateEncryptor(Key, IV), CryptoStreamMode.Write))
                    {
                        using (var sr = new StreamWriter(cs))
                        {
                            sr.Write(plainText);
                            sr.Flush();
                            cs.FlushFinalBlock();
                        }
                        encrypted = ms.ToArray();
                    }
                }
                finally
                {
                    rj.Clear();
                }
            }

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < encrypted.Length; i++)
            {
                sb.Append(encrypted[i].ToString("X2"));
            }
            return sb.ToString().ToLower();
        }

        static public String Decrypt(string input, string password)
        {
            byte[] cypher = StringToByteArray(input);

            var sRet = "";

            var encoding = new UTF8Encoding();
            //var Key = encoding.GetBytes(password);
            var Key = StringToByteArray(password); //password is 64 char hex string
            string md5 = MD5(password);
            var IV = StringToByteArray(md5);

            using (var rj = new RijndaelManaged())
            {
                try
                {
                    rj.Padding = PaddingMode.PKCS7;
                    rj.Mode = CipherMode.CBC;
                    rj.BlockSize = 128;
                    rj.Key = Key;
                    rj.IV = IV;
                    var ms = new MemoryStream(cypher);

                    using (var cs = new CryptoStream(ms, rj.CreateDecryptor(Key, IV), CryptoStreamMode.Read))
                    {
                        using (var sr = new StreamReader(cs))
                        {
                            sRet = sr.ReadToEnd();
                        }
                    }
                }
                finally
                {
                    rj.Clear();
                }
            }

            return sRet;
        }

        public static string MD5(string input)
        {
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString().ToLower();
            }
        }

        public static string Sha256(string text)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(text);
            SHA256Managed hashstring = new SHA256Managed();
            byte[] hash = hashstring.ComputeHash(bytes);
            string hashString = string.Empty;
            foreach (byte x in hash)
            {
                hashString += String.Format("{0:x2}", x);
            }
            return hashString;
        }

        public static byte[] StringToByteArray(String hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }
    }
}
