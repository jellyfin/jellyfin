using System;

namespace Jellyfin.Database.Testing;

/// <summary>
/// One known difference between providers.
/// </summary>
/// <param name="Provider">The provider that differs.</param>
/// <param name="Id">A short identifier that stays the same when the name is reworded, shown in the skip text.</param>
/// <param name="Name">What differs.</param>
/// <param name="Matches">Whether a failure is the outcome the difference causes.</param>
public sealed record KnownGapDefinition(TestDbProvider Provider, string Id, string Name, Func<Exception, bool> Matches);
