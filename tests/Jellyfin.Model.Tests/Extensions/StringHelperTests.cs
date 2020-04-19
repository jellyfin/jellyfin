using System;
using MediaBrowser.Model.Extensions;
using Xunit;

namespace Jellyfin.Model.Tests.Extensions
{
    public class StringHelperTests
    {
        [Theory]
        [InlineData("", "")]
        [InlineData("banana", "Banana")]
        [InlineData("Banana", "Banana")]
        [InlineData("ä", "Ä")]
        public void FirstToUpperTest(string str, string result)
        {
            Assert.Equal(result, StringHelper.FirstToUpper(str));
        }
    }
}
