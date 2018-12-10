using System;
using System.IO;

using Emby.XmlTv.Classes;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Emby.XmlTv.Test
{
    [TestClass]
    public class XmlTvReaderDateTimeTests
    {
        [TestMethod]
        [DeploymentItem("Xml Files\\MultilanguageData.xml")]
        public void Should_HandlePartDates()
        {
            var testFile = Path.GetFullPath(@"MultilanguageData.xml");
            var reader = new XmlTvReader(testFile, "es");
            
            Assert.AreEqual(Parse("01 Jan 2016 00:00:00"), reader.ParseDate("2016"));
            Assert.AreEqual(Parse("01 Jan 2016 00:00:00"), reader.ParseDate("201601"));
            Assert.AreEqual(Parse("01 Jan 2016 00:00:00"), reader.ParseDate("20160101"));
            Assert.AreEqual(Parse("01 Jan 2016 12:00:00"), reader.ParseDate("2016010112"));
            Assert.AreEqual(Parse("01 Jan 2016 12:34:00"), reader.ParseDate("201601011234"));
            Assert.AreEqual(Parse("01 Jan 2016 12:34:56"), reader.ParseDate("20160101123456"));
        }
        [TestMethod]
        [DeploymentItem("Xml Files\\MultilanguageData.xml")]
        public void Should_HandleDateWithOffset()
        {
            var testFile = Path.GetFullPath(@"MultilanguageData.xml");
            var reader = new XmlTvReader(testFile, "es");

            // parse variations on 1:00AM
            Assert.AreEqual(Parse("01 Jan 2016 12:00:00"), reader.ParseDate("20160101120000 +0000"));
            Assert.AreEqual(Parse("01 Jan 2016 02:00:00"), reader.ParseDate("20160101120000 +1000"));
            Assert.AreEqual(Parse("01 Jan 2016 11:00:00"), reader.ParseDate("20160101120000 +0100"));
            Assert.AreEqual(Parse("01 Jan 2016 11:50:00"), reader.ParseDate("20160101120000 +0010"));
            Assert.AreEqual(Parse("01 Jan 2016 11:59:00"), reader.ParseDate("20160101120000 +0001"));

            Assert.AreEqual(Parse("01 Jan 2016 22:00:00"), reader.ParseDate("20160101120000 -1000"));
            Assert.AreEqual(Parse("01 Jan 2016 13:00:00"), reader.ParseDate("20160101120000 -0100"));
            Assert.AreEqual(Parse("01 Jan 2016 12:10:00"), reader.ParseDate("20160101120000 -0010"));
            Assert.AreEqual(Parse("01 Jan 2016 12:01:00"), reader.ParseDate("20160101120000 -0001"));
        }

        [TestMethod]
        [DeploymentItem("Xml Files\\MultilanguageData.xml")]
        public void Should_HandlePartDatesWithOffset()
        {
            var testFile = Path.GetFullPath(@"MultilanguageData.xml");
            var reader = new XmlTvReader(testFile, "es");

            Assert.AreEqual(Parse("01 Jan 2016 01:00:00"), reader.ParseDate("2016 -0100"));
            Assert.AreEqual(Parse("01 Jan 2016 01:00:00"), reader.ParseDate("201601 -0100"));
            Assert.AreEqual(Parse("01 Jan 2016 01:00:00"), reader.ParseDate("20160101 -0100"));
            Assert.AreEqual(Parse("01 Jan 2016 13:00:00"), reader.ParseDate("2016010112 -0100"));
            Assert.AreEqual(Parse("01 Jan 2016 13:00:00"), reader.ParseDate("201601011200 -0100"));
            Assert.AreEqual(Parse("01 Jan 2016 13:00:00"), reader.ParseDate("20160101120000 -0100"));
        }

        [TestMethod]
        [DeploymentItem("Xml Files\\MultilanguageData.xml")]
        public void Should_HandleSpaces()
        {
            var testFile = Path.GetFullPath(@"MultilanguageData.xml");
            var reader = new XmlTvReader(testFile, "es");

            // parse variations on 1:00AM
            Assert.AreEqual(Parse("01 Jan 2016 12:00:00"), reader.ParseDate("20160101120000 +000"));
            Assert.AreEqual(Parse("01 Jan 2016 12:00:00"), reader.ParseDate("20160101120000 +00"));
            Assert.AreEqual(Parse("01 Jan 2016 12:00:00"), reader.ParseDate("20160101120000 +0"));
        }

        [TestMethod]
        [DeploymentItem("Xml Files\\MultilanguageData.xml")]
        public void Should_HandleSpaces2()
        {
            var testFile = Path.GetFullPath(@"MultilanguageData.xml");
            var reader = new XmlTvReader(testFile, "es");

            // parse variations on 1:00AM
            Assert.AreEqual(Parse("01 Jan 2016 12:00:00"), reader.ParseDate("20160101120000 0"));
        }

        private DateTimeOffset Parse(string value)
        {
            return new DateTimeOffset(DateTimeOffset.Parse(value).Ticks, TimeSpan.Zero);
        }
    }
}
