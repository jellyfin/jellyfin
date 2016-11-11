namespace Emby.Server.Core.FFMpeg
{
    /// <summary>
    /// Class FFMpegInfo
    /// </summary>
    public class FFMpegInfo
    {
        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        /// <value>The path.</value>
        public string EncoderPath { get; set; }
        /// <summary>
        /// Gets or sets the probe path.
        /// </summary>
        /// <value>The probe path.</value>
        public string ProbePath { get; set; }
        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        /// <value>The version.</value>
        public string Version { get; set; }
    }
}