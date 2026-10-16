using System;
using System.Linq;
using System.Reflection;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Providers.MySql;
using Jellyfin.Server.Implementations.Tests.DatabaseInfrastructure;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.MySql;

public class MySqlDataErrorClassificationTests
{
    private readonly IJellyfinDatabaseProvider _provider = new MySqlDatabaseProvider(new CollectingLogger<MySqlDatabaseProvider>());

    [Theory]
    [InlineData(1048, true)] // a column that takes no NULL
    [InlineData(1062, true)] // a duplicate key, which the retry policy takes first
    [InlineData(1216, true)]
    [InlineData(1217, true)]
    [InlineData(1451, true)]
    [InlineData(1452, true)] // a foreign key to a missing row
    [InlineData(1263, true)] // NULL given for a column that takes none
    [InlineData(1264, true)]
    [InlineData(1690, true)] // a value out of the range of its type
    [InlineData(1265, true)]
    [InlineData(1292, true)]
    [InlineData(1366, true)] // a string the column's character set cannot hold
    [InlineData(1406, true)] // a value over the length of the column
    [InlineData(1644, true)] // SIGNAL
    [InlineData(3819, true)] // a check constraint on MySQL
    [InlineData(4025, true)] // a check constraint on MariaDB
    [InlineData(-1, false)] // the command time-out of the client
    [InlineData(1040, false)] // too many connections
    [InlineData(1042, false)] // unable to connect
    [InlineData(1205, false)] // lock wait time-out
    [InlineData(1213, false)] // deadlock
    [InlineData(1020, false)] // a row changed under snapshot isolation
    [InlineData(1317, false)] // query interrupted
    [InlineData(3024, false)] // the server's execution time-out
    [InlineData(1146, false)] // no such table
    [InlineData(1064, false)] // a syntax error
    public void IsDataError_MySqlException_IsDecidedByTheErrorCode(int code, bool expected)
    {
        var exception = CreateMySqlException((MySqlErrorCode)code);

        Assert.Equal(expected, _provider.IsDataError(exception));
        Assert.Equal(expected, _provider.IsDataError(new DbUpdateException("An error occurred while saving the entity changes.", exception)));
    }

    [Fact]
    public void IsDataError_UnrelatedException_IsNotDataError()
    {
        Assert.False(_provider.IsDataError(new System.IO.IOException("Connection reset by peer")));
        Assert.False(_provider.IsDataError(new DbUpdateException("save failed", new InvalidOperationException("boom"))));
    }

    [Fact]
    public void IsDataError_NullException_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => _provider.IsDataError(null!));
    }

    private static MySqlException CreateMySqlException(MySqlErrorCode code)
    {
        var constructor = typeof(MySqlException)
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(c => c.GetParameters().Any(p => p.ParameterType == typeof(MySqlErrorCode)))
            .OrderBy(c => c.GetParameters().Length)
            .First();
        var arguments = constructor.GetParameters()
            .Select(p => p.ParameterType == typeof(MySqlErrorCode) ? (object)code
                : p.ParameterType == typeof(string) ? "refused"
                : p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType)
                : null)
            .ToArray();
        return (MySqlException)constructor.Invoke(arguments);
    }
}
