using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Model.Services;

namespace ServiceStack.Host
{
    public delegate Task<object> InstanceExecFn(IRequest requestContext, object intance, object request);
    public delegate object ActionInvokerFn(object intance, object request);
    public delegate void VoidActionInvokerFn(object intance, object request);

    public class ServiceController
    {
        private readonly Func<IEnumerable<Type>> _resolveServicesFn;

        public ServiceController(Func<IEnumerable<Type>> resolveServicesFn)
        {
            _resolveServicesFn = resolveServicesFn;
            this.RequestTypeFactoryMap = new Dictionary<Type, Func<IRequest, object>>();
        }

        public Dictionary<Type, Func<IRequest, object>> RequestTypeFactoryMap { get; set; }

        public void Init()
        {
            foreach (var serviceType in _resolveServicesFn())
            {
                RegisterService(serviceType);
            }
        }

        private Type[] GetGenericArguments(Type type)
        {
            return type.GetTypeInfo().IsGenericTypeDefinition
                ? type.GetTypeInfo().GenericTypeParameters
                : type.GetTypeInfo().GenericTypeArguments;
        }

        public void RegisterService(Type serviceType)
        {
            var processedReqs = new HashSet<Type>();

            var actions = ServiceExecGeneral.Reset(serviceType);

            var requiresRequestStreamTypeInfo = typeof(IRequiresRequestStream).GetTypeInfo();

            var appHost = ServiceStackHost.Instance;
            foreach (var mi in serviceType.GetActions())
            {
                var requestType = mi.GetParameters()[0].ParameterType;
                if (processedReqs.Contains(requestType)) continue;
                processedReqs.Add(requestType);

                ServiceExecGeneral.CreateServiceRunnersFor(requestType, actions);

                var returnMarker = requestType.GetTypeWithGenericTypeDefinitionOf(typeof(IReturn<>));
                var responseType = returnMarker != null ?
                      GetGenericArguments(returnMarker)[0]
                    : mi.ReturnType != typeof(object) && mi.ReturnType != typeof(void) ?
                      mi.ReturnType
                    : Type.GetType(requestType.FullName + "Response");

                RegisterRestPaths(requestType);

                appHost.Metadata.Add(serviceType, requestType, responseType);

                if (requiresRequestStreamTypeInfo.IsAssignableFrom(requestType.GetTypeInfo()))
                {
                    this.RequestTypeFactoryMap[requestType] = req =>
                    {
                        var restPath = req.GetRoute();
                        var request = RestHandler.CreateRequest(req, restPath, req.GetRequestParams(), ServiceStackHost.Instance.CreateInstance(requestType));

                        var rawReq = (IRequiresRequestStream)request;
                        rawReq.RequestStream = req.InputStream;
                        return rawReq;
                    };
                }
            }
        }

        public readonly Dictionary<string, List<RestPath>> RestPathMap = new Dictionary<string, List<RestPath>>();

        public void RegisterRestPaths(Type requestType)
        {
            var appHost = ServiceStackHost.Instance;
            var attrs = appHost.GetRouteAttributes(requestType);
            foreach (MediaBrowser.Model.Services.RouteAttribute attr in attrs)
            {
                var restPath = new RestPath(requestType, attr.Path, attr.Verbs, attr.Summary, attr.Notes);

                if (!restPath.IsValid)
                    throw new NotSupportedException(string.Format(
                        "RestPath '{0}' on Type '{1}' is not Valid", attr.Path, requestType.GetOperationName()));

                RegisterRestPath(restPath);
            }
        }

        private static readonly char[] InvalidRouteChars = new[] { '?', '&' };

        public void RegisterRestPath(RestPath restPath)
        {
            if (!restPath.Path.StartsWith("/"))
                throw new ArgumentException(string.Format("Route '{0}' on '{1}' must start with a '/'", restPath.Path, restPath.RequestType.GetOperationName()));
            if (restPath.Path.IndexOfAny(InvalidRouteChars) != -1)
                throw new ArgumentException(string.Format("Route '{0}' on '{1}' contains invalid chars. " +
                                            "See https://github.com/ServiceStack/ServiceStack/wiki/Routing for info on valid routes.", restPath.Path, restPath.RequestType.GetOperationName()));

            List<RestPath> pathsAtFirstMatch;
            if (!RestPathMap.TryGetValue(restPath.FirstMatchHashKey, out pathsAtFirstMatch))
            {
                pathsAtFirstMatch = new List<RestPath>();
                RestPathMap[restPath.FirstMatchHashKey] = pathsAtFirstMatch;
            }
            pathsAtFirstMatch.Add(restPath);
        }

        public void AfterInit()
        {
            var appHost = ServiceStackHost.Instance;

            //Register any routes configured on Metadata.Routes
            foreach (var restPath in appHost.RestPaths)
            {
                RegisterRestPath(restPath);
            }

            //Sync the RestPaths collections
            appHost.RestPaths.Clear();
            appHost.RestPaths.AddRange(RestPathMap.Values.SelectMany(x => x));
        }

        public RestPath GetRestPathForRequest(string httpMethod, string pathInfo)
        {
            var matchUsingPathParts = RestPath.GetPathPartsForMatching(pathInfo);

            List<RestPath> firstMatches;

            var yieldedHashMatches = RestPath.GetFirstMatchHashKeys(matchUsingPathParts);
            foreach (var potentialHashMatch in yieldedHashMatches)
            {
                if (!this.RestPathMap.TryGetValue(potentialHashMatch, out firstMatches)) continue;

                var bestScore = -1;
                foreach (var restPath in firstMatches)
                {
                    var score = restPath.MatchScore(httpMethod, matchUsingPathParts);
                    if (score > bestScore) bestScore = score;
                }
                if (bestScore > 0)
                {
                    foreach (var restPath in firstMatches)
                    {
                        if (bestScore == restPath.MatchScore(httpMethod, matchUsingPathParts))
                            return restPath;
                    }
                }
            }

            var yieldedWildcardMatches = RestPath.GetFirstMatchWildCardHashKeys(matchUsingPathParts);
            foreach (var potentialHashMatch in yieldedWildcardMatches)
            {
                if (!this.RestPathMap.TryGetValue(potentialHashMatch, out firstMatches)) continue;

                var bestScore = -1;
                foreach (var restPath in firstMatches)
                {
                    var score = restPath.MatchScore(httpMethod, matchUsingPathParts);
                    if (score > bestScore) bestScore = score;
                }
                if (bestScore > 0)
                {
                    foreach (var restPath in firstMatches)
                    {
                        if (bestScore == restPath.MatchScore(httpMethod, matchUsingPathParts))
                            return restPath;
                    }
                }
            }

            return null;
        }

        public async Task<object> Execute(object requestDto, IRequest req)
        {
            req.Dto = requestDto;
            var requestType = requestDto.GetType();
            req.OperationName = requestType.Name;

            var serviceType = ServiceStackHost.Instance.Metadata.GetServiceTypeByRequest(requestType);

            var service = ServiceStackHost.Instance.CreateInstance(serviceType);

            //var service = typeFactory.CreateInstance(serviceType);

            var serviceRequiresContext = service as IRequiresRequest;
            if (serviceRequiresContext != null)
            {
                serviceRequiresContext.Request = req;
            }

            if (req.Dto == null) // Don't override existing batched DTO[]
                req.Dto = requestDto;

            //Executes the service and returns the result
            var response = await ServiceExecGeneral.Execute(serviceType, req, service, requestDto, requestType.GetOperationName()).ConfigureAwait(false);

            return response;
        }
    }

}