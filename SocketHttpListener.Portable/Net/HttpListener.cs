using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Cryptography;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Text;
using SocketHttpListener.Primitives;

namespace SocketHttpListener.Net
{
    public sealed class HttpListener : IDisposable
    {
        internal ICryptoProvider CryptoProvider { get; private set; }
        internal IStreamFactory StreamFactory { get; private set; }
        internal ISocketFactory SocketFactory { get; private set; }
        internal IFileSystem FileSystem { get; private set; }
        internal ITextEncoding TextEncoding { get; private set; }
        internal IMemoryStreamFactory MemoryStreamFactory { get; private set; }
        internal INetworkManager NetworkManager { get; private set; }

        public bool EnableDualMode { get; set; }

        AuthenticationSchemes auth_schemes;
        HttpListenerPrefixCollection prefixes;
        AuthenticationSchemeSelector auth_selector;
        string realm;
        bool unsafe_ntlm_auth;
        bool listening;
        bool disposed;

        Dictionary<HttpListenerContext, HttpListenerContext> registry;   // Dictionary<HttpListenerContext,HttpListenerContext> 
        Dictionary<HttpConnection, HttpConnection> connections;
        private ILogger _logger;
        private ICertificate _certificate;

        public Action<HttpListenerContext> OnContext { get; set; }

        public HttpListener(ILogger logger, ICryptoProvider cryptoProvider, IStreamFactory streamFactory, ISocketFactory socketFactory, INetworkManager networkManager, ITextEncoding textEncoding, IMemoryStreamFactory memoryStreamFactory, IFileSystem fileSystem)
        {
            _logger = logger;
            CryptoProvider = cryptoProvider;
            StreamFactory = streamFactory;
            SocketFactory = socketFactory;
            NetworkManager = networkManager;
            TextEncoding = textEncoding;
            MemoryStreamFactory = memoryStreamFactory;
            FileSystem = fileSystem;
            prefixes = new HttpListenerPrefixCollection(logger, this);
            registry = new Dictionary<HttpListenerContext, HttpListenerContext>();
            connections = new Dictionary<HttpConnection, HttpConnection>();
            auth_schemes = AuthenticationSchemes.Anonymous;
        }

        public HttpListener(ICertificate certificate, ICryptoProvider cryptoProvider, IStreamFactory streamFactory, ISocketFactory socketFactory, INetworkManager networkManager, ITextEncoding textEncoding, IMemoryStreamFactory memoryStreamFactory, IFileSystem fileSystem)
            :this(new NullLogger(), certificate, cryptoProvider, streamFactory, socketFactory, networkManager, textEncoding, memoryStreamFactory, fileSystem)
        {
        }

        public HttpListener(ILogger logger, ICertificate certificate, ICryptoProvider cryptoProvider, IStreamFactory streamFactory, ISocketFactory socketFactory, INetworkManager networkManager, ITextEncoding textEncoding, IMemoryStreamFactory memoryStreamFactory, IFileSystem fileSystem)
            : this(logger, cryptoProvider, streamFactory, socketFactory, networkManager, textEncoding, memoryStreamFactory, fileSystem)
        {
            _certificate = certificate;
        }

        public void LoadCert(ICertificate cert)
        {
            _certificate = cert;
        }

        // TODO: Digest, NTLM and Negotiate require ControlPrincipal
        public AuthenticationSchemes AuthenticationSchemes
        {
            get { return auth_schemes; }
            set
            {
                CheckDisposed();
                auth_schemes = value;
            }
        }

        public AuthenticationSchemeSelector AuthenticationSchemeSelectorDelegate
        {
            get { return auth_selector; }
            set
            {
                CheckDisposed();
                auth_selector = value;
            }
        }

        public bool IsListening
        {
            get { return listening; }
        }

        public static bool IsSupported
        {
            get { return true; }
        }

        public HttpListenerPrefixCollection Prefixes
        {
            get
            {
                CheckDisposed();
                return prefixes;
            }
        }

        // TODO: use this
        public string Realm
        {
            get { return realm; }
            set
            {
                CheckDisposed();
                realm = value;
            }
        }

        public bool UnsafeConnectionNtlmAuthentication
        {
            get { return unsafe_ntlm_auth; }
            set
            {
                CheckDisposed();
                unsafe_ntlm_auth = value;
            }
        }

        //internal IMonoSslStream CreateSslStream(Stream innerStream, bool ownsStream, MSI.MonoRemoteCertificateValidationCallback callback)
        //{
        //    lock (registry)
        //    {
        //        if (tlsProvider == null)
        //            tlsProvider = MonoTlsProviderFactory.GetProviderInternal();
        //        if (tlsSettings == null)
        //            tlsSettings = MSI.MonoTlsSettings.CopyDefaultSettings();
        //        if (tlsSettings.RemoteCertificateValidationCallback == null)
        //            tlsSettings.RemoteCertificateValidationCallback = callback;
        //        return tlsProvider.CreateSslStream(innerStream, ownsStream, tlsSettings);
        //    }
        //}

        internal ICertificate Certificate
        {
            get { return _certificate; }
        }

        public void Abort()
        {
            if (disposed)
                return;

            if (!listening)
            {
                return;
            }

            Close(true);
        }

        public void Close()
        {
            if (disposed)
                return;

            if (!listening)
            {
                disposed = true;
                return;
            }

            Close(true);
            disposed = true;
        }

        void Close(bool force)
        {
            CheckDisposed();
            EndPointManager.RemoveListener(_logger, this);
            Cleanup(force);
        }

        void Cleanup(bool close_existing)
        {
            lock (registry)
            {
                if (close_existing)
                {
                    // Need to copy this since closing will call UnregisterContext
                    ICollection keys = registry.Keys;
                    var all = new HttpListenerContext[keys.Count];
                    keys.CopyTo(all, 0);
                    registry.Clear();
                    for (int i = all.Length - 1; i >= 0; i--)
                        all[i].Connection.Close(true);
                }

                lock (connections)
                {
                    ICollection keys = connections.Keys;
                    var conns = new HttpConnection[keys.Count];
                    keys.CopyTo(conns, 0);
                    connections.Clear();
                    for (int i = conns.Length - 1; i >= 0; i--)
                        conns[i].Close(true);
                }
            }
        }

        internal AuthenticationSchemes SelectAuthenticationScheme(HttpListenerContext context)
        {
            if (AuthenticationSchemeSelectorDelegate != null)
                return AuthenticationSchemeSelectorDelegate(context.Request);
            else
                return auth_schemes;
        }

        public void Start()
        {
            CheckDisposed();
            if (listening)
                return;

            EndPointManager.AddListener(_logger, this);
            listening = true;
        }

        public void Stop()
        {
            CheckDisposed();
            listening = false;
            Close(false);
        }

        void IDisposable.Dispose()
        {
            if (disposed)
                return;

            Close(true); //TODO: Should we force here or not?
            disposed = true;
        }

        internal void CheckDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException(GetType().ToString());
        }

        internal void RegisterContext(HttpListenerContext context)
        {
            if (OnContext != null && IsListening)
            {
                OnContext(context);
            }

            lock (registry)
                registry[context] = context;
        }

        internal void UnregisterContext(HttpListenerContext context)
        {
            lock (registry)
                registry.Remove(context);
        }

        internal void AddConnection(HttpConnection cnc)
        {
            lock (connections)
            {
                connections[cnc] = cnc;
            }
        }

        internal void RemoveConnection(HttpConnection cnc)
        {
            lock (connections)
            {
                connections.Remove(cnc);
            }
        }
    }
}
