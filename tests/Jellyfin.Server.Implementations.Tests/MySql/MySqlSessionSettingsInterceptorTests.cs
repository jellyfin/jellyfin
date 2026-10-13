using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Providers.MySql;
using Moq;
using Moq.Protected;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.MySql;

public class MySqlSessionSettingsInterceptorTests
{
    [Fact]
    public void Statement_SetsStrictModeReadCommittedAndUtcWithoutTheModesThatBreakTheModel()
    {
        var statement = SessionSettingsInterceptor.Statement;

        Assert.Contains("transaction_isolation='READ-COMMITTED'", statement, StringComparison.Ordinal);
        Assert.Contains("time_zone='+00:00'", statement, StringComparison.Ordinal);
        Assert.Contains("STRICT_ALL_TABLES", statement, StringComparison.Ordinal);
        Assert.Contains("NO_ENGINE_SUBSTITUTION", statement, StringComparison.Ordinal);
        Assert.DoesNotContain("NO_BACKSLASH_ESCAPES", statement, StringComparison.Ordinal);
        Assert.DoesNotContain("NO_AUTO_VALUE_ON_ZERO", statement, StringComparison.Ordinal);
        Assert.DoesNotContain("ANSI", statement, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("8.4.11")]
    [InlineData("9.1.0")]
    [InlineData("11.4.13-MariaDB-ubu2404")]
    [InlineData("5.5.5-11.4.7-MariaDB")]
    public void ConnectionOpened_SupportedServer_AppliesTheSettings(string serverVersion)
    {
        var (connection, command) = CreateConnection(serverVersion);

        SessionSettingsInterceptor.Instance.ConnectionOpened(connection.Object, null!);

        Assert.Equal(SessionSettingsInterceptor.Statement, command.Object.CommandText);
        command.Verify(c => c.ExecuteNonQuery(), Times.Once());
    }

    [Theory]
    [InlineData("unreadable")]
    [InlineData("")]
    public async Task ConnectionOpened_UnreadableVersionOfAServerThatWasNotNamed_ThrowsAndNamesTheOption(string serverVersion)
    {
        var (connection, command) = CreateConnection(serverVersion);

        var syncEx = Assert.Throws<InvalidOperationException>(() => SessionSettingsInterceptor.Instance.ConnectionOpened(connection.Object, null!));
        var asyncEx = await Assert.ThrowsAsync<InvalidOperationException>(() => SessionSettingsInterceptor.Instance.ConnectionOpenedAsync(connection.Object, null!, TestContext.Current.CancellationToken));

        Assert.Contains("server-version", syncEx.Message, StringComparison.Ordinal);
        Assert.Contains("server-version", asyncEx.Message, StringComparison.Ordinal);
        command.Verify(c => c.ExecuteNonQuery(), Times.Never());
    }

    [Fact]
    public void ConnectionOpened_UnreadableVersionOfAServerThatWasNamed_AppliesTheSettings()
    {
        var (connection, command) = CreateConnection("unreadable");

        SessionSettingsInterceptor.ForConfiguredServer.ConnectionOpened(connection.Object, null!);

        Assert.Equal(SessionSettingsInterceptor.Statement, command.Object.CommandText);
        command.Verify(c => c.ExecuteNonQuery(), Times.Once());
    }

    [Theory]
    [InlineData("8.4.11")]
    [InlineData("11.4.13-MariaDB")]
    public async Task ConnectionOpenedAsync_SupportedServer_AppliesTheSettings(string serverVersion)
    {
        var (connection, command) = CreateConnection(serverVersion);

        await SessionSettingsInterceptor.Instance.ConnectionOpenedAsync(connection.Object, null!, TestContext.Current.CancellationToken);

        Assert.Equal(SessionSettingsInterceptor.Statement, command.Object.CommandText);
        command.Verify(c => c.ExecuteNonQueryAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Theory]
    [InlineData("8.0.40", "8.4")]
    [InlineData("11.4.4-MariaDB", "11.4.7")]
    [InlineData("5.5.5-10.11.9-MariaDB", "11.4.7")]
    public async Task ConnectionOpened_ServerBelowTheFloor_ThrowsBeforeRunningAnything(string serverVersion, string floor)
    {
        var (connection, command) = CreateConnection(serverVersion);

        var syncEx = Assert.Throws<InvalidOperationException>(() => SessionSettingsInterceptor.Instance.ConnectionOpened(connection.Object, null!));
        var asyncEx = await Assert.ThrowsAsync<InvalidOperationException>(() => SessionSettingsInterceptor.Instance.ConnectionOpenedAsync(connection.Object, null!, TestContext.Current.CancellationToken));

        Assert.Contains(floor, syncEx.Message, StringComparison.Ordinal);
        Assert.Contains(floor, asyncEx.Message, StringComparison.Ordinal);
        connection.Protected().Verify("CreateDbCommand", Times.Never());
        command.Verify(c => c.ExecuteNonQuery(), Times.Never());
    }

    private static (Mock<DbConnection> Connection, Mock<DbCommand> Command) CreateConnection(string serverVersion)
    {
        var command = new Mock<DbCommand> { CallBase = false };
        command.SetupProperty(c => c.CommandText);
        command.Setup(c => c.ExecuteNonQuery()).Returns(0);
        command.Setup(c => c.ExecuteNonQueryAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(0));

        var connection = new Mock<DbConnection>();
        connection.SetupGet(c => c.ServerVersion).Returns(serverVersion);
        connection.SetupGet(c => c.State).Returns(ConnectionState.Open);
        connection.Protected().Setup<DbCommand>("CreateDbCommand").Returns(command.Object);
        return (connection, command);
    }
}
