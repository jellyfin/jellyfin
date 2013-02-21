using System;
using System.Net;
using System.Reactive.Linq;

namespace MediaBrowser.Common.Net
{
    public class HttpServer : IObservable<HttpListenerContext>, IDisposable
    {
        private readonly HttpListener _listener;
        private readonly IObservable<HttpListenerContext> _stream;

        public HttpServer(string url)
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add(url);
            _listener.Start();
            _stream = ObservableHttpContext();
        }

        private IObservable<HttpListenerContext> ObservableHttpContext()
        {
            return Observable.Create<HttpListenerContext>(obs =>
                                Observable.FromAsync(() => _listener.GetContextAsync())
                                          .Subscribe(obs))
                             .Repeat()
                             .Retry()
                             .Publish()
                             .RefCount();
        }
        public void Dispose()
        {
            _listener.Stop();
        }

        public IDisposable Subscribe(IObserver<HttpListenerContext> observer)
        {
            return _stream.Subscribe(observer);
        }
    }
}