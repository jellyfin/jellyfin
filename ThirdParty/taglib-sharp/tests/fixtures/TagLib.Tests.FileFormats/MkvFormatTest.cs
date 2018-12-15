using System;
using NUnit.Framework;
using static TagLib.Tests.FileFormats.StandardTests;

namespace TagLib.Tests.FileFormats
{   
	[TestFixture]
	public class MkvFormatTest : IFormatTest
	{
		private static string sample_file = TestPath.Samples + "Turning Lime.mkv";
		private static string sample_picture = TestPath.Samples + "sample_gimp.gif";
		private static string sample_other = TestPath.Samples + "apple_tags.m4a";
		private static string tmp_file = TestPath.Samples + "tmpwrite.mkv";
		private File file;
		

		[OneTimeSetUp]
		public void Init()
		{
			file = File.Create(sample_file);
		}
	

		[Test]
		public void ReadAudioProperties()
		{
			Assert.AreEqual(48000, file.Properties.AudioSampleRate);
			Assert.AreEqual(1120, file.Properties.Duration.TotalMilliseconds);
		}


		[Test]
		public void ReadTags()
		{
			Assert.AreEqual("Lime", file.Tag.FirstPerformer);
			Assert.AreEqual("no comments", file.Tag.Comment);
			Assert.AreEqual("Test", file.Tag.FirstGenre);
			Assert.AreEqual("Turning Lime", file.Tag.Title);
			Assert.AreEqual(2017, file.Tag.Year);
			Assert.AreEqual("Starwer", file.Tag.FirstComposer);
			Assert.AreEqual("Starwer", file.Tag.Conductor);
			Assert.AreEqual("Starwer 2017", file.Tag.Copyright);
		}


		[Test]
		public void ReadSpecificTags()
		{
			// Specific Matroska Tag test
			var mkvTag = (TagLib.Matroska.Tag)file.GetTag(TagTypes.Matroska);
			Assert.AreEqual("This is a test Video showing a lime moving on a table", mkvTag.SimpleTags["SUMMARY"][0]);

			var tracks = mkvTag.Tags.Tracks;
			Assert.AreEqual(MediaTypes.Video, tracks[0].MediaTypes);
			Assert.AreEqual(MediaTypes.Audio, tracks[1].MediaTypes);

			var videotag = mkvTag.Tags.Get(tracks[0]);
			Assert.IsNull(videotag);

			var audiotag = mkvTag.Tags.Get(tracks[1]);

			Assert.AreEqual("The Noise", audiotag.Title);
			Assert.AreEqual("Useless background noise", audiotag.SimpleTags["DESCRIPTION"][0]);
			Assert.AreEqual("und", audiotag.SimpleTags["DESCRIPTION"][0].TagLanguage);
			Assert.AreEqual(true, audiotag.SimpleTags["DESCRIPTION"][0].TagDefault);

			// Recursive read
			Assert.AreEqual("Starwer", audiotag.FirstComposer);
			Assert.AreEqual("Starwer 2017", audiotag.Copyright);

		}


		[Test]
		public void ReadPictures()
		{
			var pics = file.Tag.Pictures;
			Assert.AreEqual("cover.png", pics[0].Description);
			Assert.AreEqual(PictureType.FrontCover, pics[0].Type);
			Assert.AreEqual("image/png", pics[0].MimeType);
			Assert.AreEqual(17307, pics[0].Data.Count);
		}


		[Test]
		public void WritePictures()
		{
			if (System.IO.File.Exists(tmp_file))
				System.IO.File.Delete(tmp_file);
			File file = null;
			try
			{
				System.IO.File.Copy(sample_file, tmp_file);
				file = File.Create(tmp_file);
			}
			finally { }
			Assert.NotNull(file);

			var pics = file.Tag.Pictures;
			Assert.AreEqual(1, pics.Length);

			// Insert new picture
			Array.Resize(ref pics, 4);
			pics[0].Description = "TEST description 0";
			pics[1] = new Picture(sample_picture);
			pics[1].Type = PictureType.BackCover;
			pics[1].Description = "TEST description 1";
			pics[2] = new Picture(sample_other);
			pics[2].Description = "TEST description 2";
			pics[3] = new Picture(sample_picture);
			pics[3].Type = PictureType.Other;
			pics[3].Description = "TEST description 3";
			file.Tag.Pictures = pics;

			file.Save();

			// Read back the Matroska-specific tags 
			file = File.Create(tmp_file);
			Assert.NotNull(file);
			pics = file.Tag.Pictures;

			Assert.AreEqual(4, pics.Length);

			Assert.AreEqual("cover.png", pics[0].Filename);
			Assert.AreEqual("TEST description 0", pics[0].Description);
			Assert.AreEqual("image/png", pics[0].MimeType);
			Assert.AreEqual(PictureType.FrontCover, pics[0].Type);
			Assert.AreEqual(17307, pics[0].Data.Count);

			// Filename has been changed to keep the PictureType information
			Assert.AreEqual(PictureType.BackCover, pics[1].Type);
			Assert.AreEqual("BackCover.gif", pics[1].Filename);
			Assert.AreEqual("TEST description 1", pics[1].Description);
			Assert.AreEqual("image/gif", pics[1].MimeType);
			Assert.AreEqual(73, pics[1].Data.Count);

			Assert.AreEqual("apple_tags.m4a", pics[2].Filename);
			Assert.AreEqual("TEST description 2", pics[2].Description);
			Assert.AreEqual("audio/mp4", pics[2].MimeType);
			Assert.AreEqual(PictureType.NotAPicture, pics[2].Type);
			Assert.AreEqual(102400, pics[2].Data.Count);

			Assert.AreEqual(PictureType.Other, pics[3].Type);
			Assert.AreEqual("sample_gimp.gif", pics[3].Filename);
			Assert.AreEqual("TEST description 3", pics[3].Description);
			Assert.AreEqual("image/gif", pics[3].MimeType);
			Assert.AreEqual(73, pics[3].Data.Count);
		}


		[Test]
		public void WriteStandardTags ()
		{
			StandardTests.WriteStandardTags (sample_file, tmp_file, StandardTests.TestTagLevel.Medium);
		}
		

		[Test]
		public void WriteStandardPictures()
		{
			StandardTests.WriteStandardPictures(sample_file, tmp_file, ReadStyle.None, TestTagLevel.High);
		}

		[Test]
		public void WriteStandardPicturesLazy()
		{
			StandardTests.WriteStandardPictures(sample_file, tmp_file, ReadStyle.PictureLazy, TestTagLevel.High);
		}

		/// <summary>
		/// Use advanced Matroska-specific features.
		/// Matroska Tag Documentation: <see cref="https://www.matroska.org/technical/specs/tagging/index.html"/>.
		/// </summary>
		[Test]
		public void WriteSpecificTags()
		{
			if (System.IO.File.Exists(tmp_file))
				System.IO.File.Delete(tmp_file);
			File file = null;
			try
			{
				System.IO.File.Copy(sample_file, tmp_file);
				file = File.Create(tmp_file);
			}
			finally {}
			Assert.NotNull(file);

			// Write Matroska-specific tags 
			var mtag = (TagLib.Matroska.Tag)file.GetTag(TagLib.TagTypes.Matroska);
			Assert.NotNull(mtag);

			mtag.PerformersRole = new string[] { "TEST role 1", "TEST role 2" };
			mtag.Set("CHOREGRAPHER", null, "TEST choregrapher");

			// Retrieve Matroska 'Tags' structure
			var mtags = mtag.Tags;

			// Add a new Matroska 'Tag' structure, representing a collection, in the 'Tags' structure
			var collectag = new Matroska.Tag(mtags, Matroska.TargetType.COLLECTION);

			// Add a Matroska 'SimpleTag' (TagName: 'ARRANGER') in the 'Tag' structure
			collectag.Set("ARRANGER", null, "TEST arranger");

			// Add a Matroska 'SimpleTag' (TagName: 'TITLE') in the 'Tag' structure
			collectag.Set("TITLE", null, "TEST Album title"); // This should map to the standard TagLib Album tag

			// Get tracks
			var tracks = mtag.Tags.Tracks;

			// Create video tags
			var videotag = new Matroska.Tag(mtag.Tags, Matroska.TargetType.CHAPTER, tracks[0]);
			videotag.Title = "The Video test";
			videotag.Set("DESCRIPTION", null, "Video track Tag test");
			videotag.SimpleTags["DESCRIPTION"][0].TagLanguage = "en";
			videotag.SimpleTags["DESCRIPTION"][0].TagDefault = false;

			// Add another description in another language (and check encoding correctness at the same time)
			videotag.SimpleTags["DESCRIPTION"].Add(new Matroska.SimpleTag("Test de piste vidéo"));
			videotag.SimpleTags["DESCRIPTION"][1].TagLanguage = "fr";

			// Remove Audio tags
			var audiotag = mtag.Tags.Get(tracks[1]);
			Assert.NotNull(audiotag);
			audiotag.Clear();

			// Eventually save the changes
			file.Save();

			// Read back the Matroska-specific tags 
			file = File.Create(tmp_file);
			Assert.NotNull(mtag);

			mtag = (TagLib.Matroska.Tag)file.GetTag(TagLib.TagTypes.Matroska);
			Assert.NotNull(mtag);

			Assert.AreEqual("TEST role 1; TEST role 2", string.Join("; ", mtag.PerformersRole));
			Assert.AreEqual("TEST choregrapher", mtag.Get("CHOREGRAPHER", null)[0]);
			Assert.AreEqual("TEST arranger", mtags.Album.Get("ARRANGER", null)[0]);
			Assert.AreEqual("TEST Album title", mtag.Album);

			// Get tracks
			tracks = mtag.Tags.Tracks;
			Assert.NotNull(tracks);

			// Test Video Track Tag
			videotag = mtag.Tags.Get(tracks[0]);
			Assert.NotNull(videotag);
			Assert.AreEqual(Matroska.TargetType.CHAPTER, videotag.TargetType);
			Assert.AreEqual(30, videotag.TargetTypeValue);
			Assert.AreEqual("The Video test", videotag.Title);
			Assert.AreEqual("Video track Tag test", videotag.SimpleTags["DESCRIPTION"][0]);
			Assert.AreEqual("en", videotag.SimpleTags["DESCRIPTION"][0].TagLanguage);
			Assert.AreEqual(false, videotag.SimpleTags["DESCRIPTION"][0].TagDefault);

			// implicit or explicit conversion from TagLib.Matroska.SimpleTag to string is required to ensure a proper encoding
			Assert.AreEqual("Test de piste vidéo", videotag.SimpleTags["DESCRIPTION"][1].ToString());
			Assert.AreEqual("fr", videotag.SimpleTags["DESCRIPTION"][1].TagLanguage);
			Assert.AreEqual(true, videotag.SimpleTags["DESCRIPTION"][1].TagDefault);

			// Test Audio Track Tag
			audiotag = mtag.Tags.Get(tracks[1]);
			Assert.IsNull(audiotag);

			Assert.AreEqual("TEST Album title", mtag.Album);

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
