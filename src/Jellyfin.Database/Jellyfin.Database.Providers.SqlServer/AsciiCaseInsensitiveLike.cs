using Jellyfin.Database.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace Jellyfin.Database.Providers.SqlServer;

/// <summary>
/// The SQL of <see cref="JellyfinDbFunctions.LikeIgnoreAsciiCase"/> on SQL Server.
/// </summary>
/// <remarks>
/// The columns compare as binary text, so <c>LIKE</c> is case sensitive, but <c>LOWER()</c> folds letters outside ASCII
/// too. The function must fold ASCII letters only. <c>TRANSLATE</c> replaces each of the 26 capital ASCII letters by its
/// small letter and nothing else. The value is given the collation of the columns, so that both <c>TRANSLATE</c> and
/// <c>LIKE</c> compare characters exactly also where the value is not a column, which would otherwise compare under the
/// default collation of the database.
/// </remarks>
internal static class AsciiCaseInsensitiveLike
{
    private const string Upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string Lower = "abcdefghijklmnopqrstuvwxyz";

    /// <summary>
    /// Registers the translation of the function with the model.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    public static void Register(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDbFunction(() => JellyfinDbFunctions.LikeIgnoreAsciiCase(default, default!))
            .HasTranslation(args => Translate(args[0], args[1]));
    }

    private static LikeExpression Translate(SqlExpression value, SqlExpression pattern)
    {
        var typeMapping = value.TypeMapping;
        var folded = new SqlFunctionExpression(
            "TRANSLATE",
            new SqlExpression[]
            {
                new CollateExpression(value, SqlServerDatabaseProvider.Collation),
                new SqlConstantExpression(Upper, typeof(string), typeMapping),
                new SqlConstantExpression(Lower, typeof(string), typeMapping)
            },
            nullable: true,
            argumentsPropagateNullability: new[] { true, false, false },
            typeof(string),
            typeMapping);
        var escape = new SqlConstantExpression(LikePatterns.Escape, typeof(string), typeMapping);
        return new LikeExpression(folded, pattern, escape, typeMapping: null);
    }
}
