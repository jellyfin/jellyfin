using System;

namespace MediaBrowser.Model.IO
{
    /// <summary>
    /// Interface IIsoMount.
    /// </summary>
    public interface IIsoMount : IDisposable
    {
        /// <summary>
        /// Gets the iso path.
        /// </summary>
        /// <value>The iso path.</value>
        string IsoPath { get; }

        /// <summary>
        /// Gets the mounted path.
        /// </summary>
        /// <value>The mounted path.</value>
        string MountedPath { get; }
    }
}
