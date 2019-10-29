using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.IO;

namespace Emby.Server.Implementations.IO
{
    /// <summary>
    /// Class IsoManager.
    /// </summary>
    public class IsoManager : IIsoManager
    {
        /// <summary>
        /// The _mounters.
        /// </summary>
        private readonly List<IIsoMounter> _mounters = new List<IIsoMounter>();

        /// <summary>
        /// Mounts the specified iso path.
        /// </summary>
        /// <param name="isoPath">The iso path.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns><see creaf="IsoMount" />.</returns>
        public Task<IIsoMount> Mount(string isoPath, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(isoPath))
            {
                throw new ArgumentNullException(nameof(isoPath));
            }

            var mounter = _mounters.FirstOrDefault(i => i.CanMount(isoPath));

            if (mounter == null)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "No mounters are able to mount {0}",
                        isoPath));
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
    }
}
