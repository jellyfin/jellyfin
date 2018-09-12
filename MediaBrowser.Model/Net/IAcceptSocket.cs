using System;

namespace MediaBrowser.Model.Net
{
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
