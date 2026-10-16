using System.Collections.Generic;

namespace Jellyfin.Database.Testing.Conformance;

/// <summary>
/// The cross-engine conformance kit: one fixed list of the hazards that the rest of the suite does not see, run on
/// every engine it is enrolled for. Each row is an abstract test class in this namespace, which a test project runs by
/// deriving a sealed class from it. The rows reach the engine only through the shared model, LINQ, the members of the
/// provider and the services of the server, never through SQL of their own, so a row that fails on an engine shows a
/// difference that the server would meet as well.
/// </summary>
/// <remarks>
/// An engine joins the kit by being added to <see cref="Engines"/>. Until then its runs report every row as skipped.
/// </remarks>
public static class ConformanceKit
{
    /// <summary>
    /// The trait every row carries, so that the kit can be run on its own with <c>--filter Category=Conformance</c>.
    /// </summary>
    public const string Category = "Conformance";

    /// <summary>
    /// The engines the kit runs on.
    /// </summary>
    public static readonly IReadOnlyList<TestDbProvider> Engines = [TestDbProvider.Sqlite, TestDbProvider.Postgres, TestDbProvider.MySql, TestDbProvider.MariaDb, TestDbProvider.SqlServer];
}
