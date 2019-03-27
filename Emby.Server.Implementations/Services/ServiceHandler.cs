using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.HttpServer;
using MediaBrowser.Model.Services;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Services
{
    public class ServiceHandler
    {
        public RestPath RestPath { get; }

        public string ResponseContentType { get; }

        internal ServiceHandler(RestPath restPath, string responseContentType)
        {
            RestPath = restPath;
            ResponseContentType = responseContentType;
        }

        protected static Task<object> CreateContentTypeRequest(HttpListenerHost host, IRequest httpReq, Type requestType, string contentType)
        {
            if (!string.IsNullOrEmpty(contentType) && httpReq.ContentLength > 0)
            {
                var deserializer = RequestHelper.GetRequestReader(host, contentType);
                return deserializer?.Invoke(requestType, httpReq.InputStream);
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
            //built-in formats
            switch (format)
            {
                case "json": return "application/json";
                case "xml": return "application/xml";
                default: return null;
            }
        }

        public async Task ProcessRequestAsync(HttpListenerHost httpHost, IRequest httpReq, IResponse httpRes, ILogger logger, CancellationToken cancellationToken)
        {
            httpReq.Items["__route"] = RestPath;

            if (ResponseContentType != null)
            {
                httpReq.ResponseContentType = ResponseContentType;
            }

            var request = httpReq.Dto = await CreateRequest(httpHost, httpReq, RestPath, logger).ConfigureAwait(false);

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
                var requestParams = await GetRequestParams(httpReq).ConfigureAwait(false);
                var request = ServiceHandler.CreateRequest(httpReq, restPath, requestParams, host.CreateInstance(requestType));

                var rawReq = (IRequiresRequestStream)request;
                rawReq.RequestStream = httpReq.InputStream;
                return rawReq;
            }
            else
            {
                var requestParams = await GetFlattenedRequestParams(httpReq).ConfigureAwait(false);

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
                ? GetSanitizedPathInfo(httpReq.PathInfo, out string contentType)
                : httpReq.PathInfo;

            return restPath.CreateRequest(pathInfo, requestParams, requestDto);
        }

        /// <summary>
        /// Duplicate Params are given a unique key by appending a #1 suffix
        /// </summary>
        private static async Task<Dictionary<string, string>> GetRequestParams(IRequest request)
        {
            var map = new Dictionary<string, string>();

            foreach (var name in request.QueryString.Keys)
            {
                if (name == null)
                {
                    // thank you ASP.NET
                    continue;
                }

                var values = request.QueryString[name];
                if (values.Count == 1)
                {
                    map[name] = values[0];
                }
                else
                {
                    for (var i = 0; i < values.Count; i++)
                    {
                        map[name + (i == 0 ? "" : "#" + i)] = values[i];
                    }
                }
            }

            if ((IsMethod(request.Verb, "POST") || IsMethod(request.Verb, "PUT")))
            {
                var formData = await request.GetFormData().ConfigureAwait(false);
                if (formData != null)
                {
                    foreach (var name in formData.Keys)
                    {
                        if (name == null)
                        {
                            // thank you ASP.NET
                            continue;
                        }

                        var values = formData.GetValues(name);
                        if (values.Count == 1)
                        {
                            map[name] = values[0];
                        }
                        else
                        {
                            for (var i = 0; i < values.Count; i++)
                            {
                                map[name + (i == 0 ? "" : "#" + i)] = values[i];
                            }
                        }
                    }
                }
            }

            return map;
        }

        private static bool IsMethod(string method, string expected)
        {
            return string.Equals(method, expected, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Duplicate params have their values joined together in a comma-delimited string
        /// </summary>
        private static async Task<Dictionary<string, string>> GetFlattenedRequestParams(IRequest request)
        {
            var map = new Dictionary<string, string>();

            foreach (var name in request.QueryString.Keys)
            {
                if (name == null)
                {
                    // thank you ASP.NET
                    continue;
                }

                map[name] = request.QueryString[name];
            }

            if ((IsMethod(request.Verb, "POST") || IsMethod(request.Verb, "PUT")))
            {
                var formData = await request.GetFormData().ConfigureAwait(false);
                if (formData != null)
                {
                    foreach (var name in formData.Keys)
                    {
                        if (name == null)
                        {
                            // thank you ASP.NET
                            continue;
                        }

                        map[name] = formData[name];
                    }
                }
            }

            return map;
        }
    }
}
