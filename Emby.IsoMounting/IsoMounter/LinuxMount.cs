using System;
using MediaBrowser.Model.Diagnostics;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.System;

namespace IsoMounter
{
    internal class LinuxMount : IIsoMount
    {

        #region Private Fields

        private readonly LinuxIsoManager linuxIsoManager;

        #endregion

        #region Constructor(s)

        internal LinuxMount(LinuxIsoManager isoManager, string isoPath, string mountFolder)
        {

            linuxIsoManager = isoManager;

            IsoPath = isoPath;
            MountedPath = mountFolder;

        }

        #endregion

        #region Interface Implementation for IDisposable

        // Flag: Has Dispose already been called?
        private bool disposed = false;

        public void Dispose()
        {

            // Dispose of unmanaged resources.
            Dispose(true);

            // Suppress finalization.
            GC.SuppressFinalize(this);

        }

        protected virtual void Dispose(bool disposing)
        {

            if (disposed) {
                return;
            }
            
            if (disposing) {

                //
                // Free managed objects here.
                //

                linuxIsoManager.OnUnmount(this);

            }

            //
            // Free any unmanaged objects here.
            //

            disposed = true;

        }

        #endregion

        #region Interface Implementation for IIsoMount

        public string IsoPath { get; private set; }
        public string MountedPath { get; private set; }

        #endregion

    }

}

