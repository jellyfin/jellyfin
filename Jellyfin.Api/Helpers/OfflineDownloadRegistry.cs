using System;
using System.Runtime.CompilerServices;
using MediaBrowser.Controller.Configuration;
using Microsoft.Extensions.Hosting;

namespace Jellyfin.Api.Helpers;

// The startup service and request controllers share one host-lifetime cache owner.
internal static class OfflineDownloadRegistry
{
    private static readonly object Sync = new();
    private static readonly ConditionalWeakTable<IHostApplicationLifetime, OfflineDownloadJobs> Stores = new();

    internal static OfflineDownloadJobs Get(IServerConfigurationManager configuration, IHostApplicationLifetime lifetime, Action<string> warning)
    {
        lock (Sync)
        {
            return Stores.GetValue(lifetime, host =>
            {
                // Progressive scratch is cleared by TranscodeManager initialization and cannot hold checkpoints.
                var cacheRoot = Environment.GetEnvironmentVariable("JELLYFIN_OFFLINE_CACHE_PATH") ?? configuration.ApplicationPaths.CachePath;
                var jobs = new OfflineDownloadJobs(cacheRoot, warning: warning);
                host.ApplicationStopping.Register(jobs.Dispose);
                return jobs;
            });
        }
    }
}
