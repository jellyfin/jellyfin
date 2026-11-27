using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Metrics;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class MetricsTestCollection
{
    public const string Name = "Metrics";
}
