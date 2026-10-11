using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using MediaBrowser.Providers.Plugins.Omdb;
using Moq;
using Xunit;

namespace Jellyfin.Providers.Tests.Omdb
{
    public class OmdbProviderTests
    {
        private const string TestUrl = "http://localhost/omdb";

        [Fact]
        public void AddPeople_CommaSeparatedList_SplitsIntoIndividualPeople()
        {
            var result = new MetadataResult<Movie>();

            OmdbProvider.AddPeople(result, "Philip G. Epstein, Julius J. Epstein, Howard Koch", PersonKind.Writer);

            Assert.Equal(
                new[] { "Philip G. Epstein", "Julius J. Epstein", "Howard Koch" },
                result.People!.Select(p => p.Name));
            Assert.All(result.People!, p => Assert.Equal(PersonKind.Writer, p.Type));
        }

        [Fact]
        public void AddPeople_RoleAnnotations_AreStrippedAndDeduplicated()
        {
            var result = new MetadataResult<Movie>();

            OmdbProvider.AddPeople(result, "Mari Okada (screenplay), Mari Okada (story), Jun'ichi Satô (screenplay), Jun'ichi Satô (story)", PersonKind.Writer);

            Assert.Equal(
                new[] { "Mari Okada", "Jun'ichi Satô" },
                result.People!.Select(p => p.Name));
        }

        [Theory]
        [InlineData(
            "Jerry Siegel (created by: Superman, Superboy), Bob Kane (created by: Batman)",
            "Jerry Siegel|Bob Kane")]
        [InlineData("Alan Moore (created by: John Constantine)", "Alan Moore")]
        public void AddPeople_CommaInsideAnAnnotation_StaysOneCredit(string credits, string expected)
        {
            var result = new MetadataResult<Movie>();

            OmdbProvider.AddPeople(result, credits, PersonKind.Writer);

            Assert.Equal(expected.Split('|'), result.People!.Select(p => p.Name));
        }

        [Theory]
        [InlineData("Jack Salvatore, Jr.", "Jack Salvatore, Jr.")]
        [InlineData("Efrem Zimbalist, Jr., Tom Hanks", "Efrem Zimbalist, Jr.|Tom Hanks")]
        [InlineData("Tom Hanks, Sammy Davis, Jr", "Tom Hanks|Sammy Davis, Jr")]
        [InlineData("Harold Ramis, Ken Griffey, III (voice)", "Harold Ramis|Ken Griffey, III")]
        [InlineData("Robert Downey Jr., Gwyneth Paltrow", "Robert Downey Jr.|Gwyneth Paltrow")]
        [InlineData("Jr., Tom Hanks", "Jr.|Tom Hanks")]
        public void AddPeople_GenerationalSuffix_StaysWithItsName(string credits, string expected)
        {
            var result = new MetadataResult<Movie>();

            OmdbProvider.AddPeople(result, credits, PersonKind.Actor);

            Assert.Equal(expected.Split('|'), result.People!.Select(p => p.Name));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("(uncredited)")]
        public void AddPeople_NoUsableName_AddsNothing(string? credits)
        {
            var result = new MetadataResult<Movie>();

            OmdbProvider.AddPeople(result, credits!, PersonKind.Actor);

            Assert.Null(result.People);
        }

        [Theory]
        [InlineData("{\"Episodes\":[{\"Plot\":\"Line 1\\nLine 2\"}]}")] // Escaped line feed
        [InlineData("{\"Episodes\":[{\"Plot\":\"Line 1\nLine 2\"}]}")] // Raw line feed (0x0A)
        public async Task GetJsonObjectAsync_LineFeedInJsonString_ReturnsExpectedResult(string jsonResponse)
        {
            var provider = SetupOmdbProvider(response: jsonResponse);

            var result = await provider.GetJsonObjectAsync<OmdbProvider.SeasonRootObject>(TestUrl, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("Line 1\nLine 2", result.Episodes[0].Plot);
        }

        [Theory]
        [InlineData("{\"Episodes\":[{\"Plot\":\"Line 1\\nLine 2\"")] // Escaped line feed, truncated JSON
        [InlineData("{\"Episodes\":[{\"Plot\":\"Line 1\nLine 2\"")] // Raw line feed, truncated JSON
        public async Task GetJsonObjectAsync_TruncatedJson_ThrowsJsonException(string jsonResponse)
        {
            var provider = SetupOmdbProvider(response: jsonResponse);

            await Assert.ThrowsAsync<JsonException>(
                () => provider.GetJsonObjectAsync<OmdbProvider.SeasonRootObject>(TestUrl, CancellationToken.None));
        }

        private static OmdbProvider SetupOmdbProvider(string response)
        {
            // Mock factory to return faked client that returns response as bytes
            var client = new HttpClient(new FakeHttpMessageHandler(Encoding.UTF8.GetBytes(response)));
            var factory = new Mock<IHttpClientFactory>();

            factory.Setup(f => f.CreateClient(NamedClient.Default)).Returns(client);

            return new OmdbProvider(
                factory.Object,
                Mock.Of<IFileSystem>(),
                Mock.Of<IServerConfigurationManager>());
        }

        private sealed class FakeHttpMessageHandler : HttpMessageHandler
        {
            private readonly byte[] _bytes;

            public FakeHttpMessageHandler(byte[] bytes)
            {
                _bytes = bytes;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(_bytes)
                };

                return Task.FromResult(response);
            }
        }
    }
}
