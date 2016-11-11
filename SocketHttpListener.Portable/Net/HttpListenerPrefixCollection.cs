using System;
using System.Collections;
using System.Collections.Generic;
using MediaBrowser.Model.Logging;

namespace SocketHttpListener.Net
{
    public class HttpListenerPrefixCollection : ICollection<string>, IEnumerable<string>, IEnumerable
    {
        List<string> prefixes = new List<string>();
        HttpListener listener;

        private ILogger _logger;

        internal HttpListenerPrefixCollection(ILogger logger, HttpListener listener)
        {
            _logger = logger;
            this.listener = listener;
        }

        public int Count
        {
            get { return prefixes.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool IsSynchronized
        {
            get { return false; }
        }

        public void Add(string uriPrefix)
        {
            listener.CheckDisposed();
            ListenerPrefix.CheckUri(uriPrefix);
            if (prefixes.Contains(uriPrefix))
                return;

            prefixes.Add(uriPrefix);
            if (listener.IsListening)
                EndPointManager.AddPrefix(_logger, uriPrefix, listener);
        }

        public void Clear()
        {
            listener.CheckDisposed();
            prefixes.Clear();
            if (listener.IsListening)
                EndPointManager.RemoveListener(_logger, listener);
        }

        public bool Contains(string uriPrefix)
        {
            listener.CheckDisposed();
            return prefixes.Contains(uriPrefix);
        }

        public void CopyTo(string[] array, int offset)
        {
            listener.CheckDisposed();
            prefixes.CopyTo(array, offset);
        }

        public void CopyTo(Array array, int offset)
        {
            listener.CheckDisposed();
            ((ICollection)prefixes).CopyTo(array, offset);
        }

        public IEnumerator<string> GetEnumerator()
        {
            return prefixes.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return prefixes.GetEnumerator();
        }

        public bool Remove(string uriPrefix)
        {
            listener.CheckDisposed();
            if (uriPrefix == null)
                throw new ArgumentNullException("uriPrefix");

            bool result = prefixes.Remove(uriPrefix);
            if (result && listener.IsListening)
                EndPointManager.RemovePrefix(_logger, uriPrefix, listener);

            return result;
        }
    }
}
