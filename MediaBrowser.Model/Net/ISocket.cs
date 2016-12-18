using System;

namespace MediaBrowser.Model.Net
{
    public interface ISocket : IDisposable
    {
        bool DualMode { get; }
        IpEndPointInfo LocalEndPoint { get; }
        IpEndPointInfo RemoteEndPoint { get; }
        void Close();
        void Shutdown(bool both);
        void Listen(int backlog);
        void Bind(IpEndPointInfo endpoint);

        void StartAccept(Action<ISocket> onAccept, Func<bool> isClosed);
    }

    public class SocketCreateException : Exception
    {
        public SocketCreateException(string errorCode, Exception originalException)
            : base(errorCode, originalException)
        {
            ErrorCode = errorCode;
        }

        public string ErrorCode { get; private set; }
    }
}
