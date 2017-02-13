// Copyright (c) Service Stack LLC. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt


using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MediaBrowser.Model.Services;

namespace ServiceStack
{
    public abstract class ServiceStackHost : IDisposable
    {
        public static ServiceStackHost Instance { get; protected set; }

        protected ServiceStackHost()
        {
            GlobalResponseFilters = new List<Action<IRequest, IResponse, object>>();
        }

        public abstract object CreateInstance(Type type);

        public List<Action<IRequest, IResponse, object>> GlobalResponseFilters { get; set; }

        public abstract RouteAttribute[] GetRouteAttributes(Type requestType);

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
    }
}
