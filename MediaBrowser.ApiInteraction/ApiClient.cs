using System.Net.Cache;
using System.Net.Http;

namespace MediaBrowser.ApiInteraction
{
    public class ApiClient : BaseHttpApiClient
    {
        public ApiClient(HttpClientHandler handler)
            : base(handler)
        {
        }

        public ApiClient()
            : this(new WebRequestHandler { CachePolicy = new RequestCachePolicy(RequestCacheLevel.Revalidate) })
        {
        }
    }
}
