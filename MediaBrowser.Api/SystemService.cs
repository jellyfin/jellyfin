using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Serialization;
using MediaBrowser.Controller;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.System;
using ServiceStack.ServiceHost;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;

namespace MediaBrowser.Api
{
    [Route("/System/Info", "GET")]
    public class GetSystemInfo : IReturn<SystemInfo>
    {

    }

    /// <summary>
    /// Class RestartApplication
    /// </summary>
    [Route("/System/Restart", "POST")]
    [ServiceStack.ServiceHost.Api(("Restarts the application, if needed"))]
    public class RestartApplication
    {
    }

    /// <summary>
    /// Class GetConfiguration
    /// </summary>
    [Route("/System/Configuration", "GET")]
    public class GetConfiguration : IReturn<ServerConfiguration>
    {

    }

    /// <summary>
    /// Class UpdateConfiguration
    /// </summary>
    [Route("/System/Configuration", "POST")]
    public class UpdateConfiguration : IRequiresRequestStream
    {
        public Stream RequestStream { get; set; }
    }

    /// <summary>
    /// Class SystemInfoService
    /// </summary>
    [Export(typeof(IRestfulService))]
    public class SystemService : BaseRestService
    {
        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetSystemInfo request)
        {
            var result = Kernel.GetSystemInfo();

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetConfiguration request)
        {
            var kernel = (Kernel)Kernel;

            var dateModified = File.GetLastWriteTimeUtc(Kernel.ApplicationPaths.SystemConfigurationFilePath);

            var cacheKey = (Kernel.ApplicationPaths.SystemConfigurationFilePath + dateModified.Ticks).GetMD5();

            return ToOptimizedResultUsingCache(cacheKey, dateModified, null, () => kernel.Configuration);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(RestartApplication request)
        {
            Task.Run(async () =>
            {
                await Task.Delay(100);
                Kernel.PerformPendingRestart();
            });
        }

        /// <summary>
        /// Posts the specified configuraiton.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(UpdateConfiguration request)
        {
            var serverConfig = JsonSerializer.DeserializeFromStream<ServerConfiguration>(request.RequestStream);
            
            var kernel = (Kernel)Kernel;

            kernel.UpdateConfiguration(serverConfig);
        }
    }
}
