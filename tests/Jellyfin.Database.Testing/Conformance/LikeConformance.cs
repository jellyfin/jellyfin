using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Xunit;

namespace Jellyfin.Database.Testing.Conformance;

/// <summary>
/// Kit row: a search with <see cref="JellyfinDbFunctions.LikeIgnoreAsciiCase"/> and a pattern from
/// <see cref="LikePatterns"/> means the same on every engine. A contains pattern keeps <c>%</c> and <c>_</c> as
/// wildcards and a starts-with pattern takes them literally; both take the escape character literally, fold the case
/// of ASCII letters only, and match any other character, including one outside the Basic Multilingual Plane, exactly.
/// </summary>
public abstract class LikeConformance : ConformanceTestBase
{
    private static readonly string[] _names =
    [
        "100%", "1000", "a_b", "axb", "a\\b", "a\\\\b", "Émile", "émile", "MILE", "日本語", "日本", "Smile 😀", "Smile 😁"
    ];

    protected LikeConformance()
    {
        var items = Table<BaseItemEntity>();
        foreach (var name in _names)
        {
            Rows.Save(items, new Dictionary<string, object?> { [nameof(BaseItemEntity.Name)] = name });
        }
    }

    [ConformanceTheory]
    [InlineData("100%", new[] { "100%", "1000" })]
    [InlineData("a_b", new[] { "a\\b", "a_b", "axb" })]
    [InlineData("a\\b", new[] { "a\\b" })]
    [InlineData("\\\\", new[] { "a\\\\b" })]
    [InlineData("mile", new[] { "MILE", "Smile 😀", "Smile 😁", "Émile", "émile" })]
    [InlineData("MILE", new[] { "MILE", "Smile 😀", "Smile 😁", "Émile", "émile" })]
    [InlineData("É", new[] { "Émile" })]
    [InlineData("é", new[] { "émile" })]
    [InlineData("本", new[] { "日本", "日本語" })]
    [InlineData("😀", new[] { "Smile 😀" })]
    public void LikeIgnoreAsciiCase_ContainsPattern_MatchesTheSameOnEveryEngine(string term, string[] expected)
    {
        Assert.Equal(expected, Match(LikePatterns.ContainsIgnoreAsciiCase(term)));
    }

    [ConformanceTheory]
    [InlineData("100%", new[] { "100%" })]
    [InlineData("a_b", new[] { "a_b" })]
    [InlineData("a\\", new[] { "a\\\\b", "a\\b" })]
    [InlineData("SMILE ", new[] { "Smile 😀", "Smile 😁" })]
    [InlineData("ÉMI", new[] { "Émile" })]
    [InlineData("émi", new[] { "émile" })]
    [InlineData("日本", new[] { "日本", "日本語" })]
    [InlineData("Smile 😁", new[] { "Smile 😁" })]
    public void LikeIgnoreAsciiCase_StartsWithPattern_MatchesTheSameOnEveryEngine(string term, string[] expected)
    {
        Assert.Equal(expected, Match(LikePatterns.StartsWithIgnoreAsciiCase(term)));
    }

    private string[] Match(string pattern)
    {
        using var context = CreateDbContext();
        return context.BaseItems
            .Where(e => JellyfinDbFunctions.LikeIgnoreAsciiCase(e.Name, pattern))
            .Select(e => e.Name!)
            .AsEnumerable()
            .Where(_names.Contains)
            .Order(StringComparer.Ordinal)
            .ToArray();
    }
}
