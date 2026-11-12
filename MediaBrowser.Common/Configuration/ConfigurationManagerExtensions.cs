namespace MediaBrowser.Common.Configuration
{
    /// <summary>
    /// Extension methods for <see cref="IConfigurationManager"/>.
    /// </summary>
    public static class ConfigurationManagerExtensions
    {
        /// <summary>
        /// Gets the named configuration as <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The configuration type.</typeparam>
        /// <param name="manager">The configuration manager.</param>
        /// <param name="key">The configuration key.</param>
        /// <returns>The configuration.</returns>
        public static T GetConfiguration<T>(this IConfigurationManager manager, string key)
        {
            return (T)manager.GetConfiguration(key);
        }
    }
}
