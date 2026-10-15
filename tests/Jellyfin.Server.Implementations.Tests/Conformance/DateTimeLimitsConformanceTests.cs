using Jellyfin.Database.Testing.Conformance;
using Jellyfin.Server.Implementations.Tests.Data;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Conformance;

[Collection(TimeZoneIsolation.Name)]
public sealed class DateTimeLimitsConformanceTests : DateTimeLimitsConformance
{
}
