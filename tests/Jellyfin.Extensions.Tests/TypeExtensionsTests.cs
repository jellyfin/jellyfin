using System;
using Xunit;

namespace Jellyfin.Extensions.Tests
{
    public class TypeExtensionsTests
    {
        [Theory]
        [InlineData(typeof(byte), byte.MaxValue, false)]
        [InlineData(typeof(short), short.MinValue, false)]
        [InlineData(typeof(ushort), ushort.MaxValue, false)]
        [InlineData(typeof(int), int.MinValue, false)]
        [InlineData(typeof(uint), uint.MaxValue, false)]
        [InlineData(typeof(long), long.MinValue, false)]
        [InlineData(typeof(ulong), ulong.MaxValue, false)]
        [InlineData(typeof(decimal), -1.0, false)]
        [InlineData(typeof(bool), true, false)]
        [InlineData(typeof(char), 'a', false)]
        [InlineData(typeof(string), "", false)]
        [InlineData(typeof(object), 1, false)]
        [InlineData(typeof(byte), 0, true)]
        [InlineData(typeof(short), 0, true)]
        [InlineData(typeof(ushort), 0, true)]
        [InlineData(typeof(int), 0, true)]
        [InlineData(typeof(uint), 0, true)]
        [InlineData(typeof(long), 0, true)]
        [InlineData(typeof(ulong), 0, true)]
        [InlineData(typeof(decimal), 0, true)]
        [InlineData(typeof(bool), false, true)]
        [InlineData(typeof(char), '\x0000', true)]
        [InlineData(typeof(string), null, true)]
        [InlineData(typeof(object), null, true)]
        [InlineData(typeof(PhonyClass), null, true)]
        [InlineData(typeof(DateTime), null, true)] // Special case handled within the test.
        [InlineData(typeof(DateTime), null, false)] // Special case handled within the test.
        [InlineData(typeof(byte?), null, true)]
        [InlineData(typeof(short?), null, true)]
        [InlineData(typeof(ushort?), null, true)]
        [InlineData(typeof(int?), null, true)]
        [InlineData(typeof(uint?), null, true)]
        [InlineData(typeof(long?), null, true)]
        [InlineData(typeof(ulong?), null, true)]
        [InlineData(typeof(decimal?), null, true)]
        [InlineData(typeof(bool?), null, true)]
        [InlineData(typeof(char?), null, true)]
        public void IsNullOrDefault_Matches_Expected(Type type, object? value, bool expectedResult)
        {
            if (type == typeof(DateTime))
            {
                if (expectedResult)
                {
                    value = default(DateTime);
                }
                else
                {
                    value = DateTime.Now;
                }
            }

            Assert.Equal(expectedResult, type.IsNullOrDefault(value));
            Assert.Equal(expectedResult, value.IsNullOrDefault());
        }

        private class PhonyClass
        {
        }
    }
}
