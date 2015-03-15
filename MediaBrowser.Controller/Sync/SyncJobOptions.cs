using MediaBrowser.Model.Dlna;

namespace MediaBrowser.Controller.Sync
{
    public class SyncJobOptions<T>
        where T : AudioOptions, new ()
    {
        /// <summary>
        /// Gets or sets the conversion options.
        /// </summary>
        /// <value>The conversion options.</value>
        public T ConversionOptions { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this instance is converting.
        /// </summary>
        /// <value><c>true</c> if this instance is converting; otherwise, <c>false</c>.</value>
        public bool IsConverting { get; set; }
    }
}
