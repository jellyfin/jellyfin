using System;
using System.Security.Cryptography;
using NUnit.Framework;
using TagLib.Asf;

namespace TagLib.Tests.TaggingFormats
{   
	[TestFixture]
	public class AsfTest
	{
		private static string val_sing =
			"01234567890123456789012345678901234567890123456789";
		private static string [] val_mult = new string [] {"A123456789",
			"B123456789", "C123456789", "D123456789", "E123456789"};
		private static string [] val_gnre = new string [] {"Rap",
			"Jazz", "Non-Genre", "Blues"};
		
		[Test]
		public void TestTitle ()
		{
			MemoryFileAbstraction abst;
			Asf.File file = CreateFile (out abst);
			
			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.Title, "Initial (Null): " + m);
			});
			
			file.Tag.Title = val_sing;
			
			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.Title, "Value Set (!Null): " + m);
			});
			
			file.Tag.Title = string.Empty;
			
			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.Title, "Value Cleared (Null): " + m);
			});

		}
		
		[Test]
		public void TestPerformers ()
		{
			MemoryFileAbstraction abst;
			Asf.File file = CreateFile (out abst);
			
			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.AreEqual (0, t.Performers.Length, "Initial (Zero): " + m);
			});
			
			file.Tag.Performers = val_mult;
			
			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_mult.Length, t.Performers.Length, "Value Set: " + m);
				for (int i = 0; i < val_mult.Length; i ++) {
					Assert.AreEqual (val_mult [i], t.Performers [i], "Value Set: " + m);
				}
			});
			
			file.Tag.Performers = new string [0];
			
			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.AreEqual (0, t.Performers.Length, "Value Cleared (Zero): " + m);
			});
		}
		
		[Test]
		public void TestAlbumArtists ()
		{
			MemoryFileAbstraction abst;
			Asf.File file = CreateFile (out abst);
			
			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.AreEqual (0, t.AlbumArtists.Length, "Initial (Zero): " + m);
			});
			
			file.Tag.AlbumArtists = val_mult;
			
			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_mult.Length, t.AlbumArtists.Length, "Value Set: " + m);
				for (int i = 0; i < val_mult.Length; i ++) {
					Assert.AreEqual (val_mult [i], t.AlbumArtists [i], "Value Set: " + m);
				}
			});
			
			file.Tag.AlbumArtists = new string [0];
			
			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.AreEqual (0, t.AlbumArtists.Length, "Value Cleared (Zero): " + m);
			});
		}
		
		[Test]
		public void TestComposers ()
		{
			MemoryFileAbstraction abst;
			Asf.File file = CreateFile (out abst);
			
			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.AreEqual (0, t.Composers.Length, "Initial (Zero): " + m);
			});
			
			file.Tag.Composers = val_mult;
			
			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_mult.Length, t.Composers.Length, "Value Set: " + m);
				for (int i = 0; i < val_mult.Length; i ++) {
					Assert.AreEqual (val_mult [i], t.Composers [i], "Value Set: " + m);
				}
			});
			
			file.Tag.Composers = new string [0];
			
			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.AreEqual (0, t.Composers.Length, "Value Cleared (Zero): " + m);
			});
		}
		
		[Test]
		public void TestAlbum ()
		{
			MemoryFileAbstraction abst;
			Asf.File file = CreateFile (out abst);
			
			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.Album, "Initial (Null): " + m);
			});
			
			file.Tag.Album = val_sing;
			
			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.Album, "Value Set (!Null): " + m);
			});
			
			file.Tag.Album = string.Empty;
			
			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.Album, "Value Cleared (Null): " + m);
			});
		}
		
		[Test]
		public void TestComment ()
		{
			MemoryFileAbstraction abst;
			Asf.File file = CreateFile (out abst);

			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.Comment, "Initial (Null): " + m);
			});
			
			file.Tag.Comment = val_sing;
			
			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.Comment, "Value Set (!Null): " + m);
			});
			
			file.Tag.Comment = string.Empty;

			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.Comment, "Value Cleared (Null): " + m);
			});
		}
		
		[Test]
		public void TestGenres ()
		{
			MemoryFileAbstraction abst;
			Asf.File file = CreateFile (out abst);

			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.AreEqual (0, t.Genres.Length, "Initial (Zero): " + m);
			});
			
			file.Tag.Genres = val_gnre;
			
			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_gnre.Length, t.Genres.Length, "Value Set: " + m);
				for (int i = 0; i < val_gnre.Length; i ++) {
					Assert.AreEqual (val_gnre [i], t.Genres [i], "Value Set: " + m);
				}
			});
			
			file.Tag.Genres = val_mult;
			
			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_mult.Length, t.Genres.Length, "Value Set: " + m);
				for (int i = 0; i < val_mult.Length; i ++) {
					Assert.AreEqual (val_mult [i], t.Genres [i], "Value Set: " + m);
				}
			});
			
			file.Tag.Genres = new string [0];

			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.AreEqual (0, t.Genres.Length, "Value Cleared (Zero): " + m);
			});
		}
		
		[Test]
		public void TestYear ()
		{
			MemoryFileAbstraction abst;
			Asf.File file = CreateFile (out abst);

			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.AreEqual (0, t.Year, "Initial (Zero): " + m);
			});
			
			file.Tag.Year = 1999;
			
			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (1999, t.Year, "Value Set: " + m);
			});
			
			file.Tag.Year = 0;

			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.AreEqual (0, t.Year, "Value Cleared (Zero): " + m);
			});
		}
		
		[Test]
		public void TestTrack ()
		{
			MemoryFileAbstraction abst;
			Asf.File file = CreateFile (out abst);

			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.AreEqual (0, t.Track, "Initial (Zero): " + m);
			});
			
			file.Tag.Track = 199;
			
			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (199, t.Track, "Value Set: " + m);
			});
			
			file.Tag.Track = 0;

			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.AreEqual (0, t.Track, "Value Cleared (Zero): " + m);
			});
		}
		
		[Test]
		public void TestTrackCount ()
		{
			MemoryFileAbstraction abst;
			Asf.File file = CreateFile (out abst);

			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.AreEqual (0, t.TrackCount, "Initial (Zero): " + m);
			});
			
			file.Tag.TrackCount = 199;
			
			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (199, t.TrackCount, "Value Set: " + m);
			});
			
			file.Tag.TrackCount = 0;

			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.AreEqual (0, t.TrackCount, "Value Cleared (Zero): " + m);
			});
		}
		
		[Test]
		public void TestDisc ()
		{
			MemoryFileAbstraction abst;
			Asf.File file = CreateFile (out abst);

			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.AreEqual (0, t.Disc, "Initial (Zero): " + m);
			});
			
			file.Tag.Disc = 199;
			
			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (199, t.Disc, "Value Set: " + m);
			});
			
			file.Tag.Disc = 0;

			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.AreEqual (0, t.Disc, "Value Cleared (Zero): " + m);
			});
		}
		
		[Test]
		public void TestDiscCount ()
		{
			MemoryFileAbstraction abst;
			Asf.File file = CreateFile (out abst);

			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.AreEqual (0, t.DiscCount, "Initial (Zero): " + m);
			});
			
			file.Tag.DiscCount = 199;
			
			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (199, t.DiscCount, "Value Set: " + m);
			});
			
			file.Tag.DiscCount = 0;

			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.AreEqual (0, t.DiscCount, "Value Cleared (Zero): " + m);
			});
		}
		
		[Test]
		public void TestLyrics ()
		{
			MemoryFileAbstraction abst;
			Asf.File file = CreateFile (out abst);
			
			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.Lyrics, "Initial (Null): " + m);
			});
			
			file.Tag.Lyrics = val_sing;
			
			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.Lyrics, "Value Set (!Null): " + m);
			});
			
			file.Tag.Lyrics = string.Empty;

			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.Lyrics, "Value Cleared (Null): " + m);
			});
		}
		
		[Test]
		public void TestGrouping ()
		{
			MemoryFileAbstraction abst;
			Asf.File file = CreateFile (out abst);

			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.Grouping, "Initial (Null): " + m);
			});
			
			file.Tag.Grouping = val_sing;
			
			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.Grouping, "Value Set (!Null): " + m);
			});
			
			file.Tag.Grouping = string.Empty;

			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.Grouping, "Value Cleared (Null): " + m);
			});
		}
		
		[Test]
		public void TestBeatsPerMinute ()
		{
			MemoryFileAbstraction abst;
			Asf.File file = CreateFile (out abst);
		
			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.AreEqual (0, t.BeatsPerMinute, "Initial (Zero): " + m);
			});
			
			file.Tag.BeatsPerMinute = 199;
			
			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (199, t.BeatsPerMinute, "Value Set: " + m);
			});
			
			file.Tag.BeatsPerMinute = 0;

			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.AreEqual (0, t.BeatsPerMinute, "Value Cleared (Zero): " + m);
			});
		}
		
		[Test]
		public void TestConductor ()
		{
			MemoryFileAbstraction abst;
			Asf.File file = CreateFile (out abst);

			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.Conductor, "Initial (Null): " + m);
			});
			
			file.Tag.Conductor = val_sing;
			
			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.Conductor, "Value Set (!Null): " + m);
			});
			
			file.Tag.Conductor = string.Empty;

			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.Conductor, "Value Cleared (Null): " + m);
			});
		}
		
		[Test]
		public void TestCopyright ()
		{
			MemoryFileAbstraction abst;
			Asf.File file = CreateFile (out abst);

			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.Copyright, "Initial (Null): " + m);
			});
			
			file.Tag.Copyright = val_sing;
			
			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.Copyright, "Value Set (!Null): " + m);
			});
			
			file.Tag.Copyright = string.Empty;

			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.Copyright, "Value Cleared (Null): " + m);
			});
		}
		
		[Test]
		public void TestPictures ()
		{
			MemoryFileAbstraction abst;
			Asf.File file = CreateFile (out abst);
			
			Picture [] pictures = new Picture [] {
				new Picture (TestPath.Covers + "sample_a.png"),
				new Picture (TestPath.Covers + "sample_a.jpg"),
				new Picture (TestPath.Covers + "sample_b.png"),
				new Picture (TestPath.Covers + "sample_b.jpg"),
				new Picture (TestPath.Covers + "sample_c.png"),
				new Picture (TestPath.Covers + "sample_c.jpg")
			};
			
			for (int i = 0; i < 6; i ++)
				pictures [i].Type = (PictureType) (i * 2);
			
			pictures [3].Description = val_sing;
			
			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.AreEqual (0, t.Pictures.Length, "Initial (Zero): " + m);
			});
			
			file.Tag.Pictures = pictures;
			
			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (pictures.Length, t.Pictures.Length, "Value Set: " + m);
				for (int i = 0; i < pictures.Length; i ++) {
					string msg = "Value " + i + "Set: " + m;
					Assert.AreEqual (pictures [i].Data, t.Pictures [i].Data, msg);
					Assert.AreEqual (pictures [i].Type, t.Pictures [i].Type, msg);
					Assert.AreEqual (pictures [i].Description, t.Pictures [i].Description, msg);
					Assert.AreEqual (pictures [i].MimeType, t.Pictures [i].MimeType, msg);
				}
			});
			
			file.Tag.Pictures = new Picture [0];
			
			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.AreEqual (0, t.Pictures.Length, "Value Cleared (Zero): " + m);
			});
		}
		
		[Test]
		public void TestMusicBrainzArtistID ()
		{
			MemoryFileAbstraction abst;
			Asf.File file = CreateFile (out abst);

			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzArtistId, "Initial (Null): " + m);
			});
			
			file.Tag.MusicBrainzArtistId = val_sing;
			
			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.MusicBrainzArtistId, "Value Set (!Null): " + m);
			});
			
			file.Tag.MusicBrainzArtistId = string.Empty;

			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzArtistId, "Value Cleared (Null): " + m);
			});
		}
		
		[Test]
		public void TestMusicBrainzReleaseID ()
		{
			MemoryFileAbstraction abst;
			Asf.File file = CreateFile (out abst);

			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzReleaseId, "Initial (Null): " + m);
			});
			
			file.Tag.MusicBrainzReleaseId = val_sing;
			
			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.MusicBrainzReleaseId, "Value Set (!Null): " + m);
			});
			
			file.Tag.MusicBrainzReleaseId = string.Empty;

			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzReleaseId, "Value Cleared (Null): " + m);
			});
		}

		[Test]
		public void TestMusicBrainzReleaseGroupID()
		{
			MemoryFileAbstraction abst;
			Asf.File file = CreateFile(out abst);

			TagTestWithSave(ref file, abst, delegate (Tag t, string m) {
				Assert.IsTrue(t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull(t.MusicBrainzReleaseGroupId, "Initial (Null): " + m);
			});

			file.Tag.MusicBrainzReleaseGroupId = val_sing;

			TagTestWithSave(ref file, abst, delegate (Tag t, string m) {
				Assert.IsFalse(t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual(val_sing, t.MusicBrainzReleaseGroupId, "Value Set (!Null): " + m);
			});

			file.Tag.MusicBrainzReleaseGroupId = string.Empty;

			TagTestWithSave(ref file, abst, delegate (Tag t, string m) {
				Assert.IsTrue(t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull(t.MusicBrainzReleaseGroupId, "Value Cleared (Null): " + m);
			});
		}

		[Test]
		public void TestMusicBrainzReleaseArtistID ()
		{
			MemoryFileAbstraction abst;
			Asf.File file = CreateFile (out abst);

			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzReleaseArtistId, "Initial (Null): " + m);
			});
			
			file.Tag.MusicBrainzReleaseArtistId = val_sing;
			
			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.MusicBrainzReleaseArtistId, "Value Set (!Null): " + m);
			});
			
			file.Tag.MusicBrainzReleaseArtistId = string.Empty;

			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzReleaseArtistId, "Value Cleared (Null): " + m);
			});
		}
		
		[Test]
		public void TestMusicBrainzTrackID ()
		{
			MemoryFileAbstraction abst;
			Asf.File file = CreateFile (out abst);

			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzTrackId, "Initial (Null): " + m);
			});
			
			file.Tag.MusicBrainzTrackId = val_sing;
			
			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.MusicBrainzTrackId, "Value Set (!Null): " + m);
			});
			
			file.Tag.MusicBrainzTrackId = string.Empty;

			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzTrackId, "Value Cleared (Null): " + m);
			});
		}
		
		[Test]
		public void TestMusicBrainzDiscID ()
		{
			MemoryFileAbstraction abst;
			Asf.File file = CreateFile (out abst);

			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzDiscId, "Initial (Null): " + m);
			});
			
			file.Tag.MusicBrainzDiscId = val_sing;
			
			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.MusicBrainzDiscId, "Value Set (!Null): " + m);
			});
			
			file.Tag.MusicBrainzDiscId = string.Empty;

			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzDiscId, "Value Cleared (Null): " + m);
			});
		}
		
		[Test]
		public void TestMusicIPPUID ()
		{
			MemoryFileAbstraction abst;
			Asf.File file = CreateFile (out abst);

			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.MusicIpId, "Initial (Null): " + m);
			});
			
			file.Tag.MusicIpId = val_sing;
			
			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.MusicIpId, "Value Set (!Null): " + m);
			});
			
			file.Tag.MusicIpId = string.Empty;

			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.MusicIpId, "Value Cleared (Null): " + m);
			});
		}
		
		[Test]
		[Ignore("Skip AmazonID for ASF - not implemented")]
		public void TestAmazonID ()
		{
			MemoryFileAbstraction abst;
			Asf.File file = CreateFile (out abst);

			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.AmazonId, "Initial (Null): " + m);
			});
			
			file.Tag.AmazonId = val_sing;
			
			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.AmazonId, "Value Set (!Null): " + m);
			});
			
			file.Tag.AmazonId = string.Empty;

			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.AmazonId, "Value Cleared (Null): " + m);
			});
		}
				
		[Test]
		public void TestMusicBrainzReleaseStatus ()
		{
			MemoryFileAbstraction abst;
			Asf.File file = CreateFile (out abst);

			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzReleaseStatus, "Initial (Null): " + m);
			});
			
			file.Tag.MusicBrainzReleaseStatus = val_sing;
			
			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.MusicBrainzReleaseStatus, "Value Set (!Null): " + m);
			});
			
			file.Tag.MusicBrainzReleaseStatus = string.Empty;

			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzReleaseStatus, "Value Cleared (Null): " + m);
			});
		}
				
		[Test]
		public void TestMusicBrainzReleaseType ()
		{
			MemoryFileAbstraction abst;
			Asf.File file = CreateFile (out abst);

			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzReleaseType, "Initial (Null): " + m);
			});
			
			file.Tag.MusicBrainzReleaseType = val_sing;
			
			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.MusicBrainzReleaseType, "Value Set (!Null): " + m);
			});
			
			file.Tag.MusicBrainzReleaseType = string.Empty;

			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzReleaseType, "Value Cleared (Null): " + m);
			});
		}
				
		[Test]
		public void TestMusicBrainzReleaseCountry ()
		{
			MemoryFileAbstraction abst;
			Asf.File file = CreateFile (out abst);

			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzReleaseCountry, "Initial (Null): " + m);
			});
			
			file.Tag.MusicBrainzReleaseCountry = val_sing;
			
			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.MusicBrainzReleaseCountry, "Value Set (!Null): " + m);
			});
			
			file.Tag.MusicBrainzReleaseCountry = string.Empty;

			TagTestWithSave (ref file, abst, delegate (Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzReleaseCountry, "Value Cleared (Null): " + m);
			});
		}
		
		[Test]
		public void TestClear ()
		{
			MemoryFileAbstraction abst;
			Asf.File file = CreateFile (out abst);
			
			file.Tag.Title = "A";
			file.Tag.Performers = new string [] {"B"};
			file.Tag.AlbumArtists = new string [] {"C"};
			file.Tag.Composers = new string [] {"D"};
			file.Tag.Album = "E";
			file.Tag.Comment = "F";
			file.Tag.Genres = new string [] {"Blues"};
			file.Tag.Year = 123;
			file.Tag.Track = 234;
			file.Tag.TrackCount = 234;
			file.Tag.Disc = 234;
			file.Tag.DiscCount = 234;
			file.Tag.Lyrics = "G";
			file.Tag.Grouping = "H";
			file.Tag.BeatsPerMinute = 234;
			file.Tag.Conductor = "I";
			file.Tag.Copyright = "J";
			file.Tag.Pictures = new Picture [] {new Picture (TestPath.Covers + "sample_a.png") };
			
			Assert.IsFalse (file.Tag.IsEmpty, "Should be full.");
			file.Tag.Clear ();
			
			Assert.IsNull (file.Tag.Title, "Title");
			Assert.AreEqual (0, file.Tag.Performers.Length, "Performers");
			Assert.AreEqual (0, file.Tag.AlbumArtists.Length, "AlbumArtists");
			Assert.AreEqual (0, file.Tag.Composers.Length, "Composers");
			Assert.IsNull (file.Tag.Album, "Album");
			Assert.IsNull (file.Tag.Comment, "Comment");
			Assert.AreEqual (0, file.Tag.Genres.Length, "Genres");
			Assert.AreEqual (0, file.Tag.Year, "Year");
			Assert.AreEqual (0, file.Tag.Track, "Track");
			Assert.AreEqual (0, file.Tag.TrackCount, "TrackCount");
			Assert.AreEqual (0, file.Tag.Disc, "Disc");
			Assert.AreEqual (0, file.Tag.DiscCount, "DiscCount");
			Assert.IsNull (file.Tag.Lyrics, "Lyrics");
			Assert.IsNull (file.Tag.Comment, "Comment");
			Assert.AreEqual (0, file.Tag.BeatsPerMinute, "BeatsPerMinute");
			Assert.IsNull (file.Tag.Conductor, "Conductor");
			Assert.IsNull (file.Tag.Copyright, "Copyright");
			Assert.AreEqual (0, file.Tag.Pictures.Length, "Pictures");
			Assert.IsTrue (file.Tag.IsEmpty, "Should be empty.");
		}
		
		private Asf.File CreateFile (out MemoryFileAbstraction abst)
		{
			byte [] data = new byte [] {
				0x30, 0x26, 0xb2, 0x75, 0x8e, 0x66, 0xcf, 0x11,
				0xa6, 0xd9, 0x00, 0xaa, 0x00, 0x62, 0xce, 0x6c,
				0x4c, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
				0x01, 0x00, 0x00, 0x00, 0x00, 0x00,
				0xb5, 0x03, 0xbf, 0x5f, 0x2e, 0xa9, 0xcf, 0x11,
				0x8e, 0xe3, 0x00, 0xc0, 0x0c, 0x20, 0x53, 0x65,
				0x2e, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
				0x11, 0xd2, 0xd3, 0xab, 0xba, 0xa9, 0xcf, 0x11,
				0x8e, 0xe6, 0x00, 0xc0, 0x0c, 0x20, 0x53, 0x65,
				0x06, 0x00, 0x00, 0x00, 0x00, 0x00
			};
			
			abst = new MemoryFileAbstraction (0xffff, data);
			return new Asf.File (abst);
		}
		
		delegate void TagTestFunc (Tag tag, string msg);
		
		void TagTestWithSave (ref Asf.File file,
		                      MemoryFileAbstraction abst,
		                      TagTestFunc testFunc)
		{
			testFunc (file.Tag, "Before Save");
			file.Save ();
			file = new Asf.File (abst);
			testFunc (file.Tag, "After Save");
		}
	}
}
