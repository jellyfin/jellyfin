using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace Jellyfin.Database.Implementations;

/// <summary>
/// Database functions that shared queries call to state an intent. The SQL for each one is chosen by the model: the
/// default translation is correct on any engine whose <c>lower()</c> folds only ASCII letters, and a provider may
/// replace it in <see cref="IJellyfinDatabaseProvider.OnModelCreating"/> with one that gives the same results faster.
/// </summary>
public static class JellyfinDbFunctions
{
    /// <summary>
    /// Matches <paramref name="value"/> against a pattern from <see cref="LikePatterns"/>, ignoring the case of ASCII letters.
    /// Can only be used inside a query that is translated to SQL.
    /// </summary>
    /// <param name="value">The column to match.</param>
    /// <param name="pattern">A pattern built by <see cref="LikePatterns"/>; it is ASCII-lower-cased and escapes <c>\</c>.</param>
    /// <returns>Whether the value matches. Never returned: the call is always translated to SQL.</returns>
    /// <exception cref="InvalidOperationException">Always, when evaluated outside a database query.</exception>
    public static bool LikeIgnoreAsciiCase(string? value, string pattern)
        => throw new InvalidOperationException($"{nameof(LikeIgnoreAsciiCase)} can only be used in a database query.");

    /// <summary>
    /// Registers the default translation of every function in this class.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    internal static void ConfigureDefaults(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        // lower(value) LIKE pattern ESCAPE '\'. Exact wherever lower() folds ASCII only, as on SQLite and on PostgreSQL
        // under the "C" collation, whether or not LIKE itself ignores case.
        modelBuilder.HasDbFunction(() => LikeIgnoreAsciiCase(default, default!))
            .HasTranslation(args =>
            {
                var value = Lower(args[0]);
                var escape = new SqlConstantExpression(LikePatterns.Escape, typeof(string), value.TypeMapping);
                return new LikeExpression(value, args[1], escape, typeMapping: null);
            });
    }

    private static SqlFunctionExpression Lower(SqlExpression value)
        => new(
            "lower",
            new[] { value },
            nullable: true,
            argumentsPropagateNullability: new[] { true },
            typeof(string),
            value.TypeMapping);
}
