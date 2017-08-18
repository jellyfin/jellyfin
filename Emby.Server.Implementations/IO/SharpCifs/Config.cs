// This code is derived from jcifs smb client library <jcifs at samba dot org>
// Ported by J. Arturo <webmaster at komodosoft dot net>
//  
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
// 
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
using System;
using System.IO;
using System.Net;
using System.Security;
using SharpCifs.Util;
using SharpCifs.Util.Sharpen;

namespace SharpCifs
{
    /// <summary>
    /// This class uses a static
    /// <see cref="Properties">Sharpen.Properties</see>
    /// to act
    /// as a cental repository for all jCIFS configuration properties. It cannot be
    /// instantiated. Similar to <code>System</code> properties the namespace
    /// is global therefore property names should be unique. Before use,
    /// the <code>load</code> method should be called with the name of a
    /// <code>Properties</code> file (or <code>null</code> indicating no
    /// file) to initialize the <code>Config</code>. The <code>System</code>
    /// properties will then populate the <code>Config</code> as well potentially
    /// overwriting properties from the file. Thus properties provided on the
    /// commandline with the <code>-Dproperty.name=value</code> VM parameter
    /// will override properties from the configuration file.
    /// 
    /// There are several ways to set jCIFS properties. See
    /// the <a href="../overview-summary.html#scp">overview page of the API
    /// documentation</a> for details.
    /// </summary>
    public class Config
    {        

        /// <summary>The static <code>Properties</code>.</summary>
        /// <remarks>The static <code>Properties</code>.</remarks>
        private static Properties _prp = new Properties();

        private static LogStream _log;

        public static string DefaultOemEncoding = "UTF-8"; //"Cp850";

        static Config()
        {
            int level;
            FileInputStream fis = null;
            _log = LogStream.GetInstance();

            try
            {
                string filename = Runtime.GetProperty("jcifs.properties");
                if (filename != null && filename.Length > 1)
                {
                    fis = new FileInputStream(filename);
                }
                Load(fis);
                if (fis != null)
                {
                    fis.Close();
                }
            }
            catch (IOException ioe)
            {
                if (_log.Level > 0)
                {
                    Runtime.PrintStackTrace(ioe, _log);
                }
            }

            if ((level = GetInt("jcifs.util.loglevel", -1)) != -1)
            {
                _log.SetLevel(level);
            }

            try
            {
                Runtime.GetBytesForString(string.Empty, DefaultOemEncoding);
            }
            catch (Exception ex)
            {
                if (_log.Level >= 2)
                {
                    _log.WriteLine("WARNING: The default OEM encoding " + DefaultOemEncoding + " does not appear to be supported by this JRE. The default encoding will be US-ASCII."
                        );
                }

                //DEFAULT_OEM_ENCODING = "US-ASCII";
            }

            if (_log.Level >= 4)
            {
                try
                {
                    _prp.Store(_log);
                }
                catch (IOException)
                {
                }
            }
        }

        /// <summary>
        /// This static method registers the SMB URL protocol handler which is
        /// required to use SMB URLs with the <tt>java.net.URL</tt> class.
        /// </summary>
        /// <remarks>
        /// This static method registers the SMB URL protocol handler which is
        /// required to use SMB URLs with the <tt>java.net.URL</tt> class. If this
        /// method is not called before attempting to create an SMB URL with the
        /// URL class the following exception will occur:
        /// <blockquote><pre>
        /// Exception MalformedURLException: unknown protocol: smb
        /// at java.net.URL.<init>(URL.java:480)
        /// at java.net.URL.<init>(URL.java:376)
        /// at java.net.URL.<init>(URL.java:330)
        /// at jcifs.smb.SmbFile.<init>(SmbFile.java:355)
        /// ...
        /// </pre><blockquote>
        /// </remarks>
        public static void RegisterSmbURLHandler()
        {
            throw new NotImplementedException();
        }

        // supress javadoc constructor summary by removing 'protected'
        /// <summary>Set the default properties of the static Properties used by <tt>Config</tt>.
        /// 	</summary>
        /// <remarks>
        /// Set the default properties of the static Properties used by <tt>Config</tt>. This permits
        /// a different Properties object/file to be used as the source of properties for
        /// use by the jCIFS library. The Properties must be set <i>before jCIFS
        /// classes are accessed</i> as most jCIFS classes load properties statically once.
        /// Using this method will also override properties loaded
        /// using the <tt>-Djcifs.properties=</tt> commandline parameter.
        /// </remarks>
        public static void SetProperties(Properties prp)
        {
            Config._prp = new Properties(prp);
            try
            {
                Config._prp.PutAll(Runtime.GetProperties());
            }
            catch (SecurityException)
            {
                if (_log.Level > 1)
                {
                    _log.WriteLine("SecurityException: jcifs will ignore System properties");
                }
            }
        }

        /// <summary>
        /// Load the <code>Config</code> with properties from the stream
        /// <code>in</code> from a <code>Properties</code> file.
        /// </summary>
        /// <remarks>
        /// Load the <code>Config</code> with properties from the stream
        /// <code>in</code> from a <code>Properties</code> file.
        /// </remarks>
        /// <exception cref="System.IO.IOException"></exception>
        public static void Load(InputStream input)
        {
            if (input != null)
            {
                _prp.Load(input);
            }
            try
            {
                _prp.PutAll(Runtime.GetProperties());
            }
            catch (SecurityException)
            {
                if (_log.Level > 1)
                {
                    _log.WriteLine("SecurityException: jcifs will ignore System properties");
                }
            }
        }

        /// <exception cref="System.IO.IOException"></exception>
        public static void Store(OutputStream output, string header)
        {
            _prp.Store(output);
        }

        /// <summary>Add a property.</summary>
        /// <remarks>Add a property.</remarks>
        public static void SetProperty(string key, string value)
        {
            _prp.SetProperty(key, value);
        }

        /// <summary>Retrieve a property as an <code>Object</code>.</summary>
        /// <remarks>Retrieve a property as an <code>Object</code>.</remarks>
        public static object Get(string key)
        {
            return _prp.GetProperty(key);
        }

        /// <summary>Retrieve a <code>String</code>.</summary>
        /// <remarks>
        /// Retrieve a <code>String</code>. If the key cannot be found,
        /// the provided <code>def</code> default parameter will be returned.
        /// </remarks>
        public static string GetProperty(string key, string def)
        {
            return (string)_prp.GetProperty(key, def);
        }

        /// <summary>Retrieve a <code>String</code>.</summary>
        /// <remarks>Retrieve a <code>String</code>. If the property is not found, <code>null</code> is returned.
        /// 	</remarks>
        public static string GetProperty(string key)
        {
            return (string)_prp.GetProperty(key);
        }

        /// <summary>Retrieve an <code>int</code>.</summary>
        /// <remarks>
        /// Retrieve an <code>int</code>. If the key does not exist or
        /// cannot be converted to an <code>int</code>, the provided default
        /// argument will be returned.
        /// </remarks>
        public static int GetInt(string key, int def)
        {
            string s = (string)_prp.GetProperty(key);
            if (s != null)
            {
                try
                {
                    def = Convert.ToInt32(s);
                }
                catch (FormatException nfe)
                {
                    if (_log.Level > 0)
                    {
                        Runtime.PrintStackTrace(nfe, _log);
                    }
                }
            }
            return def;
        }

        /// <summary>Retrieve an <code>int</code>.</summary>
        /// <remarks>Retrieve an <code>int</code>. If the property is not found, <code>-1</code> is returned.
        /// 	</remarks>
        public static int GetInt(string key)
        {
            string s = (string)_prp.GetProperty(key);
            int result = -1;
            if (s != null)
            {
                try
                {
                    result = Convert.ToInt32(s);
                }
                catch (FormatException nfe)
                {
                    if (_log.Level > 0)
                    {
                        Runtime.PrintStackTrace(nfe, _log);
                    }
                }
            }
            return result;
        }

        /// <summary>Retrieve a <code>long</code>.</summary>
        /// <remarks>
        /// Retrieve a <code>long</code>. If the key does not exist or
        /// cannot be converted to a <code>long</code>, the provided default
        /// argument will be returned.
        /// </remarks>
        public static long GetLong(string key, long def)
        {
            string s = (string)_prp.GetProperty(key);
            if (s != null)
            {
                try
                {
                    def = long.Parse(s);
                }
                catch (FormatException nfe)
                {
                    if (_log.Level > 0)
                    {
                        Runtime.PrintStackTrace(nfe, _log);
                    }
                }
            }
            return def;
        }

        /// <summary>Retrieve an <code>InetAddress</code>.</summary>
        /// <remarks>
        /// Retrieve an <code>InetAddress</code>. If the address is not
        /// an IP address and cannot be resolved <code>null</code> will
        /// be returned.
        /// </remarks>
        public static IPAddress GetInetAddress(string key, IPAddress def)
        {
            string addr = (string)_prp.GetProperty(key);
            if (addr != null)
            {
                try
                {
                    def = Extensions.GetAddressByName(addr);
                }
                catch (UnknownHostException uhe)
                {
                    if (_log.Level > 0)
                    {
                        _log.WriteLine(addr);
                        Runtime.PrintStackTrace(uhe, _log);
                    }
                }
            }
            return def;
        }

        public static IPAddress GetLocalHost()
        {
            string addr = (string)_prp.GetProperty("jcifs.smb.client.laddr");
            if (addr != null)
            {
                try
                {
                    return Extensions.GetAddressByName(addr);
                }
                catch (UnknownHostException uhe)
                {
                    if (_log.Level > 0)
                    {
                        _log.WriteLine("Ignoring jcifs.smb.client.laddr address: " + addr);
                        Runtime.PrintStackTrace(uhe, _log);
                    }
                }
            }
            return null;
        }

        /// <summary>Retrieve a boolean value.</summary>
        /// <remarks>Retrieve a boolean value. If the property is not found, the value of <code>def</code> is returned.
        /// 	</remarks>
        public static bool GetBoolean(string key, bool def)
        {
            string b = GetProperty(key);
            if (b != null)
            {
                def = b.ToLower().Equals("true");
            }
            return def;
        }

        /// <summary>
        /// Retrieve an array of <tt>InetAddress</tt> created from a property
        /// value containting a <tt>delim</tt> separated list of hostnames and/or
        /// ipaddresses.
        /// </summary>
        /// <remarks>
        /// Retrieve an array of <tt>InetAddress</tt> created from a property
        /// value containting a <tt>delim</tt> separated list of hostnames and/or
        /// ipaddresses.
        /// </remarks>
        public static IPAddress[] GetInetAddressArray(string key, string delim, IPAddress
            [] def)
        {
            string p = GetProperty(key);
            if (p != null)
            {
                StringTokenizer tok = new StringTokenizer(p, delim);
                int len = tok.CountTokens();
                IPAddress[] arr = new IPAddress[len];
                for (int i = 0; i < len; i++)
                {
                    string addr = tok.NextToken();
                    try
                    {
                        arr[i] = Extensions.GetAddressByName(addr);
                    }
                    catch (UnknownHostException uhe)
                    {
                        if (_log.Level > 0)
                        {
                            _log.WriteLine(addr);
                            Runtime.PrintStackTrace(uhe, _log);
                        }
                        return def;
                    }
                }
                return arr;
            }
            return def;
        }
    }
}
