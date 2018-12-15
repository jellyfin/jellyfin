using System;
using NUnit.Framework;

namespace TagLib.Tests.FileFormats
{   
	[TestFixture]
	public class Id3BothFormatTest : IFormatTest
	{
		private static string sample_file = TestPath.Samples + "sample_both.mp3";
		private static string tmp_file = TestPath.Samples + "tmpwrite_both.mp3";
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
			Assert.AreEqual("MP3 album v2", file.Tag.Album);
			Assert.AreEqual("MP3 artist", file.Tag.FirstPerformer);
			Assert.AreEqual("MP3 comment v2", file.Tag.Comment);
			Assert.AreEqual("Acid Punk", file.Tag.FirstGenre);
			Assert.AreEqual("MP3 title v2", file.Tag.Title);
			Assert.AreEqual(6, file.Tag.Track);
			Assert.AreEqual(7, file.Tag.TrackCount);
			Assert.AreEqual(1234, file.Tag.Year);
		}
		
		[Test]
		public void FirstTag()
		{
			Assert.AreEqual("MP3 title v2", file.GetTag (TagTypes.Id3v2).Title);
			Assert.AreEqual("MP3 album v2", file.GetTag (TagTypes.Id3v2).Album);
			Assert.AreEqual("MP3 comment v2", file.GetTag (TagTypes.Id3v2).Comment);
			Assert.AreEqual(1234, (int)file.GetTag (TagTypes.Id3v2).Year);
			Assert.AreEqual(6, (int)file.GetTag (TagTypes.Id3v2).Track);
			Assert.AreEqual(7, (int)file.GetTag (TagTypes.Id3v2).TrackCount);
		}

		[Test]
		public void SecondTag()
		{
			Assert.AreEqual("MP3 title", file.GetTag (TagTypes.Id3v1).Title);
			Assert.AreEqual("MP3 album", file.GetTag (TagTypes.Id3v1).Album);
			Assert.AreEqual("MP3 comment", file.GetTag (TagTypes.Id3v1).Comment);
			Assert.AreEqual("MP3 artist", file.GetTag (TagTypes.Id3v1).FirstPerformer);
			Assert.AreEqual(1235, (int)file.GetTag (TagTypes.Id3v1).Year);
			Assert.AreEqual(6, (int)file.GetTag (TagTypes.Id3v1).Track);
			Assert.AreEqual(0, (int)file.GetTag (TagTypes.Id3v1).TrackCount);
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
			StandardTests.TestCorruptionResistance (TestPath.Samples + "corrupt/a.mp3");
		}

		[Test]
		public void TestRemoveTags ()
		{
			string file_name = TestPath.Samples + "remove_tags.mp3";
			ByteVector.UseBrokenLatin1Behavior = true;
			var file = File.Create (file_name);
			Assert.AreEqual (TagTypes.Id3v1 | TagTypes.Id3v2 | TagTypes.Ape, file.TagTypesOnDisk);

			file.RemoveTags (TagTypes.Id3v1);
			Assert.AreEqual (TagTypes.Id3v2 | TagTypes.Ape, file.TagTypes);

			file = File.Create (file_name);
			file.RemoveTags(TagTypes.Id3v2);
			Assert.AreEqual (TagTypes.Id3v1 | TagTypes.Ape, file.TagTypes);

			file = File.Create (file_name);
			file.RemoveTags(TagTypes.Ape);
			Assert.AreEqual (TagTypes.Id3v1 | TagTypes.Id3v2, file.TagTypes);

			file = File.Create (file_name);
			file.RemoveTags (TagTypes.Xiph);
			Assert.AreEqual (TagTypes.Id3v1 | TagTypes.Id3v2 | TagTypes.Ape, file.TagTypes);

			file = File.Create (file_name);
			file.RemoveTags (TagTypes.AllTags);
			Assert.AreEqual (TagTypes.None, file.TagTypes);
		}
	}
}
