using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Configuration;
using Moq;
using Xunit;

namespace Jellyfin.Controller.Tests.Entities;

public class UserViewBuilderFilterTests
{
    public UserViewBuilderFilterTests()
    {
        var configManager = new Mock<IServerConfigurationManager>();
        configManager.Setup(m => m.Configuration).Returns(new ServerConfiguration());
        BaseItem.ConfigurationManager = configManager.Object;
    }

    private static Video CreateVideo(string name) => new Video { Name = name };

    [Theory]
    // Numeric names: NameStartsWith should match after stripping zero-padding
    [InlineData("12 Years a Slave", "1", true)]
    [InlineData("12 Years a Slave", "12", true)]
    [InlineData("12 Years a Slave", "2", false)]
    [InlineData("300", "3", true)]
    [InlineData("300", "30", true)]
    [InlineData("300", "0", false)]
    // Alphabetic names: standard prefix match on SortName
    [InlineData("Avatar", "A", true)]
    [InlineData("Avatar", "a", true)]
    [InlineData("Avatar", "B", false)]
    [InlineData("The Matrix", "m", true)]
    [InlineData("The Matrix", "t", false)]
    // Mixed alpha-numeric input: handled via ModifySortChunks padding
    [InlineData("Apollo 13", "apollo 13", true)]
    [InlineData("Apollo 13", "apollo 2", false)]
    // Edge case: "0" should only match items whose name is literally "0"
    [InlineData("0", "0", true)]
    [InlineData("1", "0", false)]
    public void Filter_NameStartsWith_MatchesNumericAndAlphabeticPrefixes(string name, string nameStartsWith, bool expected)
    {
        var item = CreateVideo(name);

        var query = new InternalItemsQuery
        {
            NameStartsWith = nameStartsWith
        };

        var result = UserViewBuilder.Filter(item, null!, query, null!, null!);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Filter_NameStartsWith_NullOrEmpty_PassesThrough()
    {
        var item = CreateVideo("Avatar");

        var queryNull = new InternalItemsQuery { NameStartsWith = null };
        var queryEmpty = new InternalItemsQuery { NameStartsWith = string.Empty };

        Assert.True(UserViewBuilder.Filter(item, null!, queryNull, null!, null!));
        Assert.True(UserViewBuilder.Filter(item, null!, queryEmpty, null!, null!));
    }

    [Theory]
    // Input is plain user value — padding is applied internally
    [InlineData("12 Years a Slave", "12", true)]
    [InlineData("12 Years a Slave", "13", false)]
    [InlineData("Avatar", "a", true)]
    [InlineData("Avatar", "avatar", true)]
    [InlineData("Avatar", "b", false)]
    public void Filter_NameStartsWithOrGreater_PadsInputBeforeComparing(string name, string nameStartsWithOrGreater, bool expected)
    {
        var item = CreateVideo(name);

        var query = new InternalItemsQuery
        {
            NameStartsWithOrGreater = nameStartsWithOrGreater
        };

        var result = UserViewBuilder.Filter(item, null!, query, null!, null!);

        Assert.Equal(expected, result);
    }

    [Theory]
    // Input is plain user value — padding is applied internally
    [InlineData("12 Years a Slave", "13", true)]
    [InlineData("12 Years a Slave", "12", false)]
    [InlineData("Avatar", "b", true)]
    [InlineData("Avatar", "a", false)]
    public void Filter_NameLessThan_PadsInputBeforeComparing(string name, string nameLessThan, bool expected)
    {
        var item = CreateVideo(name);

        var query = new InternalItemsQuery
        {
            NameLessThan = nameLessThan
        };

        var result = UserViewBuilder.Filter(item, null!, query, null!, null!);

        Assert.Equal(expected, result);
    }
}
