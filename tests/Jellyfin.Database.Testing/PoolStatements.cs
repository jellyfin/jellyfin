namespace Jellyfin.Database.Testing;

/// <summary>
/// The statements that the self-tests of a pool run on its databases. A query gives back one column, named
/// <c>Value</c>; a statement that changes the schema names <c>Scratch</c> in what it adds.
/// </summary>
public sealed class PoolStatements
{
    /// <summary>
    /// Gets a query for the names of the tables of the database.
    /// </summary>
    public required string ListTables { get; init; }

    /// <summary>
    /// Gets a query for the number of rows of a table, as a 64-bit number, with <c>{0}</c> for the delimited name of the
    /// table.
    /// </summary>
    public required string CountRows { get; init; }

    /// <summary>
    /// Gets a query for the names of the tables, indexes, triggers and columns of the database.
    /// </summary>
    public required string ListSchemaObjectNames { get; init; }

    /// <summary>
    /// Gets a statement that stops the checks of foreign keys on the connection that runs it, or, on an engine without such
    /// a switch, on every table of the database. There it changes the tables until the purge of the provider, which resets
    /// a returned database, turns the checks on again and has them trust their rows; see
    /// <see cref="CountDistrustedForeignKeys"/>.
    /// </summary>
    public required string DisableForeignKeys { get; init; }

    /// <summary>
    /// Gets a query for the number of foreign keys of the database that are switched off or do not trust their rows, as a
    /// 64-bit number. An engine whose switch belongs to the connection has none, and gives 0.
    /// </summary>
    public required string CountDistrustedForeignKeys { get; init; }

    /// <summary>
    /// Gets a statement that creates a table.
    /// </summary>
    public required string CreateTable { get; init; }

    /// <summary>
    /// Gets a statement that creates an index on a table of the model.
    /// </summary>
    public required string CreateIndex { get; init; }

    /// <summary>
    /// Gets a statement that creates a trigger on a table of the model.
    /// </summary>
    public required string CreateTrigger { get; init; }

    /// <summary>
    /// Gets a statement that adds a column to a table of the model.
    /// </summary>
    public required string AddColumn { get; init; }
}
