using System;
using MediaBrowser.Model.IO;

namespace IsoMounter
{
    /// <summary>
    /// Class LinuxMount.
    /// </summary>
    internal class LinuxMount : IIsoMount
    {
        private readonly LinuxIsoManager _linuxIsoManager;

        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="LinuxMount" /> class.
        /// </summary>
        /// <param name="isoManager">The ISO manager that mounted this ISO file.</param>
        /// <param name="isoPath">The path to the ISO file.</param>
        /// <param name="mountFolder">The folder the ISO is mounted in.</param>
        internal LinuxMount(LinuxIsoManager isoManager, string isoPath, string mountFolder)
        {
            _linuxIsoManager = isoManager;

            IsoPath = isoPath;
            MountedPath = mountFolder;
        }

        /// <inheritdoc />
        public string IsoPath { get; }

        /// <inheritdoc />
        public string MountedPath { get; }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources and disposes of the managed resources used.
        /// </summary>
        /// <param name="disposing">Whether or not the managed resources should be disposed.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            _linuxIsoManager.OnUnmount(this);

            _disposed = true;
        }
    }
}
