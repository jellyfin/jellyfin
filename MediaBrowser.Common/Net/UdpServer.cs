using System;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowser.Common.Net
{
    /// <summary>
    /// Provides a Udp Server
    /// </summary>
    public class UdpServer : IObservable<UdpReceiveResult>, IDisposable
    {
        /// <summary>
        /// The _udp client
        /// </summary>
        private readonly UdpClient _udpClient;
        /// <summary>
        /// The _stream
        /// </summary>
        private readonly IObservable<UdpReceiveResult> _stream;

        /// <summary>
        /// Initializes a new instance of the <see cref="UdpServer" /> class.
        /// </summary>
        /// <param name="endPoint">The end point.</param>
        /// <exception cref="System.ArgumentNullException">endPoint</exception>
        public UdpServer(IPEndPoint endPoint)
        {
            if (endPoint == null)
            {
                throw new ArgumentNullException("endPoint");
            }
            
            _udpClient = new UdpClient(endPoint);

            _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            //_udpClient.ExclusiveAddressUse = false; 
            
            _stream = CreateObservable();
        }

        /// <summary>
        /// Creates the observable.
        /// </summary>
        /// <returns>IObservable{UdpReceiveResult}.</returns>
        private IObservable<UdpReceiveResult> CreateObservable()
        {
            return Observable.Create<UdpReceiveResult>(obs =>
                                Observable.FromAsync(() => _udpClient.ReceiveAsync())
                                          .Subscribe(obs))
                             .Repeat()
                             .Retry()
                             .Publish()
                             .RefCount();
        }

        /// <summary>
        /// Subscribes the specified observer.
        /// </summary>
        /// <param name="observer">The observer.</param>
        /// <returns>IDisposable.</returns>
        /// <exception cref="System.ArgumentNullException">observer</exception>
        public IDisposable Subscribe(IObserver<UdpReceiveResult> observer)
        {
            if (observer == null)
            {
                throw new ArgumentNullException("observer");
            }
            
            return _stream.Subscribe(observer);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool dispose)
        {
            if (dispose)
            {
                _udpClient.Close();
            }
        }

        /// <summary>
        /// Sends the async.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="endPoint">The end point.</param>
        /// <returns>Task{System.Int32}.</returns>
        /// <exception cref="System.ArgumentNullException">data</exception>
        public async Task<int> SendAsync(string data, IPEndPoint endPoint)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            if (endPoint == null)
            {
                throw new ArgumentNullException("endPoint");
            }
            
            var bytes = Encoding.UTF8.GetBytes(data);

            return await _udpClient.SendAsync(bytes, bytes.Length, endPoint).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends the async.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <param name="endPoint">The end point.</param>
        /// <returns>Task{System.Int32}.</returns>
        /// <exception cref="System.ArgumentNullException">bytes</exception>
        public async Task<int> SendAsync(byte[] bytes, IPEndPoint endPoint)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException("bytes");
            }

            if (endPoint == null)
            {
                throw new ArgumentNullException("endPoint");
            }
            
            return await _udpClient.SendAsync(bytes, bytes.Length, endPoint).ConfigureAwait(false);
        }
    }
}
