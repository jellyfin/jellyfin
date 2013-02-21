using MediaBrowser.Common.Logging;
using MediaBrowser.Model.Logging;
using System;
using System.Threading.Tasks;

namespace MediaBrowser.Common.Events
{
    /// <summary>
    /// Class EventHelper
    /// </summary>
    public static class EventHelper
    {
        /// <summary>
        /// The logger
        /// </summary>
        private static readonly ILogger Logger = LogManager.GetLogger("EventHelper");

        /// <summary>
        /// Fires the event.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="EventArgs" /> instance containing the event data.</param>
        public static void QueueEventIfNotNull(EventHandler handler, object sender, EventArgs args)
        {
            if (handler != null)
            {
                Task.Run(() =>
                {
                    try
                    {
                        handler(sender, args);
                    }
                    catch (Exception ex)
                    {
                        Logger.ErrorException("Error in event handler", ex);
                    }
                });
            }
        }

        /// <summary>
        /// Queues the event.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handler">The handler.</param>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The args.</param>
        public static void QueueEventIfNotNull<T>(EventHandler<T> handler, object sender, T args)
        {
            if (handler != null)
            {
                Task.Run(() =>
                {
                    try
                    {
                        handler(sender, args);
                    }
                    catch (Exception ex)
                    {
                        Logger.ErrorException("Error in event handler", ex);
                    }
                });
            }
        }

        /// <summary>
        /// Fires the event.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="EventArgs" /> instance containing the event data.</param>
        public static void FireEventIfNotNull(EventHandler handler, object sender, EventArgs args)
        {
            if (handler != null)
            {
                try
                {
                    handler(sender, args);
                }
                catch (Exception ex)
                {
                    Logger.ErrorException("Error in event handler", ex);
                }
            }
        }

        /// <summary>
        /// Fires the event.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handler">The handler.</param>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The args.</param>
        public static void FireEventIfNotNull<T>(EventHandler<T> handler, object sender, T args)
        {
            if (handler != null)
            {
                try
                {
                    handler(sender, args);
                }
                catch (Exception ex)
                {
                    Logger.ErrorException("Error in event handler", ex);
                }
            }
        }
    }
}
