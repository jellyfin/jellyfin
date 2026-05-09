using System;
using System.Collections.Generic;
using System.Text;
using MediaBrowser.Model.Dlna;
using Xunit;

namespace Jellyfin.Model.Tests.Dlna;

public class StreamInfoTests
{
    private const string BaseUrl = "/test/";
    private const int RandomSeed = 298347823;

    /// <summary>
    /// Returns a random float.
    /// </summary>
    /// <param name="random">The <see cref="Random"/> instance.</param>
    /// <returns>A random <see cref="float"/>.</returns>
    private static float RandomFloat(Random random)
    {
        var buffer = new byte[4];
        random.NextBytes(buffer);
        return BitConverter.ToSingle(buffer, 0);
    }

    /// <summary>
    /// Creates a random array.
    /// </summary>
    /// <param name="random">The <see cref="Random"/> instance.</param>
    /// <param name="elementType">The element <see cref="Type"/> of the array.</param>
    /// <returns>An <see cref="Array"/> of <see cref="Type"/>.</returns>
    private static object? RandomArray(Random random, Type? elementType)
    {
        if (elementType is null)
        {
            return null;
        }

        if (elementType == typeof(string))
        {
            return RandomStringArray(random);
        }

        if (elementType == typeof(int))
        {
            return RandomIntArray(random);
        }

        if (elementType.IsEnum)
        {
            var values = Enum.GetValues(elementType);
            return RandomIntArray(random, 0, values.Length - 1);
        }

        throw new ArgumentException("Unsupported array type " + elementType.ToString());
    }

    /// <summary>
    /// Creates a random length string.
    /// </summary>
    /// <param name="random">The <see cref="Random"/> instance.</param>
    /// <param name="minLength">The minimum length of the string.</param>
    /// <param name="maxLength">The maximum length of the string.</param>
    /// <returns>The string.</returns>
    private static string RandomString(Random random, int minLength = 0, int maxLength = 256)
    {
        var len = random.Next(minLength, maxLength);
        var sb = new StringBuilder(len);

        while (len > 0)
        {
            sb.Append((char)random.Next(65, 97));
            len--;
        }

        return sb.ToString();
    }

    /// <summary>
    /// Creates a random long.
    /// </summary>
    /// <param name="random">The <see cref="Random"/> instance.</param>
    /// <param name="min">Min value.</param>
    /// <param name="max">Max value.</param>
    /// <returns>A random <see cref="long"/> between <paramref name="min"/> and <paramref name="max"/>.</returns>
    private static long RandomLong(Random random, long min = -9223372036854775808, long max = 9223372036854775807)
    {
        long result = random.Next((int)(min >> 32), (int)(max >> 32));
        result <<= 32;
        result |= (long)random.Next((int)(min >> 32) << 32, (int)(max >> 32) << 32);
        return result;
    }

    /// <summary>
    /// Creates a random string array containing between <paramref name="minLength"/> and <paramref name="maxLength"/>.
    /// </summary>
    /// <param name="random">The <see cref="Random"/> instance.</param>
    /// <param name="minLength">The minimum number of elements.</param>
    /// <param name="maxLength">The maximum number of elements.</param>
    /// <returns>A random <see cref="string[]"/> instance.</returns>
    private static string[] RandomStringArray(Random random, int minLength = 0, int maxLength = 9)
    {
        var len = random.Next(minLength, maxLength);
        var arr = new List<string>(len);
        while (len > 0)
        {
            arr.Add(RandomString(random, 1, 30));
            len--;
        }

        return arr.ToArray();
    }

    /// <summary>
    /// Creates a random int array containing between <paramref name="minLength"/> and <paramref name="maxLength"/>.
    /// </summary>
    /// <param name="random">The <see cref="Random"/> instance.</param>
    /// <param name="minLength">The minimum number of elements.</param>
    /// <param name="maxLength">The maximum number of elements.</param>
    /// <returns>A random <see cref="int[]"/> instance.</returns>
    private static int[] RandomIntArray(Random random, int minLength = 0, int maxLength = 9)
    {
        var len = random.Next(minLength, maxLength);
        var arr = new List<int>(len);
        while (len > 0)
        {
            arr.Add(random.Next());
            len--;
        }

        return arr.ToArray();
    }

    /// <summary>
    /// Fills most properties with random data.
    /// </summary>
    /// <param name="destination">The instance to fill with data.</param>
    private static void FillAllProperties<T>(T destination)
    {
        var random = new Random(RandomSeed);
        var objectType = destination!.GetType();
        foreach (var property in objectType.GetProperties())
        {
            if (!(property.CanRead && property.CanWrite))
            {
                continue;
            }

            var type = property.PropertyType;
            // If nullable, then set it to null, 25% of the time.
            if (Nullable.GetUnderlyingType(type) is not null)
            {
                if (random.Next(0, 4) == 0)
                {
                    // Set it to null.
                    property.SetValue(destination, null);
                    continue;
                }
            }

            if (type == typeof(Guid))
            {
                property.SetValue(destination, Guid.NewGuid());
                continue;
            }

            if (type.IsEnum)
            {
                Array values = Enum.GetValues(property.PropertyType);
                property.SetValue(destination, values.GetValue(random.Next(0, values.Length - 1)));
                continue;
            }

            if (type == typeof(long))
            {
                property.SetValue(destination, RandomLong(random));
                continue;
            }

            if (type == typeof(string))
            {
                property.SetValue(destination, RandomString(random));
                continue;
            }

            if (type == typeof(bool))
            {
                property.SetValue(destination, random.Next(0, 1) == 1);
                continue;
            }

            if (type == typeof(float))
            {
                property.SetValue(destination, RandomFloat(random));
                continue;
            }

            if (type.IsArray)
            {
                property.SetValue(destination, RandomArray(random, type.GetElementType()));
                continue;
            }
        }
    }

    [InlineData(DlnaProfileType.Audio)]
    [InlineData(DlnaProfileType.Video)]
    [InlineData(DlnaProfileType.Photo)]
    [Theory]
    public void Test_Blank_Url_Method(DlnaProfileType type)
    {
        var streamInfo = new LegacyStreamInfo(Guid.Empty, type)
        {
            DeviceProfile = new DeviceProfile()
        };

        string legacyUrl = streamInfo.ToUrl_Original(BaseUrl, "123");

        string newUrl = streamInfo.ToUrl(BaseUrl, "123", null);

        Assert.Equal(legacyUrl, newUrl, ignoreCase: true);
    }

    [Fact]
    public void Fuzzy_Comparison()
    {
        var streamInfo = new LegacyStreamInfo(Guid.Empty, DlnaProfileType.Video)
        {
            DeviceProfile = new DeviceProfile()
        };
        for (int i = 0; i < 100000; i++)
        {
            FillAllProperties(streamInfo);
            string legacyUrl = streamInfo.ToUrl_Original(BaseUrl, "123");

            string newUrl = streamInfo.ToUrl(BaseUrl, "123", null);

            Assert.Equal(legacyUrl, newUrl, ignoreCase: true);
        }
    }
}
