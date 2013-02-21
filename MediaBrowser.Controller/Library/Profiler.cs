using System;
using System.Diagnostics;
using MediaBrowser.Common.Logging;
using MediaBrowser.Model.Logging;

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
        readonly string name;
        /// <summary>
        /// The stopwatch
        /// </summary>
        readonly Stopwatch stopwatch;

        /// <summary>
        /// The _logger
        /// </summary>
        private ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="Profiler" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="logger">The logger.</param>
        public Profiler(string name, ILogger logger)
        {
            this.name = name;

            _logger = logger;

            stopwatch = new Stopwatch();
            stopwatch.Start();
        }
        #region IDisposable Members

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
            if (dispose)
            {
                stopwatch.Stop();
                string message;
                if (stopwatch.ElapsedMilliseconds > 300000)
                {
                    message = string.Format("{0} took {1} minutes.",
                        name, ((float)stopwatch.ElapsedMilliseconds / 60000).ToString("F"));
                }
                else
                {
                    message = string.Format("{0} took {1} seconds.",
                        name, ((float)stopwatch.ElapsedMilliseconds / 1000).ToString("#0.000"));
                }
                _logger.Info(message);
            }
        }

        #endregion
    }
}
