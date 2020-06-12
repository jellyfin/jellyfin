#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Emby.Server.Implementations.HttpServer;
using MediaBrowser.Model.Services;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Services
{
    public delegate object ActionInvokerFn(object intance, object request);

    public delegate void VoidActionInvokerFn(object intance, object request);

    public class ServiceController
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceController"/> class.
        /// </summary>
        /// <param name="logger">The <see cref="ServiceController"/> logger.</param>
        public ServiceController(ILogger<ServiceController> logger)
        {
            _logger = logger;
        }

        public void Init(HttpListenerHost appHost, IEnumerable<Type> serviceTypes)
        {
            foreach (var serviceType in serviceTypes)
            {
                RegisterService(appHost, serviceType);
            }
        }

        public void RegisterService(HttpListenerHost appHost, Type serviceType)
        {
            // Make sure the provided type implements IService
            if (!typeof(IService).IsAssignableFrom(serviceType))
            {
                _logger.LogWarning("Tried to register a service that does not implement IService: {ServiceType}", serviceType);
                return;
            }

            var processedReqs = new HashSet<Type>();

            var actions = ServiceExecGeneral.Reset(serviceType);

            foreach (var mi in serviceType.GetActions())
            {
                var requestType = mi.GetParameters()[0].ParameterType;
                if (processedReqs.Contains(requestType))
                {
                    continue;
                }

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

        public readonly RestPath.RestPathMap RestPathMap = new RestPath.RestPathMap();

        public void RegisterRestPaths(HttpListenerHost appHost, Type requestType, Type serviceType)
        {
            var attrs = appHost.GetRouteAttributes(requestType);
            foreach (var attr in attrs)
            {
                var restPath = new RestPath(appHost.CreateInstance, appHost.GetParseFn, requestType, serviceType, attr.Path, attr.Verbs, attr.IsHidden, attr.Summary, attr.Description);

                RegisterRestPath(restPath);
            }
        }

        private static readonly char[] InvalidRouteChars = new[] { '?', '&' };

        public void RegisterRestPath(RestPath restPath)
        {
            if (restPath.Path[0] != '/')
            {
                throw new ArgumentException(string.Format("Route '{0}' on '{1}' must start with a '/'", restPath.Path, restPath.RequestType.GetMethodName()));
            }

            if (restPath.Path.IndexOfAny(InvalidRouteChars) != -1)
            {
                throw new ArgumentException(string.Format("Route '{0}' on '{1}' contains invalid chars. ", restPath.Path, restPath.RequestType.GetMethodName()));
            }

            if (RestPathMap.TryGetValue(restPath.FirstMatchHashKey, out List<RestPath> pathsAtFirstMatch))
            {
                pathsAtFirstMatch.Add(restPath);
            }
            else
            {
                RestPathMap[restPath.FirstMatchHashKey] = new List<RestPath>() { restPath };
            }
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

        public Task<object> Execute(HttpListenerHost httpHost, object requestDto, IRequest req)
        {
            var requestType = requestDto.GetType();
            req.OperationName = requestType.Name;

            var serviceType = httpHost.GetServiceTypeByRequest(requestType);

            var service = httpHost.CreateInstance(serviceType);

            var serviceRequiresContext = service as IRequiresRequest;
            if (serviceRequiresContext != null)
            {
                serviceRequiresContext.Request = req;
            }

            //Executes the service and returns the result
            return ServiceExecGeneral.Execute(serviceType, req, service, requestDto, requestType.GetMethodName());
        }
    }

}
