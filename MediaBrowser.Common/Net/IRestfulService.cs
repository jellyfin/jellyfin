using ServiceStack.ServiceHost;
using System;

namespace MediaBrowser.Common.Net
{
    /// <summary>
    /// Interface IRestfulService
    /// </summary>
    public interface IRestfulService : IService, IRequiresRequestContext, IDisposable
    {
    }
}
