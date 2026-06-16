#pragma warning disable CA1031

using System;
using System.Buffers;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.MediaEncoding.Encoder;

/// <summary>
/// Helper class for Apple platform specific operations.
/// </summary>
[SupportedOSPlatform("macos")]
public static partial class ApplePlatformHelper
{
    private static readonly string[] _av1DecodeBlacklistedCpuClass = ["M1", "M2"];

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

    [LibraryImport("libc", EntryPoint = "sysctlbyname", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    internal static partial int sysctlbyname([MarshalAs(UnmanagedType.LPStr)] string name, Span<byte> oldp, ref nuint oldlenp, IntPtr newp, nuint newlen);
}
