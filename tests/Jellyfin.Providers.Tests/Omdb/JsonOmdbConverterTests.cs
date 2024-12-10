using System.Text.Json;
using System.Text.Json.Serialization;
using MediaBrowser.Providers.Plugins.Omdb;
using Xunit;

namespace Jellyfin.Providers.Tests.Omdb
{
    public class JsonOmdbConverterTests
    {
        private readonly JsonSerializerOptions _options;

        public JsonOmdbConverterTests()
        {
            _options = new JsonSerializerOptions();
            _options.Converters.Add(new JsonOmdbNotAvailableStringConverter());
            _options.Converters.Add(new JsonOmdbNotAvailableInt32Converter());
            _options.NumberHandling = JsonNumberHandling.AllowReadingFromString;
        }

        [Fact]
        public void Deserialize_Omdb_Response_Not_Available_Success()
        {
            const string Input = "{\"Title\":\"Chapter 1\",\"Year\":\"2013\",\"Rated\":\"TV-MA\",\"Released\":\"01 Feb 2013\",\"Season\":\"N/A\",\"Episode\":\"N/A\",\"Runtime\":\"55 min\",\"Genre\":\"Drama\",\"Director\":\"David Fincher\",\"Writer\":\"Michael Dobbs (based on the novels by), Andrew Davies (based on the mini-series by), Beau Willimon (created for television by), Beau Willimon, Sam Forman (staff writer)\",\"Actors\":\"Kevin Spacey, Robin Wright, Kate Mara, Corey Stoll\",\"Plot\":\"Congressman Francis Underwood has been declined the chair for Secretary of State. He's now gathering his own team to plot his revenge. Zoe Barnes, a reporter for the Washington Herald, will do anything to get her big break.\",\"Language\":\"English\",\"Country\":\"USA\",\"Awards\":\"N/A\",\"Poster\":\"https://m.media-amazon.com/images/M/MV5BMTY5MTU4NDQzNV5BMl5BanBnXkFtZTgwMzk2ODcxMzE@._V1_SX300.jpg\",\"Ratings\":[{\"Source\":\"Internet Movie Database\",\"Value\":\"8.7/10\"}],\"Metascore\":\"N/A\",\"imdbRating\":\"8.7\",\"imdbVotes\":\"6736\",\"imdbID\":\"tt2161930\",\"seriesID\":\"N/A\",\"Type\":\"episode\",\"Response\":\"True\"}";
            var seasonRootObject = JsonSerializer.Deserialize<OmdbProvider.RootObject>(Input, _options);
            Assert.NotNull(seasonRootObject);
            Assert.Null(seasonRootObject?.Awards);
            Assert.Null(seasonRootObject?.Episode);
            Assert.Null(seasonRootObject?.Metascore);
        }

        [Theory]
        [InlineData("\"N/A\"")]
        [InlineData("null")]
        public void Deserialization_To_Nullable_Int_Should_Be_Null(string input)
        {
            var result = JsonSerializer.Deserialize<int?>(input, _options);
            Assert.Null(result);
        }

        [Theory]
        [InlineData("\"8\"", 8)]
        [InlineData("8", 8)]
        public void Deserialize_NullableInt_Success(string input, int? expected)
        {
            var result = JsonSerializer.Deserialize<int?>(input, _options);
            Assert.Equal(result, expected);
        }

        [Theory]
        [InlineData("\"N/A\"")]
        [InlineData("null")]
        public void Deserialization_To_Nullable_String_Should_Be_Null(string input)
        {
            var result = JsonSerializer.Deserialize<string?>(input, _options);
            Assert.Null(result);
        }

        [Theory]
        [InlineData("\"Jellyfin\"", "Jellyfin")]
        public void Deserialize_Normal_String_Success(string input, string expected)
        {
            var result = JsonSerializer.Deserialize<string?>(input, _options);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Roundtrip_Valid_Success()
        {
            const string Input = "{\"Title\":\"Chapter 1\",\"Year\":\"2013\",\"Rated\":\"TV-MA\",\"Released\":\"01 Feb 2013\",\"Season\":\"N/A\",\"Episode\":\"N/A\",\"Runtime\":\"55 min\",\"Genre\":\"Drama\",\"Director\":\"David Fincher\",\"Writer\":\"Michael Dobbs (based on the novels by), Andrew Davies (based on the mini-series by), Beau Willimon (created for television by), Beau Willimon, Sam Forman (staff writer)\",\"Actors\":\"Kevin Spacey, Robin Wright, Kate Mara, Corey Stoll\",\"Plot\":\"Congressman Francis Underwood has been declined the chair for Secretary of State. He's now gathering his own team to plot his revenge. Zoe Barnes, a reporter for the Washington Herald, will do anything to get her big break.\",\"Language\":\"English\",\"Country\":\"USA\",\"Awards\":\"N/A\",\"Poster\":\"https://m.media-amazon.com/images/M/MV5BMTY5MTU4NDQzNV5BMl5BanBnXkFtZTgwMzk2ODcxMzE@._V1_SX300.jpg\",\"Ratings\":[{\"Source\":\"Internet Movie Database\",\"Value\":\"8.7/10\"}],\"Metascore\":\"N/A\",\"imdbRating\":\"8.7\",\"imdbVotes\":\"6736\",\"imdbID\":\"tt2161930\",\"seriesID\":\"N/A\",\"Type\":\"episode\",\"Response\":\"True\"}";
            var trip1 = JsonSerializer.Deserialize<OmdbProvider.RootObject>(Input, _options);
            Assert.NotNull(trip1);
            Assert.NotNull(trip1?.Title);
            Assert.Null(trip1?.Awards);
            Assert.Null(trip1?.Episode);
            Assert.Null(trip1?.Metascore);

            var serializedTrip1 = JsonSerializer.Serialize(trip1!, _options);
            var trip2 = JsonSerializer.Deserialize<OmdbProvider.RootObject>(serializedTrip1, _options);
            Assert.NotNull(trip2);
            Assert.NotNull(trip2?.Title);
            Assert.Null(trip2?.Awards);
            Assert.Null(trip2?.Episode);
            Assert.Null(trip2?.Metascore);
        }
    }
}
