using System;
using Jellyfin.Database.Providers.MySql;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.MySql;

public class MySqlServerInfoTests
{
    [Theory]
    [InlineData("8.4.11", false, "8.4.11")]
    [InlineData("8.4.0", false, "8.4.0")]
    [InlineData("9.1.0", false, "9.1.0")]
    [InlineData("8.4.11-debug", false, "8.4.11")]
    [InlineData("11.4.13-MariaDB-ubu2404", true, "11.4.13")]
    [InlineData("5.5.5-11.4.13-MariaDB-ubu2404", true, "11.4.13")]
    [InlineData("5.5.5-10.11.9-mariadb", true, "10.11.9")]
    public void TryParseReported_ServerVersion_IsRecognised(string reported, bool mariaDb, string version)
    {
        Assert.True(ServerInfo.TryParseReported(reported, out var info));

        Assert.NotNull(info);
        Assert.Equal(mariaDb ? ServerFlavor.MariaDb : ServerFlavor.MySql, info.Flavor);
        Assert.Equal(Version.Parse(version), info.Version);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a version")]
    [InlineData("8")]
    public void TryParseReported_Garbage_IsNotRecognised(string? reported)
    {
        Assert.False(ServerInfo.TryParseReported(reported, out var info));

        Assert.Null(info);
    }

    [Theory]
    [InlineData("8.4-mysql", false, "8.4")]
    [InlineData("8.4.11-mysql", false, "8.4.11")]
    [InlineData("11.4-mariadb", true, "11.4")]
    [InlineData("11.4.7-MariaDB", true, "11.4.7")]
    public void TryParseConfigured_ServerVersionOption_IsRecognised(string configured, bool mariaDb, string version)
    {
        Assert.True(ServerInfo.TryParseConfigured(configured, out var info));

        Assert.NotNull(info);
        Assert.Equal(mariaDb ? ServerFlavor.MariaDb : ServerFlavor.MySql, info.Flavor);
        Assert.Equal(Version.Parse(version), info.Version);
    }

    [Theory]
    [InlineData("8.4")]
    [InlineData("mysql")]
    [InlineData("8.4-postgres")]
    [InlineData("-mysql")]
    [InlineData("8.4.11.2-mysql")]
    public void TryParseConfigured_InvalidOption_IsNotRecognised(string configured)
    {
        Assert.False(ServerInfo.TryParseConfigured(configured, out _));
    }

    [Theory]
    [InlineData(false, "8.4.0", true)]
    [InlineData(false, "8.4", true)]
    [InlineData(false, "8.3.0", false)]
    [InlineData(false, "8.0.40", false)]
    [InlineData(false, "9.0.0", true)]
    [InlineData(true, "11.4.7", true)]
    [InlineData(true, "11.4.13", true)]
    [InlineData(true, "11.4.6", false)]
    [InlineData(true, "11.4.4", false)]
    [InlineData(true, "11.4", true)]
    [InlineData(true, "11.3.2", false)]
    [InlineData(true, "10.11.9", false)]
    [InlineData(true, "11.8.1", true)]
    public void MeetsFloor_IsDecidedByTheVersionOfTheFlavor(bool mariaDb, string version, bool expected)
    {
        var info = new ServerInfo(mariaDb ? ServerFlavor.MariaDb : ServerFlavor.MySql, Version.Parse(version));

        Assert.Equal(expected, info.MeetsFloor);
    }

    [Theory]
    [InlineData(false, "8.0.40", "8.4")]
    [InlineData(true, "11.4.4", "11.4.7")]
    public void UnsupportedMessage_NamesTheFloor(bool mariaDb, string version, string floor)
    {
        var info = new ServerInfo(mariaDb ? ServerFlavor.MariaDb : ServerFlavor.MySql, Version.Parse(version));

        Assert.Contains(floor, info.UnsupportedMessage, StringComparison.Ordinal);
        Assert.Contains(version, info.UnsupportedMessage, StringComparison.Ordinal);
    }
}
