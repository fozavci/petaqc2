using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;

namespace PetaqService
{
    public static class Common
    {
        public static byte[] key = new byte[16];
        public static byte[] iv = new byte[16];


        public static T[] SubArray<T>(this T[] data, int index, int length)
        {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }

        public static string Encrypt(string text)
        {
            Array.Copy(Encoding.UTF8.GetBytes(Program.configuration["SESSION_KEY"]), key, 16);
            Array.Copy(Encoding.UTF8.GetBytes(Program.configuration["SESSION_IV"]), iv, 16);
            SymmetricAlgorithm algorithm = Aes.Create();
            ICryptoTransform transform = algorithm.CreateEncryptor(key, iv);
            byte[] inputbuffer = Encoding.UTF8.GetBytes(text);
            byte[] outputBuffer = transform.TransformFinalBlock(inputbuffer, 0, inputbuffer.Length);
            return Convert.ToBase64String(outputBuffer);
        }

        public static string Decrypt(string text)
        {
            Array.Copy(Encoding.UTF8.GetBytes(Program.configuration["SESSION_KEY"]), key, 16);
            Array.Copy(Encoding.UTF8.GetBytes(Program.configuration["SESSION_IV"]), iv, 16);
            byte[] inputbuffer = Convert.FromBase64String(text);
            SymmetricAlgorithm algorithm = Aes.Create();
            ICryptoTransform transform = algorithm.CreateDecryptor(key, iv);
            byte[] outputBuffer = transform.TransformFinalBlock(inputbuffer, 0, inputbuffer.Length);
            return Encoding.UTF8.GetString(outputBuffer);
        }
    }
}
