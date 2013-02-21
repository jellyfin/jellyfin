using MediaBrowser.Common.IO;
using MediaBrowser.Model.Logging;
using System;

namespace MediaBrowser.IsoMounter
{
    /// <summary>
    /// Class IsoMount
    /// </summary>
    internal class PismoMount : IIsoMount
    {
        /// <summary>
        /// Gets or sets the iso path.
        /// </summary>
        /// <value>The iso path.</value>
        public string IsoPath { get; internal set; }

        /// <summary>
        /// Gets the mounted path.
        /// </summary>
        /// <value>The mounted path.</value>
        public string MountedPath { get; internal set; }

        /// <summary>
        /// The PFM file mount
        /// </summary>
        private PfmFileMount _pfmFileMount;

        /// <summary>
        /// The _iso manager
        /// </summary>
        private readonly PismoIsoManager _isoManager;

        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        /// <value>The logger.</value>
        private ILogger Logger { get; set; }

        /// <summary>
        /// Prevents a default instance of the <see cref="PismoMount" /> class from being created.
        /// </summary>
        /// <param name="mount">The mount.</param>
        /// <param name="isoPath">The iso path.</param>
        /// <param name="isoManager">The iso manager.</param>
        /// <param name="logger">The logger.</param>
        internal PismoMount(PfmFileMount mount, string isoPath, PismoIsoManager isoManager, ILogger logger)
        {
            _pfmFileMount = mount;
            IsoPath = isoPath;
            _isoManager = isoManager;
            Logger = logger;

            MountedPath = mount.GetMount().GetUncName();
        }

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
            UnMount();
        }

        /// <summary>
        /// Uns the mount.
        /// </summary>
        private void UnMount()
        {
            if (_pfmFileMount != null)
            {
                Logger.Info("Unmounting {0}", IsoPath);

                _pfmFileMount.Cancel();
                _pfmFileMount.Detach();

                _isoManager.OnUnmount(this);

                _pfmFileMount.Dispose();
                _pfmFileMount = null;
            }
        }
    }
}
