using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Jellyfin.Extensions.Tests
{
    public static class DictionaryExtensionTests
    {
        public static TheoryData<IReadOnlyDictionary<string, string>, IReadOnlyList<string>, string?> FirstNotNullNorWhiteSpace_TestData()
        {
            var input = new Dictionary<string, string>
            {
                { "Key1", "Value1" },
                { "Key2", string.Empty },
                { "Key3", "   " },
                { "Key4", null! },
                { "Key5", "Value5" }
            };

            var data = new TheoryData<IReadOnlyDictionary<string, string>, IReadOnlyList<string>, string?>
            {
                { input, ["Key1"], "Value1" },
                { input, ["Key2"], null },
                { input, ["Key3"], null },
                { input, ["Key4"], null },
                { input, ["Key2", "Key3", "Key4", "Key5"], "Value5" },
            };

            return data;
        }

        [Theory]
        [MemberData(nameof(FirstNotNullNorWhiteSpace_TestData))]
        public static void Returns_First_NonNull_NonWhitespace_Value(IReadOnlyDictionary<string, string> input, IReadOnlyList<string> keys, string? expectedOutput)
        {
            var result = input.GetFirstNotNullNorWhiteSpaceValue(keys.ToArray());
            Assert.Equal(expectedOutput, result);
        }
    }
}
