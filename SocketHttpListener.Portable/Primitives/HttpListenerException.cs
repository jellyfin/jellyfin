using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketHttpListener.Primitives
{
    public class HttpListenerException : Exception
    {
        public HttpListenerException(int statusCode, string message)
            : base(message)
        {
            
        }
    }
}
