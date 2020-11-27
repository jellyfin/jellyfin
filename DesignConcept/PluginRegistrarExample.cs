namespace Jellyfin.Plugin
{
    public interface IRequiredInTwoPlugins
    {
        string value { get; set; }
    }


    /// <summary>
    /// Defines the <see cref="DlnaPlayToRegistrar" />.
    /// </summary>
    public class DlnaPlayToRegistrar : IPluginRegistrar
    {
        /// <summary>
        /// Register services.
        /// </summary>
        /// <param name="serviceCollection">The <see cref="IServiceCollection"/>.</param>
        public void RegisterServices(IServiceCollection serviceCollection)
        {
            // If the shared code hasn't already been loaded. Load it now.
            SharedCodeManager.RegisterSharedCode(serviceCollection, IRequiredInTwoPlugins);
            serviceCollection.AddSingleton<IDlnaServerPlayTo, DlnaServerManager>();
        }
    }
}
