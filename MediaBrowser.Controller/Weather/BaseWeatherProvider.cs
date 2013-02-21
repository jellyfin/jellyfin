using MediaBrowser.Common.Logging;
using MediaBrowser.Model.Weather;
using System;
using System.Net;
using System.Net.Cache;
using System.Net.Http;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Weather
{
    public abstract class BaseWeatherProvider : IDisposable
    {
        protected HttpClient HttpClient { get; private set; }

        protected BaseWeatherProvider()
        {
            var handler = new WebRequestHandler { };

            handler.AutomaticDecompression = DecompressionMethods.Deflate;
            handler.CachePolicy = new RequestCachePolicy(RequestCacheLevel.Revalidate);

            HttpClient = new HttpClient(handler);
        }

        public virtual void Dispose()
        {
            Logger.LogInfo("Disposing " + GetType().Name);

            HttpClient.Dispose();
        }

        public abstract Task<WeatherInfo> GetWeatherInfoAsync(string zipCode);
    }
}
