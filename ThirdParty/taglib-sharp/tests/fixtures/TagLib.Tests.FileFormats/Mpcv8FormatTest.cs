using System;
using NUnit.Framework;
using TagLib;
using static TagLib.Tests.FileFormats.StandardTests;

namespace TagLib.Tests.FileFormats
{   
	[TestFixture]
	public class MpcV8FormatTest : IFormatTest
	{
		private static string sample_file = TestPath.Samples + "sample_v8.mpc";
		private static string tmp_file = TestPath.Samples + "tmpwrite_v8.mpc";
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
			Assert.AreEqual("Mpc Album", file.Tag.Album);
			Assert.AreEqual("Mpc Artist", file.Tag.FirstPerformer);
			Assert.AreEqual("Mpc Comment", file.Tag.Comment);
			Assert.AreEqual("Pop", file.Tag.FirstGenre);
			Assert.AreEqual("Mpc Title", file.Tag.Title);
			Assert.AreEqual(1, file.Tag.Track);
			Assert.AreEqual(10, file.Tag.TrackCount);
			Assert.AreEqual(2016, file.Tag.Year);
		}
		
		[Test]
		public void WriteStandardTags ()
		{
			StandardTests.WriteStandardTags (sample_file, tmp_file);
		}


		[Test]
		public void WriteStandardPictures()
		{
			StandardTests.WriteStandardPictures(sample_file, tmp_file, ReadStyle.None, TestTagLevel.Normal);
		}

		[Test]
		[Ignore("PictureLazy not supported yet")]
		public void WriteStandardPicturesLazy()
		{
			StandardTests.WriteStandardPictures(sample_file, tmp_file, ReadStyle.PictureLazy, TestTagLevel.Normal);
		}


		[Test]
		public void TestCorruptionResistance()
		{
			StandardTests.TestCorruptionResistance (TestPath.Samples + "corrupt/a.mpc");
		}
	}
}
