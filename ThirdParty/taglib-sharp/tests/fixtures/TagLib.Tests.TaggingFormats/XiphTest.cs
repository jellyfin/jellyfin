using System;
using System.Security.Cryptography;
using NUnit.Framework;
using TagLib.Ogg;

namespace TagLib.Tests.TaggingFormats
{   
	[TestFixture]
	public class OggTest
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
			Ogg.XiphComment tag = new Ogg.XiphComment ();
			
			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.Title, "Initial (Null): " + m);
			});
			
			tag.Title = val_sing;
			
			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.Title, "Value Set (!Null): " + m);
			});
			
			tag.Title = string.Empty;
			
			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.Title, "Value Cleared (Null): " + m);
			});

		}
		
		[Test]
		public void TestPerformers ()
		{
			Ogg.XiphComment tag = new Ogg.XiphComment ();
			
			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.AreEqual (0, t.Performers.Length, "Initial (Zero): " + m);
			});
			
			tag.Performers = val_mult;
			
			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_mult.Length, t.Performers.Length, "Value Set: " + m);
				for (int i = 0; i < val_mult.Length; i ++) {
					Assert.AreEqual (val_mult [i], t.Performers [i], "Value Set: " + m);
				}
			});
			
			tag.Performers = new string [0];
			
			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.AreEqual (0, t.Performers.Length, "Value Cleared (Zero): " + m);
			});
		}
		
		[Test]
		public void TestAlbumArtists ()
		{
			Ogg.XiphComment tag = new Ogg.XiphComment ();
			
			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.AreEqual (0, t.AlbumArtists.Length, "Initial (Zero): " + m);
			});
			
			tag.AlbumArtists = val_mult;
			
			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_mult.Length, t.AlbumArtists.Length, "Value Set: " + m);
				for (int i = 0; i < val_mult.Length; i ++) {
					Assert.AreEqual (val_mult [i], t.AlbumArtists [i], "Value Set: " + m);
				}
			});
			
			tag.AlbumArtists = new string [0];
			
			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.AreEqual (0, t.AlbumArtists.Length, "Value Cleared (Zero): " + m);
			});
		}
		
		[Test]
		public void TestComposers ()
		{
			Ogg.XiphComment tag = new Ogg.XiphComment ();
			
			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.AreEqual (0, t.Composers.Length, "Initial (Zero): " + m);
			});
			
			tag.Composers = val_mult;
			
			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_mult.Length, t.Composers.Length, "Value Set: " + m);
				for (int i = 0; i < val_mult.Length; i ++) {
					Assert.AreEqual (val_mult [i], t.Composers [i], "Value Set: " + m);
				}
			});
			
			tag.Composers = new string [0];
			
			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.AreEqual (0, t.Composers.Length, "Value Cleared (Zero): " + m);
			});
		}
		
		[Test]
		public void TestAlbum ()
		{
			Ogg.XiphComment tag = new Ogg.XiphComment ();
			
			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.Album, "Initial (Null): " + m);
			});
			
			tag.Album = val_sing;
			
			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.Album, "Value Set (!Null): " + m);
			});
			
			tag.Album = string.Empty;
			
			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.Album, "Value Cleared (Null): " + m);
			});
		}
		
		[Test]
		public void TestComment ()
		{
			Ogg.XiphComment tag = new Ogg.XiphComment ();

			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.Comment, "Initial (Null): " + m);
			});
			
			tag.Comment = val_sing;
			
			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.Comment, "Value Set (!Null): " + m);
			});
			
			tag.Comment = string.Empty;

			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.Comment, "Value Cleared (Null): " + m);
			});
		}
		
		[Test]
		public void TestGenres ()
		{
			Ogg.XiphComment tag = new Ogg.XiphComment ();

			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.AreEqual (0, t.Genres.Length, "Initial (Zero): " + m);
			});
			
			tag.Genres = val_gnre;
			
			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_gnre.Length, t.Genres.Length, "Value Set: " + m);
				for (int i = 0; i < val_gnre.Length; i ++) {
					Assert.AreEqual (val_gnre [i], t.Genres [i], "Value Set: " + m);
				}
			});
			
			tag.Genres = val_mult;
			
			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_mult.Length, t.Genres.Length, "Value Set: " + m);
				for (int i = 0; i < val_mult.Length; i ++) {
					Assert.AreEqual (val_mult [i], t.Genres [i], "Value Set: " + m);
				}
			});
			
			tag.Genres = new string [0];

			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.AreEqual (0, t.Genres.Length, "Value Cleared (Zero): " + m);
			});
		}
		
		[Test]
		public void TestYear ()
		{
			Ogg.XiphComment tag = new Ogg.XiphComment ();

			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.AreEqual (0, tag.Year, "Initial (Zero): " + m);
			});
			
			tag.Year = 1999;
			
			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (1999, tag.Year, "Value Set: " + m);
			});
			
			tag.Year = 0;

			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.AreEqual (0, t.Year, "Value Cleared (Zero): " + m);
			});
		}
		
		[Test]
		public void TestTrack ()
		{
			Ogg.XiphComment tag = new Ogg.XiphComment ();

			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.AreEqual (0, tag.Track, "Initial (Zero): " + m);
			});
			
			tag.Track = 199;
			
			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (199, tag.Track, "Value Set: " + m);
			});
			
			tag.Track = 0;

			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.AreEqual (0, t.Track, "Value Cleared (Zero): " + m);
			});
		}
		
		[Test]
		public void TestTrackCount ()
		{
			Ogg.XiphComment tag = new Ogg.XiphComment ();

			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.AreEqual (0, tag.TrackCount, "Initial (Zero): " + m);
			});
			
			tag.TrackCount = 199;
			
			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (199, tag.TrackCount, "Value Set: " + m);
			});
			
			tag.TrackCount = 0;

			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.AreEqual (0, t.TrackCount, "Value Cleared (Zero): " + m);
			});
		}
		
		[Test]
		public void TestDisc ()
		{
			Ogg.XiphComment tag = new Ogg.XiphComment ();

			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.AreEqual (0, tag.Disc, "Initial (Zero): " + m);
			});
			
			tag.Disc = 199;
			
			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (199, tag.Disc, "Value Set: " + m);
			});
			
			tag.Disc = 0;

			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.AreEqual (0, t.Disc, "Value Cleared (Zero): " + m);
			});
		}
		
		[Test]
		public void TestDiscCount ()
		{
			Ogg.XiphComment tag = new Ogg.XiphComment ();

			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.AreEqual (0, tag.DiscCount, "Initial (Zero): " + m);
			});
			
			tag.DiscCount = 199;
			
			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (199, tag.DiscCount, "Value Set: " + m);
			});
			
			tag.DiscCount = 0;

			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.AreEqual (0, t.DiscCount, "Value Cleared (Zero): " + m);
			});
		}
		
		[Test]
		public void TestLyrics ()
		{
			Ogg.XiphComment tag = new Ogg.XiphComment ();
			
			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.Lyrics, "Initial (Null): " + m);
			});
			
			tag.Lyrics = val_sing;
			
			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.Lyrics, "Value Set (!Null): " + m);
			});
			
			tag.Lyrics = string.Empty;

			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.Lyrics, "Value Cleared (Null): " + m);
			});
		}
		
		[Test]
		public void TestGrouping ()
		{
			Ogg.XiphComment tag = new Ogg.XiphComment ();

			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.Grouping, "Initial (Null): " + m);
			});
			
			tag.Grouping = val_sing;
			
			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.Grouping, "Value Set (!Null): " + m);
			});
			
			tag.Grouping = string.Empty;

			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.Grouping, "Value Cleared (Null): " + m);
			});
		}
		
		[Test]
		public void TestBeatsPerMinute ()
		{
			Ogg.XiphComment tag = new Ogg.XiphComment ();
		
			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.AreEqual (0, tag.BeatsPerMinute, "Initial (Zero): " + m);
			});
			
			tag.BeatsPerMinute = 199;
			
			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (199, tag.BeatsPerMinute, "Value Set: " + m);
			});
			
			tag.BeatsPerMinute = 0;

			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.AreEqual (0, t.BeatsPerMinute, "Value Cleared (Zero): " + m);
			});
		}
		
		[Test]
		public void TestConductor ()
		{
			Ogg.XiphComment tag = new Ogg.XiphComment ();

			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.Conductor, "Initial (Null): " + m);
			});
			
			tag.Conductor = val_sing;
			
			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.Conductor, "Value Set (!Null): " + m);
			});
			
			tag.Conductor = string.Empty;

			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.Conductor, "Value Cleared (Null): " + m);
			});
		}
		
		[Test]
		public void TestCopyright ()
		{
			Ogg.XiphComment tag = new Ogg.XiphComment ();

			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.Copyright, "Initial (Null): " + m);
			});
			
			tag.Copyright = val_sing;
			
			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.Copyright, "Value Set (!Null): " + m);
			});
			
			tag.Copyright = string.Empty;

			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.Copyright, "Value Cleared (Null): " + m);
			});
		}
		
		[Test]
		public void TestPictures ()
		{
			Ogg.XiphComment tag = new Ogg.XiphComment ();
			
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
			
			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.AreEqual (0, t.Pictures.Length, "Initial (Zero): " + m);
			});
			
			tag.Pictures = pictures;
			
			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue (!t.IsEmpty, "Value Set (IsEmpty): " + m);
			});
			
			tag.Pictures = new Picture [0];
			
			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.AreEqual (0, t.Pictures.Length, "Value Cleared (Zero): " + m);
			});

			// Test that COVERART fields are parsed in Pictures property
			string[] pictureStrings = new string[pictures.Length];
			for (int i = 0; i < 6; ++i)
				pictureStrings[i] = Convert.ToBase64String(pictures[i].Data.Data);
			tag.SetField("COVERART", pictureStrings);

			var parsedPictures = tag.Pictures;
			Assert.IsTrue(!tag.IsEmpty, "Legacy Value Set (IsEmpty)");
			Assert.AreEqual(6, parsedPictures.Length, "Legacy Value Set (Length)");
			
			TagTestWithSave(ref tag, delegate (Ogg.XiphComment t, string m) {
				// COVERART should be preserved
				Assert.AreEqual(6, t.GetField("COVERART").Length, "Legacy Field Set (Length): " + m);
			});

			// Setting the pictures array should replace COVERART with METADATA_BLOCK_PICTURE
			tag.Pictures = pictures;

			TagTestWithSave(ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.AreEqual(0, t.GetField("COVERART").Length, "Legacy Field Set (Length): " + m);
				Assert.AreEqual(6, t.GetField("METADATA_BLOCK_PICTURE").Length, "Current Field Set (Length): " + m);
			});

			// The user should be able to clear the pictures array
			tag.Pictures = null;

			TagTestWithSave(ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.AreEqual(0, t.Pictures.Length, "Pictures Length (null): " + m);
			});
		}
		
		[Test]
		public void TestMusicBrainzArtistID ()
		{
			Ogg.XiphComment tag = new Ogg.XiphComment ();

			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzArtistId, "Initial (Null): " + m);
			});
			
			tag.MusicBrainzArtistId = val_sing;
			
			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.MusicBrainzArtistId, "Value Set (!Null): " + m);
			});
			
			tag.MusicBrainzArtistId = string.Empty;

			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzArtistId, "Value Cleared (Null): " + m);
			});
		}

		[Test]
		public void TestMusicBrainzReleaseGroupID()
		{
			Ogg.XiphComment tag = new Ogg.XiphComment();

			TagTestWithSave(ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue(t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull(t.MusicBrainzReleaseGroupId, "Initial (Null): " + m);
			});

			tag.MusicBrainzReleaseGroupId = val_sing;

			TagTestWithSave(ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsFalse(t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual(val_sing, t.MusicBrainzReleaseGroupId, "Value Set (!Null): " + m);
			});

			tag.MusicBrainzReleaseGroupId = string.Empty;

			TagTestWithSave(ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue(t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull(t.MusicBrainzReleaseGroupId, "Value Cleared (Null): " + m);
			});
		}

		[Test]
		public void TestMusicBrainzReleaseID ()
		{
			Ogg.XiphComment tag = new Ogg.XiphComment ();

			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzReleaseId, "Initial (Null): " + m);
			});
			
			tag.MusicBrainzReleaseId = val_sing;
			
			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.MusicBrainzReleaseId, "Value Set (!Null): " + m);
			});
			
			tag.MusicBrainzReleaseId = string.Empty;

			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzReleaseId, "Value Cleared (Null): " + m);
			});
		}
		
		[Test]
		public void TestMusicBrainzReleaseArtistID ()
		{
			Ogg.XiphComment tag = new Ogg.XiphComment ();

			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzReleaseArtistId, "Initial (Null): " + m);
			});
			
			tag.MusicBrainzReleaseArtistId = val_sing;
			
			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.MusicBrainzReleaseArtistId, "Value Set (!Null): " + m);
			});
			
			tag.MusicBrainzReleaseArtistId = string.Empty;

			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzReleaseArtistId, "Value Cleared (Null): " + m);
			});
		}
		
		[Test]
		public void TestMusicBrainzTrackID ()
		{
			Ogg.XiphComment tag = new Ogg.XiphComment ();

			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzTrackId, "Initial (Null): " + m);
			});
			
			tag.MusicBrainzTrackId = val_sing;
			
			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.MusicBrainzTrackId, "Value Set (!Null): " + m);
			});
			
			tag.MusicBrainzTrackId = string.Empty;

			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzTrackId, "Value Cleared (Null): " + m);
			});
		}
		
		[Test]
		public void TestMusicBrainzDiscID ()
		{
			Ogg.XiphComment tag = new Ogg.XiphComment ();

			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzDiscId, "Initial (Null): " + m);
			});
			
			tag.MusicBrainzDiscId = val_sing;
			
			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.MusicBrainzDiscId, "Value Set (!Null): " + m);
			});
			
			tag.MusicBrainzDiscId = string.Empty;

			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzDiscId, "Value Cleared (Null): " + m);
			});
		}
		
		[Test]
		public void TestMusicIPPUID ()
		{
			Ogg.XiphComment tag = new Ogg.XiphComment ();

			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.MusicIpId, "Initial (Null): " + m);
			});
			
			tag.MusicIpId = val_sing;
			
			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.MusicIpId, "Value Set (!Null): " + m);
			});
			
			tag.MusicIpId = string.Empty;

			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.MusicIpId, "Value Cleared (Null): " + m);
			});
		}
		
		[Test]
		public void TestAmazonID ()
		{
			Ogg.XiphComment tag = new Ogg.XiphComment ();

			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.AmazonId, "Initial (Null): " + m);
			});
			
			tag.AmazonId = val_sing;
			
			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.AmazonId, "Value Set (!Null): " + m);
			});
			
			tag.AmazonId = string.Empty;

			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.AmazonId, "Value Cleared (Null): " + m);
			});
		}
				
		[Test]
		public void TestMusicBrainzReleaseStatus ()
		{
			Ogg.XiphComment tag = new Ogg.XiphComment ();

			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzReleaseStatus, "Initial (Null): " + m);
			});
			
			tag.MusicBrainzReleaseStatus = val_sing;
			
			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.MusicBrainzReleaseStatus, "Value Set (!Null): " + m);
			});
			
			tag.MusicBrainzReleaseStatus = string.Empty;

			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzReleaseStatus, "Value Cleared (Null): " + m);
			});
		}
				
		[Test]
		public void TestMusicBrainzReleaseType ()
		{
			Ogg.XiphComment tag = new Ogg.XiphComment ();

			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzReleaseType, "Initial (Null): " + m);
			});
			
			tag.MusicBrainzReleaseType = val_sing;
			
			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.MusicBrainzReleaseType, "Value Set (!Null): " + m);
			});
			
			tag.MusicBrainzReleaseType = string.Empty;

			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzReleaseType, "Value Cleared (Null): " + m);
			});
		}
				
		[Test]
		public void TestMusicBrainzReleaseCountry ()
		{
			Ogg.XiphComment tag = new Ogg.XiphComment ();

			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzReleaseCountry, "Initial (Null): " + m);
			});
			
			tag.MusicBrainzReleaseCountry = val_sing;
			
			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.MusicBrainzReleaseCountry, "Value Set (!Null): " + m);
			});
			
			tag.MusicBrainzReleaseCountry = string.Empty;

			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzReleaseCountry, "Value Cleared (Null): " + m);
			});
		}

		[Test]
		public void TestIsCompilation ()
		{
			var tag = new Ogg.XiphComment ();

			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsFalse (t.IsCompilation, "Initial (False): " + m);
			});

			tag.IsCompilation = true;

			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.IsTrue (t.IsCompilation, "Value Set (True): " + m);
			});

			tag.IsCompilation = false;

			TagTestWithSave (ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsFalse (t.IsCompilation, "Value Cleared (False): " + m);
			});
		}

		[Test]
		public void TestInitialKey()
		{
			Ogg.XiphComment tag = new Ogg.XiphComment();

			TagTestWithSave(ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue(t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull(t.InitialKey, "Initial (Null): " + m);
			});

			tag.InitialKey = val_sing;

			TagTestWithSave(ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsFalse(t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual(val_sing, t.InitialKey, "Value Set (!Null): " + m);
			});

			tag.InitialKey = string.Empty;

			TagTestWithSave(ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue(t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull(t.InitialKey, "Value Cleared (Null): " + m);
			});
		}

		[Test]
		public void TestISRC()
		{
			Ogg.XiphComment tag = new Ogg.XiphComment();

			TagTestWithSave(ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue(t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull(t.ISRC, "Initial (Null): " + m);
			});

			tag.ISRC = val_sing;

			TagTestWithSave(ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsFalse(t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual(val_sing, t.ISRC, "Value Set (!Null): " + m);
			});

			tag.ISRC = string.Empty;

			TagTestWithSave(ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue(t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull(t.ISRC, "Value Cleared (Null): " + m);
			});
		}

		[Test]
		public void TestPublisher()
		{
			Ogg.XiphComment tag = new Ogg.XiphComment();

			TagTestWithSave(ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue(t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull(t.Publisher, "Initial (Null): " + m);
			});

			tag.Publisher = val_sing;

			TagTestWithSave(ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsFalse(t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual(val_sing, t.Publisher, "Value Set (!Null): " + m);
			});

			tag.Publisher = string.Empty;

			TagTestWithSave(ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue(t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull(t.Publisher, "Value Cleared (Null): " + m);
			});
		}

		[Test]
		public void TestRemixedBy()
		{
			Ogg.XiphComment tag = new Ogg.XiphComment();

			TagTestWithSave(ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue(t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull(t.RemixedBy, "Initial (Null): " + m);
			});

			tag.RemixedBy = val_sing;

			TagTestWithSave(ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsFalse(t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual(val_sing, t.RemixedBy, "Value Set (!Null): " + m);
			});

			tag.RemixedBy = string.Empty;

			TagTestWithSave(ref tag, delegate (Ogg.XiphComment t, string m) {
				Assert.IsTrue(t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull(t.RemixedBy, "Value Cleared (Null): " + m);
			});
		}

		[Test]
		public void TestClear ()
		{
			Ogg.XiphComment tag = new Ogg.XiphComment ();
			
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
			tag.InitialKey = "K";
			tag.Publisher = "L";
			tag.ISRC = "M";
			tag.RemixedBy = "N";
			
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
			Assert.IsNull (tag.InitialKey, "InitialKey");
			Assert.IsNull (tag.Publisher, "Publisher");
			Assert.IsNull (tag.ISRC, "ISRC");
			Assert.IsNull (tag.RemixedBy, "RemixedBy");
		}
		
		delegate void TagTestFunc (Ogg.XiphComment tag, string msg);
		
		void TagTestWithSave (ref Ogg.XiphComment tag,
		                      TagTestFunc testFunc)
		{
			testFunc (tag, "Before Save");
			tag = new Ogg.XiphComment (tag.Render (true));
			testFunc (tag, "After Save");
		}
	}
}
