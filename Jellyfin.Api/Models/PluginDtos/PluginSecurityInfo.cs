namespace Jellyfin.Api.Models.PluginDtos
{
    /// <summary>
    /// Plugin security info.
    /// </summary>
    public class PluginSecurityInfo
    {
        /// <summary>
        /// Gets or sets the supporter key.
        /// </summary>
        public string? SupporterKey { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether is mb supporter.
        /// </summary>
        public bool IsMbSupporter { get; set; }
    }
}
