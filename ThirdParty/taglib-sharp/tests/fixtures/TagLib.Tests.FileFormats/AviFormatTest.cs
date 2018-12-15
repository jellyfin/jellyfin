using System;
using NUnit.Framework;
using TagLib;

namespace TagLib.Tests.FileFormats
{   
	[TestFixture]
	public class AviFormatTest : IFormatTest
	{
		private static string sample_file = TestPath.Samples + "sample.avi";
		private static string tmp_file = TestPath.Samples + "tmpwrite.avi";
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
			Assert.AreEqual("Avi album", file.Tag.Album);
			Assert.AreEqual("Dan Drake", file.Tag.FirstAlbumArtist);
			Assert.AreEqual("AVI artist", file.Tag.FirstPerformer);
			Assert.AreEqual("AVI comment", file.Tag.Comment);
			Assert.AreEqual("Brit Pop", file.Tag.FirstGenre);
			Assert.AreEqual("AVI title", file.Tag.Title);
			Assert.AreEqual(5, file.Tag.Track);
			Assert.AreEqual(2005, file.Tag.Year);
		}
		
		[Test]
		public void WriteStandardTags ()
		{
			StandardTests.WriteStandardTags (sample_file, tmp_file, StandardTests.TestTagLevel.Medium);
		}


		[Test]
		public void WriteStandardPictures()
		{
			StandardTests.WriteStandardPictures(sample_file, tmp_file, ReadStyle.None);
		}

		[Test]
		public void WriteStandardPicturesLazy()
		{
			StandardTests.WriteStandardPictures(sample_file, tmp_file, ReadStyle.PictureLazy);
		}


		[Test]
		public void WriteStandardTagsID3v2()
		{
			StandardTests.WriteStandardTags(sample_file, tmp_file, StandardTests.TestTagLevel.Medium, TagTypes.Id3v2);
		}

		[Test]
		public void TestCorruptionResistance()
		{
			StandardTests.TestCorruptionResistance (TestPath.Samples + "corrupt/a.avi");
		}
	}
}
