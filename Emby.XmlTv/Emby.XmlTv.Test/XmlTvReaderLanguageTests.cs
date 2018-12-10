using System;
using System.IO;
using System.Linq;
using System.Threading;

using Emby.XmlTv.Classes;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Emby.XmlTv.Test
{
    [TestClass]
    public class XmlTvReaderLanguageTests
    {
        /*
            <title lang="es">Homes Under the Hammer - Spanish</title>
		    <title lang="es">Homes Under the Hammer - Spanish 2</title>
		    <title lang="en">Homes Under the Hammer - English</title>
		    <title lang="en">Homes Under the Hammer - English 2</title>
		    <title lang="">Homes Under the Hammer - Empty Language</title>
		    <title lang="">Homes Under the Hammer - Empty Language 2</title>
		    <title>Homes Under the Hammer - No Language</title>
		    <title>Homes Under the Hammer - No Language 2</title>
            */

        /*  Expected Behaviour:
            - Language = Null   Homes Under the Hammer - No Language
            - Language = ""   Homes Under the Hammer - No Language
            - Language = es     Homes Under the Hammer - Spanish
            - Language = en     Homes Under the Hammer - English
        */

        [TestMethod]
        [DeploymentItem("Xml Files\\MultilanguageData.xml")]
        public void Should_Return_The_First_Matching_Language_ES()
        {
            var testFile = Path.GetFullPath(@"MultilanguageData.xml");
            var reader = new XmlTvReader(testFile, "es");
            var channel = reader.GetChannels().FirstOrDefault();
            Assert.IsNotNull(channel);

            var startDate = new DateTime(2015, 11, 26);
            var cancellationToken = new CancellationToken();
            var programme = reader.GetProgrammes(channel.Id, startDate, startDate.AddDays(1), cancellationToken).FirstOrDefault();

            Assert.IsNotNull(programme);
            Assert.AreEqual("Homes Under the Hammer - Spanish", programme.Title);
            Assert.AreEqual(1, programme.Categories.Count);
            Assert.AreEqual("Property - Spanish", programme.Categories[0]);
        }

        [TestMethod]
        [DeploymentItem("Xml Files\\MultilanguageData.xml")]
        public void Should_Return_The_First_Matching_Language_EN()
        {
            var testFile = Path.GetFullPath(@"MultilanguageData.xml");
            var reader = new XmlTvReader(testFile, "en");

            var channel = reader.GetChannels().FirstOrDefault();
            Assert.IsNotNull(channel);

            var startDate = new DateTime(2015, 11, 26);
            var cancellationToken = new CancellationToken();
            var programme = reader.GetProgrammes(channel.Id, startDate, startDate.AddDays(1), cancellationToken).FirstOrDefault();

            Assert.IsNotNull(programme);
            Assert.AreEqual("Homes Under the Hammer - English", programme.Title);
            Assert.AreEqual(1, programme.Categories.Count);
            Assert.AreEqual("Property - English", programme.Categories[0]);
        }

        [TestMethod]
        [DeploymentItem("Xml Files\\MultilanguageData.xml")]
        public void Should_Return_The_First_Matching_With_No_Language()
        {
            var testFile = Path.GetFullPath(@"MultilanguageData.xml");
            var reader = new XmlTvReader(testFile, null);

            var channel = reader.GetChannels().FirstOrDefault();
            Assert.IsNotNull(channel);

            var startDate = new DateTime(2015, 11, 26);
            var cancellationToken = new CancellationToken();
            var programme = reader.GetProgrammes(channel.Id, startDate, startDate.AddDays(1), cancellationToken).FirstOrDefault();

            Assert.IsNotNull(programme);
            Assert.AreEqual("Homes Under the Hammer - No Language", programme.Title);
            Assert.AreEqual(1, programme.Categories.Count);
            Assert.AreEqual("Property - No Language", programme.Categories[0]);
        }

        [TestMethod]
        [DeploymentItem("Xml Files\\MultilanguageData.xml")]
        public void Should_Return_The_First_Matching_With_Empty_Language()
        {
            var testFile = Path.GetFullPath(@"MultilanguageData.xml");
            var reader = new XmlTvReader(testFile, String.Empty);

            var channel = reader.GetChannels().FirstOrDefault();
            Assert.IsNotNull(channel);

            var startDate = new DateTime(2015, 11, 26);
            var cancellationToken = new CancellationToken();
            var programme = reader.GetProgrammes(channel.Id, startDate, startDate.AddDays(1), cancellationToken).FirstOrDefault();

            Assert.IsNotNull(programme);
            Assert.AreEqual("Homes Under the Hammer - Empty Language", programme.Title);
            Assert.AreEqual(1, programme.Categories.Count);
            Assert.AreEqual("Property - Empty Language", programme.Categories[0]);
        }

        [TestMethod]
        [DeploymentItem("Xml Files\\MultilanguageData.xml")]
        public void Should_Return_The_First_When_NoMatchFound()
        {
            var testFile = Path.GetFullPath(@"MultilanguageData.xml");
            var reader = new XmlTvReader(testFile, "es"); // There are no titles or categories for spanish

            var channel = reader.GetChannels().FirstOrDefault();
            Assert.IsNotNull(channel);

            var startDate = new DateTime(2015, 11, 26);
            var cancellationToken = new CancellationToken();
            var programme = reader.GetProgrammes(channel.Id, startDate, startDate.AddDays(1), cancellationToken).Skip(1).FirstOrDefault();

            Assert.IsNotNull(programme);
            Assert.AreEqual("Homes Under the Hammer - English", programme.Title);

            // Should return all categories
            Assert.AreEqual(2, programme.Categories.Count);
            Assert.IsTrue(programme.Categories.Contains("Property - English"));
            Assert.IsTrue(programme.Categories.Contains("Property - Empty Language"));
        }

        [TestMethod]
        [DeploymentItem("Xml Files\\MultilanguageData.xml")]
        public void Should_Return_The_First_When_NoLanguage()
        {
            var testFile = Path.GetFullPath(@"MultilanguageData.xml");
            var reader = new XmlTvReader(testFile, null);

            var channel = reader.GetChannels().FirstOrDefault();
            Assert.IsNotNull(channel);

            var startDate = new DateTime(2015, 11, 26);
            var cancellationToken = new CancellationToken();
            var programme = reader.GetProgrammes(channel.Id, startDate, startDate.AddDays(1), cancellationToken).Skip(1).FirstOrDefault();

            Assert.IsNotNull(programme);
            Assert.AreEqual("Homes Under the Hammer - English", programme.Title); // Should return the first in the list

            // Should return all categories
            Assert.AreEqual(2, programme.Categories.Count);
            Assert.IsTrue(programme.Categories.Contains("Property - English"));
            Assert.IsTrue(programme.Categories.Contains("Property - Empty Language"));
        }

        [TestMethod]
        [DeploymentItem("Xml Files\\MultilanguageData.xml")]
        public void Should_Return_All_Languages()
        {
            var testFile = Path.GetFullPath(@"MultilanguageData.xml");
            var reader = new XmlTvReader(testFile);
            var cancellationToken = new CancellationToken();

            var results = reader.GetLanguages(cancellationToken);
            Assert.IsNotNull(results);

            foreach (var result in results)
            {
                Console.WriteLine("{0} - {1}", result.Name, result.Relevance);
            }

            Assert.AreEqual(2, results.Count);
            Assert.AreEqual("en", results[0].Name);
            Assert.AreEqual(11, results[0].Relevance);
            Assert.AreEqual("es", results[1].Name);
            Assert.AreEqual(3, results[1].Relevance);
        }

    }
}
