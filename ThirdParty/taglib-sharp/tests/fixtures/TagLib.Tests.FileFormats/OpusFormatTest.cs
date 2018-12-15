using System;
using NUnit.Framework;
using TagLib;

namespace TagLib.Tests.FileFormats
{
	[TestFixture]
	public class OpusFormatTest : IFormatTest
	{
		private static string sample_file = TestPath.Samples + "sample.opus";
		private static string tmp_file = TestPath.Samples + "tmpwrite.opus";
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
			Assert.AreEqual("Opus album", file.Tag.Album);
			Assert.AreEqual("Opus artist", file.Tag.FirstPerformer);
			Assert.AreEqual("Opus comment", file.Tag.Description);
			Assert.AreEqual("Acid Punk", file.Tag.FirstGenre);
			Assert.AreEqual("Opus title", file.Tag.Title);
			Assert.AreEqual(6, file.Tag.Track);
			Assert.AreEqual(7, file.Tag.TrackCount);
			Assert.AreEqual(1234, file.Tag.Year);
		}

		[Test]
		public void WriteStandardTags ()
		{
			StandardTests.WriteStandardTags (sample_file, tmp_file);
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
			StandardTests.TestCorruptionResistance (TestPath.Samples + "corrupt/a.opus");
		}
	}
}
