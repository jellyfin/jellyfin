using System;
using System.Linq;
using Jellyfin.Database.Implementations;
using Jellyfin.Server.Implementations.Tests.Postgres;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.MySql;

public class MySqlLikeTranslationTests
{
    [Theory]
    [InlineData("8.4-mysql")]
    [InlineData("11.4.13-mariadb")]
    public void LikeIgnoreAsciiCase_FoldsOnlyTheCapitalLettersOfTheAlphabet(string server)
    {
        var sql = Translate(server, LikePatterns.ContainsIgnoreAsciiCase("World"));

        Assert.Equal(26, CountOccurrences(sql, "REPLACE("));
        Assert.Contains("'A', 'a'), 'B', 'b'", sql, StringComparison.Ordinal);
        Assert.Contains("'Y', 'y'), 'Z', 'z')", sql, StringComparison.Ordinal);
        Assert.Contains("LIKE '%world%' ESCAPE '\\\\'", sql, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("8.4-mysql")]
    [InlineData("11.4.13-mariadb")]
    public void LikeIgnoreAsciiCase_TestsTheLoweredTextBeforeTheChain(string server)
    {
        var sql = Translate(server, LikePatterns.ContainsIgnoreAsciiCase("World"));

        var prefilter = sql.IndexOf("LOWER(`b`.`OriginalTitle`) LIKE LOWER(", StringComparison.Ordinal);
        var chain = sql.IndexOf("REPLACE(", StringComparison.Ordinal);
        Assert.InRange(prefilter, 0, chain - 1);
        Assert.Contains(" AND (REPLACE(", sql, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("8.4-mysql")]
    [InlineData("11.4.13-mariadb")]
    public void LikeIgnoreAsciiCase_GivesThePatternOfThePrefilterTheCollationOfTheColumns(string server)
    {
        var sql = Translate(server, LikePatterns.ContainsIgnoreAsciiCase("World"));

        Assert.Contains("COLLATE utf8mb4_0900_bin) ESCAPE", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void LikeIgnoreAsciiCase_PatternWithABackslash_IsWrittenWithTheEscapeOfTheStringLiteral()
    {
        var sql = Translate("8.4-mysql", LikePatterns.ContainsIgnoreAsciiCase("a\\b"));

        Assert.Equal(2, CountOccurrences(sql, "'%a\\\\\\\\b%'"));
    }

    private static string Translate(string server, string pattern)
    {
        using var context = OfflineContexts.MySql(server);
        return context.BaseItems
            .Where(e => JellyfinDbFunctions.LikeIgnoreAsciiCase(e.OriginalTitle, pattern))
            .Select(e => e.Id)
            .ToQueryString();
    }

    private static int CountOccurrences(string text, string value)
    {
        return text.Split(value).Length - 1;
    }
}
