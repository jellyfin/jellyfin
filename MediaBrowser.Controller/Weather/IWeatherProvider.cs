using MediaBrowser.Model.Weather;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Weather
{
    /// <summary>
    /// Interface IWeatherProvider
    /// </summary>
    public interface IWeatherProvider
    {
        /// <summary>
        /// Gets the weather info async.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{WeatherInfo}.</returns>
        Task<WeatherInfo> GetWeatherInfoAsync(string location, CancellationToken cancellationToken);
    }
}
