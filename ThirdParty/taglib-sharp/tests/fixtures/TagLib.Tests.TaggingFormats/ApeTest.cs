using System;
using System.Security.Cryptography;
using NUnit.Framework;
using TagLib.Ape;

namespace TagLib.Tests.TaggingFormats
{   
	[TestFixture]
	public class ApeTest
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
			Ape.Tag tag = new Ape.Tag ();
			
			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.Title, "Initial (Null): " + m);
			});
			
			tag.Title = val_sing;
			
			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.Title, "Value Set (!Null): " + m);
			});
			
			tag.Title = string.Empty;
			
			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.Title, "Value Cleared (Null): " + m);
			});

		}
		
		[Test]
		public void TestPerformers ()
		{
			Ape.Tag tag = new Ape.Tag ();
			
			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.AreEqual (0, t.Performers.Length, "Initial (Zero): " + m);
			});
			
			tag.Performers = val_mult;
			
			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_mult.Length, t.Performers.Length, "Value Set: " + m);
				for (int i = 0; i < val_mult.Length; i ++) {
					Assert.AreEqual (val_mult [i], t.Performers [i], "Value Set: " + m);
				}
			});
			
			tag.Performers = new string [0];
			
			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.AreEqual (0, t.Performers.Length, "Value Cleared (Zero): " + m);
			});
		}
		
		[Test]
		public void TestAlbumArtists ()
		{
			Ape.Tag tag = new Ape.Tag ();
			
			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.AreEqual (0, t.AlbumArtists.Length, "Initial (Zero): " + m);
			});
			
			tag.AlbumArtists = val_mult;
			
			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_mult.Length, t.AlbumArtists.Length, "Value Set: " + m);
				for (int i = 0; i < val_mult.Length; i ++) {
					Assert.AreEqual (val_mult [i], t.AlbumArtists [i], "Value Set: " + m);
				}
			});
			
			tag.AlbumArtists = new string [0];
			
			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.AreEqual (0, t.AlbumArtists.Length, "Value Cleared (Zero): " + m);
			});
		}
		
		[Test]
		public void TestComposers ()
		{
			Ape.Tag tag = new Ape.Tag ();
			
			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.AreEqual (0, t.Composers.Length, "Initial (Zero): " + m);
			});
			
			tag.Composers = val_mult;
			
			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_mult.Length, t.Composers.Length, "Value Set: " + m);
				for (int i = 0; i < val_mult.Length; i ++) {
					Assert.AreEqual (val_mult [i], t.Composers [i], "Value Set: " + m);
				}
			});
			
			tag.Composers = new string [0];
			
			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.AreEqual (0, t.Composers.Length, "Value Cleared (Zero): " + m);
			});
		}
		
		[Test]
		public void TestAlbum ()
		{
			Ape.Tag tag = new Ape.Tag ();
			
			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.Album, "Initial (Null): " + m);
			});
			
			tag.Album = val_sing;
			
			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.Album, "Value Set (!Null): " + m);
			});
			
			tag.Album = string.Empty;
			
			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.Album, "Value Cleared (Null): " + m);
			});
		}
		
		[Test]
		public void TestComment ()
		{
			Ape.Tag tag = new Ape.Tag ();

			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.Comment, "Initial (Null): " + m);
			});
			
			tag.Comment = val_sing;
			
			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.Comment, "Value Set (!Null): " + m);
			});
			
			tag.Comment = string.Empty;

			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.Comment, "Value Cleared (Null): " + m);
			});
		}
		
		[Test]
		public void TestGenres ()
		{
			Ape.Tag tag = new Ape.Tag ();

			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.AreEqual (0, t.Genres.Length, "Initial (Zero): " + m);
			});
			
			tag.Genres = val_gnre;
			
			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_gnre.Length, t.Genres.Length, "Value Set: " + m);
				for (int i = 0; i < val_gnre.Length; i ++) {
					Assert.AreEqual (val_gnre [i], t.Genres [i], "Value Set: " + m);
				}
			});
			
			tag.Genres = val_mult;
			
			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_mult.Length, t.Genres.Length, "Value Set: " + m);
				for (int i = 0; i < val_mult.Length; i ++) {
					Assert.AreEqual (val_mult [i], t.Genres [i], "Value Set: " + m);
				}
			});
			
			tag.Genres = new string [0];

			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.AreEqual (0, t.Genres.Length, "Value Cleared (Zero): " + m);
			});
		}
		
		[Test]
		public void TestYear ()
		{
			Ape.Tag tag = new Ape.Tag ();

			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.AreEqual (0, tag.Year, "Initial (Zero): " + m);
			});
			
			tag.Year = 1999;
			
			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (1999, tag.Year, "Value Set: " + m);
			});
			
			tag.Year = 0;

			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.AreEqual (0, t.Year, "Value Cleared (Zero): " + m);
			});
		}
		
		[Test]
		public void TestTrack ()
		{
			Ape.Tag tag = new Ape.Tag ();

			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.AreEqual (0, tag.Track, "Initial (Zero): " + m);
			});
			
			tag.Track = 199;
			
			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (199, tag.Track, "Value Set: " + m);
			});
			
			tag.Track = 0;

			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.AreEqual (0, t.Track, "Value Cleared (Zero): " + m);
			});
		}
		
		[Test]
		public void TestTrackCount ()
		{
			Ape.Tag tag = new Ape.Tag ();

			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.AreEqual (0, tag.TrackCount, "Initial (Zero): " + m);
			});
			
			tag.TrackCount = 199;
			
			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (199, tag.TrackCount, "Value Set: " + m);
			});
			
			tag.TrackCount = 0;

			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.AreEqual (0, t.TrackCount, "Value Cleared (Zero): " + m);
			});
		}
		
		[Test]
		public void TestDisc ()
		{
			Ape.Tag tag = new Ape.Tag ();

			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.AreEqual (0, tag.Disc, "Initial (Zero): " + m);
			});
			
			tag.Disc = 199;
			
			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (199, tag.Disc, "Value Set: " + m);
			});
			
			tag.Disc = 0;

			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.AreEqual (0, t.Disc, "Value Cleared (Zero): " + m);
			});
		}
		
		[Test]
		public void TestDiscCount ()
		{
			Ape.Tag tag = new Ape.Tag ();

			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.AreEqual (0, tag.DiscCount, "Initial (Zero): " + m);
			});
			
			tag.DiscCount = 199;
			
			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (199, tag.DiscCount, "Value Set: " + m);
			});
			
			tag.DiscCount = 0;

			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.AreEqual (0, t.DiscCount, "Value Cleared (Zero): " + m);
			});
		}
		
		[Test]
		public void TestLyrics ()
		{
			Ape.Tag tag = new Ape.Tag ();
			
			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.Lyrics, "Initial (Null): " + m);
			});
			
			tag.Lyrics = val_sing;
			
			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.Lyrics, "Value Set (!Null): " + m);
			});
			
			tag.Lyrics = string.Empty;

			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.Lyrics, "Value Cleared (Null): " + m);
			});
		}
		
		[Test]
		public void TestGrouping ()
		{
			Ape.Tag tag = new Ape.Tag ();

			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.Grouping, "Initial (Null): " + m);
			});
			
			tag.Grouping = val_sing;
			
			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.Grouping, "Value Set (!Null): " + m);
			});
			
			tag.Grouping = string.Empty;

			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.Grouping, "Value Cleared (Null): " + m);
			});
		}
		
		[Test]
		public void TestBeatsPerMinute ()
		{
			Ape.Tag tag = new Ape.Tag ();
		
			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.AreEqual (0, tag.BeatsPerMinute, "Initial (Zero): " + m);
			});
			
			tag.BeatsPerMinute = 199;
			
			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (199, tag.BeatsPerMinute, "Value Set: " + m);
			});
			
			tag.BeatsPerMinute = 0;

			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.AreEqual (0, t.BeatsPerMinute, "Value Cleared (Zero): " + m);
			});
		}
		
		[Test]
		public void TestConductor ()
		{
			Ape.Tag tag = new Ape.Tag ();

			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.Conductor, "Initial (Null): " + m);
			});
			
			tag.Conductor = val_sing;
			
			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.Conductor, "Value Set (!Null): " + m);
			});
			
			tag.Conductor = string.Empty;

			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.Conductor, "Value Cleared (Null): " + m);
			});
		}
		
		[Test]
		public void TestCopyright ()
		{
			Ape.Tag tag = new Ape.Tag ();

			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.Copyright, "Initial (Null): " + m);
			});
			
			tag.Copyright = val_sing;
			
			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.Copyright, "Value Set (!Null): " + m);
			});
			
			tag.Copyright = string.Empty;

			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.Copyright, "Value Cleared (Null): " + m);
			});
		}
		
		[Test]
		public void TestPictures ()
		{
			Ape.Tag tag = new Ape.Tag ();
			
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
			
			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.AreEqual (0, t.Pictures.Length, "Initial (Zero): " + m);
			});
			
			tag.Pictures = pictures;
			
			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
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
			
			tag.Pictures = new Picture [0];
			
			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.AreEqual (0, t.Pictures.Length, "Value Cleared (Zero): " + m);
			});
		}
		
		[Test]
		public void TestMusicBrainzArtistID ()
		{
			Ape.Tag tag = new Ape.Tag ();

			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzArtistId, "Initial (Null): " + m);
			});
			
			tag.MusicBrainzArtistId = val_sing;
			
			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.MusicBrainzArtistId, "Value Set (!Null): " + m);
			});
			
			tag.MusicBrainzArtistId = string.Empty;

			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzArtistId, "Value Cleared (Null): " + m);
			});
		}

		[Test]
		public void TestMusicBrainzReleaseGroupID()
		{
			Ape.Tag tag = new Ape.Tag();

			TagTestWithSave(ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsTrue(t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull(t.MusicBrainzReleaseGroupId, "Initial (Null): " + m);
			});

			tag.MusicBrainzReleaseGroupId = val_sing;

			TagTestWithSave(ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsFalse(t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual(val_sing, t.MusicBrainzReleaseGroupId, "Value Set (!Null): " + m);
			});

			tag.MusicBrainzReleaseGroupId = string.Empty;

			TagTestWithSave(ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsTrue(t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull(t.MusicBrainzReleaseGroupId, "Value Cleared (Null): " + m);
			});
		}

		[Test]
		public void TestMusicBrainzReleaseID ()
		{
			Ape.Tag tag = new Ape.Tag ();

			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzReleaseId, "Initial (Null): " + m);
			});
			
			tag.MusicBrainzReleaseId = val_sing;
			
			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.MusicBrainzReleaseId, "Value Set (!Null): " + m);
			});
			
			tag.MusicBrainzReleaseId = string.Empty;

			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzReleaseId, "Value Cleared (Null): " + m);
			});
		}
		
		[Test]
		public void TestMusicBrainzReleaseArtistID ()
		{
			Ape.Tag tag = new Ape.Tag ();

			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzReleaseArtistId, "Initial (Null): " + m);
			});
			
			tag.MusicBrainzReleaseArtistId = val_sing;
			
			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.MusicBrainzReleaseArtistId, "Value Set (!Null): " + m);
			});
			
			tag.MusicBrainzReleaseArtistId = string.Empty;

			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzReleaseArtistId, "Value Cleared (Null): " + m);
			});
		}
		
		[Test]
		public void TestMusicBrainzTrackID ()
		{
			Ape.Tag tag = new Ape.Tag ();

			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzTrackId, "Initial (Null): " + m);
			});
			
			tag.MusicBrainzTrackId = val_sing;
			
			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.MusicBrainzTrackId, "Value Set (!Null): " + m);
			});
			
			tag.MusicBrainzTrackId = string.Empty;

			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzTrackId, "Value Cleared (Null): " + m);
			});
		}
		
		[Test]
		public void TestMusicBrainzDiscID ()
		{
			Ape.Tag tag = new Ape.Tag ();

			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzDiscId, "Initial (Null): " + m);
			});
			
			tag.MusicBrainzDiscId = val_sing;
			
			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.MusicBrainzDiscId, "Value Set (!Null): " + m);
			});
			
			tag.MusicBrainzDiscId = string.Empty;

			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzDiscId, "Value Cleared (Null): " + m);
			});
		}
		
		[Test]
		public void TestMusicIPPUID ()
		{
			Ape.Tag tag = new Ape.Tag ();

			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.MusicIpId, "Initial (Null): " + m);
			});
			
			tag.MusicIpId = val_sing;
			
			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.MusicIpId, "Value Set (!Null): " + m);
			});
			
			tag.MusicIpId = string.Empty;

			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.MusicIpId, "Value Cleared (Null): " + m);
			});
		}
		
		[Test]
		public void TestAmazonID ()
		{
			Ape.Tag tag = new Ape.Tag ();

			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.AmazonId, "Initial (Null): " + m);
			});
			
			tag.AmazonId = val_sing;
			
			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.AmazonId, "Value Set (!Null): " + m);
			});
			
			tag.AmazonId = string.Empty;

			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.AmazonId, "Value Cleared (Null): " + m);
			});
		}
				
		[Test]
		public void TestMusicBrainzReleaseStatus ()
		{
			Ape.Tag tag = new Ape.Tag ();

			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzReleaseStatus, "Initial (Null): " + m);
			});
			
			tag.MusicBrainzReleaseStatus = val_sing;
			
			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.MusicBrainzReleaseStatus, "Value Set (!Null): " + m);
			});
			
			tag.MusicBrainzReleaseStatus = string.Empty;

			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzReleaseStatus, "Value Cleared (Null): " + m);
			});
		}
				
		[Test]
		public void TestMusicBrainzReleaseType ()
		{
			Ape.Tag tag = new Ape.Tag ();

			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzReleaseType, "Initial (Null): " + m);
			});
			
			tag.MusicBrainzReleaseType = val_sing;
			
			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.MusicBrainzReleaseType, "Value Set (!Null): " + m);
			});
			
			tag.MusicBrainzReleaseType = string.Empty;

			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzReleaseType, "Value Cleared (Null): " + m);
			});
		}
				
		[Test]
		public void TestMusicBrainzReleaseCountry ()
		{
			Ape.Tag tag = new Ape.Tag ();

			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzReleaseCountry, "Initial (Null): " + m);
			});
			
			tag.MusicBrainzReleaseCountry = val_sing;
			
			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.MusicBrainzReleaseCountry, "Value Set (!Null): " + m);
			});
			
			tag.MusicBrainzReleaseCountry = string.Empty;

			TagTestWithSave (ref tag, delegate (Ape.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzReleaseCountry, "Value Cleared (Null): " + m);
			});
		}
		
		[Test]
		public void TestClear ()
		{
			Ape.Tag tag = new Ape.Tag ();
			
			tag.Title = "A";
			tag.Performers = new string [] {"B"};
			tag.AlbumArtists = new string [] {"C"};
			tag.Composers = new string [] {"D"};
			tag.Album = "E";
			tag.Comment = "F";
			tag.Genres = new string [] {"Blues"};
			tag.Year = 123;
			tag.Track = 234;
			tag.TrackCount = 234;
			tag.Disc = 234;
			tag.DiscCount = 234;
			tag.Lyrics = "G";
			tag.Grouping = "H";
			tag.BeatsPerMinute = 234;
			tag.Conductor = "I";
			tag.Copyright = "J";
			tag.Pictures = new Picture [] {new Picture (TestPath.Covers + "sample_a.png") };
			
			Assert.IsFalse (tag.IsEmpty, "Should be full.");
			tag.Clear ();
			
			Assert.IsNull (tag.Title, "Title");
			Assert.AreEqual (0, tag.Performers.Length, "Performers");
			Assert.AreEqual (0, tag.AlbumArtists.Length, "AlbumArtists");
			Assert.AreEqual (0, tag.Composers.Length, "Composers");
			Assert.IsNull (tag.Album, "Album");
			Assert.IsNull (tag.Comment, "Comment");
			Assert.AreEqual (0, tag.Genres.Length, "Genres");
			Assert.AreEqual (0, tag.Year, "Year");
			Assert.AreEqual (0, tag.Track, "Track");
			Assert.AreEqual (0, tag.TrackCount, "TrackCount");
			Assert.AreEqual (0, tag.Disc, "Disc");
			Assert.AreEqual (0, tag.DiscCount, "DiscCount");
			Assert.IsNull (tag.Lyrics, "Lyrics");
			Assert.IsNull (tag.Comment, "Comment");
			Assert.AreEqual (0, tag.BeatsPerMinute, "BeatsPerMinute");
			Assert.IsNull (tag.Conductor, "Conductor");
			Assert.IsNull (tag.Copyright, "Copyright");
			Assert.AreEqual (0, tag.Pictures.Length, "Pictures");
			Assert.IsTrue (tag.IsEmpty, "Should be empty.");
		}
		
		delegate void TagTestFunc (Ape.Tag tag, string msg);
		
		void TagTestWithSave (ref Ape.Tag tag,
		                      TagTestFunc testFunc)
		{
			testFunc (tag, "Before Save");
			tag = new Ape.Tag (tag.Render ());
			testFunc (tag, "After Save");
		}
	}
}
