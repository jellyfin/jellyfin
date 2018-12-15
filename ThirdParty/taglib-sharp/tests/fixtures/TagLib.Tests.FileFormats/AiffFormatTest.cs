using System;
using NUnit.Framework;
using TagLib;

namespace TagLib.Tests.FileFormats
{
	[TestFixture]
	public class AiffFormatTest : IFormatTest
	{
		private static string sample_file = TestPath.Samples + "sample.aif";
		private static string corrupt_file = TestPath.Samples + "corrupta.aif";
		private static string tmp_file = TestPath.Samples + "tmpwrite.aif";
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
			Assert.AreEqual(2, file.Properties.Duration.Seconds);
		}

		[Test]
		public void ReadTags()
		{
			Assert.AreEqual("Aiff Album", file.Tag.Album);
			Assert.AreEqual("Aiff Artist", file.Tag.FirstPerformer);
			Assert.AreEqual("Aiff Comment", file.Tag.Comment);
			Assert.AreEqual("Blues", file.Tag.FirstGenre);
			Assert.AreEqual("Aiff Title", file.Tag.Title);
			Assert.AreEqual(5, file.Tag.Track);
			Assert.AreEqual(10, file.Tag.TrackCount);

			// sample.aif contains a TDAT (and no TYER) with 2009 in it, but TDAT
			// is supposed to contain MMDD - so the following should not be equal
			Assert.AreNotEqual(2009, file.Tag.Year);
		}

		[Test]
		public void WriteStandardTags()
		{
			StandardTests.WriteStandardTags(sample_file, tmp_file);
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
		public void WriteStandardPicturesLazy()
		{
			StandardTests.WriteStandardPictures(sample_file, tmp_file, ReadStyle.PictureLazy);
		}

		[Test]
		public void TestCorruptionResistance()
		{
			StandardTests.TestCorruptionResistance(corrupt_file);
		}
	}
}
