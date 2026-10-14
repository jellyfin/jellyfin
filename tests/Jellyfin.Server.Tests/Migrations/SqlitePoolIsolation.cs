using Xunit;

namespace Jellyfin.Server.Tests.Migrations;

/// <summary>
/// Defines the xunit collection of the tests that list the files the process holds open.
/// They run alone, so that no test clears the SQLite connection pools of the whole process
/// while they look, which would close a file that a connection left open.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class SqlitePoolIsolation
{
    public const string Name = "Process wide SQLite connection pools";
}
