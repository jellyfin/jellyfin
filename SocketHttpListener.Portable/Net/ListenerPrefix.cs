using System;
using System.Net;
using MediaBrowser.Model.Net;

namespace SocketHttpListener.Net
{
    sealed class ListenerPrefix
    {
        string original;
        string host;
        ushort port;
        string path;
        bool secure;
        IpAddressInfo[] addresses;
        public HttpListener Listener;

        public ListenerPrefix(string prefix)
        {
            this.original = prefix;
            Parse(prefix);
        }

        public override string ToString()
        {
            return original;
        }

        public IpAddressInfo[] Addresses
        {
            get { return addresses; }
            set { addresses = value; }
        }
        public bool Secure
        {
            get { return secure; }
        }

        public string Host
        {
            get { return host; }
        }

        public int Port
        {
            get { return (int)port; }
        }

        public string Path
        {
            get { return path; }
        }

        // Equals and GetHashCode are required to detect duplicates in HttpListenerPrefixCollection.
        public override bool Equals(object o)
        {
            ListenerPrefix other = o as ListenerPrefix;
            if (other == null)
                return false;

            return (original == other.original);
        }

        public override int GetHashCode()
        {
            return original.GetHashCode();
        }

        void Parse(string uri)
        {
            ushort default_port = 80;
            if (uri.StartsWith("https://"))
            {
                default_port = 443;
                secure = true;
            }

            int length = uri.Length;
            int start_host = uri.IndexOf(':') + 3;
            if (start_host >= length)
                throw new ArgumentException("No host specified.");

            int colon = uri.IndexOf(':', start_host, length - start_host);
            int root;
            if (colon > 0)
            {
                host = uri.Substring(start_host, colon - start_host);
                root = uri.IndexOf('/', colon, length - colon);
                port = (ushort)Int32.Parse(uri.Substring(colon + 1, root - colon - 1));
                path = uri.Substring(root);
            }
            else
            {
                root = uri.IndexOf('/', start_host, length - start_host);
                host = uri.Substring(start_host, root - start_host);
                port = default_port;
                path = uri.Substring(root);
            }
            if (path.Length != 1)
                path = path.Substring(0, path.Length - 1);
        }

        public static void CheckUri(string uri)
        {
            if (uri == null)
                throw new ArgumentNullException("uriPrefix");

            if (!uri.StartsWith("http://") && !uri.StartsWith("https://"))
                throw new ArgumentException("Only 'http' and 'https' schemes are supported.");

            int length = uri.Length;
            int start_host = uri.IndexOf(':') + 3;
            if (start_host >= length)
                throw new ArgumentException("No host specified.");

            int colon = uri.IndexOf(':', start_host, length - start_host);
            if (start_host == colon)
                throw new ArgumentException("No host specified.");

            int root;
            if (colon > 0)
            {
                root = uri.IndexOf('/', colon, length - colon);
                if (root == -1)
                    throw new ArgumentException("No path specified.");

                try
                {
                    int p = Int32.Parse(uri.Substring(colon + 1, root - colon - 1));
                    if (p <= 0 || p >= 65536)
                        throw new Exception();
                }
                catch
                {
                    throw new ArgumentException("Invalid port.");
                }
            }
            else
            {
                root = uri.IndexOf('/', start_host, length - start_host);
                if (root == -1)
                    throw new ArgumentException("No path specified.");
            }

            if (uri[uri.Length - 1] != '/')
                throw new ArgumentException("The prefix must end with '/'");
        }
    }
}
