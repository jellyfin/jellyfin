using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Services;
using ServiceStack.Host;

namespace ServiceStack
{
    public class HttpHandlerFactory
    {
        // Entry point for HttpListener
        public static RestHandler GetHandler(IHttpRequest httpReq, ILogger logger)
        {
            var pathInfo = httpReq.PathInfo;

            var pathParts = pathInfo.TrimStart('/').Split('/');
            if (pathParts.Length == 0)
            {
                logger.Error("Path parts empty for PathInfo: {0}, Url: {1}", pathInfo, httpReq.RawUrl);
                return null;
            }

            string contentType;
            var restPath = RestHandler.FindMatchingRestPath(httpReq.HttpMethod, pathInfo, out contentType);
            if (restPath != null)
                return new RestHandler { RestPath = restPath, RequestName = restPath.RequestType.GetOperationName(), ResponseContentType = contentType };

            return null;
        }
    }
}