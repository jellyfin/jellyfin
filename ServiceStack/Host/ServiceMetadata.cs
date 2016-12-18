using System;
using System.Collections.Generic;

namespace ServiceStack.Host
{
    public class ServiceMetadata
    {
        public ServiceMetadata()
        {
            this.OperationsMap = new Dictionary<Type, Type>();
        }

        public Dictionary<Type, Type> OperationsMap { get; protected set; }

        public void Add(Type serviceType, Type requestType, Type responseType)
        {
            this.OperationsMap[requestType] = serviceType;
        }

        public Type GetServiceTypeByRequest(Type requestType)
        {
            Type serviceType;
            OperationsMap.TryGetValue(requestType, out serviceType);
            return serviceType;
        }
    }
}
