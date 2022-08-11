namespace MediaBrowser.Common.Configuration
{
    /// <summary>
    /// A configuration store that can be validated.
    /// </summary>
    public interface IValidatingConfiguration
    {
        /// <summary>
        /// Validation method to be invoked before saving the configuration.
        /// </summary>
        /// <param name="oldConfig">The old configuration.</param>
        /// <param name="newConfig">The new configuration.</param>
        void Validate(object oldConfig, object newConfig);
    }
}
