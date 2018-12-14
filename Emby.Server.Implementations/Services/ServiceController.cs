using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Emby.Server.Implementations.HttpServer;
using Microsoft.Extensions.Logging;
using MediaBrowser.Model.Services;

namespace Emby.Server.Implementations.Services
{
    public delegate Task<object> InstanceExecFn(IRequest requestContext, object intance, object request);
    public delegate object ActionInvokerFn(object intance, object request);
    public delegate void VoidActionInvokerFn(object intance, object request);

    public class ServiceController
    {
        public static ServiceController Instance;

        public ServiceController()
        {
            Instance = this;
        }

        public void Init(HttpListenerHost appHost, Type[] serviceTypes)
        {
            foreach (var serviceType in serviceTypes)
            {
                RegisterService(appHost, serviceType);
            }
        }

        public void RegisterService(HttpListenerHost appHost, Type serviceType)
        {
            var processedReqs = new HashSet<Type>();

            var actions = ServiceExecGeneral.Reset(serviceType);

            foreach (var mi in serviceType.GetActions())
            {
                var requestType = mi.GetParameters()[0].ParameterType;
                if (processedReqs.Contains(requestType)) continue;
                processedReqs.Add(requestType);

                ServiceExecGeneral.CreateServiceRunnersFor(requestType, actions);

                //var returnMarker = GetTypeWithGenericTypeDefinitionOf(requestType, typeof(IReturn<>));
                //var responseType = returnMarker != null ?
                //      GetGenericArguments(returnMarker)[0]
                //    : mi.ReturnType != typeof(object) && mi.ReturnType != typeof(void) ?
                //      mi.ReturnType
                //    : Type.GetType(requestType.FullName + "Response");

                RegisterRestPaths(appHost, requestType, serviceType);

                appHost.AddServiceInfo(serviceType, requestType);
            }
        }

        public static Type FirstGenericType(Type type)
        {
            while (type != null)
            {
                if (type.GetTypeInfo().IsGenericType)
                    return type;

                type = type.GetTypeInfo().BaseType;
            }
            return null;
        }

        public readonly RestPath.RestPathMap RestPathMap = new RestPath.RestPathMap();

        public void RegisterRestPaths(HttpListenerHost appHost, Type requestType, Type serviceType)
        {
            var attrs = appHost.GetRouteAttributes(requestType);
            foreach (RouteAttribute attr in attrs)
            {
                var restPath = new RestPath(appHost.CreateInstance, appHost.GetParseFn, requestType, serviceType, attr.Path, attr.Verbs, attr.IsHidden, attr.Summary, attr.Description);

                RegisterRestPath(restPath);
            }
        }

        private static readonly char[] InvalidRouteChars = new[] { '?', '&' };

        public void RegisterRestPath(RestPath restPath)
        {
            if (!restPath.Path.StartsWith("/"))
                throw new ArgumentException(string.Format("Route '{0}' on '{1}' must start with a '/'", restPath.Path, restPath.RequestType.GetMethodName()));
            if (restPath.Path.IndexOfAny(InvalidRouteChars) != -1)
                throw new ArgumentException(string.Format("Route '{0}' on '{1}' contains invalid chars. ", restPath.Path, restPath.RequestType.GetMethodName()));

            List<RestPath> pathsAtFirstMatch;
            if (!RestPathMap.TryGetValue(restPath.FirstMatchHashKey, out pathsAtFirstMatch))
            {
                pathsAtFirstMatch = new List<RestPath>();
                RestPathMap[restPath.FirstMatchHashKey] = pathsAtFirstMatch;
            }
            pathsAtFirstMatch.Add(restPath);
        }

        public RestPath GetRestPathForRequest(string httpMethod, string pathInfo)
        {
            var matchUsingPathParts = RestPath.GetPathPartsForMatching(pathInfo);

            List<RestPath> firstMatches;

            var yieldedHashMatches = RestPath.GetFirstMatchHashKeys(matchUsingPathParts);
            foreach (var potentialHashMatch in yieldedHashMatches)
            {
                if (!this.RestPathMap.TryGetValue(potentialHashMatch, out firstMatches))
                {
                    continue;
                }

                var bestScore = -1;
                RestPath bestMatch = null;
                foreach (var restPath in firstMatches)
                {
                    var score = restPath.MatchScore(httpMethod, matchUsingPathParts);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestMatch = restPath;
                    }
                }

                if (bestScore > 0 && bestMatch != null)
                {
                    return bestMatch;
                }
            }

            var yieldedWildcardMatches = RestPath.GetFirstMatchWildCardHashKeys(matchUsingPathParts);
            foreach (var potentialHashMatch in yieldedWildcardMatches)
            {
                if (!this.RestPathMap.TryGetValue(potentialHashMatch, out firstMatches)) continue;

                var bestScore = -1;
                RestPath bestMatch = null;
                foreach (var restPath in firstMatches)
                {
                    var score = restPath.MatchScore(httpMethod, matchUsingPathParts);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestMatch = restPath;
                    }
                }

                if (bestScore > 0 && bestMatch != null)
                {
                    return bestMatch;
                }
            }

            return null;
        }

        public Task<object> Execute(HttpListenerHost appHost, object requestDto, IRequest req)
        {
            req.Dto = requestDto;
            var requestType = requestDto.GetType();
            req.OperationName = requestType.Name;

            var serviceType = appHost.GetServiceTypeByRequest(requestType);

            var service = appHost.CreateInstance(serviceType);

            //var service = typeFactory.CreateInstance(serviceType);

            var serviceRequiresContext = service as IRequiresRequest;
            if (serviceRequiresContext != null)
            {
                serviceRequiresContext.Request = req;
            }

            if (req.Dto == null) // Don't override existing batched DTO[]
                req.Dto = requestDto;

            //Executes the service and returns the result
            return ServiceExecGeneral.Execute(serviceType, req, service, requestDto, requestType.GetMethodName());
        }
    }

}
