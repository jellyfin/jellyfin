using System;
using System.Net.Http;

namespace MediaBrowser.ApiInteraction
{
    /// <summary>
    /// Provides a base class used by the api and image services
    /// </summary>
    public abstract class BaseClient : IDisposable
    {
        public string ApiUrl { get; set; }

        protected HttpClient HttpClient { get; private set; }

        public BaseClient()
        {
            HttpClient = new HttpClient();
        }

        public void Dispose()
        {
            HttpClient.Dispose();
        }
    }
}
