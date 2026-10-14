using System;
using System.Collections.Generic;

namespace Jellyfin.Database.Testing;

public static class ServerVersionFloors
{
    private static readonly Dictionary<TestDbProvider, Version> _floors = new()
    {
        [TestDbProvider.Postgres] = new Version(16, 0),
        [TestDbProvider.MySql] = new Version(8, 4),
        [TestDbProvider.MariaDb] = new Version(11, 4, 7)
    };

    public static Version For(TestDbProvider provider) => _floors.GetValueOrDefault(provider, new Version(0, 0));
}
