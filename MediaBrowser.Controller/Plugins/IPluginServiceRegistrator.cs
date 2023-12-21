using Microsoft.Extensions.DependencyInjection;

namespace MediaBrowser.Controller.Plugins;

/// <summary>
/// Defines the <see cref="IPluginServiceRegistrator" />.
/// </summary>
/// <remarks>
/// This interface is only used for service registration and requires a parameterless constructor.
/// </remarks>
public interface IPluginServiceRegistrator
{
    /// <summary>
    /// Registers the plugin's services with the service collection.
    /// </summary>
    /// <param name="serviceCollection">The service collection.</param>
    /// <param name="applicationHost">The server application host.</param>
    void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost);
}
