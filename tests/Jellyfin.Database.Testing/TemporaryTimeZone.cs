using System;
using Xunit;

namespace Jellyfin.Database.Testing;

/// <summary>
/// Makes <see cref="TimeZoneInfo.Local"/> the given zone for the lifetime of the instance, on Linux, where the
/// local zone comes from the TZ environment variable. The zone belongs to the whole process, so a test that uses it
/// has to be in a collection that does not run in parallel with any other test.
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
