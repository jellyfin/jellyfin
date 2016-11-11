using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MediaBrowser.Model.Services;

namespace ServiceStack.Host
{
    public class ContentTypes
    {
        public static ContentTypes Instance = new ContentTypes();

        public void SerializeToStream(IRequest req, object response, Stream responseStream)
        {
            var contentType = req.ResponseContentType;
            var serializer = GetResponseSerializer(contentType);
            if (serializer == null)
                throw new NotSupportedException("ContentType not supported: " + contentType);

            var httpRes = new HttpResponseStreamWrapper(responseStream, req)
            {
                Dto = req.Response.Dto
            };
            serializer(req, response, httpRes);
        }

        public Action<IRequest, object, IResponse> GetResponseSerializer(string contentType)
        {
            var serializer = GetStreamSerializer(contentType);
            if (serializer == null) return null;

            return (httpReq, dto, httpRes) => serializer(httpReq, dto, httpRes.OutputStream);
        }

        public Action<IRequest, object, Stream> GetStreamSerializer(string contentType)
        {
            switch (GetRealContentType(contentType))
            {
                case "application/xml":
                case "text/xml":
                case "text/xml; charset=utf-8": //"text/xml; charset=utf-8" also matches xml
                    return (r, o, s) => ServiceStackHost.Instance.SerializeToXml(o, s);

                case "application/json":
                case "text/json":
                    return (r, o, s) => ServiceStackHost.Instance.SerializeToJson(o, s);
            }

            return null;
        }

        public Func<Type, Stream, object> GetStreamDeserializer(string contentType)
        {
            switch (GetRealContentType(contentType))
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

        private static string GetRealContentType(string contentType)
        {
            return contentType == null
                       ? null
                       : contentType.Split(';')[0].ToLower().Trim();
        }

    }
}