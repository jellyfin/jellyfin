using System;
using System.Threading.Tasks;
using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller;
using MediaBrowser.Model.Weather;

namespace MediaBrowser.Api.HttpHandlers
{
    class WeatherHandler : BaseSerializationHandler<WeatherInfo>
    {
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
