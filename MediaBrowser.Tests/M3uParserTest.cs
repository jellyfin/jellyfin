using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emby.Common.Implementations.Cryptography;
using Emby.Server.Implementations.LiveTv.TunerHosts;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Model.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MediaBrowser.Tests
{
    [TestClass]
    public class M3uParserTest
    {
        [TestMethod]
        public void TestFormat1()
        {
            BaseExtensions.CryptographyProvider = new CryptographyProvider();

            var result = new M3uParser(new NullLogger(), null, null, null).ParseString("#EXTINF:0,84. VOX Schweiz\nhttp://mystream", "-", "-");
            Assert.AreEqual(1, result.Count);

            Assert.AreEqual("VOX Schweiz", result[0].Name);
            Assert.AreEqual("84", result[0].Number);
        }
        [TestMethod]
        public void TestFormat2()
        {
            BaseExtensions.CryptographyProvider = new CryptographyProvider();

            var input = "#EXTINF:-1 tvg-id=\"\" tvg-name=\"ABC News 04\" tvg-logo=\"\" group-title=\"ABC Group\",ABC News 04";
            input += "\n";
            input += "http://mystream";

            var result = new M3uParser(new NullLogger(), null, null, null).ParseString(input, "-", "-");
            Assert.AreEqual(1, result.Count);

            Assert.AreEqual("ABC News 04", result[0].Name);
            Assert.IsNull(result[0].Number);
        }

        [TestMethod]
        public void TestFormat3()
        {
            BaseExtensions.CryptographyProvider = new CryptographyProvider();

            var result = new M3uParser(new NullLogger(), null, null, null).ParseString("#EXTINF:0, 3.2 - Movies!\nhttp://mystream", "-", "-");
            Assert.AreEqual(1, result.Count);

            Assert.AreEqual("Movies!", result[0].Name);
            Assert.AreEqual("3.2", result[0].Number);
        }

        [TestMethod]
        public void TestFormat4()
        {
            BaseExtensions.CryptographyProvider = new CryptographyProvider();

            var result = new M3uParser(new NullLogger(), null, null, null).ParseString("#EXTINF:0 tvg-id=\"abckabclosangeles.path.to\" tvg-logo=\"path.to / channel_logos / abckabclosangeles.png\", ABC KABC Los Angeles\nhttp://mystream", "-", "-");
            Assert.AreEqual(1, result.Count);

            Assert.IsNull(result[0].Number);
            Assert.AreEqual("ABC KABC Los Angeles", result[0].Name);
        }

        [TestMethod]
        public void TestFormat5()
        {
            BaseExtensions.CryptographyProvider = new CryptographyProvider();

            var result = new M3uParser(new NullLogger(), null, null, null).ParseString("#EXTINF:-1 channel-id=\"2101\" tvg-id=\"I69387.json.schedulesdirect.org\" group-title=\"Entertainment\",BBC 1 HD\nhttp://mystream", "-", "-");
            Assert.AreEqual(1, result.Count);

            Assert.AreEqual("BBC 1 HD", result[0].Name);
            Assert.AreEqual("2101", result[0].Number);
        }

        [TestMethod]
        public void TestFormat6()
        {
            BaseExtensions.CryptographyProvider = new CryptographyProvider();

            var result = new M3uParser(new NullLogger(), null, null, null).ParseString("#EXTINF:-1 tvg-id=\"2101\" group-title=\"Entertainment\",BBC 1 HD\nhttp://mystream", "-", "-");
            Assert.AreEqual(1, result.Count);

            Assert.AreEqual("BBC 1 HD", result[0].Name);
            Assert.AreEqual("2101", result[0].Number);
        }
    }
}
