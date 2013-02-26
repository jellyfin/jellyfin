using MediaBrowser.Controller;
using MediaBrowser.Model.Weather;
using MediaBrowser.Networking.HttpServer;
using ServiceStack.ServiceHost;
using System.Linq;
using System.Threading;

namespace MediaBrowser.Api
{
    /// <summary>
    /// Class Weather
    /// </summary>
    [Route("/Weather", "GET")]
    public class GetWeather : IReturn<WeatherInfo>
    {
        /// <summary>
        /// Gets or sets the location.
        /// </summary>
        /// <value>The location.</value>
        public string Location { get; set; }
    }

    /// <summary>
    /// Class WeatherService
    /// </summary>
    public class WeatherService : BaseRestService
    {
        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetWeather request)
        {
            var kernel = (Kernel) Kernel;

            var location = string.IsNullOrWhiteSpace(request.Location) ? kernel.Configuration.WeatherLocation : request.Location;

            var result = kernel.WeatherProviders.First().GetWeatherInfoAsync(location, CancellationToken.None).Result;

            return ToOptimizedResult(result);
        }
    }
}
