namespace MediaBrowser.Model.Plugins;

/// <summary>
/// Interface to indicate that the plugin has configuration validation.
/// </summary>
/// <typeparam name="TPluginConfiguration">The type of plugin configuration.</typeparam>
public interface IHasConfigurationValidation<in TPluginConfiguration>
    where TPluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Validate the plugin configuration.
    /// </summary>
    /// <param name="pluginConfiguration">The plugin configuration.</param>
    void Validate(TPluginConfiguration pluginConfiguration);
}
