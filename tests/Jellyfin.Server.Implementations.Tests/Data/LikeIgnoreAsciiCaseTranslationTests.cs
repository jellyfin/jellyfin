using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Providers.Sqlite;
using Jellyfin.Database.Testing;
using Jellyfin.Server.Implementations.Tests.Item;
using MediaBrowser.Common.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Data;

/// <summary>
/// <see cref="JellyfinDbFunctions.LikeIgnoreAsciiCase"/>: the shared default translation, the SQLite provider's
/// translation, and the SQLite fact that makes the two return the same rows.
/// </summary>
public sealed class LikeIgnoreAsciiCaseTranslationTests
{
    private static readonly MethodInfo _function = typeof(JellyfinDbFunctions).GetMethod(nameof(JellyfinDbFunctions.LikeIgnoreAsciiCase))!;

    private static readonly string[] _values =
    [
        "Hello World", "hello world", "Émile", "émile", "AC\\DC", "ACDC", "50% off", "a_b", string.Empty
    ];

    private static readonly string[] _terms =
    [
        "hello", "HELLO", "émile", "ÉMILE", "ac\\dc", "AC\\DC", "%", "_", "\\", string.Empty
    ];

    [Fact]
    public void DefaultTranslation_LowersTheValueAndEscapesWithBackslash()
    {
        var modelBuilder = new ModelBuilder();
        JellyfinDbFunctions.ConfigureDefaults(modelBuilder);

        var (value, like) = Translate(modelBuilder);

        var match = Assert.IsType<SqlFunctionExpression>(like.Match);
        Assert.Equal("lower", match.Name);
        Assert.Same(value, Assert.Single(match.Arguments!));
        Assert.Equal("\\", Assert.IsType<SqlConstantExpression>(like.EscapeChar).Value);
    }

    [Fact]
    public void SqliteProvider_ReplacesTheDefaultWithAPlainLike()
    {
        var modelBuilder = new ModelBuilder();
        JellyfinDbFunctions.ConfigureDefaults(modelBuilder);
        new SqliteDatabaseProvider(new Mock<IApplicationPaths>().Object, NullLogger<SqliteDatabaseProvider>.Instance)
            .OnModelCreating(modelBuilder);

        var (value, like) = Translate(modelBuilder);

        Assert.Same(value, like.Match);
        Assert.Equal("\\", Assert.IsType<SqlConstantExpression>(like.EscapeChar).Value);
    }

    [ProviderFact(TestDbProvider.Sqlite)]
    public void JellyfinDbContext_OnSqlite_TranslatesToAPlainLike()
    {
        using var fixture = new SqliteContext();

        var sql = fixture.QueryString();

        Assert.Contains("\"OriginalTitle\" LIKE @", sql, StringComparison.Ordinal);
        Assert.Contains("ESCAPE '\\'", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("lower(", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LikeIgnoreAsciiCase_OutsideAQuery_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => JellyfinDbFunctions.LikeIgnoreAsciiCase("a", "%a%"));
    }

    /// <summary>
    /// The SQLite translation drops <c>lower()</c>. That is exact because, with <c>case_sensitive_like</c> off (Jellyfin
    /// never turns it on), SQLite's <c>LIKE</c> folds ASCII letters only and <c>lower()</c> folds ASCII letters only.
    /// </summary>
    [ProviderFact(TestDbProvider.Sqlite)]
    public void Sqlite_LikeAndLowerLike_AgreeOnEveryValueAndPattern()
    {
        using var fixture = new SqliteContext();

        var disagreements = new List<string>();
        var matches = 0;
        foreach (var v in _values)
        {
            foreach (var t in _terms)
            {
                var (plain, lowered) = fixture.Like(v, LikePatterns.ContainsIgnoreAsciiCase(t));
                matches += (int)plain;
                if (plain != lowered)
                {
                    disagreements.Add($"'{v}' / '{t}': LIKE={plain}, lower LIKE={lowered}");
                }
            }
        }

        Assert.Empty(disagreements);
        Assert.True(matches > 0, "The grid should contain matches as well as misses.");
    }

    private static (SqlExpression Value, LikeExpression Like) Translate(ModelBuilder modelBuilder)
    {
        var function = modelBuilder.Model.FindDbFunction(_function);
        Assert.NotNull(function?.Translation);

        var value = new SqlParameterExpression("value", typeof(string), null);
        var pattern = new SqlParameterExpression("pattern", typeof(string), null);
        var translated = function.Translation([value, pattern]);

        return (value, Assert.IsType<LikeExpression>(translated));
    }

    private sealed class SqliteContext : JellyfinDbTestFixture
    {
        public string QueryString()
        {
            using var context = CreateDbContext();
            var pattern = LikePatterns.ContainsIgnoreAsciiCase("World");
            return context.BaseItems
                .Where(e => JellyfinDbFunctions.LikeIgnoreAsciiCase(e.OriginalTitle, pattern))
                .Select(e => e.Id)
                .ToQueryString();
        }

        public (long Plain, long Lowered) Like(string value, string pattern)
        {
            using var context = CreateDbContext();
            var plain = context.Database
                .SqlQueryRaw<long>("""SELECT {0} LIKE {1} ESCAPE '\' AS "Value" """, value, pattern)
                .Single();
            var lowered = context.Database
                .SqlQueryRaw<long>("""SELECT lower({0}) LIKE {1} ESCAPE '\' AS "Value" """, value, pattern)
                .Single();
            return (plain, lowered);
        }
    }
}
