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

public class MySqlWriteConflictClassificationTests
{
    private readonly IJellyfinDatabaseProvider _provider = new MySqlDatabaseProvider(new CollectingLogger<MySqlDatabaseProvider>());

    [Theory]
    [InlineData(MySqlErrorCode.DuplicateKeyEntry, true)]
    [InlineData(MySqlErrorCode.LockDeadlock, true)]
    [InlineData(MySqlErrorCode.LockWaitTimeout, true)]
    [InlineData(MySqlErrorCode.CheckRead, true)]
    [InlineData(MySqlErrorCode.RowIsReferenced2, false)]
    [InlineData(MySqlErrorCode.NoReferencedRow2, false)]
    [InlineData(MySqlErrorCode.WarningDataOutOfRange, false)]
    [InlineData(MySqlErrorCode.DataTooLong, false)]
    [InlineData((MySqlErrorCode)1048, false)]
    [InlineData(MySqlErrorCode.NoSuchTable, false)]
    [InlineData(MySqlErrorCode.ParseError, false)]
    [InlineData(MySqlErrorCode.QueryInterrupted, false)]
    [InlineData((MySqlErrorCode)1040, false)]
    public void IsRetryableWriteConflict_MySqlException_IsDecidedByTheErrorCode(MySqlErrorCode code, bool expectedRetryable)
    {
        Assert.Equal(expectedRetryable, _provider.IsRetryableWriteConflict(CreateMySqlException(code)));
    }

    [Theory]
    [InlineData(MySqlErrorCode.DuplicateKeyEntry, true)]
    [InlineData(MySqlErrorCode.LockDeadlock, true)]
    [InlineData(MySqlErrorCode.LockWaitTimeout, true)]
    [InlineData(MySqlErrorCode.CheckRead, true)]
    [InlineData(MySqlErrorCode.RowIsReferenced2, false)]
    [InlineData(MySqlErrorCode.NoReferencedRow2, false)]
    [InlineData(MySqlErrorCode.DataTooLong, false)]
    public void IsRetryableWriteConflict_ViolationWrappedInDbUpdateException_IsClassifiedByTheInnerException(
        MySqlErrorCode code,
        bool expectedRetryable)
    {
        var wrapped = new DbUpdateException("An error occurred while saving the entity changes.", CreateMySqlException(code));

        Assert.Equal(expectedRetryable, _provider.IsRetryableWriteConflict(wrapped));
    }

    [Fact]
    public void IsRetryableWriteConflict_ViolationNestedDeeper_IsFound()
    {
        var nested = new InvalidOperationException(
            "outer",
            new DbUpdateException("middle", CreateMySqlException(MySqlErrorCode.LockDeadlock)));

        Assert.True(_provider.IsRetryableWriteConflict(nested));
    }

    [Fact]
    public void IsRetryableWriteConflict_ConnectionFailure_IsNotRetryable()
    {
        var exception = CreateMySqlException(MySqlErrorCode.UnableToConnectToHost);

        Assert.False(_provider.IsRetryableWriteConflict(exception));
        Assert.False(_provider.IsRetryableWriteConflict(new DbUpdateException("save failed", exception)));
    }

    [Fact]
    public void IsRetryableWriteConflict_UnrelatedException_IsNotRetryable()
    {
        Assert.False(_provider.IsRetryableWriteConflict(new InvalidOperationException("boom")));
        Assert.False(_provider.IsRetryableWriteConflict(new TimeoutException("timed out")));
        Assert.False(_provider.IsRetryableWriteConflict(new DbUpdateException("save failed", new InvalidOperationException("boom"))));
    }

    [Fact]
    public void IsRetryableWriteConflict_NullException_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => _provider.IsRetryableWriteConflict(null!));
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
                : p.ParameterType == typeof(string) ? "conflict"
                : p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType)
                : null)
            .ToArray();
        return (MySqlException)constructor.Invoke(arguments);
    }
}
