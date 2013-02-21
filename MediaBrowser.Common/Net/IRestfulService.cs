using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints;
using System;

namespace MediaBrowser.Common.Net
{
    /// <summary>
    /// Interface IRestfulService
    /// </summary>
    public interface IRestfulService : IService, IRequiresRequestContext, IDisposable
    {
        void Configure(IAppHost appHost);
    }
}
