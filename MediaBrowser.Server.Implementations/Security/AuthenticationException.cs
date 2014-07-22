using System;

namespace MediaBrowser.Server.Implementations.Security
{
    public class AuthenticationException : Exception
    {
        public AuthenticationException(string message)
            : base(message)
        {
        }

        public AuthenticationException()
        {
        }
    }
}
