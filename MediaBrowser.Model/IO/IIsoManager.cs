#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Model.IO
{
    public interface IIsoManager
    {
        /// <summary>
        /// Mounts the specified iso path.
        /// </summary>
        /// <param name="isoPath">The iso path.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>IsoMount.</returns>
        /// <exception cref="IOException">Unable to create mount.</exception>
        Task<IIsoMount> Mount(string isoPath, CancellationToken cancellationToken);

        /// <summary>
        /// Determines whether this instance can mount the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns><c>true</c> if this instance can mount the specified path; otherwise, <c>false</c>.</returns>
        bool CanMount(string path);

        /// <summary>
        /// Adds the parts.
        /// </summary>
        /// <param name="mounters">The mounters.</param>
        void AddParts(IEnumerable<IIsoMounter> mounters);
    }
}
