using System;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Providers.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Data;

public class DateTimeKindValueConverterTests
{
    [Fact]
    public void ConvertToProvider_PremiereDateUnspecifiedKind_KeepsDateUnchanged()
    {
        var modelBuilder = new ModelBuilder();
        modelBuilder.Entity<BaseItemEntity>().Property(e => e.PremiereDate);
        modelBuilder.SetDefaultDateTimeKind(DateTimeKind.Utc);

        var property = modelBuilder.Model.FindEntityType(typeof(BaseItemEntity))!.FindProperty(nameof(BaseItemEntity.PremiereDate))!;
        var converter = property.GetValueConverter()!;

        // A provider air date parsed without a kind must not be shifted by the local time zone.
        var airDate = new DateTime(2000, 5, 18, 0, 0, 0, DateTimeKind.Unspecified);

        object? stored = converter.ConvertToProvider(airDate);

        Assert.Equal(new DateTime(2000, 5, 18, 0, 0, 0, DateTimeKind.Utc), stored);
    }
}
