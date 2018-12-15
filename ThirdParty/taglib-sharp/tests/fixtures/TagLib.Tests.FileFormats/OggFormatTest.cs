using System;
using NUnit.Framework;
using TagLib;

namespace TagLib.Tests.FileFormats
{   
	[TestFixture]
	public class OggFormatTest : IFormatTest
	{
		private static string sample_file = TestPath.Samples + "sample.ogg";
		private static string tmp_file = TestPath.Samples + "tmpwrite.ogg";
		private File file;
		
		[OneTimeSetUp]
		public void Init()
		{
			file = File.Create(sample_file);
		}
	
		[Test]
		public void ReadAudioProperties()
		{
			StandardTests.ReadAudioProperties (file);
		}
		
		[Test]
		public void ReadTags()
		{
			Assert.AreEqual("OGG album", file.Tag.Album);
			Assert.AreEqual("OGG artist", file.Tag.FirstPerformer);
			Assert.AreEqual("OGG comment", file.Tag.Comment);
			Assert.AreEqual("Acid Punk", file.Tag.FirstGenre);
			Assert.AreEqual("OGG title", file.Tag.Title);
			Assert.AreEqual(6, file.Tag.Track);
			Assert.AreEqual(7, file.Tag.TrackCount);
			Assert.AreEqual(1234, file.Tag.Year);
		}
		
		[Test]
		public void WriteStandardTags ()
		{
			StandardTests.WriteStandardTags (sample_file, tmp_file, StandardTests.TestTagLevel.Medium);
		}

		[Test]
		public void WriteExtendedTags()
		{
			ExtendedTests.WriteExtendedTags(sample_file, tmp_file);
		}

		[Test]
		public void WriteStandardPictures()
		{
			StandardTests.WriteStandardPictures(sample_file, tmp_file, ReadStyle.None);
		}

		[Test]
		[Ignore("PictureLazy not supported yet")]
		public void WriteStandardPicturesLazy()
		{
			StandardTests.WriteStandardPictures(sample_file, tmp_file, ReadStyle.PictureLazy);
		}


		[Test]
		public void TestCorruptionResistance()
		{
			StandardTests.TestCorruptionResistance (TestPath.Samples + "corrupt/a.ogg");
		}
	}
}
