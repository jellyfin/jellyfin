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

        /// <summary>
        /// Examine a list of strings assumed to be file paths to see if it contains a parent of 
        /// the provided path.
        /// </summary>
        /// <param name="lst"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool ContainsParentFolder(this List<string> lst, string path)
        {
            path = path.TrimEnd('\\');
            foreach (var str in lst)
            {
                //this should be a little quicker than examining each actual parent folder...
                var compare = str.TrimEnd('\\');
                if (path.Equals(compare,StringComparison.OrdinalIgnoreCase) 
                    || (path.StartsWith(compare, StringComparison.OrdinalIgnoreCase) && path[compare.Length] == '\\')) return true;
            }
            return false;
        }

        /// <summary>
        /// Helper method for Dictionaries since they throw on not-found keys
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <param name="dictionary"></param>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static U GetValueOrDefault<T, U>(this Dictionary<T, U> dictionary, T key, U defaultValue)
        {
            U val;
            if (!dictionary.TryGetValue(key, out val))
            {
                val = defaultValue;
            }
            return val;

        }

    }
}
