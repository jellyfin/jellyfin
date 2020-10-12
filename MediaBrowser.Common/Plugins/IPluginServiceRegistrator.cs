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
        /// This object is created prior to the plugin creation, so access to other classes is limited.
        /// </summary>
        /// <param name="serviceCollection">The service collection.</param>
        void RegisterServices(IServiceCollection serviceCollection);
    }
}
