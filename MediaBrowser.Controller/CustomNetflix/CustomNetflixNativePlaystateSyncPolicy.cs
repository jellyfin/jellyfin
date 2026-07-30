#pragma warning disable CS1591

using System;
using System.Security.Cryptography;
using System.Text;

namespace MediaBrowser.Controller.CustomNetflix;

public static class CustomNetflixNativePlaystateSyncPolicy
{
    public static bool ShouldSync(CustomNetflixProfileDto? profile)
        => profile?.IsDefault == true;

    public static long SecondsToTicks(double seconds)
        => TimeSpan.FromSeconds(Math.Max(0, seconds)).Ticks;

    public static string HashToken(string? token)
        => string.IsNullOrWhiteSpace(token)
            ? "no-token"
            : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
