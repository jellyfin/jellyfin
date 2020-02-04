#pragma warning disable CS1591
#pragma warning disable SA1600

using System;

namespace MediaBrowser.Common.Configuration
{
    public class ConfigurationUpdateEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the key.
        /// </summary>
        /// <value>The key.</value>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the new configuration.
        /// </summary>
        /// <value>The new configuration.</value>
        public object NewConfiguration { get; set; }
    }
}
