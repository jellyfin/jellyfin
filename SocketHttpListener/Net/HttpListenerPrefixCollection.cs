using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace SocketHttpListener.Net
{
    public class HttpListenerPrefixCollection : ICollection<string>, IEnumerable<string>, IEnumerable
    {
        private List<string> _prefixes = new List<string>();
        private HttpListener _listener;

        private ILogger _logger;

        internal HttpListenerPrefixCollection(ILogger logger, HttpListener listener)
        {
            _logger = logger;
            _listener = listener;
        }

        public int Count => _prefixes.Count;

        public bool IsReadOnly => false;

        public bool IsSynchronized => false;

        public void Add(string uriPrefix)
        {
            _listener.CheckDisposed();
            //ListenerPrefix.CheckUri(uriPrefix);
            if (_prefixes.Contains(uriPrefix))
            {
                return;
            }

            _prefixes.Add(uriPrefix);
            if (_listener.IsListening)
            {
                HttpEndPointManager.AddPrefix(_logger, uriPrefix, _listener);
            }
        }

        public void AddRange(IEnumerable<string> uriPrefixes)
        {
            _listener.CheckDisposed();

            foreach (var uriPrefix in uriPrefixes)
            {
                if (_prefixes.Contains(uriPrefix))
                {
                    continue;
                }

                _prefixes.Add(uriPrefix);
                if (_listener.IsListening)
                {
                    HttpEndPointManager.AddPrefix(_logger, uriPrefix, _listener);
                }
            }
        }

        public void Clear()
        {
            _listener.CheckDisposed();
            _prefixes.Clear();
            if (_listener.IsListening)
            {
                HttpEndPointManager.RemoveListener(_logger, _listener);
            }
        }

        public bool Contains(string uriPrefix)
        {
            _listener.CheckDisposed();
            return _prefixes.Contains(uriPrefix);
        }

        public void CopyTo(string[] array, int offset)
        {
            _listener.CheckDisposed();
            _prefixes.CopyTo(array, offset);
        }

        public void CopyTo(Array array, int offset)
        {
            _listener.CheckDisposed();
            ((ICollection)_prefixes).CopyTo(array, offset);
        }

        public IEnumerator<string> GetEnumerator()
        {
            return _prefixes.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _prefixes.GetEnumerator();
        }

        public bool Remove(string uriPrefix)
        {
            _listener.CheckDisposed();
            if (uriPrefix == null)
            {
                throw new ArgumentNullException(nameof(uriPrefix));
            }

            bool result = _prefixes.Remove(uriPrefix);
            if (result && _listener.IsListening)
            {
                HttpEndPointManager.RemovePrefix(_logger, uriPrefix, _listener);
            }

            return result;
        }
    }
}
