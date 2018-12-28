using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using MediaBrowser.Model.Services;
using MediaBrowser.Model.Extensions;

namespace Emby.Server.Implementations.Services
{
    public static class ServiceExecExtensions
    {
        public static string[] AllVerbs = new[] {
            "OPTIONS", "GET", "HEAD", "POST", "PUT", "DELETE", "TRACE", "CONNECT", // RFC 2616
            "PROPFIND", "PROPPATCH", "MKCOL", "COPY", "MOVE", "LOCK", "UNLOCK",    // RFC 2518
            "VERSION-CONTROL", "REPORT", "CHECKOUT", "CHECKIN", "UNCHECKOUT",
            "MKWORKSPACE", "UPDATE", "LABEL", "MERGE", "BASELINE-CONTROL", "MKACTIVITY",  // RFC 3253
            "ORDERPATCH", // RFC 3648
            "ACL",        // RFC 3744
            "PATCH",      // https://datatracker.ietf.org/doc/draft-dusseault-http-patch/
            "SEARCH",     // https://datatracker.ietf.org/doc/draft-reschke-webdav-search/
            "BCOPY", "BDELETE", "BMOVE", "BPROPFIND", "BPROPPATCH", "NOTIFY",
            "POLL",  "SUBSCRIBE", "UNSUBSCRIBE"
        };

        public static HashSet<string> AllVerbsSet = new HashSet<string>(AllVerbs);

        public static List<MethodInfo> GetActions(this Type serviceType)
        {
            var list = new List<MethodInfo>();

            foreach (var mi in serviceType.GetRuntimeMethods())
            {
                if (!mi.IsPublic)
                {
                    continue;
                }

                if (mi.IsStatic)
                {
                    continue;
                }

                if (mi.GetParameters().Length != 1)
                    continue;

                var actionName = mi.Name;
                if (!AllVerbs.Contains(actionName, StringComparer.OrdinalIgnoreCase))
                    continue;

                list.Add(mi);
            }

            return list;
        }
    }

    internal static class ServiceExecGeneral
    {
        private static Dictionary<string, ServiceMethod> execMap = new Dictionary<string, ServiceMethod>();

        public static void CreateServiceRunnersFor(Type requestType, List<ServiceMethod> actions)
        {
            foreach (var actionCtx in actions)
            {
                if (execMap.ContainsKey(actionCtx.Id)) continue;

                execMap[actionCtx.Id] = actionCtx;
            }
        }

        public static Task<object> Execute(Type serviceType, IRequest request, object instance, object requestDto, string requestName)
        {
            var actionName = request.Verb ?? "POST";

            ServiceMethod actionContext;
            if (ServiceExecGeneral.execMap.TryGetValue(ServiceMethod.Key(serviceType, actionName, requestName), out actionContext))
            {
                if (actionContext.RequestFilters != null)
                {
                    foreach (var requestFilter in actionContext.RequestFilters)
                    {
                        requestFilter.RequestFilter(request, request.Response, requestDto);
                        if (request.Response.IsClosed)
                        {
                            Task.FromResult<object>(null);
                        }
                    }
                }

                var response = actionContext.ServiceAction(instance, requestDto);

                var taskResponse = response as Task;
                if (taskResponse != null)
                {
                    return GetTaskResult(taskResponse);
                }

                return Task.FromResult(response);
            }

            var expectedMethodName = actionName.Substring(0, 1) + actionName.Substring(1).ToLower();
            throw new NotImplementedException(string.Format("Could not find method named {1}({0}) or Any({0}) on Service {2}", requestDto.GetType().GetMethodName(), expectedMethodName, serviceType.GetMethodName()));
        }

        private static async Task<object> GetTaskResult(Task task)
        {
            try
            {
                var taskObject = task as Task<object>;
                if (taskObject != null)
                {
                    return await taskObject.ConfigureAwait(false);
                }

                await task.ConfigureAwait(false);

                var type = task.GetType().GetTypeInfo();
                if (!type.IsGenericType)
                {
                    return null;
                }

                var resultProperty = type.GetDeclaredProperty("Result");
                if (resultProperty == null)
                {
                    return null;
                }

                var result = resultProperty.GetValue(task);

                // hack alert
                if (result.GetType().Name.IndexOf("voidtaskresult", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    return null;
                }

                return result;
            }
            catch (TypeAccessException)
            {
                return null; //return null for void Task's
            }
        }

        public static List<ServiceMethod> Reset(Type serviceType)
        {
            var actions = new List<ServiceMethod>();

            foreach (var mi in serviceType.GetActions())
            {
                var actionName = mi.Name;
                var args = mi.GetParameters();

                var requestType = args[0].ParameterType;
                var actionCtx = new ServiceMethod
                {
                    Id = ServiceMethod.Key(serviceType, actionName, requestType.GetMethodName())
                };

                try
                {
                    actionCtx.ServiceAction = CreateExecFn(serviceType, requestType, mi);
                }
                catch
                {
                    //Potential problems with MONO, using reflection for fallback
                    actionCtx.ServiceAction = (service, request) =>
                                              mi.Invoke(service, new[] { request });
                }

                var reqFilters = new List<IHasRequestFilter>();

                foreach (var attr in mi.GetCustomAttributes(true))
                {
                    var hasReqFilter = attr as IHasRequestFilter;

                    if (hasReqFilter != null)
                        reqFilters.Add(hasReqFilter);
                }

                if (reqFilters.Count > 0)
                    actionCtx.RequestFilters = reqFilters.OrderBy(i => i.Priority).ToArray();

                actions.Add(actionCtx);
            }

            return actions;
        }

        private static ActionInvokerFn CreateExecFn(Type serviceType, Type requestType, MethodInfo mi)
        {
            var serviceParam = Expression.Parameter(typeof(object), "serviceObj");
            var serviceStrong = Expression.Convert(serviceParam, serviceType);

            var requestDtoParam = Expression.Parameter(typeof(object), "requestDto");
            var requestDtoStrong = Expression.Convert(requestDtoParam, requestType);

            Expression callExecute = Expression.Call(
            serviceStrong, mi, requestDtoStrong);

            if (mi.ReturnType != typeof(void))
            {
                var executeFunc = Expression.Lambda<ActionInvokerFn>
                (callExecute, serviceParam, requestDtoParam).Compile();

                return executeFunc;
            }
            else
            {
                var executeFunc = Expression.Lambda<VoidActionInvokerFn>
                (callExecute, serviceParam, requestDtoParam).Compile();

                return (service, request) =>
                {
                    executeFunc(service, request);
                    return null;
                };
            }
        }
    }
}
