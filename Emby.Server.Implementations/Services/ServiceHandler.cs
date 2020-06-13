#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.HttpServer;
using MediaBrowser.Model.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Services
{
    public class ServiceHandler
    {
        private RestPath _restPath;

        private string _responseContentType;

        internal ServiceHandler(RestPath restPath, string responseContentType)
        {
            _restPath = restPath;
            _responseContentType = responseContentType;
        }

        protected static Task<object> CreateContentTypeRequest(HttpListenerHost host, IRequest httpReq, Type requestType, string contentType)
        {
            if (!string.IsNullOrEmpty(contentType) && httpReq.ContentLength > 0)
            {
                var deserializer = RequestHelper.GetRequestReader(host, contentType);
                if (deserializer != null)
                {
                    return deserializer.Invoke(requestType, httpReq.InputStream);
                }
            }

            return Task.FromResult(host.CreateInstance(requestType));
        }

        public static string GetSanitizedPathInfo(string pathInfo, out string contentType)
        {
            contentType = null;
            var pos = pathInfo.LastIndexOf('.');
            if (pos != -1)
            {
                var format = pathInfo.Substring(pos + 1);
                contentType = GetFormatContentType(format);
                if (contentType != null)
                {
                    pathInfo = pathInfo.Substring(0, pos);
                }
            }

            return pathInfo;
        }

        private static string GetFormatContentType(string format)
        {
            // built-in formats
            switch (format)
            {
                case "json": return "application/json";
                case "xml": return "application/xml";
                default: return null;
            }
        }

        public async Task ProcessRequestAsync(HttpListenerHost httpHost, IRequest httpReq, HttpResponse httpRes, ILogger logger, CancellationToken cancellationToken)
        {
            httpReq.Items["__route"] = _restPath;

            if (_responseContentType != null)
            {
                httpReq.ResponseContentType = _responseContentType;
            }

            var request = await CreateRequest(httpHost, httpReq, _restPath, logger).ConfigureAwait(false);

            httpHost.ApplyRequestFilters(httpReq, httpRes, request);

            var response = await httpHost.ServiceController.Execute(httpHost, request, httpReq).ConfigureAwait(false);

            // Apply response filters
            foreach (var responseFilter in httpHost.ResponseFilters)
            {
                responseFilter(httpReq, httpRes, response);
            }

            await ResponseHelper.WriteToResponse(httpRes, httpReq, response, cancellationToken).ConfigureAwait(false);
        }

        public static async Task<object> CreateRequest(HttpListenerHost host, IRequest httpReq, RestPath restPath, ILogger logger)
        {
            var requestType = restPath.RequestType;

            if (RequireqRequestStream(requestType))
            {
                // Used by IRequiresRequestStream
                var requestParams = GetRequestParams(httpReq.Response.HttpContext.Request);
                var request = ServiceHandler.CreateRequest(httpReq, restPath, requestParams, host.CreateInstance(requestType));

                var rawReq = (IRequiresRequestStream)request;
                rawReq.RequestStream = httpReq.InputStream;
                return rawReq;
            }
            else
            {
                var requestParams = GetFlattenedRequestParams(httpReq.Response.HttpContext.Request);

                var requestDto = await CreateContentTypeRequest(host, httpReq, restPath.RequestType, httpReq.ContentType).ConfigureAwait(false);

                return CreateRequest(httpReq, restPath, requestParams, requestDto);
            }
        }

        public static bool RequireqRequestStream(Type requestType)
        {
            var requiresRequestStreamTypeInfo = typeof(IRequiresRequestStream).GetTypeInfo();

            return requiresRequestStreamTypeInfo.IsAssignableFrom(requestType.GetTypeInfo());
        }

        public static object CreateRequest(IRequest httpReq, RestPath restPath, Dictionary<string, string> requestParams, object requestDto)
        {
            var pathInfo = !restPath.IsWildCardPath
                ? GetSanitizedPathInfo(httpReq.PathInfo, out _)
                : httpReq.PathInfo;

            return restPath.CreateRequest(pathInfo, requestParams, requestDto);
        }

        /// <summary>
        /// Duplicate Params are given a unique key by appending a #1 suffix
        /// </summary>
        private static Dictionary<string, string> GetRequestParams(HttpRequest request)
        {
            var map = new Dictionary<string, string>();

            foreach (var pair in request.Query)
            {
                var values = pair.Value;
                if (values.Count == 1)
                {
                    map[pair.Key] = values[0];
                }
                else
                {
                    for (var i = 0; i < values.Count; i++)
                    {
                        map[pair.Key + (i == 0 ? string.Empty : "#" + i)] = values[i];
                    }
                }
            }

            if ((IsMethod(request.Method, "POST") || IsMethod(request.Method, "PUT"))
                && request.HasFormContentType)
            {
                foreach (var pair in request.Form)
                {
                    var values = pair.Value;
                    if (values.Count == 1)
                    {
                        map[pair.Key] = values[0];
                    }
                    else
                    {
                        for (var i = 0; i < values.Count; i++)
                        {
                            map[pair.Key + (i == 0 ? string.Empty : "#" + i)] = values[i];
                        }
                    }
                }
            }

            return map;
        }

        private static bool IsMethod(string method, string expected)
            => string.Equals(method, expected, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Duplicate params have their values joined together in a comma-delimited string
        /// </summary>
        private static Dictionary<string, string> GetFlattenedRequestParams(HttpRequest request)
        {
            var map = new Dictionary<string, string>();

            foreach (var pair in request.Query)
            {
                map[pair.Key] = pair.Value;
            }

            if ((IsMethod(request.Method, "POST") || IsMethod(request.Method, "PUT"))
                && request.HasFormContentType)
            {
                foreach (var pair in request.Form)
                {
                    map[pair.Key] = pair.Value;
                }
            }

            return map;
        }
    }
}
