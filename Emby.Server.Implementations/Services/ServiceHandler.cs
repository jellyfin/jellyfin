using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.HttpServer;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Services;

namespace Emby.Server.Implementations.Services
{
    public class ServiceHandler
    {
        public async Task<object> HandleResponseAsync(object response)
        {
            var taskResponse = response as Task;

            if (taskResponse == null)
            {
                return response;
            }

            await taskResponse.ConfigureAwait(false);

            var taskResult = GetTaskResult(taskResponse);

            var subTask = taskResult as Task;
            if (subTask != null)
            {
                taskResult = GetTaskResult(subTask);
            }

            return taskResult;
        }

        internal static object GetTaskResult(Task task)
        {
            try
            {
                var taskObject = task as Task<object>;
                if (taskObject != null)
                {
                    return taskObject.Result;
                }

                task.Wait();

                var type = task.GetType().GetTypeInfo();
                if (!type.IsGenericType)
                {
                    return null;
                }

                return type.GetDeclaredProperty("Result").GetValue(task);
            }
            catch (TypeAccessException)
            {
                return null; //return null for void Task's
            }
        }

        protected static object CreateContentTypeRequest(HttpListenerHost host, IRequest httpReq, Type requestType, string contentType)
        {
            if (!string.IsNullOrEmpty(contentType) && httpReq.ContentLength > 0)
            {
                var deserializer = RequestHelper.GetRequestReader(host, contentType);
                if (deserializer != null)
                {
                    return deserializer(requestType, httpReq.InputStream);
                }
            }
            return host.CreateInstance(requestType); 
        }

        public static RestPath FindMatchingRestPath(string httpMethod, string pathInfo, ILogger logger, out string contentType)
        {
            pathInfo = GetSanitizedPathInfo(pathInfo, out contentType);

            return ServiceController.Instance.GetRestPathForRequest(httpMethod, pathInfo, logger);
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
                this.RestPath = FindMatchingRestPath(httpMethod, pathInfo, new NullLogger(), out contentType);

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

            var request = httpReq.Dto = CreateRequest(appHost, httpReq, restPath, logger);

            appHost.ApplyRequestFilters(httpReq, httpRes, request);

            var rawResponse = await appHost.ServiceController.Execute(appHost, request, httpReq).ConfigureAwait(false);

            //var response = await HandleResponseAsync(rawResponse).ConfigureAwait(false);
            var response = rawResponse;

            // Apply response filters
            foreach (var responseFilter in appHost.ResponseFilters)
            {
                responseFilter(httpReq, httpRes, response);
            }

            await ResponseHelper.WriteToResponse(httpRes, httpReq, response, cancellationToken).ConfigureAwait(false);
        }

        public static object CreateRequest(HttpListenerHost host, IRequest httpReq, RestPath restPath, ILogger logger)
        {
            var requestType = restPath.RequestType;

            if (RequireqRequestStream(requestType))
            {
                // Used by IRequiresRequestStream
                return CreateRequiresRequestStreamRequest(host, httpReq, requestType);
            }

            var requestParams = GetFlattenedRequestParams(httpReq);
            return CreateRequest(host, httpReq, restPath, requestParams);
        }

        private static bool RequireqRequestStream(Type requestType)
        {
            var requiresRequestStreamTypeInfo = typeof(IRequiresRequestStream).GetTypeInfo();

            return requiresRequestStreamTypeInfo.IsAssignableFrom(requestType.GetTypeInfo());
        }

        private static IRequiresRequestStream CreateRequiresRequestStreamRequest(HttpListenerHost host, IRequest req, Type requestType)
        {
            var restPath = GetRoute(req);
            var request = ServiceHandler.CreateRequest(req, restPath, GetRequestParams(req), host.CreateInstance(requestType));

            var rawReq = (IRequiresRequestStream)request;
            rawReq.RequestStream = req.InputStream;
            return rawReq;
        }

        public static object CreateRequest(HttpListenerHost host, IRequest httpReq, RestPath restPath, Dictionary<string, string> requestParams)
        {
            var requestDto = CreateContentTypeRequest(host, httpReq, restPath.RequestType, httpReq.ContentType);

            return CreateRequest(httpReq, restPath, requestParams, requestDto);
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
        private static Dictionary<string, string> GetRequestParams(IRequest request)
        {
            var map = new Dictionary<string, string>();

            foreach (var name in request.QueryString.Keys)
            {
                if (name == null) continue; //thank you ASP.NET

                var values = request.QueryString.GetValues(name);
                if (values.Length == 1)
                {
                    map[name] = values[0];
                }
                else
                {
                    for (var i = 0; i < values.Length; i++)
                    {
                        map[name + (i == 0 ? "" : "#" + i)] = values[i];
                    }
                }
            }

            if ((IsMethod(request.Verb, "POST") || IsMethod(request.Verb, "PUT")) && request.FormData != null)
            {
                foreach (var name in request.FormData.Keys)
                {
                    if (name == null) continue; //thank you ASP.NET

                    var values = request.FormData.GetValues(name);
                    if (values.Length == 1)
                    {
                        map[name] = values[0];
                    }
                    else
                    {
                        for (var i = 0; i < values.Length; i++)
                        {
                            map[name + (i == 0 ? "" : "#" + i)] = values[i];
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
        private static Dictionary<string, string> GetFlattenedRequestParams(IRequest request)
        {
            var map = new Dictionary<string, string>();

            foreach (var name in request.QueryString.Keys)
            {
                if (name == null) continue; //thank you ASP.NET
                map[name] = request.QueryString[name];
            }

            if ((IsMethod(request.Verb, "POST") || IsMethod(request.Verb, "PUT")) && request.FormData != null)
            {
                foreach (var name in request.FormData.Keys)
                {
                    if (name == null) continue; //thank you ASP.NET
                    map[name] = request.FormData[name];
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
