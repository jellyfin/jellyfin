
namespace MediaBrowser.Model.ApiClient
{
    public class ConnectionOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether [enable web socket].
        /// </summary>
        /// <value><c>true</c> if [enable web socket]; otherwise, <c>false</c>.</value>
        public bool EnableWebSocket { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether [report capabilities].
        /// </summary>
        /// <value><c>true</c> if [report capabilities]; otherwise, <c>false</c>.</value>
        public bool ReportCapabilities { get; set; }

        public ConnectionOptions()
        {
            EnableWebSocket = true;
            ReportCapabilities = true;
        }
    }
}
