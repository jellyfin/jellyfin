namespace Jellyfin.Plugin
{
    /// This is the interface in the load as required dll.
    /// By providing it here, we should be able to fully compile this plugin.
    /// Mark this interface to identify that it needs to be transmuted.
    [SharedCodeAttribute("IRequiredInTwoPlugins")]
    public interface IRequiredInTwoPlugins
    {
        string value { get; set; }
    }


    /// <summary>
    /// This is the DI registration code.
    /// </summary>
    public class SamplePlugin : IPluginRegistrar
    {
        /// <summary>
        /// Register services.
        /// </summary>
        /// <param name="serviceCollection">The <see cref="IServiceCollection"/>.</param>
        public void RegisterServices(IServiceCollection serviceCollection)
        {
            // If the shared code hasn't already been defined in the DI. Load it now, and add it's shared code.
            SharedCodeManager.RegisterSharedCode(serviceCollection, IRequiredInTwoPlugins);

            serviceCollection.AddSingleton<IMyPluginOther, MyPluginOther>();
        }
    }
}
