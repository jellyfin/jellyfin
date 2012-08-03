using System;
using System.Net;
using System.Net.Http;

namespace MediaBrowser.ApiInteraction
{
    /// <summary>
    /// Provides a base class used by the api and image services
    /// </summary>
    public abstract class BaseClient : IDisposable
    {
        /// <summary>
        /// Gets or sets the server host name (myserver or 192.168.x.x)
        /// </summary>
        public string ServerHostName { get; set; }

        /// <summary>
        /// Gets or sets the port number used by the API
        /// </summary>
        public int ServerApiPort { get; set; }

        /// <summary>
        /// Gets the current api url based on hostname and port.
        /// </summary>
        protected string ApiUrl
        {
            get
            {
                return string.Format("http://{0}:{1}/mediabrowser/api", ServerHostName, ServerApiPort);
            }
        }

        protected HttpClient HttpClient { get; private set; }

        public BaseClient()
            : this(new HttpClientHandler())
        {
        }

        public BaseClient(HttpClientHandler clientHandler)
        {
            clientHandler.AutomaticDecompression = DecompressionMethods.GZip;

            HttpClient = new HttpClient(clientHandler);
        }

        public void Dispose()
        {
            HttpClient.Dispose();
        }
    }
}
