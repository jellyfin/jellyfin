using System;
using System.IO;
using ServiceStack;

namespace Emby.Server.Implementations.Services
{
    public class RequestHelper
    {
        public static Func<Type, Stream, object> GetRequestReader(string contentType)
        {
            switch (GetContentTypeWithoutEncoding(contentType))
            {
                case "application/xml":
                case "text/xml":
                case "text/xml; charset=utf-8": //"text/xml; charset=utf-8" also matches xml
                    return ServiceStackHost.Instance.DeserializeXml;

                case "application/json":
                case "text/json":
                    return ServiceStackHost.Instance.DeserializeJson;
            }

            return null;
        }

        public static Action<object, Stream> GetResponseWriter(string contentType)
        {
            switch (GetContentTypeWithoutEncoding(contentType))
            {
                case "application/xml":
                case "text/xml":
                case "text/xml; charset=utf-8": //"text/xml; charset=utf-8" also matches xml
                    return (o, s) => ServiceStackHost.Instance.SerializeToXml(o, s);

                case "application/json":
                case "text/json":
                    return (o, s) => ServiceStackHost.Instance.SerializeToJson(o, s);
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