using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Postgres;

/// <summary>
/// Defines the xunit collection of the tests that clear the Npgsql connection pools of the whole process.
/// They run alone, so that they do not close the idle connections of tests that run in parallel.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class PostgresPoolIsolation
{
    public const string Name = "Process wide PostgreSQL connection pools";
}
