using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using MediaBrowser.Model.Services;

namespace ServiceStack.Host
{
    public class RestHandler
    {
        public string RequestName { get; set; }

        public async Task<object> HandleResponseAsync(object response)
        {
            var taskResponse = response as Task;

            if (taskResponse == null)
            {
                return response;
            }

            await taskResponse.ConfigureAwait(false);

            var taskResult = ServiceStackHost.Instance.GetTaskResult(taskResponse, RequestName);

            var subTask = taskResult as Task;
            if (subTask != null)
            {
                taskResult = ServiceStackHost.Instance.GetTaskResult(subTask, RequestName);
            }

            return taskResult;
        }

        protected static object CreateContentTypeRequest(IRequest httpReq, Type requestType, string contentType)
        {
            if (!string.IsNullOrEmpty(contentType) && httpReq.ContentLength > 0)
            {
                var deserializer = ContentTypes.Instance.GetStreamDeserializer(contentType);
                if (deserializer != null)
                {
                    return deserializer(requestType, httpReq.InputStream);
                }
            }
            return ServiceStackHost.Instance.CreateInstance(requestType); //Return an empty DTO, even for empty request bodies
        }

        protected static object GetCustomRequestFromBinder(IRequest httpReq, Type requestType)
        {
            Func<IRequest, object> requestFactoryFn;
            ServiceStackHost.Instance.ServiceController.RequestTypeFactoryMap.TryGetValue(
                requestType, out requestFactoryFn);

            return requestFactoryFn != null ? requestFactoryFn(httpReq) : null;
        }

        public static RestPath FindMatchingRestPath(string httpMethod, string pathInfo, out string contentType)
        {
            pathInfo = GetSanitizedPathInfo(pathInfo, out contentType);

            return ServiceStackHost.Instance.ServiceController.GetRestPathForRequest(httpMethod, pathInfo);
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

        public async Task ProcessRequestAsync(IRequest httpReq, IResponse httpRes, string operationName)
        {
            var appHost = ServiceStackHost.Instance;

            var restPath = GetRestPath(httpReq.Verb, httpReq.PathInfo);
            if (restPath == null)
            {
                throw new NotSupportedException("No RestPath found for: " + httpReq.Verb + " " + httpReq.PathInfo);
            }
            httpReq.SetRoute(restPath);

            if (ResponseContentType != null)
                httpReq.ResponseContentType = ResponseContentType;

            var request = httpReq.Dto = CreateRequest(httpReq, restPath);

            appHost.ApplyRequestFilters(httpReq, httpRes, request);

            var rawResponse = await ServiceStackHost.Instance.ServiceController.Execute(request, httpReq).ConfigureAwait(false);

            var response = await HandleResponseAsync(rawResponse).ConfigureAwait(false);

            appHost.ApplyResponseFilters(httpReq, httpRes, response);

            await httpRes.WriteToResponse(httpReq, response).ConfigureAwait(false);
        }

        public static object CreateRequest(IRequest httpReq, RestPath restPath)
        {
            var dtoFromBinder = GetCustomRequestFromBinder(httpReq, restPath.RequestType);
            if (dtoFromBinder != null)
                return dtoFromBinder;

            var requestParams = httpReq.GetFlattenedRequestParams();
            return CreateRequest(httpReq, restPath, requestParams);
        }

        public static object CreateRequest(IRequest httpReq, RestPath restPath, Dictionary<string, string> requestParams)
        {
            var requestDto = CreateContentTypeRequest(httpReq, restPath.RequestType, httpReq.ContentType);

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
        /// Used in Unit tests
        /// </summary>
        /// <returns></returns>
        public object CreateRequest(IRequest httpReq, string operationName)
        {
            if (this.RestPath == null)
                throw new ArgumentNullException("No RestPath found");

            return CreateRequest(httpReq, this.RestPath);
        }
    }

}
