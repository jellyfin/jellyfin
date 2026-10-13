using System;
using System.Linq;
using Emby.Server.Implementations.Data;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Testing;
using Jellyfin.Server.Implementations.Tests.Item;
using Xunit;
using BaseItemKind = Jellyfin.Data.Enums.BaseItemKind;

namespace Jellyfin.Server.Implementations.Tests.Data;

/// <summary>
/// <see cref="JellyfinDbFunctions.LikeIgnoreAsciiCase"/> run in a query on the selected provider: it folds the case of
/// ASCII letters and of nothing else, whatever the engine's <c>lower()</c> and collation do to other characters.
/// The Kelvin sign (U+212A) and the dotted capital I (U+0130) lower-case to ASCII letters in Unicode, and U+1E9E is the
/// capital form of the sharp s.
/// </summary>
public sealed class LikeIgnoreAsciiCaseBehaviourTests : JellyfinDbTestFixture
{
    private static readonly string[] _values =
    [
        "Émile", "émile", "\u212A", "k", "K", "\u0130", "\u0130stanbul", "i", "I", "ß", "\u1E9E", "STRASSE", "Straße",
        "Hello World", "hello world", "HELLO"
    ];

    public LikeIgnoreAsciiCaseBehaviourTests()
    {
        var itemTypeLookup = new ItemTypeLookup();
        using var context = CreateDbContext();
        foreach (var value in _values)
        {
            var id = Guid.NewGuid();
            context.BaseItems.Add(new BaseItemEntity
            {
                Id = id,
                PresentationUniqueKey = id.ToString("N"),
                Type = itemTypeLookup.BaseItemKindNames[BaseItemKind.Movie],
                Name = value,
                SortName = value,
                MediaType = "Video",
                IsMovie = true,
                IsFolder = false,
                IsVirtualItem = false
            });
        }

        context.SaveChanges();
    }

    [Theory]
    [InlineData("é", new[] { "émile" })]
    [InlineData("É", new[] { "Émile" })]
    [InlineData("k", new[] { "K", "k" })]
    [InlineData("K", new[] { "K", "k" })]
    [InlineData("\u212A", new[] { "\u212A" })]
    [InlineData("i", new[] { "I", "i", "Émile", "émile" })]
    [InlineData("I", new[] { "I", "i", "Émile", "émile" })]
    [InlineData("\u0130", new[] { "\u0130", "\u0130stanbul" })]
    [InlineData("ß", new[] { "Straße", "ß" })]
    [InlineData("\u1E9E", new[] { "\u1E9E" })]
    [InlineData("ss", new[] { "STRASSE" })]
    [InlineData("SS", new[] { "STRASSE" })]
    [InlineData("hello", new[] { "HELLO", "Hello World", "hello world" })]
    [InlineData("HELLO", new[] { "HELLO", "Hello World", "hello world" })]
    [InlineData("o W", new[] { "Hello World", "hello world" })]
    public void LikeIgnoreAsciiCase_ContainsPattern_FoldsAsciiLettersOnly(string term, string[] expected)
    {
        var pattern = LikePatterns.ContainsIgnoreAsciiCase(term);

        Assert.Equal(expected, Match(pattern));
    }

    [Theory]
    [InlineData("é", new[] { "émile" })]
    [InlineData("É", new[] { "Émile" })]
    [InlineData("\u212A", new[] { "\u212A" })]
    [InlineData("k", new[] { "K", "k" })]
    [InlineData("\u0130", new[] { "\u0130", "\u0130stanbul" })]
    [InlineData("i", new[] { "I", "i" })]
    [InlineData("stra", new[] { "STRASSE", "Straße" })]
    [InlineData("STRAS", new[] { "STRASSE" })]
    [InlineData("hello w", new[] { "Hello World", "hello world" })]
    public void LikeIgnoreAsciiCase_StartsWithPattern_FoldsAsciiLettersOnly(string term, string[] expected)
    {
        var pattern = LikePatterns.StartsWithIgnoreAsciiCase(term);

        Assert.Equal(expected, Match(pattern));
    }

    private string[] Match(string pattern)
    {
        using var context = CreateDbContext();
        return context.BaseItems
            .Where(e => JellyfinDbFunctions.LikeIgnoreAsciiCase(e.SortName, pattern))
            .Select(e => e.SortName!)
            .ToList()
            .Order(StringComparer.Ordinal)
            .ToArray();
    }
}
