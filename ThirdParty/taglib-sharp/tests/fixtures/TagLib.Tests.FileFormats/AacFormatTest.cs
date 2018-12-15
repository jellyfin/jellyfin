using System;
using NUnit.Framework;
using TagLib;
using TagLib.Aac;

namespace TagLib.Tests.FileFormats
{
	[TestFixture]
	public class AacFormatTest : IFormatTest
	{
		private static string sample_file = TestPath.Samples + "sample.aac";
		private static string tmp_file = TestPath.Samples + "tmpwrite.aac";
		private File file;

		[OneTimeSetUp]
		public void Init()
		{
			file = File.Create(sample_file);
		}

		[Test]
		public void ReadAudioProperties()
		{
			StandardTests.ReadAudioProperties(file);
		}

		[Test]
		public void ReadTags()
		{
			Assert.AreEqual("AAC album", file.Tag.Album);
			Assert.AreEqual("AAC artist", file.Tag.FirstPerformer);
			Assert.AreEqual("AAC comment", file.Tag.Comment);
			Assert.AreEqual("Acid Punk", file.Tag.FirstGenre);
			Assert.AreEqual("AAC title", file.Tag.Title);
			Assert.AreEqual(6, file.Tag.Track);
			Assert.AreEqual(1234, file.Tag.Year);
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
			StandardTests.TestCorruptionResistance(TestPath.Samples + "corrupt/a.aac");
		}

		[Test]
		public void TestBitStream()
		{
			byte[] data = { 0xAA, 0xAD, 0xFE, 0xE9, 0xFF, 0xFF, 0xFF };
			BitStream stream = new BitStream(data);

			//  2   5    5      45
			// (10)(101)(010 1)(0101101)
			//  0xAA         0xAD       

			//  63         745
			// (111111)(10 11101001)
			//  0xFE       0xAA

			// 16777215
			// (11111111 11111111 11111111)
			//  0xFF     0xFF     0xFF

			Assert.AreEqual(2, stream.ReadInt32(2));
			Assert.AreEqual(5, stream.ReadInt32(3));
			Assert.AreEqual(5, stream.ReadInt32(4));
			Assert.AreEqual(45, stream.ReadInt32(7));
			Assert.AreEqual(63, stream.ReadInt32(6));
			Assert.AreEqual(745, stream.ReadInt32(10));
			Assert.AreEqual(16777215, stream.ReadInt32(24));
		}
	}
}
