using System.Net.Http;

namespace MediaBrowser.ApiInteraction
{
    public class ApiClient : BaseHttpApiClient
    {
        public ApiClient(HttpClientHandler handler)
            : base(handler)
        {
        }
    }
}
