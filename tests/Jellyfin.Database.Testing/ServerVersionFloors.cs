using System;
using System.Collections.Generic;

namespace Jellyfin.Database.Testing;

public static class ServerVersionFloors
{
    private static readonly Dictionary<TestDbProvider, Version> _floors = new()
    {
        [TestDbProvider.Postgres] = new Version(16, 0)
    };

    public static Version For(TestDbProvider provider) => _floors.GetValueOrDefault(provider, new Version(0, 0));
}
