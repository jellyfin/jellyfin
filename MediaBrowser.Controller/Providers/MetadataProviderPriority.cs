
namespace MediaBrowser.Controller.Providers
{
    /// <summary>
    /// Determines when a provider should execute, relative to others
    /// </summary>
    public enum MetadataProviderPriority
    {
        // Run this provider at the beginning
        /// <summary>
        /// The first
        /// </summary>
        First = 1,

        // Run this provider after all first priority providers
        /// <summary>
        /// The second
        /// </summary>
        Second = 2,

        // Run this provider after all second priority providers
        /// <summary>
        /// The third
        /// </summary>
        Third = 3,

        /// <summary>
        /// The fourth
        /// </summary>
        Fourth = 4,

        Fifth = 5,

        // Run this provider last
        /// <summary>
        /// The last
        /// </summary>
        Last = 999
    }
}
