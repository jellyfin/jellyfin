using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using ServiceStack;

namespace ServiceStack.Support.WebHost
{
    public static class FilterAttributeCache
    {
        public static MediaBrowser.Model.Services.IHasRequestFilter[] GetRequestFilterAttributes(Type requestDtoType)
        {
            var attributes = requestDtoType.AllAttributes().OfType<MediaBrowser.Model.Services.IHasRequestFilter>().ToList();

            var serviceType = ServiceStackHost.Instance.Metadata.GetServiceTypeByRequest(requestDtoType);
            if (serviceType != null)
            {
                attributes.AddRange(serviceType.AllAttributes().OfType<MediaBrowser.Model.Services.IHasRequestFilter>());
            }

			attributes.Sort((x,y) => x.Priority - y.Priority);

            return attributes.ToArray();
        }
    }
}
