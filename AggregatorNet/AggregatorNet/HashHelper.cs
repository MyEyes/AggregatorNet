using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace AggregatorNet
{
    public static class HashHelper
    {

        public static byte[] GetSHA256(string inputString)
        {
            using (HashAlgorithm algorithm = SHA256.Create())
                return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
        }

        public static string GetSHA256String(string inputString)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in GetSHA256(inputString))
                sb.Append(b.ToString("X2"));

            return sb.ToString();
        }

        public static byte[] GetSHA256FromFile(string path)
        {
            using (FileStream f = new FileStream(path, FileMode.Open, FileAccess.Read))
            using (HashAlgorithm algorithm = SHA256.Create())
                return algorithm.ComputeHash(f);
        }
        public static string GetSHA256StringFromFile(string path)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in GetSHA256FromFile(path))
                sb.Append(b.ToString("X2"));

            return sb.ToString();
        }
    }
}
