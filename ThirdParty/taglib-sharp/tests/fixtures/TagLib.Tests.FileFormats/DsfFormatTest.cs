using System;
using NUnit.Framework;
using TagLib;

namespace TagLib.Tests.FileFormats
{
	[TestFixture]
	public class DsfFormatTest : IFormatTest
	{
		private static string sample_file = TestPath.Samples + "sample.dsf";
		private static string tmp_file = TestPath.Samples + "tmpwrite.dsf";
		private File file;

		[OneTimeSetUp]
		public void Init()
		{
			file = File.Create(sample_file);
		}

		[Test]
		public void ReadAudioProperties()
		{
			Assert.AreEqual(2822400, file.Properties.AudioSampleRate);
		}

		[Test]
		public void ReadTags()
		{
			Assert.AreEqual("Dsf Album", file.Tag.Album);
			Assert.AreEqual("Dsf Artist", file.Tag.FirstPerformer);
			Assert.AreEqual("Dsf Comment", file.Tag.Comment);
			Assert.AreEqual("Rock", file.Tag.FirstGenre);
			Assert.AreEqual("Dsf Title", file.Tag.Title);
			Assert.AreEqual(1, file.Tag.Track);
			Assert.AreEqual(2016, file.Tag.Year);
		}

		[Test]
		public void WriteStandardTags()
		{
			StandardTests.WriteStandardTags(sample_file, tmp_file);
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
			StandardTests.TestCorruptionResistance(TestPath.Samples + "corrupt/a.dsf");
		}
	}
}
