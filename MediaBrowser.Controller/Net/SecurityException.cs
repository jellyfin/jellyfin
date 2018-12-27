using System;

namespace MediaBrowser.Controller.Net
{
    public class SecurityException : Exception
    {
        public SecurityException(string message)
            : base(message)
        {

        }

        public SecurityExceptionType SecurityExceptionType { get; set; }
    }

    public enum SecurityExceptionType
    {
        Unauthenticated = 0,
        ParentalControl = 1
    }
}
