using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.HttpServer;
using Microsoft.Extensions.Logging;
using MediaBrowser.Model.Services;

namespace Emby.Server.Implementations.Services
{
    public class ServiceHandler
    {
        protected static Task<object> CreateContentTypeRequest(HttpListenerHost host, IRequest httpReq, Type requestType, string contentType)
        {
            if (!string.IsNullOrEmpty(contentType) && httpReq.ContentLength > 0)
            {
                var deserializer = RequestHelper.GetRequestReader(host, contentType);
                if (deserializer != null)
                {
                    return deserializer(requestType, httpReq.InputStream);
                }
            }
            return Task.FromResult(host.CreateInstance(requestType)); 
        }

        public static RestPath FindMatchingRestPath(string httpMethod, string pathInfo, out string contentType)
        {
            pathInfo = GetSanitizedPathInfo(pathInfo, out contentType);

            return ServiceController.Instance.GetRestPathForRequest(httpMethod, pathInfo);
        }

        public static string GetSanitizedPathInfo(string pathInfo, out string contentType)
        {
            contentType = null;
            var pos = pathInfo.LastIndexOf('.');
            if (pos >= 0)
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
            if (format == "json")
                return "application/json";
            if (format == "xml")
                return "application/xml";

            return null;
        }

        public RestPath GetRestPath(string httpMethod, string pathInfo)
        {
            if (this.RestPath == null)
            {
                string contentType;
                this.RestPath = FindMatchingRestPath(httpMethod, pathInfo, out contentType);

                if (contentType != null)
                    ResponseContentType = contentType;
            }
            return this.RestPath;
        }

        public RestPath RestPath { get; set; }

        // Set from SSHHF.GetHandlerForPathInfo()
        public string ResponseContentType { get; set; }

        public async Task ProcessRequestAsync(HttpListenerHost appHost, IRequest httpReq, IResponse httpRes, ILogger logger, string operationName, CancellationToken cancellationToken)
        {
            var restPath = GetRestPath(httpReq.Verb, httpReq.PathInfo);
            if (restPath == null)
            {
                throw new NotSupportedException("No RestPath found for: " + httpReq.Verb + " " + httpReq.PathInfo);
            }

            SetRoute(httpReq, restPath);

            if (ResponseContentType != null)
                httpReq.ResponseContentType = ResponseContentType;

            var request = httpReq.Dto = await CreateRequest(appHost, httpReq, restPath, logger).ConfigureAwait(false);

            appHost.ApplyRequestFilters(httpReq, httpRes, request);

            var response = await appHost.ServiceController.Execute(appHost, request, httpReq).ConfigureAwait(false);

            // Apply response filters
            foreach (var responseFilter in appHost.ResponseFilters)
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
            string contentType;
            var pathInfo = !restPath.IsWildCardPath
                ? GetSanitizedPathInfo(httpReq.PathInfo, out contentType)
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
                if (name == null) continue; //thank you ASP.NET

                var values = request.QueryString.GetValues(name);
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
                        if (name == null) continue; //thank you ASP.NET

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
                if (name == null) continue; //thank you ASP.NET
                map[name] = request.QueryString[name];
            }

            if ((IsMethod(request.Verb, "POST") || IsMethod(request.Verb, "PUT")))
            {
                var formData = await request.GetFormData().ConfigureAwait(false);
                if (formData != null)
                {
                    foreach (var name in formData.Keys)
                    {
                        if (name == null) continue; //thank you ASP.NET
                        map[name] = formData[name];
                    }
                }
            }

            return map;
        }

        private static void SetRoute(IRequest req, RestPath route)
        {
            req.Items["__route"] = route;
        }

        private static RestPath GetRoute(IRequest req)
        {
            object route;
            req.Items.TryGetValue("__route", out route);
            return route as RestPath;
        }
    }

}
