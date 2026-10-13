using System;
using Jellyfin.Database.Providers.MySql;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.MySql;

public class MySqlClampedUtcDateTimeConverterTests
{
    private readonly ClampedUtcDateTimeConverter _converter = new();

    [Fact]
    public void MaxValue_SurvivesAWriteAndARead()
    {
        var stored = (DateTime)_converter.ConvertToProvider(DateTime.MaxValue)!;
        var read = (DateTime)_converter.ConvertFromProvider(stored)!;

        Assert.Equal(DateTime.MaxValue, read);
        Assert.Equal(DateTimeKind.Utc, read.Kind);
    }

    [Fact]
    public void MaxValue_IsStoredAsTheLastMicrosecondOfTheYear9999()
    {
        var stored = (DateTime)_converter.ConvertToProvider(DateTime.MaxValue)!;

        Assert.Equal(new DateTime(9999, 12, 31, 23, 59, 59, DateTimeKind.Utc).AddTicks(9_999_990), stored);
        Assert.Equal(0, stored.Ticks % 10);
    }

    [Theory]
    [InlineData(DateTimeKind.Unspecified)]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Utc)]
    public void MaxValue_IsStoredTheSameWhateverItsKind(DateTimeKind kind)
    {
        var value = DateTime.SpecifyKind(DateTime.MaxValue, kind);

        var stored = (DateTime)_converter.ConvertToProvider(value)!;

        Assert.Equal(ClampedUtcDateTimeConverter.LastStored, stored);
        Assert.Equal(DateTime.MaxValue, (DateTime)_converter.ConvertFromProvider(stored)!);
    }

    [Theory]
    [InlineData(DateTimeKind.Unspecified)]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Utc)]
    public void MinValue_IsStoredTheSameWhateverItsKind(DateTimeKind kind)
    {
        var value = DateTime.SpecifyKind(DateTime.MinValue, kind);

        var stored = (DateTime)_converter.ConvertToProvider(value)!;

        Assert.Equal(DateTime.MinValue.Ticks, stored.Ticks);
        Assert.Equal(DateTimeKind.Utc, stored.Kind);
        Assert.Equal(DateTime.MinValue.Ticks, ((DateTime)_converter.ConvertFromProvider(stored)!).Ticks);
    }

    [Fact]
    public void ValueJustBelowTheLimit_IsKept()
    {
        var value = new DateTime(9999, 12, 31, 23, 59, 59, DateTimeKind.Utc).AddTicks(9_999_980);

        Assert.Equal(value, (DateTime)_converter.ConvertToProvider(value)!);
        Assert.Equal(value, (DateTime)_converter.ConvertFromProvider(value)!);
    }

    [Fact]
    public void LocalValue_IsStoredAsUtc()
    {
        var local = new DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Local);

        var stored = (DateTime)_converter.ConvertToProvider(local)!;

        Assert.Equal(local.ToUniversalTime(), stored);
        Assert.Equal(DateTimeKind.Utc, stored.Kind);
    }

    [Fact]
    public void ValueRead_HasTheKindUtc()
    {
        var read = (DateTime)_converter.ConvertFromProvider(new DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Unspecified))!;

        Assert.Equal(DateTimeKind.Utc, read.Kind);
        Assert.Equal(new DateTime(2024, 6, 1, 12, 0, 0), read);
    }
}
