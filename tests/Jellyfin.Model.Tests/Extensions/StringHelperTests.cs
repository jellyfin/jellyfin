using System;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
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
        [InlineData("\027", "\027")]
        public void StringHelper_ValidArgs_Success(string input, string expectedResult)
        {
            Assert.Equal(expectedResult, StringHelper.FirstToUpper(input));
        }

        [Property]
        public Property FirstToUpper_RandomArg_Correct(NonEmptyString input)
        {
            var result = StringHelper.FirstToUpper(input.Item);

            // We check IsLower instead of IsUpper because both return false for non-letters
            return (!char.IsLower(result[0])).Label("First char is uppercase")
                .And(input.Item.Length == 1 || result[1..].Equals(input.Item[1..], StringComparison.Ordinal)).Label("Remaining chars are unmodified");
        }
    }
}
