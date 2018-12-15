using System;
using NUnit.Framework;

namespace TagLib.Tests.FileFormats
{   
	[TestFixture]
	public class WavFormatTest : IFormatTest
	{
		private static string sample_file = TestPath.Samples + "sample.wav";
		private static string sample_picture = TestPath.Samples + "sample_gimp.gif";
		private static string sample_other = TestPath.Samples + "apple_tags.m4a";
		private static string tmp_file = TestPath.Samples + "tmpwrite.wav";
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
			Assert.AreEqual(2000, file.Properties.Duration.TotalMilliseconds);
			Assert.AreEqual(16, file.Properties.BitsPerSample);
			Assert.AreEqual(706, file.Properties.AudioBitrate);
			Assert.AreEqual(1, file.Properties.AudioChannels);
		}


		[Test]
		public void ReadTags()
		{
			Assert.AreEqual("Artist", file.Tag.FirstPerformer);
			Assert.AreEqual("yepa", file.Tag.Comment);
			Assert.AreEqual("Genre", file.Tag.FirstGenre);
			Assert.AreEqual("Album", file.Tag.Album);
			Assert.AreEqual("Title", file.Tag.Title);
			Assert.AreEqual(2009, file.Tag.Year);
			Assert.IsNull(file.Tag.FirstComposer);
			Assert.IsNull(file.Tag.Conductor);
			Assert.IsNull(file.Tag.Copyright);
		}


		[Test]
		public void ReadPictures()
		{
			var pics = file.Tag.Pictures;
			Assert.AreEqual(PictureType.FrontCover, pics[0].Type);
			Assert.AreEqual("image/jpeg", pics[0].MimeType);
			Assert.AreEqual(10210, pics[0].Data.Count);
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
		public void WriteStandardTags ()
		{
			StandardTests.WriteStandardTags (sample_file, tmp_file, StandardTests.TestTagLevel.Medium);
		}

		[Test]
		public void RemoveStandardTags()
		{
			StandardTests.RemoveStandardTags(sample_file, tmp_file);
		}

		[Test]
		public void TestCorruptionResistance()
		{
			StandardTests.TestCorruptionResistance (TestPath.Samples + "corrupt/a.mkv");
		}
	}
}
