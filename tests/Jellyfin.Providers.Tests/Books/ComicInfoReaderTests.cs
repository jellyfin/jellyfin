using System;
using System.Globalization;
using System.Xml.Linq;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Providers.Books.ComicInfo;
using Xunit;

namespace Jellyfin.Providers.Tests.Books
{
    public class ComicInfoReaderTests
    {
        private readonly XDocument _document;

        public ComicInfoReaderTests()
        {
            _document = GenerateTestData();
        }

        [Fact]
        public void ReadTitle_Success()
        {
            var actual = ComicInfoReader.ReadComicBookMetadata(_document);
            Assert.NotNull(actual);
            Assert.Equal("The Desperate Battle Begins!", actual.Name);
        }

        [Fact]
        public void ReadAlternativeSeries_Success()
        {
            // Based on the The Anansi Project, some US comics can be part of cross-over
            // story arcs. This field is used to specify an alternate series
            // https://anansi-project.github.io/docs/comicinfo/documentation#alternateseries--alternatenumber--alternatecount
            // However, software like ComicTagger (https://github.com/comictagger/comictagger) uses
            // this field for the series name in the original language when tagging manga
            var actual = ComicInfoReader.ReadComicBookMetadata(_document);
            Assert.NotNull(actual);
            Assert.Equal("進撃の巨人", actual.OriginalTitle);
        }

        [Fact]
        public void ReadSeries_Success()
        {
            var actual = ComicInfoReader.ReadComicBookMetadata(_document);
            Assert.NotNull(actual);
            Assert.Equal("Attack on Titan", actual.SeriesName);
        }

        [Fact(DisplayName = "Check that the issue equals the index number.")]
        public void ReadNumber_Success()
        {
            var actual = ComicInfoReader.ReadComicBookMetadata(_document);
            Assert.NotNull(actual);
            Assert.Equal(1, actual.IndexNumber);
        }

        [Fact]
        public void ReadSummary_Success()
        {
            var actual = ComicInfoReader.ReadComicBookMetadata(_document);
            var expected = "Eren Jaeger lives in city surrounded by monolithic walls. Outside dwell human murdering Titans. For decades members of the " +
                "Scouting Legion have been the only humans who dared to leave the safety of the walls and gather information on the Titans. Every time " +
                "they return, many of them are dead. Freedom loving Eren has no greater wish than to join them. \n\n Chapter TitlesEpisode 1: To You, " +
                "2,000 Years From NowEpisode 2: That DayEpisode 3: Night of the Disbanding CeremonyEpisode 4: First Battle";

            Assert.NotNull(actual);
            Assert.Equal(expected, actual.Overview);
        }

        [Fact]
        public void ReadProductionYear_Success()
        {
            var actual = ComicInfoReader.ReadComicBookMetadata(_document);
            Assert.NotNull(actual);
            Assert.Equal(2012, actual.ProductionYear);
        }

        [Fact]
        public void ReadDate_Success()
        {
            var actual = ComicInfoReader.ReadComicBookMetadata(_document);
            var expected = new DateTime(2012, 6, 30, 0, 0, 0, DateTimeKind.Unspecified);

            Assert.NotNull(actual);
            Assert.Equal(expected, actual.PremiereDate);
        }

        [Fact]
        public void ReadGenres_Success()
        {
            var actual = ComicInfoReader.ReadComicBookMetadata(_document);
            Assert.NotNull(actual);
            Assert.Equal(["Action", "Dark fantasy", "Post-apocalyptic"], actual.Genres);
        }

        [Fact]
        public void ReadPublisher_Success()
        {
            var actual = ComicInfoReader.ReadComicBookMetadata(_document);
            Assert.NotNull(actual);
            Assert.Single(actual.Studios);
            Assert.Equal("Kodansha Comics USA", actual.Studios[0]);
        }

        [Fact]
        public void ReadPeopleMetadata_Success()
        {
            var metadataResult = new MetadataResult<Book> { Item = new Book(), HasMetadata = true };

            ComicInfoReader.ReadPeopleMetadata(_document, metadataResult);

            Assert.Collection(
                metadataResult.People,
                person =>
                {
                    Assert.Equal("Hajime Isayama", person.Name);
                    Assert.Equal(PersonKind.Author, person.Type);
                },
                person =>
                {
                    Assert.Equal("A Penciller", person.Name);
                    Assert.Equal(PersonKind.Penciller, person.Type);
                },
                person =>
                {
                    Assert.Equal("An Inker", person.Name);
                    Assert.Equal(PersonKind.Inker, person.Type);
                },
                person =>
                {
                    Assert.Equal("Steve Wands", person.Name);
                    Assert.Equal(PersonKind.Letterer, person.Type);
                },
                person =>
                {
                    Assert.Equal("Artist A", person.Name);
                    Assert.Equal(PersonKind.CoverArtist, person.Type);
                },
                person =>
                {
                    Assert.Equal("Takashi Shimoyama", person.Name);
                    Assert.Equal(PersonKind.CoverArtist, person.Type);
                },
                person =>
                {
                    Assert.Equal("An Colourist", person.Name);
                    Assert.Equal(PersonKind.Colorist, person.Type);
                });
        }

        [Fact]
        public void ReadCultureInfoInto_Success()
        {
            CultureInfo? actual = null;

            ComicInfoReader.ReadCultureInfoInto(_document, "ComicInfo/LanguageISO", cultureInfo => actual = cultureInfo);

            Assert.NotNull(actual);
            Assert.Equal(new CultureInfo("en").CompareInfo, actual.CompareInfo);
        }

        [Fact]
        public void ReadCultureInfoInto_MissingElement_DoesNotCommit()
        {
            var committed = false;

            ComicInfoReader.ReadCultureInfoInto(_document, "ComicInfo/NotALanguageElement", _ => committed = true);

            Assert.False(committed);
        }

        private static XDocument GenerateTestData()
        {
            var document = new XDocument(new XDeclaration("1.0", string.Empty, string.Empty));

            var xsi = XNamespace.Get("http://www.w3.org/2001/XMLSchema-instance");
            var xsd = XNamespace.Get("http://www.w3.org/2001/XMLSchema");
            var comicInfo = new XElement("ComicInfo", new XAttribute(XNamespace.Xmlns + "xsi", xsi), new XAttribute(XNamespace.Xmlns + "xsd", xsd));
            document.Add(comicInfo);

            comicInfo.Add(new XElement("Title", "The Desperate Battle Begins!"));
            comicInfo.Add(new XElement("AlternateSeries", "進撃の巨人"));
            comicInfo.Add(new XElement("Series", "Attack on Titan"));
            comicInfo.Add(new XElement("Number", "1"));
            comicInfo.Add(new XElement("Count", "1"));
            comicInfo.Add(new XElement("Volume", "1"));
            comicInfo.Add(new XElement("Summary", "Eren Jaeger lives in city surrounded by monolithic walls. Outside dwell human murdering Titans. For decades " +
                    "members of the Scouting Legion have been the only humans who dared to leave the safety of the walls and gather information on the Titans. " +
                    "Every time they return, many of them are dead. Freedom loving Eren has no greater wish than to join them. \n\n Chapter TitlesEpisode 1: " +
                    "To You, 2,000 Years From NowEpisode 2: That DayEpisode 3: Night of the Disbanding CeremonyEpisode 4: First Battle"));
            comicInfo.Add(new XElement("Notes", "Tagged with ComicTagger 1.3.0a0 using info from Comic Vine on 2021-07-24 01:15:20.  [Issue ID 342215]"));
            comicInfo.Add(new XElement("Year", "2012"));
            comicInfo.Add(new XElement("Month", "6"));
            comicInfo.Add(new XElement("Day", "30"));
            comicInfo.Add(new XElement("Writer", "Hajime Isayama"));
            comicInfo.Add(new XElement("Penciller", "A Penciller"));
            comicInfo.Add(new XElement("Inker", "An Inker"));
            comicInfo.Add(new XElement("Colourist", "An Colourist"));
            comicInfo.Add(new XElement("Letterer", "Steve Wands"));
            comicInfo.Add(new XElement("CoverArtist", "Artist A, Takashi Shimoyama"));
            comicInfo.Add(new XElement("Publisher", "Kodansha Comics USA"));
            comicInfo.Add(new XElement("Genre", "Action, Dark fantasy, Post-apocalyptic"));
            comicInfo.Add(new XElement("Web", "https://comicvine.gamespot.com/attack-on-titan-1-the-desperate-battle-begins/4000-342215"));
            comicInfo.Add(new XElement("PageCount", "210"));
            comicInfo.Add(new XElement("LanguageISO", "en"));
            comicInfo.Add(new XElement("Format", "Black & White"));
            comicInfo.Add(new XElement("Manga", "Yes"));
            comicInfo.Add(new XElement("Characters", "Annie Leonhart, Armin Arlert, Bertolt Hoover, Carla Yeager, Connie Springer, Eren Yeager, Franz Kefka, " +
                    "Grisha Yeager, Hannah Diamant, Hannes, Jean Kirstein, Krista Lenz, Marco Bott, Mikasa Ackerman, Mina Carolina, Reiner Braun, Samuel " +
                    "Linke-Jackson, Sasha Blouse, Thomas Wagner"));
            comicInfo.Add(new XElement("Teams", "Titans"));
            comicInfo.Add(new XElement("Locations", "Shiganshina District, Trost District, Wall Maria, Wall Rose"));
            comicInfo.Add(new XElement("ScanInformation", "vol1"));

            // add the cover page and example pages
            var pages = new XElement("Pages");
            comicInfo.Add(pages);
            // image size is arbitrarily chosen instead of using a real image size (in bytes) for each page
            pages.Add(new XElement("Page", new XAttribute("Image", "0"), new XAttribute("Type", "FrontCover"), new XAttribute("ImageSize", "41911")));
            // add the remaining 209 pages, starting from 1, as page 0 has already been added
            for (int i = 1; i <= 210; i++)
            {
                pages.Add(new XElement("Page", new XAttribute("Image", i), new XAttribute("ImageSize", "14922")));
            }

            return document;
        }
    }
}
