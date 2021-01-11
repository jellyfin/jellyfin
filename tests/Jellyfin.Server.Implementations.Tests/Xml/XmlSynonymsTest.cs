#pragma warning disable SA1402
#pragma warning disable CA5369

using System;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using Emby.Server.Implementations.Serialization;
using MediaBrowser.Controller.Serialization;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Xml
{
    public class XmlSynonymsTest
    {
        private readonly MyXmlSerializer _serializer;

        public XmlSynonymsTest()
        {
            _serializer = new MyXmlSerializer();
        }

        [Fact]
        public void Parse_Element_Success()
        {
            var result = _serializer.DeserializeFromFile(typeof(TestClass), "Xml/Test Data/Without Synonym.xml") as TestClass;
            Assert.NotNull(result);
            Assert.Equal("Jellyfin", result?.Name);
        }

        [Fact]
        public void Parse_Synonym_Success()
        {
            var result = _serializer.DeserializeFromFile(typeof(TestClass), "Xml/Test Data/Without Synonym.xml") as TestClass;
            Assert.NotNull(result);
            Assert.Equal("Jellyfin", result?.Name);
        }

        [Fact]
        public void Parse_ElementAndSynonym_Success()
        {
            var result = _serializer.DeserializeFromFile(typeof(TestClass), "Xml/Test Data/With Synonym.xml") as TestClass;
            Assert.NotNull(result);
            Assert.Equal("Jellyfin", result?.Name);
        }

        [Fact]
        public void Write_Object_Success()
        {
            using var resultStream = new MemoryStream();
            var testClass = new TestClass { Name = "Jellyfin" };
            _serializer.SerializeToStream(testClass, resultStream);

            var xml = Encoding.UTF8.GetString(resultStream.ToArray());
            Assert.Contains("<name>Jellyfin</name>", xml, StringComparison.OrdinalIgnoreCase);
        }
    }

    [XmlRoot("test")]
    public class TestClass
    {
        [XmlElement("name")]
        [XmlSynonyms("title", "localname")]
        public string? Name { get; set; }
    }
}
