using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Jellyfin.Database.Implementations;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Jellyfin.Database.Testing.Conformance;

/// <summary>
/// The base of every row of the kit: a database with the schema of the migrations on the selected engine, and a
/// factory for rows of any table.
/// </summary>
public abstract class ConformanceTestBase : DatabaseTestFixture
{
    protected ConformanceTestBase()
    {
        Rows = new ModelRowFactory(CreateDbContext);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConformanceTestBase"/> class on a backend of the test's own, for a
    /// variant of the database of the selected engine.
    /// </summary>
    /// <param name="backend">The backend that creates the database.</param>
    protected ConformanceTestBase(ITestDatabaseBackend backend)
        : base(backend, EngineTraits.For(backend.Provider).DefaultSchemaSource, false)
    {
        Rows = new ModelRowFactory(CreateDbContext);
    }

    protected static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    protected ModelRowFactory Rows { get; }

    /// <summary>
    /// Fails with every problem on a line of its own, when there is any.
    /// </summary>
    /// <param name="problems">The problems found.</param>
    protected static void AssertNone(IReadOnlyCollection<string> problems)
    {
        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    protected IReadOnlyList<IEntityType> Tables()
    {
        using var context = CreateDbContext();
        return ModelRowFactory.Tables(context.Model);
    }

    protected IReadOnlyList<IEntityType> TablesWithIdentity()
        => Tables().Where(table => ModelRowFactory.IdentityKey(table) is not null).ToList();

    protected IEntityType Table<TEntity>()
    {
        using var context = CreateDbContext();
        return context.Model.FindEntityType(typeof(TEntity))!;
    }

    protected IReadOnlyList<string> DescribeRows(IEntityType table, Func<IProperty, bool>? include = null)
    {
        using var context = CreateDbContext();
        return ModelRowFactory.DescribeRows(context, table, include);
    }

    /// <summary>
    /// Reads the values of the identity column of a table, in ascending order.
    /// </summary>
    /// <param name="table">The entity type of a table with an identity column.</param>
    /// <param name="createContext">Creates a context on the database to read; the database of the test by default.</param>
    /// <returns>The values.</returns>
    protected IReadOnlyList<long> IdentityValues(IEntityType table, Func<JellyfinDbContext>? createContext = null)
    {
        using var context = (createContext ?? CreateDbContext)();
        return ModelRowFactory.Keys(context, table)
            .Select(key => Convert.ToInt64(key.Single(), CultureInfo.InvariantCulture))
            .Order()
            .ToList();
    }

    /// <summary>
    /// Saves a row without a value for its identity column and gives back the value the engine chose.
    /// </summary>
    /// <param name="table">The entity type of a table with an identity column.</param>
    /// <returns>The identity of the new row.</returns>
    protected long NextIdentity(IEntityType table)
    {
        var key = Rows.Save(table).Key;
        return Convert.ToInt64(key.Single(), CultureInfo.InvariantCulture);
    }
}
