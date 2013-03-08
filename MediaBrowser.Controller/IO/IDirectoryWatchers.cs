using System;

namespace MediaBrowser.Controller.IO
{
    public interface IDirectoryWatchers : IDisposable
    {
        /// <summary>
        /// Add the path to our temporary ignore list.  Use when writing to a path within our listening scope.
        /// </summary>
        /// <param name="path">The path.</param>
        void TemporarilyIgnore(string path);

        /// <summary>
        /// Removes the temp ignore.
        /// </summary>
        /// <param name="path">The path.</param>
        void RemoveTempIgnore(string path);

        /// <summary>
        /// Starts this instance.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops this instance.
        /// </summary>
        void Stop();
    }
}