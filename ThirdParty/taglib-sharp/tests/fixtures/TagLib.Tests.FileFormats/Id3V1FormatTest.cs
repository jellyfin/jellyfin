using System;
using NUnit.Framework;
using TagLib;

namespace TagLib.Tests.FileFormats
{   
	[TestFixture]
	public class Id3V1FormatTest : IFormatTest
	{
		private static string sample_file = TestPath.Samples + "sample_v1_only.mp3";
		private static string tmp_file = TestPath.Samples + "tmpwrite_v1_only.mp3";
		private File file;
		
		[OneTimeSetUp]
		public void Init()
		{
			file = File.Create(sample_file);
		}
	
		[Test]
		public void ReadAudioProperties()
		{
			Assert.AreEqual(44100, file.Properties.AudioSampleRate);
			Assert.AreEqual(1, file.Properties.Duration.Seconds);
		}
		
		[Test]
		public void ReadTags()
		{
			Assert.AreEqual("MP3 album", file.Tag.Album);
			Assert.AreEqual("MP3 artist", file.Tag.FirstPerformer);
			Assert.AreEqual("MP3 comment", file.Tag.Comment);
			Assert.AreEqual("Acid Punk", file.Tag.FirstGenre);
			Assert.AreEqual("MP3 title", file.Tag.Title);
			Assert.AreEqual(6, file.Tag.Track);
			Assert.AreEqual(1234, file.Tag.Year);
		}
		
		[Test]
		public void WriteStandardTags ()
		{
			StandardTests.WriteStandardTags (sample_file, tmp_file);
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
		public void TestCorruptionResistance()
		{
		}
	}
}
