using System;
using System.IO;
using Emby.Server.Implementations.HttpServer;

namespace Emby.Server.Implementations.Services
{
    public class RequestHelper
    {
        public static Func<Type, Stream, object> GetRequestReader(HttpListenerHost host, string contentType)
        {
            switch (GetContentTypeWithoutEncoding(contentType))
            {
                case "application/xml":
                case "text/xml":
                case "text/xml; charset=utf-8": //"text/xml; charset=utf-8" also matches xml
                    return host.DeserializeXml;

                case "application/json":
                case "text/json":
                    return host.DeserializeJson;
            }

            return null;
        }

        public static Action<object, Stream> GetResponseWriter(HttpListenerHost host, string contentType)
        {
            switch (GetContentTypeWithoutEncoding(contentType))
            {
                case "application/xml":
                case "text/xml":
                case "text/xml; charset=utf-8": //"text/xml; charset=utf-8" also matches xml
                    return host.SerializeToXml;

                case "application/json":
                case "text/json":
                    return host.SerializeToJson;
            }

            return null;
        }

        private static string GetContentTypeWithoutEncoding(string contentType)
        {
            return contentType == null
                       ? null
                       : contentType.Split(';')[0].ToLower().Trim();
        }

    }
}