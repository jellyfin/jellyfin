using System;
using NUnit.Framework;
using TagLib;

namespace TagLib.Tests.FileFormats
{   
	public static class StandardTests
	{
		private static string sample_picture = TestPath.Samples + "sample_gimp.gif";
		private static string sample_other = TestPath.Samples + "apple_tags.m4a";

		public enum TestTagLevel
		{
			Normal,
			Medium, 
			High
		}

		public static void ReadAudioProperties (File file)
		{
			Assert.AreEqual(44100, file.Properties.AudioSampleRate);
			Assert.AreEqual(5, file.Properties.Duration.Seconds);
		}
		
		public static void WriteStandardTags (string sample_file, string tmp_file, 
			TestTagLevel level = TestTagLevel.Normal, TagTypes types = TagTypes.AllTags)
		{

			if (sample_file != tmp_file && 
				System.IO.File.Exists (tmp_file))
				System.IO.File.Delete (tmp_file);
			
			try {
				if (sample_file != tmp_file)
					System.IO.File.Copy(sample_file, tmp_file);
				
				File tmp = File.Create (tmp_file);

				if (types != TagTypes.AllTags)
				{
					tmp.RemoveTags(~types);
				}

				SetTags (tmp.Tag, level);
				tmp.Save();
				
				tmp = File.Create (tmp_file);
				CheckTags (tmp.Tag, level);
			} finally {
//                if (System.IO.File.Exists (tmp_file))
//                    System.IO.File.Delete (tmp_file);
			}
		}



		public static void WriteStandardPictures(string sample_file, string tmp_file,
												 ReadStyle readStyle = ReadStyle.Average,
												 TestTagLevel level = TestTagLevel.Medium)
		{
			if (System.IO.File.Exists(tmp_file))
				System.IO.File.Delete(tmp_file);
			File file = null;
			try
			{
				System.IO.File.Copy(sample_file, tmp_file);
				file = File.Create(tmp_file, readStyle);
			}
			finally { }
			Assert.NotNull(file);

			var pics = file.Tag.Pictures;

			// Raw Picture data references
			var raws = new ByteVector[3];

			// Insert new picture
			Array.Resize(ref pics, 3);
			raws[0] = ByteVector.FromPath(sample_picture);
			pics[0] = new Picture(sample_picture);
			pics[0].Type = PictureType.BackCover;
			pics[0].Description = "TEST description 1";

			raws[1] = ByteVector.FromPath(sample_other);
			pics[1] = new Picture(sample_other);
			pics[1].Description = "TEST description 2";

			raws[2] = raws[0];
			pics[2] = new Picture(sample_picture);
			pics[2].Filename = "renamed.gif";
			pics[2].Type = PictureType.Other;
			pics[2].Description = "TEST description 3";
			file.Tag.Pictures = pics;

			file.Save();

			// Read back the tags 
			file = File.Create(tmp_file, readStyle);
			Assert.NotNull(file);
			pics = file.Tag.Pictures;

			Assert.AreEqual(3, pics.Length);

			// Lazy picture check
			var isLazy = (readStyle & ReadStyle.PictureLazy) != 0;
			for (int i = 0; i < 3; i++)
			{
				if (isLazy)
				{
					Assert.IsTrue(pics[i] is ILazy);
					if (pics[i] is ILazy lazy)
					{
						Assert.IsFalse(lazy.IsLoaded);
					}
				}
				else
				{
					if (pics[i] is ILazy lazy)
					{
						Assert.IsTrue(lazy.IsLoaded);
					}
				}
			}

			Assert.AreEqual("TEST description 1", pics[0].Description);
			Assert.AreEqual("image/gif", pics[0].MimeType);
			Assert.AreEqual(73, pics[0].Data.Count);
			Assert.AreEqual(raws[0], pics[0].Data);

			Assert.AreEqual("TEST description 2", pics[1].Description);
			Assert.AreEqual(102400, pics[1].Data.Count);
			Assert.AreEqual(raws[1], pics[1].Data);

			Assert.AreEqual("TEST description 3", pics[2].Description);
			Assert.AreEqual("image/gif", pics[2].MimeType);
			Assert.AreEqual(73, pics[2].Data.Count);
			Assert.AreEqual(raws[2], pics[2].Data);

			// Types and Mime-Types assumed to be properly supported at Medium level test
			if (level >= TestTagLevel.Medium)
			{
				Assert.AreEqual("audio/mp4", pics[1].MimeType);
				Assert.AreEqual(PictureType.BackCover, pics[0].Type);
				Assert.AreEqual(PictureType.NotAPicture, pics[1].Type);
				Assert.AreEqual(PictureType.Other, pics[2].Type);
			}
			else
			{
				Assert.AreNotEqual(PictureType.NotAPicture, pics[0].Type);
				Assert.AreEqual(PictureType.NotAPicture, pics[1].Type);
				Assert.AreNotEqual(PictureType.NotAPicture, pics[2].Type);
			}

			// Filename assumed to be properly supported at High level test
			if (level >= TestTagLevel.High)
			{
				Assert.AreEqual("apple_tags.m4a", pics[1].Filename);
			}
			else if (level >= TestTagLevel.Medium)
			{
				if (pics[1].Filename != null)
					Assert.AreEqual("apple_tags.m4a", pics[1].Filename);
			}

		}


		public static void RemoveStandardTags(string sample_file, string tmp_file, TagTypes types = TagTypes.AllTags)
		{
			if (System.IO.File.Exists(tmp_file))
				System.IO.File.Delete(tmp_file);

			try
			{
				System.IO.File.Copy(sample_file, tmp_file);

				File tmp = File.Create(tmp_file);
				tmp.RemoveTags(types);

				tmp.Save();

				// Check only if all tags have been removed
				if (types == TagTypes.AllTags)
				{
					tmp = File.Create(tmp_file);
					CheckNoTags(tmp.Tag);

				}
			}
			finally
			{
				//                if (System.IO.File.Exists (tmp_file))
				//                    System.IO.File.Delete (tmp_file);
			}
		}


		public static void SetTags (Tag tag, TestTagLevel level = TestTagLevel.Normal)
		{
			if (level >= TestTagLevel.Medium)
			{
				tag.TitleSort = "title sort, TEST";
				tag.AlbumSort = "album sort, TEST";
				tag.PerformersSort = new string[] { "performer sort 1, TEST", "performer sort 2, TEST" };
				tag.ComposersSort = new string[] { "composer sort 1, TEST", "composer sort 2, TEST" };
				tag.AlbumArtistsSort = new string[] { "album artist sort 1, TEST", "album artist sort 2, TEST" };
			}

			tag.Album = "TEST album";
			tag.AlbumArtists = new string [] {"TEST artist 1", "TEST artist 2"};
			tag.BeatsPerMinute = 120;
			tag.Comment = "TEST comment";
			tag.Composers = new string [] {"TEST composer 1", "TEST composer 2"};
			tag.Conductor = "TEST conductor";
			tag.Copyright = "TEST copyright";
			tag.DateTagged = new DateTime(2017, 09, 12, 22, 47, 42);
			tag.Disc = 100;
			tag.DiscCount = 101;
			tag.Genres = new string [] {"TEST genre 1", "TEST genre 2"};
			tag.Grouping = "TEST grouping";
			tag.Lyrics = "TEST lyrics 1\r\nTEST lyrics 2";
			tag.Performers = new string[] { "TEST performer 1", "TEST performer 2" };
			tag.PerformersRole = new string[] { "TEST role 1a; TEST role 1b", "TEST role 2" };
			tag.Title = "TEST title";
			tag.Subtitle = "TEST subtitle";
			tag.Description = "TEST description";
			tag.Track = 98;
			tag.TrackCount = 99;
			tag.Year = 1999;
		}

		public static void CheckTags (Tag tag, TestTagLevel level = TestTagLevel.Normal)
		{

			Assert.AreEqual ("TEST album", tag.Album);
			Assert.AreEqual ("TEST artist 1; TEST artist 2", tag.JoinedAlbumArtists);
			Assert.AreEqual ("TEST comment", tag.Comment);
			Assert.AreEqual ("TEST composer 1; TEST composer 2", tag.JoinedComposers);
			Assert.AreEqual ("TEST conductor", tag.Conductor);
			Assert.AreEqual ("TEST copyright", tag.Copyright);
			Assert.AreEqual (100, tag.Disc);
			Assert.AreEqual (101, tag.DiscCount);
			Assert.AreEqual ("TEST genre 1; TEST genre 2", tag.JoinedGenres);
			Assert.AreEqual ("TEST grouping", tag.Grouping);
			Assert.AreEqual ("TEST lyrics 1\r\nTEST lyrics 2", tag.Lyrics);
			Assert.AreEqual ("TEST performer 1; TEST performer 2", tag.JoinedPerformers);
			Assert.AreEqual ("TEST title", tag.Title);
			Assert.AreEqual ("TEST subtitle", tag.Subtitle);
			Assert.AreEqual ("TEST description", tag.Description);
			Assert.AreEqual (98, tag.Track);
			Assert.AreEqual (99, tag.TrackCount);
			Assert.AreEqual (1999, tag.Year);

			if (level >= TestTagLevel.Medium)
			{
				Assert.AreEqual ("title sort, TEST", tag.TitleSort);
				Assert.AreEqual ("album sort, TEST", tag.AlbumSort);
				Assert.AreEqual ("performer sort 1, TEST; performer sort 2, TEST", tag.JoinedPerformersSort);
				Assert.AreEqual ("composer sort 1, TEST; composer sort 2, TEST", string.Join("; ", tag.ComposersSort));
				Assert.AreEqual ("album artist sort 1, TEST; album artist sort 2, TEST", string.Join("; ", tag.AlbumArtistsSort));
				Assert.AreEqual (120, tag.BeatsPerMinute);
				Assert.AreEqual (new DateTime(2017, 09, 12, 22, 47, 42), tag.DateTagged);
				Assert.AreEqual ("TEST role 1a; TEST role 1b\nTEST role 2", string.Join("\n", tag.PerformersRole));
			}
		}


		public static void CheckNoTags(Tag tag)
		{
			Assert.IsNull(tag.Album);
			Assert.IsNull(tag.JoinedAlbumArtists);
			Assert.IsNull(tag.Comment);
			Assert.IsNull(tag.Conductor);
			Assert.IsNull(tag.Copyright);
			Assert.IsNull(tag.Grouping);
			Assert.IsNull(tag.Lyrics);

			Assert.AreEqual(0, tag.BeatsPerMinute);
			Assert.AreEqual(0, tag.Disc);
			Assert.AreEqual(0, tag.DiscCount);
			Assert.AreEqual(0, tag.Track);
			Assert.AreEqual(0, tag.TrackCount);
			Assert.AreEqual(0, tag.Year);

			Assert.IsTrue(string.IsNullOrEmpty(tag.JoinedComposers));
			Assert.IsTrue(string.IsNullOrEmpty(tag.JoinedGenres));
			Assert.IsTrue(string.IsNullOrEmpty(tag.JoinedPerformers));

			Assert.IsNull(tag.Title);
			Assert.IsNull(tag.Description);
			Assert.IsNull(tag.DateTagged);
			Assert.IsTrue(tag.Performers == null || tag.Performers.Length == 0);
			Assert.IsTrue(tag.PerformersSort == null || tag.PerformersSort.Length == 0);
			Assert.IsTrue(tag.PerformersRole == null || tag.PerformersRole.Length == 0);
			Assert.IsTrue(tag.AlbumArtistsSort == null || tag.AlbumArtistsSort.Length == 0);
			Assert.IsTrue(tag.AlbumArtists == null || tag.AlbumArtists.Length == 0);
			Assert.IsTrue(tag.Composers == null || tag.Composers.Length == 0);
			Assert.IsTrue(tag.ComposersSort == null || tag.ComposersSort.Length == 0);

			Assert.IsTrue(tag.IsEmpty);
		}


		public static void TestCorruptionResistance (string path)
		{
			try {
				File.Create (path);
			} catch(CorruptFileException) {
			} catch(NullReferenceException e) {
				throw e;
			} catch {
			}
		}
	}
}
