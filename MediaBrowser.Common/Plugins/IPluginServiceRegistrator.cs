namespace MediaBrowser.Common.Plugins
{
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Defines the <see cref="IPluginServiceRegistrator" />.
    /// </summary>
    public interface IPluginServiceRegistrator
    {
        /// <summary>
        /// Registers the plugin's services with the service collection.
        /// </summary>
        /// <remarks>
        /// This interface is only used for service registration and requires a parameterless constructor.
        /// </remarks>
        /// <param name="serviceCollection">The service collection.</param>
        void RegisterServices(IServiceCollection serviceCollection);
    }
}
