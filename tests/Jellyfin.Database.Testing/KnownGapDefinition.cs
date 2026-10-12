using System;

namespace Jellyfin.Database.Testing;

public sealed record KnownGapDefinition(TestDbProvider Provider, string Name, Func<Exception, bool> Matches);
