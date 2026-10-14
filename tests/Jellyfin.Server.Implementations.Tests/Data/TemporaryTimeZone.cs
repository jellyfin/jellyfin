using System;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Data;

/// <summary>
/// Makes <see cref="TimeZoneInfo.Local"/> the given zone for the lifetime of the instance, on Linux, where the
/// local zone comes from the TZ environment variable. Tests that use it belong to <see cref="TimeZoneIsolation"/>.
/// </summary>
public sealed class TemporaryTimeZone : IDisposable
{
    private const string Variable = "TZ";

    private readonly string? _previous = Environment.GetEnvironmentVariable(Variable);

    public TemporaryTimeZone(string zoneId)
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), "Linux-only test");

        try
        {
            Set(zoneId);
            Assert.Equal(zoneId, TimeZoneInfo.Local.Id);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        Set(_previous);
    }

    private static void Set(string? zoneId)
    {
        Environment.SetEnvironmentVariable(Variable, zoneId);
        TimeZoneInfo.ClearCachedData();
    }
}
