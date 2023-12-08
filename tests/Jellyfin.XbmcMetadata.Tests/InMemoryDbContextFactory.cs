using System;
using Jellyfin.Server.Implementations;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.XbmcMetadata.Tests;

public class InMemoryDbContextFactory : IDbContextFactory<LibraryDbContext>
{
    public LibraryDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<LibraryDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new LibraryDbContext(options);
    }
}
