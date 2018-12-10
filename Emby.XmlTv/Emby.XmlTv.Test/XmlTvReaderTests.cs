using System;
using System.IO;
using System.Linq;
using System.Threading;

using Emby.XmlTv.Classes;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Emby.XmlTv.Test
{
    [TestClass]
    public class XmlTvReaderTests
    {
        [TestMethod]
        [DeploymentItem("Xml Files\\UK_Data.xml")]
        public void UK_DataTest_ChannelsTest()
        {
            var testFile = Path.GetFullPath(@"UK_Data.xml");
            var reader = new XmlTvReader(testFile);

            var channels = reader.GetChannels().ToList();
            Assert.AreEqual(5, channels.Count);

            // Check each channel
            var channel = channels.SingleOrDefault(c => c.Id == "UK_RT_2667");
            Assert.IsNotNull(channel);
            Assert.AreEqual("BBC1 HD", channel.DisplayName);
            Assert.AreEqual("7.1", channel.Number);
            Assert.IsNotNull(channel.Icon);
            Assert.AreEqual("Logo_UK_RT_2667", channel.Icon.Source);
            Assert.AreEqual(100, channel.Icon.Width);
            Assert.AreEqual(200, channel.Icon.Height);

            channel = channels.SingleOrDefault(c => c.Id == "UK_RT_105");
            Assert.IsNotNull(channel);
            Assert.AreEqual("BBC2", channel.DisplayName);
            Assert.IsNotNull(channel.Icon);
            Assert.AreEqual("Logo_UK_RT_105", channel.Icon.Source);
            Assert.IsFalse(channel.Icon.Width.HasValue);
            Assert.IsFalse(channel.Icon.Height.HasValue);

            channel = channels.SingleOrDefault(c => c.Id == "UK_RT_2118");
            Assert.IsNotNull(channel);
            Assert.AreEqual("ITV1 HD", channel.DisplayName);
            Assert.IsNotNull(channel.Icon);
            Assert.AreEqual("Logo_UK_RT_2118", channel.Icon.Source);
            Assert.AreEqual(100, channel.Icon.Width);
            Assert.IsFalse(channel.Icon.Height.HasValue);

            channel = channels.SingleOrDefault(c => c.Id == "UK_RT_2056");
            Assert.IsNotNull(channel);
            Assert.AreEqual("Channel 4 HD", channel.DisplayName);
            Assert.IsNotNull(channel.Icon);
            Assert.AreEqual("Logo_UK_RT_2056", channel.Icon.Source);
            Assert.IsFalse(channel.Icon.Width.HasValue);
            Assert.AreEqual(200, channel.Icon.Height);

            channel = channels.SingleOrDefault(c => c.Id == "UK_RT_134");
            Assert.IsNotNull(channel);
            Assert.AreEqual("Channel 5", channel.DisplayName);
            Assert.IsNull(channel.Icon);
        }

        [TestMethod]
        [DeploymentItem("Xml Files\\UK_Data.xml")]
        public void UK_DataTest_GeneralTest()
        {
            var testFile = Path.GetFullPath(@"UK_Data.xml");
            var reader = new XmlTvReader(testFile, null);

            var channels = reader.GetChannels().ToList();
            Assert.AreEqual(5, channels.Count);

            // Pick a channel to check the data for
            var channel = channels.SingleOrDefault(c => c.Id == "UK_RT_2056");
            Assert.IsNotNull(channel);

            var startDate = new DateTime(2015, 11, 26);
            var cancellationToken = new CancellationToken();
            var programmes = reader.GetProgrammes(channel.Id, startDate, startDate.AddDays(1), cancellationToken).ToList();

            Assert.AreEqual(27, programmes.Count);
            var programme = programmes.SingleOrDefault(p => p.Title == "The Secret Life of");

            Assert.IsNotNull(programme);
            Assert.AreEqual(new DateTime(2015, 11, 26, 20, 0, 0), programme.StartDate);
            Assert.AreEqual(new DateTime(2015, 11, 26, 21, 0, 0), programme.EndDate);
            Assert.AreEqual("Cameras follow the youngsters' development after two weeks apart and time has made the heart grow fonder for Alfie and Emily, who are clearly happy to be back together. And although Alfie struggled to empathise with the rest of his peers before, a painting competition proves to be a turning point for him. George takes the children's rejection of his family recipe to heart, but goes on to triumph elsewhere, and romance is in the air when newcomer Sienna captures Arthur's heart.", programme.Description);
            Assert.AreEqual("Documentary", programme.Categories.Single());
            Assert.IsNotNull(programme.Episode);
            Assert.AreEqual("The Secret Life of 5 Year Olds", programme.Episode.Title);
            Assert.AreEqual(1, programme.Episode.Series);
            Assert.IsNull(programme.Episode.SeriesCount);
            Assert.AreEqual(4, programme.Episode.Episode);
            Assert.AreEqual(6, programme.Episode.EpisodeCount);
            Assert.IsNotNull(programme.Premiere);
            //Assert.AreEqual("First showing on national terrestrial TV", programme.Premiere.Details);
            Assert.IsTrue(programme.IsNew);
        }

        [TestMethod]
        [DeploymentItem("Xml Files\\UK_Data.xml")]
        public void UK_DataTest_MultipleTitles_SameLanguage_Should_ReturnFirstValue()
        {
            var testFile = Path.GetFullPath(@"UK_Data.xml");
            var reader = new XmlTvReader(testFile, null);

            /*
                <title lang="en">Homes Under the Hammer - Title 1</title>
                <title lang="en">Homes Under the Hammer - Title 2</title>
                <title lang="en">Homes Under the Hammer - Title 3</title>
            */

            var startDate = new DateTime(2015, 11, 26);
            var cancellationToken = new CancellationToken();
            var programmes = reader.GetProgrammes("UK_RT_2667", startDate, startDate.AddDays(1), cancellationToken).ToList();
            var programme = programmes.SingleOrDefault(p => p.Title == "Homes Under the Hammer - Title 1");

            Assert.IsNotNull(programme);
        }

        [TestMethod]
        [DeploymentItem("Xml Files\\UK_Data.xml")]
        public void UK_DataTest_MultipleTitles_NoLanguage_Should_ReturnFirstValue()
        {
            var testFile = Path.GetFullPath(@"UK_Data.xml");
            var reader = new XmlTvReader(testFile, null);

            /*
                <title>Oxford Street Revealed - Title 1</title>
                <title>Oxford Street Revealed - Title 2</title>
                <title>Oxford Street Revealed - Title 3</title>
            */

            var startDate = new DateTime(2015, 11, 26);
            var cancellationToken = new CancellationToken();
            var programmes = reader.GetProgrammes("UK_RT_2667", startDate, startDate.AddDays(1), cancellationToken).ToList();
            var programme = programmes.SingleOrDefault(p => p.Title == "Oxford Street Revealed - Title 1");

            Assert.IsNotNull(programme);
        }

        [TestMethod]
        [DeploymentItem("Xml Files\\ES_MultiLanguageData.xml")]
        public void ES_MultiLanguageDataTest()
        {
            var testFile = Path.GetFullPath(@"ES_MultiLanguageData.xml");
            var reader = new XmlTvReader(testFile, "es"); // Specify the spanish language explicitly

            var channels = reader.GetChannels().ToList();
            Assert.AreEqual(141, channels.Count);

            // Pick a channel to check the data for
            var channel = channels.SingleOrDefault(c => c.Id == "Canal + HD" && c.DisplayName == "Canal + HD");
            Assert.IsNotNull(channel);

            var startDate = new DateTime(2016, 02, 18);
            var cancellationToken = new CancellationToken();
            var programmes = reader.GetProgrammes(channel.Id, startDate, startDate.AddDays(1), cancellationToken).ToList();

            Assert.AreEqual(22, programmes.Count);
            var programme = programmes.SingleOrDefault(p => p.Title == "This is Comedy. Judd Apatow & Co.");

            /*
            <programme start="20160218055100 +0100" stop="20160218065400 +0100" channel="Canal + HD">
                <title lang="es">This is Comedy. Judd Apatow &amp; Co.</title>
                <title lang="en">This is Comedy</title>
                <desc lang="es">El resurgir creativo de la comedia estadounidense en los últimos 15 años ha tenido un nombre indiscutible, Judd Apatow, y unos colaboradores indispensables, sus amigos (actores, cómicos, escritores) Jonah Hill, Steve Carrell, Paul Rudd, Seth Rogen, Lena Dunham... A través de extractos de sus filmes y de entrevistas a algunos los miembros de su 'banda' (Adam Sandler, Lena Dunham o Jason Segel), este documental muestra la carrera de un productor y director excepcional que ha sido capaz de llevar la risa a su máxima expresión</desc>
                <credits>
                  <director>Jacky Goldberg</director>
                </credits>
                <date>2014</date>
                <category lang="es">Documentales</category>
                <category lang="es">Sociedad</category>
                <icon src="http://www.plus.es/recorte/n/caratula4/F3027798" />
                <country>Francia</country>
                <rating system="MPAA">
                  <value>TV-G</value>
                </rating>
                <star-rating>
                  <value>3/5</value>
                </star-rating>
            </programme>
            */

            Assert.IsNotNull(programme);
            Assert.AreEqual(new DateTime(2016, 02, 18, 4, 51, 0), programme.StartDate);
            Assert.AreEqual(new DateTime(2016, 02, 18, 5, 54, 0), programme.EndDate);
            Assert.AreEqual("El resurgir creativo de la comedia estadounidense en los últimos 15 años ha tenido un nombre indiscutible, Judd Apatow, y unos colaboradores indispensables, sus amigos (actores, cómicos, escritores) Jonah Hill, Steve Carrell, Paul Rudd, Seth Rogen, Lena Dunham... A través de extractos de sus filmes y de entrevistas a algunos los miembros de su 'banda' (Adam Sandler, Lena Dunham o Jason Segel), este documental muestra la carrera de un productor y director excepcional que ha sido capaz de llevar la risa a su máxima expresión", programme.Description);
            Assert.AreEqual(2, programme.Categories.Count);
            Assert.AreEqual("Documentales", programme.Categories[0]);
            Assert.AreEqual("Sociedad", programme.Categories[1]);
            Assert.IsNotNull(programme.Episode);
            Assert.IsNull(programme.Episode.Episode);
            Assert.IsNull(programme.Episode.EpisodeCount);
            Assert.IsNull(programme.Episode.Part);
            Assert.IsNull(programme.Episode.PartCount);
            Assert.IsNull(programme.Episode.Series);
            Assert.IsNull(programme.Episode.SeriesCount);
            Assert.IsNull(programme.Episode.Title);
        }

        [TestMethod]
        [DeploymentItem("Xml Files\\honeybee.xml")]
        public void HoneybeeTest()
        {
            var testFile = Path.GetFullPath(@"honeybee.xml");
            var reader = new XmlTvReader(testFile, null);

            var channels = reader.GetChannels().ToList();
            Assert.AreEqual(16, channels.Count);

            var programs = reader.GetProgrammes("2013.honeybee.it", DateTime.UtcNow.AddYears(-1),
                DateTime.UtcNow.AddYears(1), CancellationToken.None).ToList();
            Assert.AreEqual(297, programs.Count);
        }
    }
}
