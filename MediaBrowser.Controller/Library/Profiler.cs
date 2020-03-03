using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Controller.Library
{
    /// <summary>
    /// Class Profiler
    /// </summary>
    public class Profiler : IDisposable
    {
        /// <summary>
        /// The name
        /// </summary>
        readonly string _name;
        /// <summary>
        /// The stopwatch
        /// </summary>
        readonly Stopwatch _stopwatch;

        /// <summary>
        /// The _logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="Profiler" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="logger">The logger.</param>
        public Profiler(string name, ILogger<Profiler> logger)
        {
            this._name = name;

            _logger = logger;

            _stopwatch = new Stopwatch();
            _stopwatch.Start();
        }
        #region IDisposable Members

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool dispose)
        {
            if (dispose)
            {
                _stopwatch.Stop();
                string message;
                if (_stopwatch.ElapsedMilliseconds > 300000)
                {
                    message = string.Format("{0} took {1} minutes.",
                        _name, ((float)_stopwatch.ElapsedMilliseconds / 60000).ToString("F"));
                }
                else
                {
                    message = string.Format("{0} took {1} seconds.",
                        _name, ((float)_stopwatch.ElapsedMilliseconds / 1000).ToString("#0.000"));
                }
                _logger.LogInformation(message);
            }
        }

        #endregion
    }
}
