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
    /// Gets a query for the names of the tables, indexes, triggers and columns of the database.
    /// </summary>
    public required string ListSchemaObjectNames { get; init; }

    /// <summary>
    /// Gets a statement that stops the checks of foreign keys on the connection that runs it.
    /// </summary>
    public required string DisableForeignKeys { get; init; }

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
