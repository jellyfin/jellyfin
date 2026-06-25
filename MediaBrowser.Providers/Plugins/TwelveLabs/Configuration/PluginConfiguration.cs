#pragma warning disable CS1591

using MediaBrowser.Model.Plugins;

namespace MediaBrowser.Providers.Plugins.TwelveLabs.Configuration
{
    /// <summary>
    /// Configuration for the TwelveLabs subtitle provider.
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        /// <summary>
        /// Gets or sets the TwelveLabs API key. Get a free key at https://twelvelabs.io.
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Pegasus model name used for analysis.
        /// </summary>
        public string ModelName { get; set; } = "pegasus1.5";

        /// <summary>
        /// Gets or sets the prompt sent to Pegasus to produce the subtitle text.
        /// </summary>
        public string Prompt { get; set; } = "Transcribe the spoken dialogue in this video as plain text. "
            + "Return only the spoken words, in order, with no commentary, timestamps, or speaker labels.";

        /// <summary>
        /// Gets or sets the public base URL that local media paths are exposed under so the
        /// TwelveLabs API can fetch them. The portion of the media path after
        /// <see cref="MediaPathPrefix"/> is appended to this value to form the URL sent to TwelveLabs.
        /// Leave empty to disable the provider.
        /// </summary>
        public string MediaUrlPrefix { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the local path prefix that maps to <see cref="MediaUrlPrefix"/>.
        /// </summary>
        public string MediaPathPrefix { get; set; } = string.Empty;
    }
}
