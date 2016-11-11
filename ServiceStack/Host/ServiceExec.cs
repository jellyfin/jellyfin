//Copyright (c) Service Stack LLC. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using MediaBrowser.Model.Services;

namespace ServiceStack.Host
{
    public static class ServiceExecExtensions
    {
        public static IEnumerable<MethodInfo> GetActions(this Type serviceType)
        {
            foreach (var mi in serviceType.GetRuntimeMethods().Where(i => i.IsPublic && !i.IsStatic))
            {
                if (mi.GetParameters().Length != 1)
                    continue;

                var actionName = mi.Name.ToUpper();
                if (!HttpMethods.AllVerbs.Contains(actionName) && actionName != ActionContext.AnyAction)
                    continue;

                yield return mi;
            }
        }
    }

    internal static class ServiceExecGeneral
    {
        public static Dictionary<string, ActionContext> execMap = new Dictionary<string, ActionContext>();

        public static void CreateServiceRunnersFor(Type requestType, List<ActionContext> actions)
        {
            foreach (var actionCtx in actions)
            {
                if (execMap.ContainsKey(actionCtx.Id)) continue;

                execMap[actionCtx.Id] = actionCtx;
            }
        }

        public static async Task<object> Execute(Type serviceType, IRequest request, object instance, object requestDto, string requestName)
        {
            var actionName = request.Verb
                ?? HttpMethods.Post; //MQ Services

            ActionContext actionContext;
            if (ServiceExecGeneral.execMap.TryGetValue(ActionContext.Key(serviceType, actionName, requestName), out actionContext)
                || ServiceExecGeneral.execMap.TryGetValue(ActionContext.AnyKey(serviceType, requestName), out actionContext))
            {
                if (actionContext.RequestFilters != null)
                {
                    foreach (var requestFilter in actionContext.RequestFilters)
                    {
                        requestFilter.RequestFilter(request, request.Response, requestDto);
                        if (request.Response.IsClosed) return null;
                    }
                }

                var response = actionContext.ServiceAction(instance, requestDto);

                var taskResponse = response as Task;
                if (taskResponse != null)
                {
                    await taskResponse.ConfigureAwait(false);
                    response = ServiceStackHost.Instance.GetTaskResult(taskResponse, requestName);
                }

                return response;
            }

            var expectedMethodName = actionName.Substring(0, 1) + actionName.Substring(1).ToLower();
            throw new NotImplementedException(string.Format("Could not find method named {1}({0}) or Any({0}) on Service {2}", requestDto.GetType().GetOperationName(), expectedMethodName, serviceType.GetOperationName()));
        }

        public static List<ActionContext> Reset(Type serviceType)
        {
            var actions = new List<ActionContext>();

            foreach (var mi in serviceType.GetActions())
            {
                var actionName = mi.Name.ToUpper();
                var args = mi.GetParameters();

                var requestType = args[0].ParameterType;
                var actionCtx = new ActionContext
                {
                    Id = ActionContext.Key(serviceType, actionName, requestType.GetOperationName())
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