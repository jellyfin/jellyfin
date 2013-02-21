using MediaBrowser.Common.IO;
using MediaBrowser.Model.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.IsoMounter
{
    /// <summary>
    /// Class IsoManager
    /// </summary>
    public class PismoIsoManager : IIsoManager
    {
        /// <summary>
        /// The mount semaphore - limit to four at a time.
        /// </summary>
        private readonly SemaphoreSlim _mountSemaphore = new SemaphoreSlim(4,4);

        /// <summary>
        /// The PFM API
        /// </summary>
        private PfmApi _pfmApi;
        /// <summary>
        /// The _PFM API initialized
        /// </summary>
        private bool _pfmApiInitialized;
        /// <summary>
        /// The _PFM API sync lock
        /// </summary>
        private object _pfmApiSyncLock = new object();
        /// <summary>
        /// Gets the display prefs.
        /// </summary>
        /// <value>The display prefs.</value>
        private PfmApi PfmApi
        {
            get
            {
                LazyInitializer.EnsureInitialized(ref _pfmApi, ref _pfmApiInitialized, ref _pfmApiSyncLock, () =>
                {
                    var err = PfmStatic.InstallCheck();

                    if (err != PfmInst.installed)
                    {
                        throw new Exception("Pismo File Mount Audit Package is not installed");
                    } 
                    
                    PfmApi pfmApi;

                    err = PfmStatic.ApiFactory(out pfmApi);

                    if (err != 0)
                    {
                        throw new IOException("Unable to open PFM Api. Pismo File Mount Audit Package is probably not installed.");
                    }

                    return pfmApi;
                });
                return _pfmApi;
            }
        }

        /// <summary>
        /// The _has initialized
        /// </summary>
        private bool _hasInitialized;

        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        /// <value>The logger.</value>
        private ILogger Logger { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PismoIsoManager" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public PismoIsoManager(ILogger logger)
        {
            Logger = logger;

            _myPfmFileMountUi = new MyPfmFileMountUi(Logger);
        }

        /// <summary>
        /// The _my PFM file mount UI
        /// </summary>
        private readonly MyPfmFileMountUi _myPfmFileMountUi;

        /// <summary>
        /// Mounts the specified iso path.
        /// </summary>
        /// <param name="isoPath">The iso path.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="visibleToAllProcesses">if set to <c>true</c> [visible to all processes].</param>
        /// <returns>IsoMount.</returns>
        /// <exception cref="System.ArgumentNullException">isoPath</exception>
        /// <exception cref="System.IO.IOException">Unable to create mount.</exception>
        public async Task<IIsoMount> Mount(string isoPath, CancellationToken cancellationToken, bool visibleToAllProcesses = true)
        {
            if (string.IsNullOrEmpty(isoPath))
            {
                throw new ArgumentNullException("isoPath");
            }

            PfmFileMount mount;
            var err = PfmApi.FileMountCreate(out mount);

            if (err != 0)
            {
                throw new IOException("Unable to create mount for " + isoPath);
            }

            _hasInitialized = true;

            var fmp = new PfmFileMountCreateParams { };

            fmp.ui = _myPfmFileMountUi;

            fmp.fileMountFlags |= PfmFileMountFlag.inProcess;

            if (visibleToAllProcesses)
            {
                fmp.visibleProcessId = PfmVisibleProcessId.all;
            }

            fmp.mountFileName = isoPath;

            // unc only
            fmp.mountFlags |= PfmMountFlag.uncOnly;
            fmp.mountFlags |= PfmMountFlag.noShellNotify;
            fmp.mountFlags |= PfmMountFlag.readOnly;

            Logger.Info("Mounting {0}", isoPath);

            await _mountSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            err = mount.Start(fmp);

            if (err != 0)
            {
                _mountSemaphore.Release();
                mount.Dispose();
                throw new IOException("Unable to start mount for " + isoPath);
            }

            err = mount.WaitReady();

            if (err != 0)
            {
                _mountSemaphore.Release();
                mount.Dispose();
                throw new IOException("Unable to start mount for " + isoPath);
            }

            return new PismoMount(mount, isoPath, this, Logger);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool dispose)
        {
            if (dispose)
            {
                if (_hasInitialized)
                {
                    Logger.Info("Disposing PfmPapi");
                    _pfmApi.Dispose();

                    Logger.Info("PfmStatic.ApiUnload");
                    PfmStatic.ApiUnload();
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance can mount.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns><c>true</c> if this instance can mount the specified path; otherwise, <c>false</c>.</returns>
        /// <value><c>true</c> if this instance can mount; otherwise, <c>false</c>.</value>
        public bool CanMount(string path)
        {
            try
            {
                return string.Equals(Path.GetExtension(path), ".iso", StringComparison.OrdinalIgnoreCase) && PfmApi != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Called when [unmount].
        /// </summary>
        /// <param name="mount">The mount.</param>
        internal void OnUnmount(PismoMount mount)
        {
            _mountSemaphore.Release();
        }
    }
}
