#pragma warning disable CA1031

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.MediaEncoding.Encoder;

/// <summary>
/// Helper class for Apple platform specific operations.
/// </summary>
[SupportedOSPlatform("macos")]
public static partial class ApplePlatformHelper
{
    private static readonly string[] _av1DecodeBlacklistedCpuClass = ["M1", "M2"];

    // The first Intel model of each Mac line released in 2017 or later. Those support HEVC and hardware tone-mapping,
    // except for the MacBook Air (13-inch, 2017), which shares MacBookAir7,2 with the 2015 model and is therefore not listed.
    // See https://jellyfin.org/docs/general/post-install/transcoding/hardware-acceleration/apple
    private static readonly Dictionary<string, int> _firstFullVideoToolboxIntelModels = new(StringComparer.Ordinal)
    {
        ["MacBook"] = 10,
        ["MacBookAir"] = 8,
        ["MacBookPro"] = 14,
        ["iMac"] = 18,
        ["iMacPro"] = 1,
        ["Macmini"] = 8,
        ["MacPro"] = 7
    };

    [GeneratedRegex(@"^(?<line>[A-Za-z]+)(?<major>\d+),\d+$")]
    private static partial Regex ModelIdentifierRegex();

    internal static string GetSysctlValue(string name)
    {
        nuint length = 0;
        // Get length of the value
        int osStatus = sysctlbyname(name, Span<byte>.Empty, ref length, IntPtr.Zero, 0);
        if (osStatus != 0 || length == 0)
        {
            throw new NotSupportedException($"Failed to get sysctl value for {name} with error {osStatus}");
        }

        byte[] buffer = ArrayPool<byte>.Shared.Rent((int)length);
        try
        {
            osStatus = sysctlbyname(name, buffer.AsSpan()[..(int)length], ref length, IntPtr.Zero, 0);
            if (osStatus != 0)
            {
                throw new NotSupportedException($"Failed to get sysctl value for {name} with error {osStatus}");
            }

            if (length < 1)
            {
                return string.Empty;
            }

            ReadOnlySpan<byte> data = buffer.AsSpan()[..(int)(length - 1)];
            return Encoding.UTF8.GetString(data);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Check if the current system has hardware acceleration for AV1 decoding.
    /// </summary>
    /// <param name="logger">The logger used for error logging.</param>
    /// <returns>Boolean indicates the hwaccel support.</returns>
    public static bool HasAv1HardwareAccel(ILogger logger)
    {
        if (!RuntimeInformation.OSArchitecture.Equals(Architecture.Arm64))
        {
            return false;
        }

        try
        {
            string cpuBrandString = GetSysctlValue("machdep.cpu.brand_string");
            return !_av1DecodeBlacklistedCpuClass.Any(blacklistedCpuClass => cpuBrandString.Contains(blacklistedCpuClass, StringComparison.OrdinalIgnoreCase));
        }
        catch (NotSupportedException e)
        {
            logger.LogError("Error getting CPU brand string: {Message}", e.Message);
        }
        catch (Exception e)
        {
            logger.LogError("Unknown error occured: {Exception}", e);
        }

        return false;
    }

    /// <summary>
    /// Applies the VideoToolbox settings the Jellyfin documentation recommends for this Mac.
    /// </summary>
    /// <param name="options">The encoding options to update.</param>
    /// <param name="logger">The logger used for logging the applied settings.</param>
    public static void ApplyRecommendedVideoToolboxOptions(EncodingOptions options, ILogger logger)
    {
        string model = string.Empty;
        try
        {
            model = GetSysctlValue("hw.model");
        }
        catch (NotSupportedException e)
        {
            logger.LogError(e, "Error getting Mac model");
        }

        var isAppleSilicon = RuntimeInformation.OSArchitecture.Equals(Architecture.Arm64);
        ApplyRecommendedVideoToolboxOptions(options, model, isAppleSilicon, isAppleSilicon && HasAv1HardwareAccel(logger));

        logger.LogInformation(
            "Enabled VideoToolbox hardware acceleration for {Model}, decoding {Codecs} in hardware, tone-mapping {Tonemapping}",
            model,
            options.HardwareDecodingCodecs,
            options.EnableVideoToolboxTonemapping ? "enabled" : "disabled");
    }

    internal static void ApplyRecommendedVideoToolboxOptions(EncodingOptions options, string model, bool isAppleSilicon, bool hasAv1HardwareAccel)
    {
        options.HardwareAccelerationType = HardwareAccelerationType.videotoolbox;
        options.EnableHardwareEncoding = true;

        if (isAppleSilicon)
        {
            options.HardwareDecodingCodecs = hasAv1HardwareAccel ? ["h264", "hevc", "av1"] : ["h264", "hevc"];
            options.EnableVideoToolboxTonemapping = true;
        }
        else if (IsFullVideoToolboxIntelModel(model))
        {
            options.HardwareDecodingCodecs = ["h264", "hevc"];
            options.EnableVideoToolboxTonemapping = true;
        }
        else
        {
            // Any Mac with VideoToolbox can decode and encode H.264
            options.HardwareDecodingCodecs = ["h264"];
            options.EnableVideoToolboxTonemapping = false;
        }
    }

    internal static bool IsFullVideoToolboxIntelModel(string model)
    {
        var match = ModelIdentifierRegex().Match(model);
        return match.Success
            && _firstFullVideoToolboxIntelModels.TryGetValue(match.Groups["line"].Value, out var firstMajor)
            && int.TryParse(match.Groups["major"].ValueSpan, CultureInfo.InvariantCulture, out var major)
            && major >= firstMajor;
    }

    [LibraryImport("libc", EntryPoint = "sysctlbyname", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    internal static partial int sysctlbyname([MarshalAs(UnmanagedType.LPStr)] string name, Span<byte> oldp, ref nuint oldlenp, IntPtr newp, nuint newlen);
}
