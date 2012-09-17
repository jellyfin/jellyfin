using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace MediaBrowser.Common.Extensions
{
    public static class BaseExtensions
    {
        static MD5CryptoServiceProvider md5Provider = new MD5CryptoServiceProvider();

        public static Guid GetMD5(this string str)
        {
            lock (md5Provider)
            {
                return new Guid(md5Provider.ComputeHash(Encoding.Unicode.GetBytes(str)));
            }
        }

        public static bool ContainsStartsWith(this List<string> lst, string value)
        {
            foreach (var str in lst)
            {
                if (str.StartsWith(value, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }
    }
}
