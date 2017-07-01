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
using SharpCifs.Dcerpc.Msrpc;
using SharpCifs.Util.Sharpen;

namespace SharpCifs.Dcerpc
{
    public class DcerpcBinding
    {
        private static Hashtable _interfaces;

        static DcerpcBinding()
        {
            _interfaces = new Hashtable();
            _interfaces.Put("srvsvc", Srvsvc.GetSyntax());
            _interfaces.Put("lsarpc", Lsarpc.GetSyntax());
            _interfaces.Put("samr", Samr.GetSyntax());
            _interfaces.Put("netdfs", Netdfs.GetSyntax());
        }

        public static void AddInterface(string name, string syntax)
        {
            _interfaces.Put(name, syntax);
        }

        internal string Proto;

        internal string Server;

        internal string Endpoint;

        internal Hashtable Options;

        internal Uuid Uuid;

        internal int Major;

        internal int Minor;

        internal DcerpcBinding(string proto, string server)
        {
            this.Proto = proto;
            this.Server = server;
        }

        /// <exception cref="SharpCifs.Dcerpc.DcerpcException"></exception>
        internal virtual void SetOption(string key, object val)
        {
            if (key.Equals("endpoint"))
            {
                Endpoint = val.ToString().ToLower();
                if (Endpoint.StartsWith("\\pipe\\"))
                {
                    string iface = (string)_interfaces.Get(Runtime.Substring(Endpoint, 6));
                    if (iface != null)
                    {
                        int c;
                        int p;
                        c = iface.IndexOf(':');
                        p = iface.IndexOf('.', c + 1);
                        Uuid = new Uuid(Runtime.Substring(iface, 0, c));
                        Major = Convert.ToInt32(Runtime.Substring(iface, c + 1, p));
                        Minor = Convert.ToInt32(Runtime.Substring(iface, p + 1));
                        return;
                    }
                }
                throw new DcerpcException("Bad endpoint: " + Endpoint);
            }
            if (Options == null)
            {
                Options = new Hashtable();
            }
            Options.Put(key, val);
        }

        internal virtual object GetOption(string key)
        {
            if (key.Equals("endpoint"))
            {
                return Endpoint;
            }
            if (Options != null)
            {
                return Options.Get(key);
            }
            return null;
        }

        public override string ToString()
        {
            /*	
            string ret = proto + ":" + server + "[" + endpoint;
            if (options != null)
            {
                Iterator iter = (Iterator) options.Keys.GetEnumerator();
                while (iter.HasNext())
                {
                    object key = iter.Next();
                    object val = options.Get(key);
                    ret += "," + key + "=" + val;
                }
            }
            ret += "]";
            return ret; 
            */
            return null;
        }
    }
}
