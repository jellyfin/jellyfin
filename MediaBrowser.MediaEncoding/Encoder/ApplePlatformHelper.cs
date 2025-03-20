#pragma warning disable CA1031

using System;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.MediaEncoding.Encoder;

/// <summary>
/// Helper class for Apple platform specific operations.
/// </summary>
public static class ApplePlatformHelper
{
    private static readonly string[] _av1DecodeBlacklistedCpuClass = ["M1", "M2"];

    private static string GetSysctlValue(string name)
    {
        IntPtr length = IntPtr.Zero;
        // Get length of the value
        int osStatus = SysctlByName(name, IntPtr.Zero, ref length, IntPtr.Zero, 0);

        if (osStatus != 0)
        {
            throw new NotSupportedException($"Failed to get sysctl value for {name} with error {osStatus}");
        }

        IntPtr buffer = Marshal.AllocHGlobal(length.ToInt32());
        try
        {
            osStatus = SysctlByName(name, buffer, ref length, IntPtr.Zero, 0);
            if (osStatus != 0)
            {
                throw new NotSupportedException($"Failed to get sysctl value for {name} with error {osStatus}");
            }

            return Marshal.PtrToStringAnsi(buffer) ?? string.Empty;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static int SysctlByName(string name, IntPtr oldp, ref IntPtr oldlenp, IntPtr newp, uint newlen)
    {
        return NativeMethods.SysctlByName(System.Text.Encoding.ASCII.GetBytes(name), oldp, ref oldlenp, newp, newlen);
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

    private static class NativeMethods
    {
        [DllImport("libc", EntryPoint = "sysctlbyname", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static extern int SysctlByName(byte[] name, IntPtr oldp, ref IntPtr oldlenp, IntPtr newp, uint newlen);
    }
}
