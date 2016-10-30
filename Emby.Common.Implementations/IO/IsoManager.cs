using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.IO;

namespace Emby.Common.Implementations.IO
{
    /// <summary>
    /// Class IsoManager
    /// </summary>
    public class IsoManager : IIsoManager
    {
        /// <summary>
        /// The _mounters
        /// </summary>
        private readonly List<IIsoMounter> _mounters = new List<IIsoMounter>();

        /// <summary>
        /// Mounts the specified iso path.
        /// </summary>
        /// <param name="isoPath">The iso path.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>IsoMount.</returns>
        /// <exception cref="System.ArgumentNullException">isoPath</exception>
        /// <exception cref="System.ArgumentException"></exception>
        public Task<IIsoMount> Mount(string isoPath, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(isoPath))
            {
                throw new ArgumentNullException("isoPath");
            }

            var mounter = _mounters.FirstOrDefault(i => i.CanMount(isoPath));

            if (mounter == null)
            {
                throw new ArgumentException(string.Format("No mounters are able to mount {0}", isoPath));
            }

            return mounter.Mount(isoPath, cancellationToken);
        }

        /// <summary>
        /// Determines whether this instance can mount the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns><c>true</c> if this instance can mount the specified path; otherwise, <c>false</c>.</returns>
        public bool CanMount(string path)
        {
            return _mounters.Any(i => i.CanMount(path));
        }

        /// <summary>
        /// Adds the parts.
        /// </summary>
        /// <param name="mounters">The mounters.</param>
        public void AddParts(IEnumerable<IIsoMounter> mounters)
        {
            _mounters.AddRange(mounters);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            foreach (var mounter in _mounters)
            {
                mounter.Dispose();
            }
        }
    }
}
