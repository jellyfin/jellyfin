using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace SharpCifs.Util.Sharpen
{
    public class Runtime
    {
        private static Runtime _instance;
        private List<ShutdownHook> _shutdownHooks = new List<ShutdownHook>();

        internal void AddShutdownHook(IRunnable r)
        {
            ShutdownHook item = new ShutdownHook();
            item.Runnable = r;
            _shutdownHooks.Add(item);
        }

        internal int AvailableProcessors()
        {
            return Environment.ProcessorCount;
        }

        public static long CurrentTimeMillis()
        {
            return DateTime.UtcNow.ToMillisecondsSinceEpoch();
        }

        static Hashtable _properties;

        public static Hashtable GetProperties()
        {
            if (_properties == null)
            {
                _properties = new Hashtable();
                _properties["jgit.fs.debug"] = "false";
                _properties["file.encoding"] = "UTF-8";
                if (Path.DirectorySeparatorChar != '\\')
                    _properties["os.name"] = "Unix";
                else
                    _properties["os.name"] = "Windows";
            }
            return _properties;
        }

        public static string GetProperty(string key)
        {
            if (GetProperties().Keys.Contains(key))
            {
                return ((string)GetProperties()[key]);
            }
            return null;
        }

        public static void SetProperty(string key, string value)
        {
            GetProperties()[key] = value;
        }

        public static Runtime GetRuntime()
        {
            if (_instance == null)
            {
                _instance = new Runtime();
            }
            return _instance;
        }

        public static int IdentityHashCode(object ob)
        {
            return RuntimeHelpers.GetHashCode(ob);
        }

        internal long MaxMemory()
        {
            return int.MaxValue;
        }

        private class ShutdownHook
        {
            public IRunnable Runnable;

            ~ShutdownHook()
            {
                Runnable.Run();
            }
        }

        public static void DeleteCharAt(StringBuilder sb, int index)
        {
            sb.Remove(index, 1);
        }

        public static byte[] GetBytesForString(string str)
        {
            return Encoding.UTF8.GetBytes(str);
        }

        public static byte[] GetBytesForString(string str, string encoding)
        {
            return Encoding.GetEncoding(encoding).GetBytes(str);
        }

        public static FieldInfo[] GetDeclaredFields(Type t)
        {
            throw new NotImplementedException("Type.GetFields not found on .NetStandard");
            //return t.GetFields (BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        }

        public static void NotifyAll(object ob)
        {
            Monitor.PulseAll(ob);
        }

        public static void Notify(object obj)
        {
            Monitor.Pulse(obj);
        }

        public static void PrintStackTrace(Exception ex)
        {
            Console.WriteLine(ex);
        }

        public static void PrintStackTrace(Exception ex, TextWriter tw)
        {
            tw.WriteLine(ex);
        }

        public static string Substring(string str, int index)
        {
            return str.Substring(index);
        }

        public static string Substring(string str, int index, int endIndex)
        {
            return str.Substring(index, endIndex - index);
        }

        public static void Wait(object ob)
        {
            Monitor.Wait(ob);
        }

        public static bool Wait(object ob, long milis)
        {
            return Monitor.Wait(ob, (int)milis);
        }

        public static Type GetType(string name)
        {
            throw new NotImplementedException(
                "AppDomain.CurrentDomain.GetAssemblies not found on .NetStandard");
            //foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies ()) {
            //	Type t = a.GetType (name);
            //	if (t != null)
            //		return t;
            //}
            //never used
            //throw new InvalidOperationException ("Type not found: " + name);
        }

        public static void SetCharAt(StringBuilder sb, int index, char c)
        {
            sb[index] = c;
        }

        public static bool EqualsIgnoreCase(string s1, string s2)
        {
            return s1.Equals(s2, StringComparison.CurrentCultureIgnoreCase);
        }

        internal static long NanoTime()
        {
            return Environment.TickCount * 1000 * 1000;
        }

        internal static int CompareOrdinal(string s1, string s2)
        {
            return string.CompareOrdinal(s1, s2);
        }

        public static string GetStringForBytes(byte[] chars)
        {
            return Encoding.UTF8.GetString(chars, 0, chars.Length);
        }

        public static string GetStringForBytes(byte[] chars, string encoding)
        {
            return GetEncoding(encoding).GetString(chars, 0, chars.Length);
        }

        public static string GetStringForBytes(byte[] chars, int start, int len)
        {
            return Encoding.UTF8.GetString(chars, start, len);
        }

        public static string GetStringForBytes(byte[] chars, int start, int len, string encoding)
        {
            return GetEncoding(encoding).Decode(chars, start, len);
        }

        public static Encoding GetEncoding(string name)
        {
            Encoding e = Encoding.GetEncoding(name.Replace('_', '-'));
            if (e is UTF8Encoding)
                return new UTF8Encoding(false, true);
            return e;
        }
    }
}
