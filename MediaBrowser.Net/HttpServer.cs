using System;
using System.Net;
using System.Reactive.Linq;

namespace MediaBrowser.Net
{
    public class HttpServer : IObservable<RequestContext>, IDisposable
    {
        private readonly HttpListener listener;
        private readonly IObservable<RequestContext> stream;

        public HttpServer(string url)
        {
            listener = new HttpListener();
            listener.Prefixes.Add(url);
            listener.Start();
            stream = ObservableHttpContext();
        }

        private IObservable<RequestContext> ObservableHttpContext()
        {
            return Observable.Create<RequestContext>(obs =>
                                Observable.FromAsyncPattern<HttpListenerContext>(listener.BeginGetContext,
                                                                                 listener.EndGetContext)()
                                          .Select(c => new RequestContext(c))
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

        public IDisposable Subscribe(IObserver<RequestContext> observer)
        {
            return stream.Subscribe(observer);
        }
    }
}