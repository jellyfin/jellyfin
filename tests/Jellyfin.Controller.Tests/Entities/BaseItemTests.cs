using MediaBrowser.Controller.Entities;
using Xunit;

namespace Jellyfin.Controller.Tests.Entities
{
    public class BaseItemTests
    {
        [Theory]
        [InlineData("", "")]
        [InlineData("1", "0000000001")]
        [InlineData("t", "t")]
        [InlineData("test", "test")]
        [InlineData("test1", "test0000000001")]
        [InlineData("1test 2", "0000000001test 0000000002")]
        public void BaseItem_ModifySortChunks_Valid(string input, string expected)
            => Assert.Equal(expected, BaseItem.ModifySortChunks(input));
    }
}
