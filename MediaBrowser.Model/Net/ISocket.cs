using System;

namespace MediaBrowser.Model.Net
{
    public interface ISocket : IDisposable
    {
        IpEndPointInfo LocalEndPoint { get; }
        IpEndPointInfo RemoteEndPoint { get; }
        void Close();
        void Shutdown(bool both);
        void Listen(int backlog);
        void Bind(IpEndPointInfo endpoint);

        void StartAccept(Action<ISocket> onAccept, Func<bool> isClosed);
    }
}
