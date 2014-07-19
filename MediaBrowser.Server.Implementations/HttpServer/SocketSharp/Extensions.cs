using System;
using MediaBrowser.Model.Logging;
using WebSocketSharp.Net;

namespace MediaBrowser.Server.Implementations.HttpServer.SocketSharp
{
    public static class Extensions
    {
        public static string GetOperationName(this HttpListenerRequest request)
        {
            return request.Url.Segments[request.Url.Segments.Length - 1];
        }

        public static void CloseOutputStream(this HttpListenerResponse response, ILogger logger)
        {
            try
            {
                response.OutputStream.Flush();
                response.OutputStream.Close();
                response.Close();
            }
            catch (Exception ex)
            {
                logger.ErrorException("Error in HttpListenerResponseWrapper: " + ex.Message, ex);
            }
        }
    }
}
