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
    }
}
