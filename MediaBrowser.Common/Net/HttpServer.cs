using System;
using System.Net;
using System.Reactive.Linq;

namespace MediaBrowser.Common.Net
{
    public class HttpServer : IObservable<HttpListenerContext>, IDisposable
    {
        private readonly HttpListener listener;
        private readonly IObservable<HttpListenerContext> stream;

        public HttpServer(string url)
        {
            listener = new HttpListener();
            listener.Prefixes.Add(url);
            listener.Start();
            stream = ObservableHttpContext();
        }

        private IObservable<HttpListenerContext> ObservableHttpContext()
        {
            return Observable.Create<HttpListenerContext>(obs =>
                                Observable.FromAsync<HttpListenerContext>(() => listener.GetContextAsync())
                                          .Subscribe(obs))
                             .Repeat()
                             .Retry()
                             .Publish()
                             .RefCount();
        }
        public void Dispose()
        {
            listener.Stop();
        }

        public IDisposable Subscribe(IObserver<HttpListenerContext> observer)
        {
            return stream.Subscribe(observer);
        }
    }
}