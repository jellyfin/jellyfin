using Jellyfin.Plugin.PlaybackReporter.Services;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.PlaybackReporter;

/// <summary>
/// Registers the plugin's services with the Jellyfin dependency injection container.
/// </summary>
public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddHttpClient(nameof(GitHubReporter));

        serviceCollection.AddSingleton<GitHubReporter>();
        serviceCollection.AddHostedService<PlaybackMonitorService>();
    }
}
