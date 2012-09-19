using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller;
using MediaBrowser.Model.Weather;
using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace MediaBrowser.Api.HttpHandlers
{
    [Export(typeof(BaseHandler))]
    class WeatherHandler : BaseSerializationHandler<WeatherInfo>
    {
        public override bool HandlesRequest(HttpListenerRequest request)
        {
            return ApiService.IsApiUrlMatch("weather", request);
        }

        protected override Task<WeatherInfo> GetObjectToSerialize()
        {
            // If a specific zip code was requested on the query string, use that. Otherwise use the value from configuration

            string zipCode = QueryString["zipcode"];

            if (string.IsNullOrWhiteSpace(zipCode))
            {
                zipCode = Kernel.Instance.Configuration.WeatherZipCode;
            }

            return Kernel.Instance.WeatherProviders.First().GetWeatherInfoAsync(zipCode);
        }

        protected override async Task<ResponseInfo> GetResponseInfo()
        {
            var info = await base.GetResponseInfo().ConfigureAwait(false);

            info.CacheDuration = TimeSpan.FromMinutes(15);

            return info;
        }
    }
}
