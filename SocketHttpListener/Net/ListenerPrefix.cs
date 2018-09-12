using System;
using System.Net;
using MediaBrowser.Model.Net;

namespace SocketHttpListener.Net
{
    internal sealed class ListenerPrefix
    {
        private string _original;
        private string _host;
        private ushort _port;
        private string _path;
        private bool _secure;
        private IPAddress[] _addresses;
        internal HttpListener _listener;

        public ListenerPrefix(string prefix)
        {
            _original = prefix;
            Parse(prefix);
        }

        public override string ToString()
        {
            return _original;
        }

        public IPAddress[] Addresses
        {
            get { return _addresses; }
            set { _addresses = value; }
        }
        public bool Secure
        {
            get { return _secure; }
        }

        public string Host
        {
            get { return _host; }
        }

        public int Port
        {
            get { return _port; }
        }

        public string Path
        {
            get { return _path; }
        }

        // Equals and GetHashCode are required to detect duplicates in HttpListenerPrefixCollection.
        public override bool Equals(object o)
        {
            ListenerPrefix other = o as ListenerPrefix;
            if (other == null)
                return false;

            return (_original == other._original);
        }

        public override int GetHashCode()
        {
            return _original.GetHashCode();
        }

        private void Parse(string uri)
        {
            ushort default_port = 80;
            if (uri.StartsWith("https://"))
            {
                default_port = 443;
                _secure = true;
            }

            int length = uri.Length;
            int start_host = uri.IndexOf(':') + 3;
            if (start_host >= length)
                throw new ArgumentException("net_listener_host");

            int colon = uri.IndexOf(':', start_host, length - start_host);
            int root;
            if (colon > 0)
            {
                _host = uri.Substring(start_host, colon - start_host);
                root = uri.IndexOf('/', colon, length - colon);
                _port = (ushort)int.Parse(uri.Substring(colon + 1, root - colon - 1));
                _path = uri.Substring(root);
            }
            else
            {
                root = uri.IndexOf('/', start_host, length - start_host);
                _host = uri.Substring(start_host, root - start_host);
                _port = default_port;
                _path = uri.Substring(root);
            }
            if (_path.Length != 1)
                _path = _path.Substring(0, _path.Length - 1);
        }
    }
}
