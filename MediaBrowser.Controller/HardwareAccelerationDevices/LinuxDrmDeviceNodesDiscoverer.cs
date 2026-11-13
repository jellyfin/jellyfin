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
                    // /sys/devices/pci0000:00/0000:00:01.0/0000:01:00.0/0000:02:00.0/0000:03:00.0
                    var hardwareDeviceSysfsFullPath = hardwareDeviceSysfsDirectoryInfo.FullName.Trim();

                    // /sys/devices/pci0000:00/..../0000:03:00.0/uevent
                    var ueventFileFullPath = Path.Combine(realDrmDeviceNodePath, "uevent");

                    if (!File.Exists(ueventFileFullPath))
                    {
                        _logger.LogWarning("Error while discovering DRM device nodes: no 'uevent' file found in '{SysfsDrmDeviceNodeDirectory}' directory. Skipping this device...", realDrmDeviceNodePath);
                        continue;
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
                        minorLineSubstrings.Length != 2)
                    {
                        _logger.LogWarning("Error while discovering DRM device nodes: '{UeventFilePath}' file content can not be parsed correctly.. Skipping this device...", ueventFileFullPath);
                        continue;
                    }

                    // dri/renderD129
                    var devPath = devPathLineSubstrings[1].Trim();
                    // /dev/dri/renderD129
                    var devFileFullPath = Path.Combine("/dev", devPath);

                    if (!File.Exists(devFileFullPath))
                    {
                        _logger.LogWarning("Error while discovering DRM device nodes: '{DevFileFullPath}' file does not exist.. Skipping this device...", devFileFullPath);
                        continue;
                    }

                    DrmDeviceNodeType? drmDeviceNodeType = null;

                    if (int.TryParse(minorLineSubstrings[1].Trim(), out var minorNumber))
                    {
                        drmDeviceNodeType = GetDrmDeviceNodeTypeFromMinorNumber(minorNumber);
                    }

                    if (drmDeviceNodeType is null)
                    {
                        continue;
                    }

                    drmDeviceNodes.Add(new DrmDeviceNode
                    {
                        DrmDeviceNodeFileFullPath = devFileFullPath,
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
    }
}
