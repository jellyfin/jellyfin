using MediaBrowser.Model.Weather;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Weather
{
    /// <summary>
    /// Class BaseWeatherProvider
    /// </summary>
    public abstract class BaseWeatherProvider : IDisposable
    {
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool dispose)
        {
        }

        /// <summary>
        /// Gets the weather info async.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <returns>Task{WeatherInfo}.</returns>
        public abstract Task<WeatherInfo> GetWeatherInfoAsync(string location, CancellationToken cancellationToken);
    }
}
