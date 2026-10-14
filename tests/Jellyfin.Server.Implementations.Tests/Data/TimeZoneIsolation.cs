using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Data;

/// <summary>
/// Defines the xunit collection of the tests that change the time zone of the whole process.
/// They run alone, so that no other test sees the changed zone.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class TimeZoneIsolation
{
    public const string Name = "Process wide time zone";
}
