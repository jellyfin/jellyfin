using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Controller.HardwareAccelerationDevices
{
    /// <inheritdoc/>
    public class LinuxDrmDeviceNodesDiscoverer : ILinuxDrmDeviceNodesDiscoverer
    {
        private const string SysfsDrmDirPath = "/sys/class/drm";
        private const string UdevByPathDrmDeviceSymbolicLinksDirPath = "/dev/dri/by-path";
        private readonly ILogger<LinuxDrmDeviceNodesDiscoverer> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="LinuxDrmDeviceNodesDiscoverer"/> class.
        /// </summary>
        /// <param name="logger">Logger.</param>
        public LinuxDrmDeviceNodesDiscoverer(ILogger<LinuxDrmDeviceNodesDiscoverer> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<DrmDeviceNode>> DiscoverDrmDeviceNodesAsync(CancellationToken cancellationToken)
        {
            if (!OperatingSystem.IsLinux())
            {
                _logger.LogWarning("A {ClassName} method was called on a non-Linux operating system", nameof(LinuxDrmDeviceNodesDiscoverer));
                return Enumerable.Empty<DrmDeviceNode>();
            }

            if (!Directory.Exists(SysfsDrmDirPath))
            {
                _logger.LogError("Error while discovering DRM device nodes: '{SysfsDrmDirPath}' does not exist", SysfsDrmDirPath);
                return Enumerable.Empty<DrmDeviceNode>();
            }

            // "/dev/dri/renderD128": "/dev/dri/by-path/pci-0000:00:02.0-render"
            Dictionary<string, string>? udevDrmDeviceSymbolicLinksByDevFiles = GetUdevDrmDeviceSymbolicLinksByDevFiles();

            var drmDeviceNodes = new List<DrmDeviceNode>();

            var drmDeviceNodesDirs = Directory.GetDirectories(SysfsDrmDirPath);

            foreach (var drmDeviceNodeDir in drmDeviceNodesDirs)
            {
                try
                {
                    // /sys/class/drm/renderD129/dev
                    var sysfsDeviceNodeDevFilePath = Path.Combine(drmDeviceNodeDir, "dev");

                    if (!File.Exists(sysfsDeviceNodeDevFilePath))
                    {
                        // probably port device node like "card1-HDMI-A-1"
                        continue;
                    }

                    var drmNodeDeviceLinkTarget = new DirectoryInfo(drmDeviceNodeDir)
                        .ResolveLinkTarget(returnFinalTarget: true);

                    if (drmNodeDeviceLinkTarget is null)
                    {
                        _logger.LogWarning("Error while discovering DRM device nodes: can not resolve symbolic directory link '{SysfsDrmDeviceNodeDirectory}' directory. Skipping this device...", drmDeviceNodeDir);
                        continue;
                    }

                    // /sys/devices/....../drm/renderD128
                    var realDrmDeviceNodePath = drmNodeDeviceLinkTarget.FullName.Trim();

                    // /sys/devices/....../drm/renderD128/device
                    var drmDeviceHardwareDirectorySymbolicLink = Path.Combine(realDrmDeviceNodePath, "device");

                    if (!Directory.Exists(drmDeviceHardwareDirectorySymbolicLink))
                    {
                        _logger.LogWarning("Error while discovering DRM device nodes: no 'device' directory found in '{SysfsDrmDeviceNodeDirectory}' directory. Skipping this device...", realDrmDeviceNodePath);
                        continue;
                    }

                    var hardwareDeviceSysfsDirectoryInfo = new DirectoryInfo(drmDeviceHardwareDirectorySymbolicLink)
                        .ResolveLinkTarget(returnFinalTarget: true);

                    if (hardwareDeviceSysfsDirectoryInfo is null)
                    {
                        _logger.LogWarning("Error while discovering DRM device nodes: '{SysfsDrmDeviceNodeDeviceDirectory}' symbolic directory link can not be resolved. Skipping this device...", drmDeviceHardwareDirectorySymbolicLink);
                        continue;
                    }

                    // full path to the actual directory of the physical device
                    // Example: "/sys/devices/pci0000:00/0000:00:01.0/0000:01:00.0/0000:02:00.0/0000:03:00.0"
                    var hardwareDeviceSysfsFullPath = hardwareDeviceSysfsDirectoryInfo.FullName.Trim();

                    // Example: "/sys/devices/pci0000:00/..../0000:03:00.0/uevent"
                    var ueventFileFullPath = Path.Combine(realDrmDeviceNodePath, "uevent");

                    (int MinorNumber, string DevFileFullPath) parsedUeventData;
                    try
                    {
                        parsedUeventData = await ParseUeventFileAsync(ueventFileFullPath, cancellationToken).ConfigureAwait(false);
                    }
                    catch (UeventFileParseException e)
                    {
                        _logger.LogWarning("Error while discovering DRM device nodes: uevent file parsing error: '{ErrorMessage}'. uevent file full path: '{UeventFileFullPath}'. Skipping this device...", e.Message, e.UeventFileFullPath);
                        continue;
                    }

                    if (!File.Exists(parsedUeventData.DevFileFullPath))
                    {
                        _logger.LogWarning("Error while discovering DRM device nodes: '{DevFileFullPath}' file does not exist. Skipping this device...", parsedUeventData.DevFileFullPath);
                        continue;
                    }

                    DrmDeviceNodeType? drmDeviceNodeType = GetDrmDeviceNodeTypeFromMinorNumber(parsedUeventData.MinorNumber);

                    if (drmDeviceNodeType is null)
                    {
                        _logger.LogWarning("Error while discovering DRM device nodes: Can not recognize DRM device node type from its minor number: '{MinorNumber}'. Skipping this device...", parsedUeventData.MinorNumber);
                        continue;
                    }

                    drmDeviceNodes.Add(new DrmDeviceNode
                    {
                        DrmDeviceNodeFileFullPath = parsedUeventData.DevFileFullPath,
                        UdevByPathDrmDeviceNodeSymbolicLinkFullPath = udevDrmDeviceSymbolicLinksByDevFiles?.GetValueOrDefault(parsedUeventData.DevFileFullPath),
                        DrmDeviceNodeType = (DrmDeviceNodeType)drmDeviceNodeType,
                        HardwareDeviceSysfsFullPath = hardwareDeviceSysfsFullPath
                    });
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    _logger.LogError("Error while discovering DRM device nodes: '{ErrorMessage}'. Skipping this device...", e.Message);
                }
            }

            return drmDeviceNodes;
        }

        private static Dictionary<string, string>? GetUdevDrmDeviceSymbolicLinksByDevFiles()
        {
            Dictionary<string, string>? udevDrmDeviceSymbolicLinksByDevFiles = null;

            if (Directory.Exists(UdevByPathDrmDeviceSymbolicLinksDirPath))
            {
                var devFileLinks = Directory.GetFiles(UdevByPathDrmDeviceSymbolicLinksDirPath);

                if (devFileLinks.Length > 0)
                {
                    udevDrmDeviceSymbolicLinksByDevFiles = new Dictionary<string, string>(devFileLinks.Length);

                    foreach (var devFileLink in devFileLinks)
                    {
                        var devFileLinkInfo = new FileInfo(devFileLink).ResolveLinkTarget(true);

                        if (devFileLinkInfo is not null)
                        {
                            udevDrmDeviceSymbolicLinksByDevFiles[devFileLinkInfo.FullName] = devFileLink;
                        }
                    }
                }
            }

            return udevDrmDeviceSymbolicLinksByDevFiles;
        }

        private static async Task<(int MinorNumber, string DevFileFullPath)> ParseUeventFileAsync(string ueventFileFullPath, CancellationToken cancellationToken)
        {
            if (!File.Exists(ueventFileFullPath))
            {
                throw new UeventFileParseException(ueventFileFullPath, "File does not exist");
            }

            var ueventLines = await File.ReadAllLinesAsync(ueventFileFullPath, cancellationToken).ConfigureAwait(false);

            var devPathLineSubstrings = ueventLines
                .FirstOrDefault(x => x.StartsWith("DEVNAME=", StringComparison.Ordinal))
                ?.Split('=');

            var minorLineSubstrings = ueventLines
                .FirstOrDefault(x => x.StartsWith("MINOR=", StringComparison.Ordinal))
                ?.Split('=');

            if (devPathLineSubstrings is null ||
                minorLineSubstrings is null ||
                devPathLineSubstrings.Length != 2 ||
                minorLineSubstrings.Length != 2 ||
                !int.TryParse(minorLineSubstrings[1].Trim(), out var minorNumber))
            {
                throw new UeventFileParseException(ueventFileFullPath, "File content can not be parsed correctly");
            }

            // dri/renderD129
            var devPath = devPathLineSubstrings[1].Trim();
            // /dev/dri/renderD129
            var devFileFullPath = Path.Combine("/dev", devPath);

            return (minorNumber, devFileFullPath);
        }

        private static DrmDeviceNodeType? GetDrmDeviceNodeTypeFromMinorNumber(int minorNumber)
        {
            if (minorNumber > 127)
            {
                return DrmDeviceNodeType.Render;
            }

            if (minorNumber >= 0 && minorNumber < 64)
            {
                return DrmDeviceNodeType.Primary;
            }

            return null;
        }

        private sealed class UeventFileParseException : Exception
        {
            public UeventFileParseException(string ueventFileFullPath, string message) : base(message)
            {
                UeventFileFullPath = ueventFileFullPath;
            }

            public string UeventFileFullPath { get; private set; }
        }
    }
}
