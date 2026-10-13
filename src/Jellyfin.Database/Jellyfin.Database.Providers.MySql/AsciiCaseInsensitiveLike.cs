using System.Collections.Generic;
using System.Linq.Expressions;
using Jellyfin.Database.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using Microting.EntityFrameworkCore.MySql.Query.Expressions.Internal;

namespace Jellyfin.Database.Providers.MySql;

/// <summary>
/// The SQL of <see cref="JellyfinDbFunctions.LikeIgnoreAsciiCase"/> on MySQL and MariaDB.
/// </summary>
/// <remarks>
/// <para>
/// The columns compare as binary text, so <c>LIKE</c> is case sensitive, but <c>LOWER()</c> folds every letter
/// of the alphabet and not only ASCII ones. The function must fold ASCII letters only. A chain of
/// <c>REPLACE</c> calls folds exactly the 26 capital letters, and decides the match.
/// </para>
/// <para>
/// The chain is slow on a long text, so a test with <c>LOWER()</c> comes first. It accepts every row that the chain
/// accepts, as a row that equals the pattern after the ASCII fold equals it after the full fold too, and the
/// <c>AND</c> stops at the first test that fails. The pattern is given the collation of the column in that test,
/// because MariaDB would otherwise compare it under a collation that is not the one of the column.
/// </para>
/// </remarks>
internal static class AsciiCaseInsensitiveLike
{
    private const string CharSet = "utf8mb4";

    /// <summary>
    /// Registers the translation of the function with the model.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    public static void Register(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDbFunction(() => JellyfinDbFunctions.LikeIgnoreAsciiCase(default, default!))
            .HasTranslation(args => Translate(args[0], args[1]));
    }

    private static SqlExpression Translate(SqlExpression value, SqlExpression pattern)
    {
        var typeMapping = value.TypeMapping;
        var escape = new SqlConstantExpression(LikePatterns.Escape, typeof(string), typeMapping);

#pragma warning disable EF1001 // Internal EF Core API usage: The provider has no public way to write COLLATE.
        var collatedPattern = new MySqlCollateExpression(pattern, CharSet, MySqlDatabaseProvider.Collation, pattern.TypeMapping);
#pragma warning restore EF1001

        var prefilter = new LikeExpression(Function("LOWER", value, typeMapping), Function("LOWER", collatedPattern, typeMapping), escape, typeMapping: null);

        var folded = value;
        for (var letter = 'A'; letter <= 'Z'; letter++)
        {
            folded = new SqlFunctionExpression(
                "REPLACE",
                new SqlExpression[]
                {
                    folded,
                    new SqlConstantExpression(letter.ToString(), typeof(string), typeMapping),
                    new SqlConstantExpression(char.ToLowerInvariant(letter).ToString(), typeof(string), typeMapping)
                },
                nullable: true,
                argumentsPropagateNullability: new[] { true, false, false },
                typeof(string),
                typeMapping);
        }

        var exact = new LikeExpression(folded, pattern, escape, typeMapping: null);
        return new SqlBinaryExpression(ExpressionType.AndAlso, prefilter, exact, typeof(bool), typeMapping: null);
    }

    private static SqlFunctionExpression Function(string name, SqlExpression argument, RelationalTypeMapping? typeMapping)
    {
        return new SqlFunctionExpression(
            name,
            new[] { argument },
            nullable: true,
            argumentsPropagateNullability: new[] { true },
            typeof(string),
            typeMapping);
    }
}
