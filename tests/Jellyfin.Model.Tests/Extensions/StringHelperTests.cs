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
        public void StringHelper_ValidArgs_Success(string input, string expectedResult)
        {
            Assert.Equal(expectedResult, StringHelper.FirstToUpper(input));
        }
    }
}
