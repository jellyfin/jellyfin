using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketHttpListener.Net
{
    internal class UriScheme
    {
        public const string File = "file";
        public const string Ftp = "ftp";
        public const string Gopher = "gopher";
        public const string Http = "http";
        public const string Https = "https";
        public const string News = "news";
        public const string NetPipe = "net.pipe";
        public const string NetTcp = "net.tcp";
        public const string Nntp = "nntp";
        public const string Mailto = "mailto";
        public const string Ws = "ws";
        public const string Wss = "wss";

        public const string SchemeDelimiter = "://";
    }
}
