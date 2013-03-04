using System;
using System.Threading;

namespace MediaBrowser.Common.Kernel
{
    /// <summary>
    /// This is just a collection of semaphores to control the number of concurrent executions of various resources
    /// </summary>
    public class ResourcePool : IDisposable
    {
        /// <summary>
        /// You tube
        /// </summary>
        public readonly SemaphoreSlim YouTube = new SemaphoreSlim(5, 5);

        /// <summary>
        /// The trakt
        /// </summary>
        public readonly SemaphoreSlim Trakt = new SemaphoreSlim(5, 5);

        /// <summary>
        /// The tv db
        /// </summary>
        public readonly SemaphoreSlim TvDb = new SemaphoreSlim(5, 5);

        /// <summary>
        /// The movie db
        /// </summary>
        public readonly SemaphoreSlim MovieDb = new SemaphoreSlim(5, 5);

        /// <summary>
        /// The fan art
        /// </summary>
        public readonly SemaphoreSlim FanArt = new SemaphoreSlim(5, 5);

        /// <summary>
        /// The mb
        /// </summary>
        public readonly SemaphoreSlim Mb = new SemaphoreSlim(5, 5);

        /// <summary>
        /// The mb
        /// </summary>
        public readonly SemaphoreSlim Lastfm = new SemaphoreSlim(5, 5);

        /// <summary>
        /// Apple doesn't seem to like too many simulataneous requests.
        /// </summary>
        public readonly SemaphoreSlim AppleTrailerVideos = new SemaphoreSlim(1, 1);

        /// <summary>
        /// The apple trailer images
        /// </summary>
        public readonly SemaphoreSlim AppleTrailerImages = new SemaphoreSlim(1, 1);
        
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
                YouTube.Dispose();
                Trakt.Dispose();
                TvDb.Dispose();
                MovieDb.Dispose();
                FanArt.Dispose();
                Mb.Dispose();
                AppleTrailerVideos.Dispose();
                AppleTrailerImages.Dispose();
            }
        }
    }
}
