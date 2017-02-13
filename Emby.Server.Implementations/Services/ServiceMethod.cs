using System;

namespace Emby.Server.Implementations.Services
{
    public class ServiceMethod
    {
        public const string AnyAction = "ANY";

        public string Id { get; set; }

        public ActionInvokerFn ServiceAction { get; set; }
        public MediaBrowser.Model.Services.IHasRequestFilter[] RequestFilters { get; set; }

        public static string Key(Type serviceType, string method, string requestDtoName)
        {
            return serviceType.FullName + " " + method.ToUpper() + " " + requestDtoName;
        }

        public static string AnyKey(Type serviceType, string requestDtoName)
        {
            return Key(serviceType, AnyAction, requestDtoName);
        }
    }
}