using MediaBrowser.Controller;
using MediaBrowser.Model.System;
using ServiceStack;
using System.Threading.Tasks;

namespace MediaBrowser.Api
{
    /// <summary>
    /// Class GetSystemInfo
    /// </summary>
    [Route("/System/Info", "GET", Summary = "Gets information about the server")]
    public class GetSystemInfo : IReturn<SystemInfo>
    {

    }

    /// <summary>
    /// Class RestartApplication
    /// </summary>
    [Route("/System/Restart", "POST", Summary = "Restarts the application, if needed")]
    public class RestartApplication
    {
    }

    [Route("/System/Shutdown", "POST", Summary = "Shuts down the application")]
    public class ShutdownApplication
    {
    }

    /// <summary>
    /// Class SystemInfoService
    /// </summary>
    public class SystemService : BaseApiService
    {
        /// <summary>
        /// The _app host
        /// </summary>
        private readonly IServerApplicationHost _appHost;


        /// <summary>
        /// Initializes a new instance of the <see cref="SystemService" /> class.
        /// </summary>
        /// <param name="appHost">The app host.</param>
        /// <exception cref="System.ArgumentNullException">jsonSerializer</exception>
        public SystemService(IServerApplicationHost appHost)
        {
            _appHost = appHost;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetSystemInfo request)
        {
            var result = _appHost.GetSystemInfo();

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(RestartApplication request)
        {
            Task.Run(async () =>
            {
                await Task.Delay(100).ConfigureAwait(false);
                await _appHost.Restart().ConfigureAwait(false);
            });
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(ShutdownApplication request)
        {
            Task.Run(async () =>
            {
                await Task.Delay(100).ConfigureAwait(false);
                await _appHost.Shutdown().ConfigureAwait(false);
            });
        }

    }
}
