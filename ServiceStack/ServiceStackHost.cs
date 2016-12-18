// Copyright (c) Service Stack LLC. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt


using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Services;
using ServiceStack.Host;

namespace ServiceStack
{
    public abstract partial class ServiceStackHost : IDisposable
    {
        public static ServiceStackHost Instance { get; protected set; }

        protected ServiceStackHost(string serviceName)
        {
            ServiceName = serviceName;
            ServiceController = CreateServiceController();

            RestPaths = new List<RestPath>();
            Metadata = new ServiceMetadata();
            GlobalRequestFilters = new List<Action<IRequest, IResponse, object>>();
            GlobalResponseFilters = new List<Action<IRequest, IResponse, object>>();
        }

        public abstract void Configure();

        public abstract object CreateInstance(Type type);

        protected abstract ServiceController CreateServiceController();

        public virtual ServiceStackHost Init()
        {
            Instance = this;

            ServiceController.Init();
            Configure();

            ServiceController.AfterInit();

            return this;
        }

        public virtual ServiceStackHost Start(string urlBase)
        {
            throw new NotImplementedException("Start(listeningAtUrlBase) is not supported by this AppHost");
        }

        public string ServiceName { get; set; }

        public ServiceMetadata Metadata { get; set; }

        public ServiceController ServiceController { get; set; }

        public List<RestPath> RestPaths = new List<RestPath>();

        public List<Action<IRequest, IResponse, object>> GlobalRequestFilters { get; set; }

        public List<Action<IRequest, IResponse, object>> GlobalResponseFilters { get; set; }

        public abstract T TryResolve<T>();
        public abstract T Resolve<T>();

        public virtual MediaBrowser.Model.Services.RouteAttribute[] GetRouteAttributes(Type requestType)
        {
            return requestType.AllAttributes<MediaBrowser.Model.Services.RouteAttribute>();
        }

        public abstract object GetTaskResult(Task task, string requestName);

        public abstract Func<string, object> GetParseFn(Type propertyType);

        public abstract void SerializeToJson(object o, Stream stream);
        public abstract void SerializeToXml(object o, Stream stream);
        public abstract object DeserializeXml(Type type, Stream stream);
        public abstract object DeserializeJson(Type type, Stream stream);

        public virtual void Dispose()
        {
            //JsConfig.Reset(); //Clears Runtime Attributes

            Instance = null;
        }

        protected abstract ILogger Logger
        {
            get;
        }

        public void OnLogError(Type type, string message)
        {
            Logger.Error(message);
        }

        public void OnLogError(Type type, string message, Exception ex)
        {
            Logger.ErrorException(message, ex);
        }
    }
}
