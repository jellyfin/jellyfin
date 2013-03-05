using MediaBrowser.Common.Implementations.HttpServer;
using MediaBrowser.Controller;
using MediaBrowser.Model.Weather;
using ServiceStack.ServiceHost;
using System.Linq;
using System.Threading;

namespace MediaBrowser.Api
{
    /// <summary>
    /// Class Weather
    /// </summary>
    [Route("/Weather", "GET")]
    [ServiceStack.ServiceHost.Api(Description = "Gets weather information for a given location")]
    public class GetWeather : IReturn<WeatherInfo>
    {
        /// <summary>
        /// Gets or sets the location.
        /// </summary>
        /// <value>The location.</value>
        [ApiMember(Name = "Location", Description = "Us zip / City, State, Country / City, Country", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
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
            var result = Kernel.Instance.WeatherProviders.First().GetWeatherInfoAsync(request.Location, CancellationToken.None).Result;

            return ToOptimizedResult(result);
        }
    }
}
