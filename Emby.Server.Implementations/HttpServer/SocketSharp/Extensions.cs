using SocketHttpListener.Net;

namespace Emby.Server.Implementations.HttpServer.SocketSharp
{
    public static class Extensions
    {
        public static string GetOperationName(this HttpListenerRequest request)
        {
            return request.Url.Segments[request.Url.Segments.Length - 1];
        }
    }
}
