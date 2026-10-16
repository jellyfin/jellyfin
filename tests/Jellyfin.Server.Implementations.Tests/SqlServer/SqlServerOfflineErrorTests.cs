using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Jellyfin.Database.Providers.SqlServer;
using Jellyfin.Server.Implementations.Tests.DatabaseInfrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.SqlServer;

/// <summary>
/// Holds the SQL Server provider, without a server, to telling errors apart by every error the server sent, not only the
/// first, and to ending an IDENTITY_INSERT scope without hiding the error of the inserts. The exceptions are built through
/// the internal factory of SqlClient, as the tests against the server cannot choose the order of the errors.
/// </summary>
public sealed class SqlServerOfflineErrorTests
{
    private const int StatementTerminated = 3621;

    private readonly SqlServerDatabaseProvider _provider = new(NullLogger<SqlServerDatabaseProvider>.Instance);

    [Fact]
    public void CreateSqlException_HasTheErrorsInTheirOrder()
    {
        var exception = CreateSqlException(StatementTerminated, 1946);

        Assert.Equal([StatementTerminated, 1946], exception.Errors.Cast<SqlError>().Select(error => error.Number));
        Assert.Equal(StatementTerminated, exception.Number);
    }

    [Theory]
    [InlineData(new[] { StatementTerminated, 1946 }, false, true)]
    [InlineData(new[] { StatementTerminated, 547 }, false, true)]
    [InlineData(new[] { StatementTerminated, 2627 }, true, true)]
    [InlineData(new[] { StatementTerminated, 50001 }, false, true)]
    [InlineData(new[] { StatementTerminated, 1205 }, true, false)]
    [InlineData(new[] { 1946, 1205 }, true, false)]
    [InlineData(new[] { 2628, -2 }, false, false)]
    [InlineData(new[] { StatementTerminated }, false, false)]
    public void Classifiers_LookAtEveryErrorOfTheException(int[] numbers, bool retryable, bool dataError)
    {
        var exception = new DbUpdateException("save", CreateSqlException(numbers));

        Assert.Equal(retryable, _provider.IsRetryableWriteConflict(exception));
        Assert.Equal(dataError, _provider.IsDataError(exception));
    }

    [Theory]
    [InlineData(245)]
    [InlineData(515)]
    [InlineData(547)]
    [InlineData(1946)]
    [InlineData(2601)]
    [InlineData(2627)]
    [InlineData(2628)]
    [InlineData(8114)]
    [InlineData(8115)]
    [InlineData(8152)]
    [InlineData(50000)]
    public void IsDataError_RefusalOfTheValues_IsADataError(int number)
    {
        Assert.True(_provider.IsDataError(CreateSqlException(number)));
    }

    [Theory]
    [InlineData(-2)]
    [InlineData(1205)]
    [InlineData(1222)]
    [InlineData(3960)]
    [InlineData(233)]
    [InlineData(4060)]
    [InlineData(10054)]
    public void IsDataError_ErrorThatSaysNothingAboutTheRows_IsNotADataError(int number)
    {
        Assert.False(_provider.IsDataError(CreateSqlException(number)));
    }

    [Theory]
    [InlineData(1205, true)]
    [InlineData(1222, true)]
    [InlineData(2601, true)]
    [InlineData(2627, true)]
    [InlineData(3960, true)]
    [InlineData(547, false)]
    [InlineData(1946, false)]
    [InlineData(2628, false)]
    [InlineData(8152, false)]
    [InlineData(-2, false)]
    public void IsRetryableWriteConflict_IsTheConflictsOfConcurrentWriters(int number, bool retryable)
    {
        Assert.Equal(retryable, _provider.IsRetryableWriteConflict(CreateSqlException(number)));
    }

    [Fact]
    public async Task IdentityInsertScope_TurningOffFailsWhileTheInsertsFailed_TheErrorOfTheInsertsIsThrown()
    {
        var logger = new CollectingLogger<SqlServerDatabaseProvider>();
        var closed = false;
        var scope = new SqlServerDatabaseProvider.IdentityInsertScope(
            () => throw new InvalidOperationException("The connection is closed."),
            () =>
            {
                closed = true;
                return Task.CompletedTask;
            },
            "BaseItems",
            logger);

        var thrown = await Assert.ThrowsAsync<IOException>(async () =>
        {
            await using (scope)
            {
                throw new IOException("The inserts failed.");
            }
        });

        Assert.Equal("The inserts failed.", thrown.Message);
        Assert.True(closed);
        var warning = Assert.Single(logger.Entries, entry => entry.Level == LogLevel.Warning);
        Assert.Contains("BaseItems", warning.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task IdentityInsertScope_ReleasingFails_TheErrorOfTheInsertsIsThrown()
    {
        var logger = new CollectingLogger<SqlServerDatabaseProvider>();
        var scope = new SqlServerDatabaseProvider.IdentityInsertScope(
            () => Task.CompletedTask,
            () => throw new InvalidOperationException("Cannot close."),
            "BaseItems",
            logger);

        var thrown = await Assert.ThrowsAsync<IOException>(async () =>
        {
            await using (scope)
            {
                throw new IOException("The inserts failed.");
            }
        });

        Assert.Equal("The inserts failed.", thrown.Message);
        Assert.Single(logger.Entries, entry => entry.Level == LogLevel.Warning);
    }

    // Builds a SqlException with the given errors, in that order, through the internal factory SqlClient uses itself.
    internal static SqlException CreateSqlException(params int[] numbers)
    {
        var collection = (SqlErrorCollection)Activator.CreateInstance(typeof(SqlErrorCollection), nonPublic: true)!;
        var add = typeof(SqlErrorCollection).GetMethod("Add", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new MissingMethodException(nameof(SqlErrorCollection), "Add");
        foreach (var number in numbers)
        {
            add.Invoke(collection, [CreateSqlError(number)]);
        }

        var create = typeof(SqlException).GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
            .Where(method => method.Name == "CreateException")
            .Select(method => (Method: method, Parameters: method.GetParameters()))
            .First(x => x.Parameters.Length >= 2 && x.Parameters[0].ParameterType == typeof(SqlErrorCollection) && x.Parameters[1].ParameterType == typeof(string));
        var arguments = create.Parameters.Select((parameter, i) => i switch
        {
            0 => collection,
            1 => (object?)"16.00.4295",
            _ => parameter.HasDefaultValue ? parameter.DefaultValue : null
        }).ToArray();
        return (SqlException)create.Method.Invoke(null, arguments)!;
    }

    // The constructors of SqlError take the number first, then the state, the class, and the texts.
    private static SqlError CreateSqlError(int number)
    {
        var constructor = typeof(SqlError).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(c => c.GetParameters() is [{ ParameterType: var first }, { ParameterType: var second }, { ParameterType: var third }, ..] && first == typeof(int) && second == typeof(byte) && third == typeof(byte))
            .OrderBy(c => c.GetParameters().Length)
            .First();
        var arguments = constructor.GetParameters().Select((parameter, i) => i switch
        {
            0 => number,
            1 => (object?)(byte)1,
            2 => (byte)16,
            _ when parameter.ParameterType == typeof(string) => "test",
            _ when parameter.ParameterType == typeof(int) => 0,
            _ when parameter.ParameterType == typeof(uint) => 0u,
            _ when parameter.ParameterType == typeof(byte) => (byte)0,
            _ => parameter.HasDefaultValue ? parameter.DefaultValue : null
        }).ToArray();
        return (SqlError)constructor.Invoke(arguments);
    }
}
