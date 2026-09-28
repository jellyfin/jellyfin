using System.Linq;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Providers.Plugins.Omdb;
using Xunit;

namespace Jellyfin.Providers.Tests.Omdb
{
    public class OmdbProviderTests
    {
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
    }
}
