using System;
using System.IO;
using Emby.XmlTv.Classes;
using NUnit.Framework;

namespace Jellyfin.XmlTv.Test
{
    [TestFixture]
    public class XmlTvReaderDateTimeTests
    {
        [Test]
        public void ShouldHandlePartDates()
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
        
        [Test]
        public void ShouldHandleDateWithOffset()
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
        
        [Test]
        public void ShouldHandlePartDatesWithOffset()
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
        
        [Test]
        public void ShouldHandleSpaces()
        {
            var testFile = Path.GetFullPath(@"MultilanguageData.xml");
            var reader = new XmlTvReader(testFile, "es");

            // parse variations on 1:00AM
            Assert.AreEqual(Parse("01 Jan 2016 12:00:00"), reader.ParseDate("20160101120000 +000"));
            Assert.AreEqual(Parse("01 Jan 2016 12:00:00"), reader.ParseDate("20160101120000 +00"));
            Assert.AreEqual(Parse("01 Jan 2016 12:00:00"), reader.ParseDate("20160101120000 +0"));
        }
        
        [Test]
        public void ShouldHandleSpaces2()
        {
            var testFile = Path.GetFullPath(@"MultilanguageData.xml");
            var reader = new XmlTvReader(testFile, "es");

            // parse variations on 1:00AM
            Assert.AreEqual(Parse("01 Jan 2016 12:00:00"), reader.ParseDate("20160101120000 0"));
        }
        
        private static DateTimeOffset Parse(string value)
        {
            return new DateTimeOffset(DateTimeOffset.Parse(value).Ticks, TimeSpan.Zero);
        }
    }
}