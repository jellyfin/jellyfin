using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.System;
using Microsoft.Extensions.Logging;
using OperatingSystem = MediaBrowser.Common.System.OperatingSystem;

namespace IsoMounter
{
    /// <summary>
    /// The ISO manager implementation for Linux.
    /// </summary>
    public class LinuxIsoManager : IIsoMounter
    {
        private const string MountCommand = "mount";
        private const string UnmountCommand = "umount";
        private const string SudoCommand = "sudo";

        private readonly ILogger _logger;
        private readonly string _mountPointRoot;

        /// <summary>
        /// Initializes a new instance of the <see cref="LinuxIsoManager" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public LinuxIsoManager(ILogger logger)
        {
            _logger = logger;

            _mountPointRoot = Path.DirectorySeparatorChar + "tmp" + Path.DirectorySeparatorChar + "Emby";

            _logger.LogDebug(
                "[{0}] System PATH is currently set to [{1}].",
                Name,
                Environment.GetEnvironmentVariable("PATH") ?? string.Empty);

            _logger.LogDebug(
                "[{0}] System path separator is [{1}].",
                Name,
                Path.PathSeparator);

            _logger.LogDebug(
                "[{0}] Mount point root is [{1}].",
                Name,
                _mountPointRoot);
        }

        /// <inheritdoc />
        public string Name => "LinuxMount";

#pragma warning disable SA1300
#pragma warning disable SA1400
        [DllImport("libc", SetLastError = true)]
        static extern uint getuid();

#pragma warning restore SA1300
#pragma warning restore SA1400

        /// <inheritdoc />
        public bool CanMount(string path)
        {
            if (OperatingSystem.Id != OperatingSystemId.Linux)
            {
                return false;
            }

            _logger.LogInformation(
                "[{0}] Checking we can attempt to mount [{1}], Extension = [{2}], Operating System = [{3}].",
                Name,
                path,
                Path.GetExtension(path),
                OperatingSystem.Name);

            return string.Equals(Path.GetExtension(path), ".iso", StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public Task<IIsoMount> Mount(string isoPath, CancellationToken cancellationToken)
        {
            string cmdArguments;
            string cmdFilename;
            string mountPoint = Path.Combine(_mountPointRoot, Guid.NewGuid().ToString());

            if (string.IsNullOrEmpty(isoPath))
            {
                throw new ArgumentNullException(nameof(isoPath));
            }

            _logger.LogInformation(
                "[{Name}] Attempting to mount [{Path}].",
                Name,
                isoPath);

            _logger.LogDebug(
                "[{Name}] ISO will be mounted at [{Path}].",
                Name,
                mountPoint);

            try
            {
                Directory.CreateDirectory(mountPoint);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new IOException("Unable to create mount point(Permission denied) for " + isoPath, ex);
            }
            catch (Exception ex)
            {
                throw new IOException("Unable to create mount point for " + isoPath, ex);
            }

            if (GetUID() == 0)
            {
                cmdFilename = MountCommand;
                cmdArguments = string.Format(
                    CultureInfo.InvariantCulture,
                    "\"{0}\" \"{1}\"",
                    isoPath,
                    mountPoint);
            }
            else
            {
                cmdFilename = SudoCommand;
                cmdArguments = string.Format(
                    CultureInfo.InvariantCulture,
                    "\"{0}\" \"{1}\" \"{2}\"",
                    MountCommand,
                    isoPath,
                    mountPoint);
            }

            _logger.LogDebug(
                "[{0}] Mount command [{1}], mount arguments [{2}].",
                Name,
                cmdFilename,
                cmdArguments);

            int exitcode = ExecuteCommand(cmdFilename, cmdArguments);
            if (exitcode == 0)
            {
                _logger.LogInformation(
                    "[{0}] ISO mount completed successfully.",
                    Name);

                return Task.FromResult<IIsoMount>(new LinuxMount(this, isoPath, mountPoint));
            }

            _logger.LogInformation(
                "[{0}] ISO mount completed with errors.",
                Name);

            try
            {
                Directory.Delete(mountPoint, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{Name}] Unhandled exception removing mount point.", Name);
                throw;
            }

            throw new ExternalException("Mount command failed", exitcode);
        }

        private uint GetUID()
        {
            var uid = getuid();

            _logger.LogDebug(
                "[{0}] GetUserId() returned [{2}].",
                Name,
                uid);

            return uid;
        }

        private int ExecuteCommand(string cmdFilename, string cmdArguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = cmdFilename,
                Arguments = cmdArguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                ErrorDialog = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            var process = new Process()
            {
                StartInfo = startInfo
            };

            try
            {
                process.Start();

                _logger.LogDebug(
                    "[{Name}] Standard output from process is [{Error}].",
                    Name,
                    process.StandardOutput.ReadToEnd());

                _logger.LogDebug(
                    "[{Name}] Standard error from process is [{Error}].",
                    Name,
                    process.StandardError.ReadToEnd());

                return process.ExitCode;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "[{Name}] Unhandled exception executing command.", Name);
                throw;
            }
            finally
            {
                process?.Dispose();
            }
        }

        /// <summary>
        /// Unmounts the specified mount.
        /// </summary>
        /// <param name="mount">The mount.</param>
        internal void OnUnmount(LinuxMount mount)
        {
            if (mount == null)
            {
                throw new ArgumentNullException(nameof(mount));
            }

            _logger.LogInformation(
                "[{0}] Attempting to unmount ISO [{1}] mounted on [{2}].",
                Name,
                mount.IsoPath,
                mount.MountedPath);

            string cmdArguments;
            string cmdFilename;

            if (GetUID() == 0)
            {
                cmdFilename = UnmountCommand;
                cmdArguments = string.Format(
                    CultureInfo.InvariantCulture,
                    "\"{0}\"",
                    mount.MountedPath);
            }
            else
            {
                cmdFilename = SudoCommand;
                cmdArguments = string.Format(
                    CultureInfo.InvariantCulture,
                    "\"{0}\" \"{1}\"",
                    UnmountCommand,
                    mount.MountedPath);
            }

            _logger.LogDebug(
                "[{0}] Umount command [{1}], umount arguments [{2}].",
                Name,
                cmdFilename,
                cmdArguments);

            int exitcode = ExecuteCommand(cmdFilename, cmdArguments);
            if (exitcode == 0)
            {
                _logger.LogInformation(
                    "[{0}] ISO unmount completed successfully.",
                    Name);
            }
            else
            {
                _logger.LogInformation(
                    "[{0}] ISO unmount completed with errors.",
                    Name);
            }

            try
            {
                Directory.Delete(mount.MountedPath, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{Name}] Unhandled exception removing mount point.", Name);
                throw;
            }

            throw new ExternalException("Mount command failed", exitcode);
        }
    }
}
