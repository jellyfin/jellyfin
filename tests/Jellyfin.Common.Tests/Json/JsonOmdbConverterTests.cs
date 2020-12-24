using System.Text.Json;
using MediaBrowser.Common.Json;
using MediaBrowser.Providers.Plugins.Omdb;
using Xunit;

namespace Jellyfin.Common.Tests.Json
{
    public static class JsonOmdbConverterTests
    {
        [Fact]
        public static void Deserialize_Omdb_Response_Not_Available_Success()
        {
            const string Input = "{\"Title\":\"Chapter 1\",\"Year\":\"2013\",\"Rated\":\"TV-MA\",\"Released\":\"01 Feb 2013\",\"Season\":\"N/A\",\"Episode\":\"N/A\",\"Runtime\":\"55 min\",\"Genre\":\"Drama\",\"Director\":\"David Fincher\",\"Writer\":\"Michael Dobbs (based on the novels by), Andrew Davies (based on the mini-series by), Beau Willimon (created for television by), Beau Willimon, Sam Forman (staff writer)\",\"Actors\":\"Kevin Spacey, Robin Wright, Kate Mara, Corey Stoll\",\"Plot\":\"Congressman Francis Underwood has been declined the chair for Secretary of State. He's now gathering his own team to plot his revenge. Zoe Barnes, a reporter for the Washington Herald, will do anything to get her big break.\",\"Language\":\"English\",\"Country\":\"USA\",\"Awards\":\"N/A\",\"Poster\":\"https://m.media-amazon.com/images/M/MV5BMTY5MTU4NDQzNV5BMl5BanBnXkFtZTgwMzk2ODcxMzE@._V1_SX300.jpg\",\"Ratings\":[{\"Source\":\"Internet Movie Database\",\"Value\":\"8.7/10\"}],\"Metascore\":\"N/A\",\"imdbRating\":\"8.7\",\"imdbVotes\":\"6736\",\"imdbID\":\"tt2161930\",\"seriesID\":\"N/A\",\"Type\":\"episode\",\"Response\":\"True\"}";
            var seasonRootObject = JsonSerializer.Deserialize<OmdbProvider.RootObject>(Input, JsonDefaults.GetOptions());
            Assert.NotNull(seasonRootObject);
            Assert.Null(seasonRootObject?.Awards);
            Assert.Null(seasonRootObject?.Episode);
            Assert.Null(seasonRootObject?.Metascore);
        }
    }
}
