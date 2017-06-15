using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketHttpListener.Net
{
    internal enum BoundaryType
    {
        ContentLength = 0, // Content-Length: XXX
        Chunked = 1, // Transfer-Encoding: chunked
        Multipart = 3,
        None = 4,
        Invalid = 5,
    }
}
