using System.Collections.Generic;
using System.Linq;
using System.Text;
using Emby.Server.Implementations.Localization;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Localization;

public class RatingsDataVersionTests
{
    private static KeyValuePair<string, byte[]> Resource(string name, string content)
        => new(name, Encoding.UTF8.GetBytes(content));

    [Fact]
    public void RatingsDataVersion_IsStable()
    {
        Assert.Equal(LocalizationManager.RatingsDataVersion, LocalizationManager.RatingsDataVersion);
    }

    [Fact]
    public void RatingsDataVersion_HashesTheEmbeddedRatingResources()
    {
        // A wrong resource prefix would hash nothing and give the same value on every build.
        Assert.NotEqual(LocalizationManager.ComputeRatingsDataVersion([]), LocalizationManager.RatingsDataVersion);
    }

    [Fact]
    public void RatingsDataVersion_IsLowercaseHexSha256()
    {
        var version = LocalizationManager.RatingsDataVersion;

        Assert.Equal(64, version.Length);
        Assert.All(version, c => Assert.True(char.IsAsciiDigit(c) || (c >= 'a' && c <= 'f'), $"'{c}' is not lowercase hex"));
    }

    [Fact]
    public void ComputeRatingsDataVersion_ResourceOrderDoesNotMatter()
    {
        var a = Resource("Ratings.us.json", "{\"a\":1}");
        var b = Resource("Ratings.de.json", "{\"b\":2}");

        Assert.Equal(
            LocalizationManager.ComputeRatingsDataVersion([a, b]),
            LocalizationManager.ComputeRatingsDataVersion([b, a]));
    }

    [Fact]
    public void ComputeRatingsDataVersion_ChangedByteChangesVersion()
    {
        var original = LocalizationManager.ComputeRatingsDataVersion([Resource("Ratings.us.json", "{\"score\":10}")]);
        var changed = LocalizationManager.ComputeRatingsDataVersion([Resource("Ratings.us.json", "{\"score\":11}")]);

        Assert.NotEqual(original, changed);
    }

    [Fact]
    public void ComputeRatingsDataVersion_RenamedResourceChangesVersion()
    {
        var original = LocalizationManager.ComputeRatingsDataVersion([Resource("Ratings.us.json", "{}")]);
        var renamed = LocalizationManager.ComputeRatingsDataVersion([Resource("Ratings.de.json", "{}")]);

        Assert.NotEqual(original, renamed);
    }

    [Fact]
    public void ComputeRatingsDataVersion_LineEndingsDoNotMatter()
    {
        Assert.Equal(
            LocalizationManager.ComputeRatingsDataVersion([Resource("Ratings.us.json", "{\n  \"a\": 1\n}\n")]),
            LocalizationManager.ComputeRatingsDataVersion([Resource("Ratings.us.json", "{\r\n  \"a\": 1\r\n}\r\n")]));
    }

    [Fact]
    public void ComputeRatingsDataVersion_AddedResourceChangesVersion()
    {
        var one = new[] { Resource("Ratings.us.json", "{}") };
        var two = one.Append(Resource("Ratings.de.json", "{}"));

        Assert.NotEqual(
            LocalizationManager.ComputeRatingsDataVersion(one),
            LocalizationManager.ComputeRatingsDataVersion(two));
    }
}
