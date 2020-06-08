#pragma warning disable CS1591

using System;

namespace Emby.Server.Implementations.Services
{
    public class ServiceMethod
    {
        public string Id { get; set; }

        public ActionInvokerFn ServiceAction { get; set; }
        public MediaBrowser.Model.Services.IHasRequestFilter[] RequestFilters { get; set; }

        public static string Key(Type serviceType, string method, string requestDtoName)
        {
            return serviceType.FullName + " " + method.ToUpperInvariant() + " " + requestDtoName;
        }
    }
}
