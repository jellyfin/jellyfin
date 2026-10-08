using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Controller.HardwareAccelerationDevices.ConcreteDiscoverers;

/// <inheritdoc/>
public partial class LinuxVaapiDeviceDiscoverer : IHardwareAccelerationTypeDeviceDiscoverer
{
    private readonly ILinuxDrmDeviceNodesDiscoverer _linuxDrmDeviceNodesDiscoverer;
    private readonly ILogger<LinuxVaapiDeviceDiscoverer> _logger;
    private readonly string _ffmpegPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="LinuxVaapiDeviceDiscoverer"/> class.
    /// </summary>
    /// <param name="linuxDrmDeviceNodesDiscoverer">Linux drm nodes discoverer.</param>
    /// <param name="mediaEncoder">Media encoder.</param>
    /// <param name="logger">Logger.</param>
    public LinuxVaapiDeviceDiscoverer(
        ILinuxDrmDeviceNodesDiscoverer linuxDrmDeviceNodesDiscoverer,
        IMediaEncoder mediaEncoder,
        ILogger<LinuxVaapiDeviceDiscoverer> logger)
    {
        _linuxDrmDeviceNodesDiscoverer = linuxDrmDeviceNodesDiscoverer;
        _logger = logger;
        _ffmpegPath = mediaEncoder.EncoderPath;

        if (string.IsNullOrEmpty(_ffmpegPath))
        {
            logger.LogError("FFMPEG path not specified. Linux VAAPI devices can not be discovered.");
        }
    }

    /// <inheritdoc/>
    public HardwareAccelerationType HardwareAccelerationType => HardwareAccelerationType.vaapi;

    [GeneratedRegex(@"VAAPI driver:.*?\bfor\s+(?<device>.+)\s*\([^)]*\)\s*\.?$", RegexOptions.Multiline)]
    private static partial Regex FfmpegOutputSearchRegex();

    /// <inheritdoc/>
    public async Task<IEnumerable<HardwareAccelerationDevice>> DiscoverDevicesAsync(CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsLinux())
        {
            _logger.LogWarning("A {ClassName} method was called on a non-Linux operating system", nameof(LinuxVaapiDeviceDiscoverer));
            return Enumerable.Empty<HardwareAccelerationDevice>();
        }

        var drmDevicesNodes = (await _linuxDrmDeviceNodesDiscoverer
                .DiscoverDrmDeviceNodesAsync(cancellationToken).ConfigureAwait(false))
            .GroupBy(x => x.HardwareDeviceSysfsFullPath);

        var vaapiDevices = new List<HardwareAccelerationDevice>();

        foreach (var drmDeviceNodes in drmDevicesNodes)
        {
            var renderNode = drmDeviceNodes
                .FirstOrDefault(x => x.DrmDeviceNodeType == DrmDeviceNodeType.Render);
            var primaryNode = drmDeviceNodes
                .FirstOrDefault(x => x.DrmDeviceNodeType == DrmDeviceNodeType.Primary);

            if (renderNode is not null)
            {
                var vaapiDeviceInitializationResult = await TryToInitializeVaapiDevice(renderNode, cancellationToken).ConfigureAwait(false);

                if (vaapiDeviceInitializationResult.IsSuccessful)
                {
                    vaapiDevices.Add(new HardwareAccelerationDevice
                    {
                        HardwareAccelerationType = HardwareAccelerationType.vaapi,
                        HumanReadableName = vaapiDeviceInitializationResult.DriverName ?? "Unknown device",
                        Identifier = renderNode.DrmDeviceNodeFileFullPath
                    });

                    continue;
                }
            }

            if (primaryNode is not null)
            {
                var vaapiDeviceInitializationResult = await TryToInitializeVaapiDevice(primaryNode, cancellationToken).ConfigureAwait(false);

                if (vaapiDeviceInitializationResult.IsSuccessful)
                {
                    vaapiDevices.Add(new HardwareAccelerationDevice
                    {
                        HardwareAccelerationType = HardwareAccelerationType.vaapi,
                        HumanReadableName = vaapiDeviceInitializationResult.DriverName ?? "Unknown device",
                        Identifier = primaryNode.DrmDeviceNodeFileFullPath
                    });
                }
            }
        }

        return vaapiDevices;
    }

    private async Task<VaapiDeviceInitializationResult> TryToInitializeVaapiDevice(
        DrmDeviceNode node, CancellationToken cancellationToken)
    {
        var arguments = $"-hide_banner -v verbose -init_hw_device vaapi=va:{node.DrmDeviceNodeFileFullPath}";

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                StandardErrorEncoding = Encoding.UTF8,
                RedirectStandardError = true
            }
        };

        try
        {
            process.Start();

            using var stdErr = process.StandardError;
            var stdErrText = await stdErr.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            var match = FfmpegOutputSearchRegex().Match(stdErrText);

            if (match.Success)
            {
                var driverName = match.Groups["device"].Value.Trim();

                if (driverName.Contains(" - ", StringComparison.Ordinal))
                {
                    driverName = driverName.Split(" - ")[0].Trim();
                }

                return new VaapiDeviceInitializationResult { IsSuccessful = true, DriverName = driverName };
            }
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch
                {
                }
            }

            throw;
        }
        catch (Exception e)
        {
            _logger.LogError("Error during vaapi device initialization: {ErrorText}", e.Message);
        }

        return new VaapiDeviceInitializationResult { IsSuccessful = false };
    }

    private struct VaapiDeviceInitializationResult
    {
        public bool IsSuccessful;
        public string? DriverName;
    }
}
