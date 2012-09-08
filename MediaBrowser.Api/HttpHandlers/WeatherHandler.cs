using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller;
using MediaBrowser.Model.Weather;
using System;
using System.ComponentModel.Composition;
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

            return Kernel.Instance.WeatherClient.GetWeatherInfoAsync(zipCode);
        }

        /// <summary>
        /// Tell the client to cache the weather info for 15 minutes
        /// </summary>
        public override TimeSpan CacheDuration
        {
            get
            {
                return TimeSpan.FromMinutes(15);
            }
        }
    }
}
