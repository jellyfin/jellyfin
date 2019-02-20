using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Diagnostics;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.System;
using Microsoft.Extensions.Logging;

namespace IsoMounter
{
    public class LinuxIsoManager : IIsoMounter
    {
        [DllImport("libc", SetLastError = true)]
        static extern uint getuid();

        #region Private Fields

        private readonly IEnvironmentInfo EnvironmentInfo;
        private readonly bool ExecutablesAvailable;
        private readonly ILogger _logger;
        private readonly string MountCommand;
        private readonly string MountPointRoot;
        private readonly IProcessFactory ProcessFactory;
        private readonly string SudoCommand;
        private readonly string UmountCommand;

        #endregion

        #region Constructor(s)

        public LinuxIsoManager(ILogger logger, IEnvironmentInfo environment, IProcessFactory processFactory)
        {

            EnvironmentInfo = environment;
            _logger = logger;
            ProcessFactory = processFactory;

            MountPointRoot = Path.DirectorySeparatorChar + "tmp" + Path.DirectorySeparatorChar + "Emby";

            _logger.LogDebug(
                "[{0}] System PATH is currently set to [{1}].",
                Name,
                Environment.GetEnvironmentVariable("PATH") ?? ""
            );

            _logger.LogDebug(
                "[{0}] System path separator is [{1}].",
                Name,
                Path.PathSeparator
            );

            _logger.LogDebug(
                "[{0}] Mount point root is [{1}].",
                Name,
                MountPointRoot
            );

            //
            // Get the location of the executables we need to support mounting/unmounting ISO images.
            //

            SudoCommand = GetFullPathForExecutable("sudo");

            _logger.LogInformation(
                "[{0}] Using version of [sudo] located at [{1}].",
                Name,
                SudoCommand
            );

            MountCommand = GetFullPathForExecutable("mount");

            _logger.LogInformation(
                "[{0}] Using version of [mount] located at [{1}].",
                Name,
                MountCommand
            );

            UmountCommand = GetFullPathForExecutable("umount");

            _logger.LogInformation(
                "[{0}] Using version of [umount] located at [{1}].",
                Name,
                UmountCommand
            );

            if (!string.IsNullOrEmpty(SudoCommand) && !string.IsNullOrEmpty(MountCommand) && !string.IsNullOrEmpty(UmountCommand))
            {
                ExecutablesAvailable = true;
            }
            else
            {
                ExecutablesAvailable = false;
            }

        }

        #endregion

        #region Interface Implementation for IIsoMounter

        public bool IsInstalled => true;

        public string Name => "LinuxMount";

        public bool RequiresInstallation => false;

        public bool CanMount(string path)
        {

            if (EnvironmentInfo.OperatingSystem != MediaBrowser.Model.System.OperatingSystem.Linux)
            {
                return false;
            }
            _logger.LogInformation(
                "[{0}] Checking we can attempt to mount [{1}], Extension = [{2}], Operating System = [{3}], Executables Available = [{4}].",
                Name,
                path,
                Path.GetExtension(path),
                EnvironmentInfo.OperatingSystem,
                ExecutablesAvailable
            );

            if (ExecutablesAvailable)
            {
                return string.Equals(Path.GetExtension(path), ".iso", StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                return false;
            }
        }

        public Task Install(CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public Task<IIsoMount> Mount(string isoPath, CancellationToken cancellationToken)
        {
            if (MountISO(isoPath, out LinuxMount mountedISO))
            {
                return Task.FromResult<IIsoMount>(mountedISO);
            }
            else
            {
                throw new IOException(string.Format(
                    "An error occurred trying to mount image [$0].",
                    isoPath
                ));
            }
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

            if (disposed)
            {
                return;
            }

            _logger.LogInformation(
                "[{0}] Disposing [{1}].",
                Name,
                disposing
            );

            if (disposing)
            {

                //
                // Free managed objects here.
                //

            }

            //
            // Free any unmanaged objects here.
            //

            disposed = true;

        }

        #endregion

        #region Private Methods

        private string GetFullPathForExecutable(string name)
        {

            foreach (string test in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
            {
                string path = test.Trim();

                if (!string.IsNullOrEmpty(path) && File.Exists(path = Path.Combine(path, name)))
                {
                    return Path.GetFullPath(path);
                }
            }

            return string.Empty;
        }

        private uint GetUID()
        {

            var uid = getuid();

            _logger.LogDebug(
                "[{0}] GetUserId() returned [{2}].",
                Name,
                uid
            );

            return uid;

        }

        private bool ExecuteCommand(string cmdFilename, string cmdArguments)
        {

            bool processFailed = false;

            var process = ProcessFactory.Create(
                new ProcessOptions
                {
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    FileName = cmdFilename,
                    Arguments = cmdArguments,
                    IsHidden = true,
                    ErrorDialog = false,
                    EnableRaisingEvents = true
                }
            );

            try
            {
                process.Start();

                //StreamReader outputReader = process.StandardOutput.;
                //StreamReader errorReader = process.StandardError;

                _logger.LogDebug(
                    "[{Name}] Standard output from process is [{Error}].",
                    Name,
                    process.StandardOutput.ReadToEnd()
                );

                _logger.LogDebug(
                    "[{Name}] Standard error from process is [{Error}].",
                    Name,
                    process.StandardError.ReadToEnd()
                );
            }
            catch (Exception ex)
            {
                processFailed = true;
                _logger.LogDebug(ex, "[{Name}] Unhandled exception executing command.", Name);
            }

            if (!processFailed && process.ExitCode == 0)
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        private bool MountISO(string isoPath, out LinuxMount mountedISO)
        {

            string cmdArguments;
            string cmdFilename;
            string mountPoint = Path.Combine(MountPointRoot, Guid.NewGuid().ToString());

            if (!string.IsNullOrEmpty(isoPath))
            {

                _logger.LogInformation(
                    "[{Name}] Attempting to mount [{Path}].",
                    Name,
                    isoPath
                );

                _logger.LogDebug(
                    "[{Name}] ISO will be mounted at [{Path}].",
                    Name,
                    mountPoint
                );

            }
            else
            {

                throw new ArgumentNullException(nameof(isoPath));

            }

            try
            {
                Directory.CreateDirectory(mountPoint);
            }
            catch (UnauthorizedAccessException)
            {
                throw new IOException("Unable to create mount point(Permission denied) for " + isoPath);
            }
            catch (Exception)
            {
                throw new IOException("Unable to create mount point for " + isoPath);
            }

            if (GetUID() == 0)
            {
                cmdFilename = MountCommand;
                cmdArguments = string.Format("\"{0}\" \"{1}\"", isoPath, mountPoint);
            }
            else
            {
                cmdFilename = SudoCommand;
                cmdArguments = string.Format("\"{0}\" \"{1}\" \"{2}\"", MountCommand, isoPath, mountPoint);
            }

            _logger.LogDebug(
                "[{0}] Mount command [{1}], mount arguments [{2}].",
                Name,
                cmdFilename,
                cmdArguments
            );

            if (ExecuteCommand(cmdFilename, cmdArguments))
            {

                _logger.LogInformation(
                    "[{0}] ISO mount completed successfully.",
                    Name
                );

                mountedISO = new LinuxMount(this, isoPath, mountPoint);

            }
            else
            {

                _logger.LogInformation(
                    "[{0}] ISO mount completed with errors.",
                    Name
                );

                try
                {
                    Directory.Delete(mountPoint, false);
                }
                catch (Exception ex)
                {
                    _logger.LogInformation(ex, "[{Name}] Unhandled exception removing mount point.", Name);
                }

                mountedISO = null;

            }

            return mountedISO != null;

        }

        private void UnmountISO(LinuxMount mount)
        {

            string cmdArguments;
            string cmdFilename;

            if (mount != null)
            {

                _logger.LogInformation(
                    "[{0}] Attempting to unmount ISO [{1}] mounted on [{2}].",
                    Name,
                    mount.IsoPath,
                    mount.MountedPath
                );

            }
            else
            {

                throw new ArgumentNullException(nameof(mount));

            }

            if (GetUID() == 0)
            {
                cmdFilename = UmountCommand;
                cmdArguments = string.Format("\"{0}\"", mount.MountedPath);
            }
            else
            {
                cmdFilename = SudoCommand;
                cmdArguments = string.Format("\"{0}\" \"{1}\"", UmountCommand, mount.MountedPath);
            }

            _logger.LogDebug(
                "[{0}] Umount command [{1}], umount arguments [{2}].",
                Name,
                cmdFilename,
                cmdArguments
            );

            if (ExecuteCommand(cmdFilename, cmdArguments))
            {

                _logger.LogInformation(
                    "[{0}] ISO unmount completed successfully.",
                    Name
                );

            }
            else
            {

                _logger.LogInformation(
                    "[{0}] ISO unmount completed with errors.",
                    Name
                );

            }

            try
            {
                Directory.Delete(mount.MountedPath, false);
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, "[{Name}] Unhandled exception removing mount point.", Name);
            }
        }

        #endregion

        #region Internal Methods

        internal void OnUnmount(LinuxMount mount)
        {

            UnmountISO(mount);

        }

        #endregion

    }

}

