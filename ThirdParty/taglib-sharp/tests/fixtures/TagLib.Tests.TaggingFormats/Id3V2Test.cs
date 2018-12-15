using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using NUnit.Framework;
using TagLib.Id3v2;

namespace TagLib.Tests.TaggingFormats
{
	[TestFixture]
	public class Id3V2Test
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
			Id3v2.Tag tag = new Id3v2.Tag ();
			for (byte version = 2; version <= 4; version ++) {
				tag.Version = version;

				TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
					Assert.IsNull (t.Title, "Initial (Null): " + m);
				});
				
				tag.Title = val_sing;
				
				TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
					Assert.AreEqual (val_sing, t.Title, "Value Set (!Null): " + m);
				});
				
				tag.Title = string.Empty;

				TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
					Assert.IsNull (t.Title, "Value Cleared (Null): " + m);
				});
			}
		}
		
		[Test]
		public void TestPerformers ()
		{
			Id3v2.Tag tag = new Id3v2.Tag ();
			for (byte version = 2; version <= 4; version ++) {
				tag.Version = version;

				TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
					Assert.AreEqual (0, t.Performers.Length, "Initial (Zero): " + m);
				});
				
				tag.Performers = val_mult;
				
				TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
					Assert.AreEqual (val_mult.Length, t.Performers.Length, "Value Set: " + m);
					for (int i = 0; i < val_mult.Length; i ++) {
						Assert.AreEqual (val_mult [i], t.Performers [i], "Value Set: " + m);
					}
				});
				
				tag.Performers = new string [0];

				TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
					Assert.AreEqual (0, t.Performers.Length, "Value Cleared (Zero): " + m);
				});
			}
		}
		
		[Test]
		public void TestAlbumArtists ()
		{
			Id3v2.Tag tag = new Id3v2.Tag ();
			for (byte version = 2; version <= 4; version ++) {
				tag.Version = version;

				TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
					Assert.AreEqual (0, t.AlbumArtists.Length, "Initial (Zero): " + m);
				});
				
				tag.AlbumArtists = val_mult;
				
				TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
					Assert.AreEqual (val_mult.Length, t.AlbumArtists.Length, "Value Set: " + m);
					for (int i = 0; i < val_mult.Length; i ++) {
						Assert.AreEqual (val_mult [i], t.AlbumArtists [i], "Value Set: " + m);
					}
				});
				
				tag.AlbumArtists = new string [0];

				TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
					Assert.AreEqual (0, t.AlbumArtists.Length, "Value Cleared (Zero): " + m);
				});
			}
		}
		
		[Test]
		public void TestComposers ()
		{
			Id3v2.Tag tag = new Id3v2.Tag ();
			for (byte version = 2; version <= 4; version ++) {
				tag.Version = version;

				TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
					Assert.AreEqual (0, t.Composers.Length, "Initial (Zero): " + m);
				});
				
				tag.Composers = val_mult;
				
				TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
					Assert.AreEqual (val_mult.Length, t.Composers.Length, "Value Set: " + m);
					for (int i = 0; i < val_mult.Length; i ++) {
						Assert.AreEqual (val_mult [i], t.Composers [i], "Value Set: " + m);
					}
				});
				
				tag.Composers = new string [0];

				TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
					Assert.AreEqual (0, t.Composers.Length, "Value Cleared (Zero): " + m);
				});
			}
		}
		
		[Test]
		public void TestAlbum ()
		{
			Id3v2.Tag tag = new Id3v2.Tag ();
			for (byte version = 2; version <= 4; version ++) {
				tag.Version = version;

				TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
					Assert.IsNull (t.Album, "Initial (Null): " + m);
				});
				
				tag.Album = val_sing;
				
				TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
					Assert.AreEqual (val_sing, t.Album, "Value Set (!Null): " + m);
				});
				
				tag.Album = string.Empty;

				TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
					Assert.IsNull (t.Album, "Value Cleared (Null): " + m);
				});
			}
		}
		
		[Test]
		public void TestComment ()
		{
			Id3v2.Tag tag = new Id3v2.Tag ();
			for (byte version = 2; version <= 4; version ++) {
				tag.Version = version;

				TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
					Assert.IsNull (t.Comment, "Initial (Null): " + m);
				});
				
				tag.Comment = val_sing;
				
				TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
					Assert.AreEqual (val_sing, t.Comment, "Value Set (!Null): " + m);
				});
				
				tag.Comment = string.Empty;

				TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
					Assert.IsNull (t.Comment, "Value Cleared (Null): " + m);
				});
			}
		}

		[Test]
		public void TestComment4Bytes ()
		{
			// Comment data found in the wild, see bgo#607376
			var data = new byte[] { 67, 79, 77, 77, 0, 0, 0, 4, 0, 0, 0, 203, 0, 255 };
			var frame = new CommentsFrame (data, 3);
			Assert.IsEmpty (frame.Description);
			Assert.IsEmpty (frame.Text);
		}

		[Test]
		public void TestGenres ()
		{
			Id3v2.Tag tag = new Id3v2.Tag ();
			for (byte version = 2; version <= 4; version ++) {
				tag.Version = version;

				TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
					Assert.AreEqual (0, t.Genres.Length, "Initial (Zero): " + m);
				});
				
				tag.Genres = val_gnre;
				
				TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
					Assert.AreEqual (val_gnre.Length, t.Genres.Length, "Value Set: " + m);
					for (int i = 0; i < val_gnre.Length; i ++) {
						Assert.AreEqual (val_gnre [i], t.Genres [i], "Value Set: " + m);
					}
				});
				
				tag.Genres = val_mult;
				
				TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
					Assert.AreEqual (val_mult.Length, t.Genres.Length, "Value Set: " + m);
					for (int i = 0; i < val_mult.Length; i ++) {
						Assert.AreEqual (val_mult [i], t.Genres [i], "Value Set: " + m);
					}
				});
				
				tag.Genres = new string [0];

				TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
					Assert.AreEqual (0, t.Genres.Length, "Value Cleared (Zero): " + m);
				});
			}
		}
		
		[Test]
		public void TestYear ()
		{
			Id3v2.Tag tag = new Id3v2.Tag ();
			for (byte version = 2; version <= 4; version ++) {
				tag.Version = version;

				TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
					Assert.AreEqual (0, tag.Year, "Initial (Zero): " + m);
				});
				
				tag.Year = 1999;
				
				TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
					Assert.AreEqual (1999, tag.Year, "Value Set: " + m);
				});
				
				tag.Year = 20000;

				TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
					Assert.AreEqual (0, t.Year, "Value Cleared (Zero): " + m);
				});
			}
		}
		
		[Test]
		public void TestTrack ()
		{
			Id3v2.Tag tag = new Id3v2.Tag ();
			for (byte version = 2; version <= 4; version ++) {
				tag.Version = version;

				TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
					Assert.AreEqual (0, tag.Track, "Initial (Zero): " + m);
				});
				
				tag.Track = 199;
				
				TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
					Assert.AreEqual (199, tag.Track, "Value Set: " + m);
				});
				
				tag.Track = 0;

				TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
					Assert.AreEqual (0, t.Track, "Value Cleared (Zero): " + m);
				});
			}
		}
		
		[Test]
		public void TestTrackCount ()
		{
			Id3v2.Tag tag = new Id3v2.Tag ();
			for (byte version = 2; version <= 4; version ++) {
				tag.Version = version;

				TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
					Assert.AreEqual (0, tag.TrackCount, "Initial (Zero): " + m);
				});
				
				tag.TrackCount = 199;
				
				TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
					Assert.AreEqual (199, tag.TrackCount, "Value Set: " + m);
				});
				
				tag.TrackCount = 0;

				TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
					Assert.AreEqual (0, t.TrackCount, "Value Cleared (Zero): " + m);
				});
			}
		}
		
		[Test]
		public void TestDisc ()
		{
			Id3v2.Tag tag = new Id3v2.Tag ();
			for (byte version = 2; version <= 4; version ++) {
				tag.Version = version;

				TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
					Assert.AreEqual (0, tag.Disc, "Initial (Zero): " + m);
				});
				
				tag.Disc = 199;
				
				TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
					Assert.AreEqual (199, tag.Disc, "Value Set: " + m);
				});
				
				tag.Disc = 0;

				TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
					Assert.AreEqual (0, t.Disc, "Value Cleared (Zero): " + m);
				});
			}
		}
		
		[Test]
		public void TestDiscCount ()
		{
			Id3v2.Tag tag = new Id3v2.Tag ();
			for (byte version = 2; version <= 4; version ++) {
				tag.Version = version;

				TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
					Assert.AreEqual (0, tag.DiscCount, "Initial (Zero): " + m);
				});
				
				tag.DiscCount = 199;
				
				TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
					Assert.AreEqual (199, tag.DiscCount, "Value Set: " + m);
				});
				
				tag.DiscCount = 0;

				TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
					Assert.AreEqual (0, t.DiscCount, "Value Cleared (Zero): " + m);
				});
			}
		}
		
		[Test]
		public void TestLyrics ()
		{
			Id3v2.Tag tag = new Id3v2.Tag ();
			for (byte version = 2; version <= 4; version ++) {
				tag.Version = version;

				TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
					Assert.IsNull (t.Lyrics, "Initial (Null): " + m);
				});
				
				tag.Lyrics = val_sing;
				
				TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
					Assert.AreEqual (val_sing, t.Lyrics, "Value Set (!Null): " + m);
				});
				
				tag.Lyrics = string.Empty;

				TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
					Assert.IsNull (t.Lyrics, "Value Cleared (Null): " + m);
				});
			}
		}
		
		[Test]
		public void TestGrouping ()
		{
			Id3v2.Tag tag = new Id3v2.Tag ();
			for (byte version = 2; version <= 4; version ++) {
				tag.Version = version;

				TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
					Assert.IsNull (t.Grouping, "Initial (Null): " + m);
				});
				
				tag.Grouping = val_sing;
				
				TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
					Assert.AreEqual (val_sing, t.Grouping, "Value Set (!Null): " + m);
				});
				
				tag.Grouping = string.Empty;

				TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
					Assert.IsNull (t.Grouping, "Value Cleared (Null): " + m);
				});
			}
		}
		
		[Test]
		public void TestBeatsPerMinute ()
		{
			Id3v2.Tag tag = new Id3v2.Tag ();
			for (byte version = 2; version <= 4; version ++) {
				tag.Version = version;

				TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
					Assert.AreEqual (0, tag.BeatsPerMinute, "Initial (Zero): " + m);
				});
				
				tag.BeatsPerMinute = 199;
				
				TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
					Assert.AreEqual (199, tag.BeatsPerMinute, "Value Set: " + m);
				});
				
				tag.BeatsPerMinute = 0;

				TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
					Assert.AreEqual (0, t.BeatsPerMinute, "Value Cleared (Zero): " + m);
				});
			}
		}
		
		[Test]
		public void TestConductor ()
		{
			Id3v2.Tag tag = new Id3v2.Tag ();
			for (byte version = 2; version <= 4; version ++) {
				tag.Version = version;

				TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
					Assert.IsNull (t.Conductor, "Initial (Null): " + m);
				});
				
				tag.Conductor = val_sing;
				
				TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
					Assert.AreEqual (val_sing, t.Conductor, "Value Set (!Null): " + m);
				});
				
				tag.Conductor = string.Empty;

				TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
					Assert.IsNull (t.Conductor, "Value Cleared (Null): " + m);
				});
			}
		}
		
		[Test]
		public void TestCopyright ()
		{
			Id3v2.Tag tag = new Id3v2.Tag ();
			for (byte version = 2; version <= 4; version ++) {
				tag.Version = version;

				TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
					Assert.IsNull (t.Copyright, "Initial (Null): " + m);
				});
				
				tag.Copyright = val_sing;
				
				TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
					Assert.AreEqual (val_sing, t.Copyright, "Value Set (!Null): " + m);
				});
				
				tag.Copyright = string.Empty;

				TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
					Assert.IsNull (t.Copyright, "Value Cleared (Null): " + m);
				});
			}
		}
		
		[Test]
		public void TestPictures ()
		{
			Id3v2.Tag tag = new Id3v2.Tag ();
			
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
			
			for (byte version = 2; version <= 4; version ++) {
				tag.Version = version;

				TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
					Assert.AreEqual (0, t.Pictures.Length, "Initial (Zero): " + m);
				});
				
				tag.Pictures = pictures;
				
				TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
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

				TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
					Assert.AreEqual (0, t.Pictures.Length, "Value Cleared (Zero): " + m);
				});
			}
		}
			
		[Test]
		public void TestIsCompilation ()
		{
			Id3v2.Tag tag = new Id3v2.Tag ();
			for (byte version = 2; version <= 4; version ++) {
				tag.Version = version;

				TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
					Assert.IsFalse (t.IsCompilation, "Initial (False): " + m);
				});
				
				tag.IsCompilation = true;
				
				TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
					Assert.IsTrue (t.IsCompilation, "Value Set (True): " + m);
				});
				
				tag.IsCompilation = false;

				TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
					Assert.IsFalse (t.IsCompilation, "Value Cleared (False): " + m);
				});
			}
		}
		
		[Test]
		public void TestMusicBrainzArtistID ()
		{
			Id3v2.Tag tag = new Id3v2.Tag ();
			for (byte version = 2; version <= 4; version ++) {
				tag.Version = version;

			TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzArtistId, "Initial (Null): " + m);
				});
			
			tag.MusicBrainzArtistId = val_sing;
			
			TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.MusicBrainzArtistId, "Value Set (!Null): " + m);
				});
			
			tag.MusicBrainzArtistId = string.Empty;

			TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzArtistId, "Value Cleared (Null): " + m);
				});	
			}
		}
		
		[Test]
		public void TestMusicBrainzReleaseID ()
		{
			Id3v2.Tag tag = new Id3v2.Tag ();
			for (byte version = 2; version <= 4; version ++) {
				tag.Version = version;

			TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzReleaseId, "Initial (Null): " + m);
				});
			
			tag.MusicBrainzReleaseId = val_sing;
			
			TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.MusicBrainzReleaseId, "Value Set (!Null): " + m);
				});
			
			tag.MusicBrainzReleaseId = string.Empty;

			TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzReleaseId, "Value Cleared (Null): " + m);
				});
			}
		}

		[Test]
		public void TestMusicBrainzReleaseGroupID()
		{
			Id3v2.Tag tag = new Id3v2.Tag();
			for (byte version = 2; version <= 4; version++)
			{
				tag.Version = version;

				TagTestWithSave(ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsTrue(t.IsEmpty, "Initial (IsEmpty): " + m);
					Assert.IsNull(t.MusicBrainzReleaseGroupId, "Initial (Null): " + m);
				});

				tag.MusicBrainzReleaseGroupId = val_sing;

				TagTestWithSave(ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsFalse(t.IsEmpty, "Value Set (!IsEmpty): " + m);
					Assert.AreEqual(val_sing, t.MusicBrainzReleaseGroupId, "Value Set (!Null): " + m);
				});

				tag.MusicBrainzReleaseGroupId = string.Empty;

				TagTestWithSave(ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsTrue(t.IsEmpty, "Value Cleared (IsEmpty): " + m);
					Assert.IsNull(t.MusicBrainzReleaseGroupId, "Value Cleared (Null): " + m);
				});
			}
		}

		[Test]
		public void TestMusicBrainzReleaseArtistID ()
		{
			Id3v2.Tag tag = new Id3v2.Tag ();
			for (byte version = 2; version <= 4; version ++) {
				tag.Version = version;

			TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzReleaseArtistId, "Initial (Null): " + m);
				});
			
			tag.MusicBrainzReleaseArtistId = val_sing;
			
			TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.MusicBrainzReleaseArtistId, "Value Set (!Null): " + m);
				});
			
			tag.MusicBrainzReleaseArtistId = string.Empty;

			TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzReleaseArtistId, "Value Cleared (Null): " + m);
				});
			}
		}
		
		[Test]
		public void TestMusicBrainzTrackID ()
		{
			Id3v2.Tag tag = new Id3v2.Tag ();
			for (byte version = 2; version <= 4; version ++) {
				tag.Version = version;

			TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzTrackId, "Initial (Null): " + m);
				});
			
			tag.MusicBrainzTrackId = val_sing;
			
			TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.MusicBrainzTrackId, "Value Set (!Null): " + m);
				});
			
			tag.MusicBrainzTrackId = string.Empty;

			TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzTrackId, "Value Cleared (Null): " + m);
				});
			}
		}
		
		[Test]
		public void TestMusicBrainzDiscID ()
		{
			Id3v2.Tag tag = new Id3v2.Tag ();
			for (byte version = 2; version <= 4; version ++) {
				tag.Version = version;

			TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzDiscId, "Initial (Null): " + m);
				});
			
			tag.MusicBrainzDiscId = val_sing;
			
			TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.MusicBrainzDiscId, "Value Set (!Null): " + m);
				});
			
			tag.MusicBrainzDiscId = string.Empty;

			TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzDiscId, "Value Cleared (Null): " + m);
				});
			}
		}
		
		[Test]
		public void TestMusicIPPUID ()
		{
			Id3v2.Tag tag = new Id3v2.Tag ();
			for (byte version = 2; version <= 4; version ++) {
				tag.Version = version;

			TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.MusicIpId, "Initial (Null): " + m);
				});
			
			tag.MusicIpId = val_sing;
			
			TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.MusicIpId, "Value Set (!Null): " + m);
				});
			
			tag.MusicIpId = string.Empty;

			TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.MusicIpId, "Value Cleared (Null): " + m);
				});
			}
		}
		
		[Test]
		public void TestAmazonID ()
		{
			Id3v2.Tag tag = new Id3v2.Tag ();
			for (byte version = 2; version <= 4; version ++) {
				tag.Version = version;

			TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.AmazonId, "Initial (Null): " + m);
				});
			
			tag.AmazonId = val_sing;
			
			TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.AmazonId, "Value Set (!Null): " + m);
				});
				
			tag.AmazonId = string.Empty;

			TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.AmazonId, "Value Cleared (Null): " + m);
				});
			}
		}
				
		[Test]
		public void TestMusicBrainzReleaseStatus ()
		{
			Id3v2.Tag tag = new Id3v2.Tag ();
			for (byte version = 2; version <= 4; version ++) {
				tag.Version = version;

			TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzReleaseStatus, "Initial (Null): " + m);
				});
			
			tag.MusicBrainzReleaseStatus = val_sing;
			
			TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.MusicBrainzReleaseStatus, "Value Set (!Null): " + m);
				});
			
			tag.MusicBrainzReleaseStatus = string.Empty;

			TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzReleaseStatus, "Value Cleared (Null): " + m);
				});
			}
		}
				
		[Test]
		public void TestMusicBrainzReleaseType ()
		{
			Id3v2.Tag tag = new Id3v2.Tag ();
			for (byte version = 2; version <= 4; version ++) {
				tag.Version = version;

			TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzReleaseType, "Initial (Null): " + m);
				});
			
			tag.MusicBrainzReleaseType = val_sing;
			
			TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.MusicBrainzReleaseType, "Value Set (!Null): " + m);
				});
			
			tag.MusicBrainzReleaseType = string.Empty;

			TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzReleaseType, "Value Cleared (Null): " + m);
				});
			}
		}
				
		[Test]
		public void TestMusicBrainzReleaseCountry ()
		{
			Id3v2.Tag tag = new Id3v2.Tag ();
			for (byte version = 2; version <= 4; version ++) {
				tag.Version = version;

			TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzReleaseCountry, "Initial (Null): " + m);
				});
			
			tag.MusicBrainzReleaseCountry = val_sing;
			
			TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.MusicBrainzReleaseCountry, "Value Set (!Null): " + m);
				});
			
			tag.MusicBrainzReleaseCountry = string.Empty;

			TagTestWithSave (ref tag, delegate (Id3v2.Tag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.MusicBrainzReleaseCountry, "Value Cleared (Null): " + m);
				});
			}
		}

		[Test]
		public void TestInitialKey()
		{
			Id3v2.Tag tag = new Id3v2.Tag();
			for (byte version = 2; version <= 4; version++)
			{
				tag.Version = version;

				TagTestWithSave(ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsTrue(t.IsEmpty, "Initial (IsEmpty): " + m);
					Assert.IsNull(t.InitialKey, "Initial (Null): " + m);
				});

				tag.InitialKey = val_sing;

				TagTestWithSave(ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsFalse(t.IsEmpty, "Value Set (!IsEmpty): " + m);
					Assert.AreEqual(val_sing, t.InitialKey, "Value Set (!Null): " + m);
				});

				tag.InitialKey = string.Empty;

				TagTestWithSave(ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsTrue(t.IsEmpty, "Value Cleared (IsEmpty): " + m);
					Assert.IsNull(t.InitialKey, "Value Cleared (Null): " + m);
				});
			}
		}

		[Test]
		public void TestPublisher()
		{
			Id3v2.Tag tag = new Id3v2.Tag();
			for (byte version = 2; version <= 4; version++)
			{
				tag.Version = version;

				TagTestWithSave(ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsTrue(t.IsEmpty, "Initial (IsEmpty): " + m);
					Assert.IsNull(t.Publisher, "Initial (Null): " + m);
				});

				tag.Publisher = val_sing;

				TagTestWithSave(ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsFalse(t.IsEmpty, "Value Set (!IsEmpty): " + m);
					Assert.AreEqual(val_sing, t.Publisher, "Value Set (!Null): " + m);
				});

				tag.Publisher = string.Empty;

				TagTestWithSave(ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsTrue(t.IsEmpty, "Value Cleared (IsEmpty): " + m);
					Assert.IsNull(t.Publisher, "Value Cleared (Null): " + m);
				});
			}
		}

		[Test]
		public void TestISRC()
		{
			Id3v2.Tag tag = new Id3v2.Tag();
			for (byte version = 2; version <= 4; version++)
			{
				tag.Version = version;

				TagTestWithSave(ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsTrue(t.IsEmpty, "Initial (IsEmpty): " + m);
					Assert.IsNull(t.ISRC, "Initial (Null): " + m);
				});

				tag.ISRC = val_sing;

				TagTestWithSave(ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsFalse(t.IsEmpty, "Value Set (!IsEmpty): " + m);
					Assert.AreEqual(val_sing, t.ISRC, "Value Set (!Null): " + m);
				});

				tag.ISRC = string.Empty;

				TagTestWithSave(ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsTrue(t.IsEmpty, "Value Cleared (IsEmpty): " + m);
					Assert.IsNull(t.ISRC, "Value Cleared (Null): " + m);
				});
			}
		}

		[Test]
		public void TestRemixedBy()
		{
			Id3v2.Tag tag = new Id3v2.Tag();
			for (byte version = 2; version <= 4; version++)
			{
				tag.Version = version;

				TagTestWithSave(ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsTrue(t.IsEmpty, "Initial (IsEmpty): " + m);
					Assert.IsNull(t.RemixedBy, "Initial (Null): " + m);
				});

				tag.RemixedBy = val_sing;

				TagTestWithSave(ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsFalse(t.IsEmpty, "Value Set (!IsEmpty): " + m);
					Assert.AreEqual(val_sing, t.RemixedBy, "Value Set (!Null): " + m);
				});

				tag.RemixedBy = string.Empty;

				TagTestWithSave(ref tag, delegate (Id3v2.Tag t, string m) {
					Assert.IsTrue(t.IsEmpty, "Value Cleared (IsEmpty): " + m);
					Assert.IsNull(t.RemixedBy, "Value Cleared (Null): " + m);
				});
			}
		}

		[Test]
		public void TestClear ()
		{
			Id3v2.Tag tag = new Id3v2.Tag ();
			
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
			Assert.IsNull(tag.InitialKey, "InitialKey");
			Assert.IsNull(tag.Publisher, "Publisher");
			Assert.IsNull(tag.ISRC, "ISRC");
			Assert.IsNull(tag.RemixedBy, "RemixedBy");
		}

		[Test]
		public void TestCopyTo ()
		{
			Id3v2.Tag tag1 = new Id3v2.Tag ();
			Id3v2.Tag tag2 = new Id3v2.Tag ();
			
			UserTextInformationFrame frame1 = UserTextInformationFrame.Get (tag1, "FOOBAR", true);
			UserTextInformationFrame frame2 = UserTextInformationFrame.Get (tag2, "FOOBAR", true);
			
			frame1.Text = new string [] {"1"};
			frame2.Text = new string [] {"2"};
			
			Assert.AreEqual ("2", UserTextInformationFrame.Get (tag2, "FOOBAR", false).Text [0], "Not yet copied.");
			tag1.CopyTo (tag2, false);
			Assert.AreEqual ("2", UserTextInformationFrame.Get (tag2, "FOOBAR", false).Text [0], "overwrite=false");
			tag1.CopyTo (tag2, true);
			Assert.AreEqual ("1", UserTextInformationFrame.Get (tag2, "FOOBAR", false).Text [0], "overwrite=true");
			
			UserTextInformationFrame.Get (tag2, "FOOBAR", false).Text = new string [] {"3"};
			Assert.AreEqual ("1", UserTextInformationFrame.Get (tag1, "FOOBAR", false).Text [0], "Deep copy.");
		}
		
		[Test]
		public void TestAttachedPictureFrame ()
		{
			AttachmentFrame frame = new AttachmentFrame ();
			
			string mime = "image/png";
			string desc = "description";
			PictureType type = PictureType.FrontCover;
			ByteVector data = val_sing;
			data.Add (data); data.Add (data); data.Add (data);
			data.Add (data); data.Add (data); data.Add (data);
			data.Add (data); data.Add (data); data.Add (data);
			data.Add (data); data.Add (data); data.Add (data);
			// data.Add (data); data.Add (data); data.Add (data);
			
			frame.MimeType = mime;
			frame.Description = desc;
			frame.Type = type;
			frame.Data = data;
			
			FrameTest (frame, 2,
				delegate (Frame f, StringType e) {
					(f as AttachmentFrame).TextEncoding = e;
				},
				
				delegate (ByteVector d, byte v) {
					return new AttachmentFrame (d, v);
				},
				
				delegate (Frame f, string m) {
					AttachmentFrame g = (f as AttachmentFrame);
					Assert.AreEqual (mime, g.MimeType, m);
					Assert.AreEqual (desc, g.Description, m);
					Assert.AreEqual (data, g.Data, m);
					Assert.AreEqual (type, g.Type, m);
				});
		}
		
		[Test]
		public void TestCommentsFrame ()
		{
			string desc = "description";
			string lang = "ENG";
			CommentsFrame frame = new CommentsFrame (desc, lang);
			frame.Text = val_sing;
			
			FrameTest (frame, 2,
				delegate (Frame f, StringType e) {
					(f as CommentsFrame).TextEncoding = e;
				},
				
				delegate (ByteVector d, byte v) {
					return new CommentsFrame (d, v);
				},
				
				delegate (Frame f, string m) {
					CommentsFrame g = (f as CommentsFrame);
					Assert.AreEqual (desc, g.Description, m);
					Assert.AreEqual (lang, g.Language, m);
					Assert.AreEqual (val_sing, g.Text, m);
				});
		}
		
		[Test]
		public void TestGeneralEncapsulatedObjectFrame ()
		{
			AttachmentFrame frame = new AttachmentFrame();
			
			string name = "TEST.txt";
			string mime = "text/plain";
			PictureType type = PictureType.NotAPicture;
			string desc = "description";
			ByteVector data = val_sing;
			data.Add (data); data.Add (data); data.Add (data);
			data.Add (data); data.Add (data); data.Add (data);
			data.Add (data); data.Add (data); data.Add (data);
			data.Add (data); data.Add (data); data.Add (data);
			// data.Add (data); data.Add (data); data.Add (data);
			
			
			frame.Filename = name;
			frame.MimeType = mime;
			frame.Description = desc;
			frame.Data = data;
			frame.Type = type;


			FrameTest (frame, 2,
				delegate (Frame f, StringType e) {
					(f as AttachmentFrame).TextEncoding = e;
				},
				
				delegate (ByteVector d, byte v) {
					return new AttachmentFrame(d, v);
				},
				
				delegate (Frame f, string m) {
					var g = (f as AttachmentFrame);
					Assert.AreEqual(type, g.Type, m);
					Assert.AreEqual (name, g.Filename, m);
					Assert.AreEqual(mime, g.MimeType, m);
					Assert.AreEqual (desc, g.Description, m);
					Assert.AreEqual (data, g.Data, m);
				});
		}
		
		[Test]
		public void TestMusicCdIdentifierFrame ()
		{
			MusicCdIdentifierFrame frame = new MusicCdIdentifierFrame ();
			
			ByteVector data = val_sing;
			data.Add (data); data.Add (data); data.Add (data);
			data.Add (data); data.Add (data); data.Add (data);
			data.Add (data); data.Add (data); data.Add (data);
			data.Add (data); data.Add (data); data.Add (data);
			// data.Add (data); data.Add (data); data.Add (data);
			
			frame.Data = data;
			
			FrameTest (frame, 2, null,
				delegate (ByteVector d, byte v) {
					return new MusicCdIdentifierFrame (d, v);
				},
				
				delegate (Frame f, string m) {
					MusicCdIdentifierFrame g = (f as MusicCdIdentifierFrame);
					Assert.AreEqual (data, g.Data, m);
				});
		}
		
		[Test]
		public void TestPlayCountFrame ()
		{
			PlayCountFrame frame = new PlayCountFrame ();
			
			ulong value = 0xFFFFFFFFFFFFFFFF;
			frame.PlayCount = value;
			
			FrameTest (frame, 2, null,
				delegate (ByteVector d, byte v) {
					return new PlayCountFrame (d, v);
				},
				
				delegate (Frame f, string m) {
					PlayCountFrame g = (f as PlayCountFrame);
					Assert.AreEqual (value, g.PlayCount, m);
				});
		}
		
		[Test]
		public void TestPopularimeterFrame ()
		{
			PopularimeterFrame frame = new PopularimeterFrame (val_sing);
			
			ulong pcnt = 0xFFFFFFFFFFFFFFFF;
			byte rate = 0xFF;
			frame.Rating = rate;
			frame.PlayCount = pcnt;
			
			FrameTest (frame, 2, null,
				delegate (ByteVector d, byte v) {
					return new PopularimeterFrame (d, v);
				},
				
				delegate (Frame f, string m) {
					PopularimeterFrame g = (f as PopularimeterFrame);
					Assert.AreEqual (val_sing, g.User, m);
					Assert.AreEqual (rate, g.Rating, m);
					Assert.AreEqual (pcnt, g.PlayCount, m);
				});
		}
		
		[Test]
		public void TestPrivateFrame ()
		{
			ByteVector data = val_sing;
			data.Add (data); data.Add (data); data.Add (data);
			data.Add (data); data.Add (data); data.Add (data);
			data.Add (data); data.Add (data); data.Add (data);
			data.Add (data); data.Add (data); data.Add (data);
			// data.Add (data); data.Add (data); data.Add (data);
			
			PrivateFrame frame = new PrivateFrame (val_sing, data);
			
			FrameTest (frame, 3, null,
				delegate (ByteVector d, byte v) {
					return new PrivateFrame (d, v);
				},
				
				delegate (Frame f, string m) {
					PrivateFrame g = (f as PrivateFrame);
					Assert.AreEqual (val_sing, g.Owner, m);
					Assert.AreEqual (data, g.PrivateData, m);
				});
		}
		
		[Test]
		public void TestRelativeVolumeFrame ()
		{
			for (int a = 0; a < 2; a ++) {
			for (int b = 0; b < 2; b ++) {
			for (int c = 0; c < 2; c ++) {
			for (int d = 0; d < 2; d ++) {
			for (int e = 0; e < 2; e ++) {
			for (int f = 0; f < 2; f ++) {
			for (int g = 0; g < 2; g ++) {
			for (int h = 0; h < 2; h ++) {
			for (int i = 0; i < 2; i ++) {
			
			RelativeVolumeFrame frame = new RelativeVolumeFrame (val_sing);
			
			frame.SetPeakVolume ((ChannelType) 0, (double) a);
			frame.SetVolumeAdjustment ((ChannelType) 0, (float) -a);
			frame.SetPeakVolume ((ChannelType) 1, (double) b);
			frame.SetVolumeAdjustment ((ChannelType) 1, (float) -b);
			frame.SetPeakVolume ((ChannelType) 2, (double) c);
			frame.SetVolumeAdjustment ((ChannelType) 2, (float) -c);
			frame.SetPeakVolume ((ChannelType) 3, (double) d);
			frame.SetVolumeAdjustment ((ChannelType) 3, (float) -d);
			frame.SetPeakVolume ((ChannelType) 4, (double) e);
			frame.SetVolumeAdjustment ((ChannelType) 4, (float) -e);
			frame.SetPeakVolume ((ChannelType) 5, (double) f);
			frame.SetVolumeAdjustment ((ChannelType) 5, (float) -f);
			frame.SetPeakVolume ((ChannelType) 6, (double) g);
			frame.SetVolumeAdjustment ((ChannelType) 6, (float) -g);
			frame.SetPeakVolume ((ChannelType) 7, (double) h);
			frame.SetVolumeAdjustment ((ChannelType) 7, (float) -h);
			frame.SetPeakVolume ((ChannelType) 8, (double) i);
			frame.SetVolumeAdjustment ((ChannelType) 8, (float) -i);
			
			FrameTest (frame, 2, null,
				delegate (ByteVector d_, byte v_) {
					return new RelativeVolumeFrame (d_, v_);
				},
				
				delegate (Frame f_, string m_) {
					RelativeVolumeFrame g_ = (f_ as RelativeVolumeFrame);
					Assert.AreEqual ((double) a, g_.GetPeakVolume ((ChannelType) 0), "A: " + m_);
					Assert.AreEqual ((float) -a, g_.GetVolumeAdjustment ((ChannelType) 0), "A: " + m_);
					Assert.AreEqual ((double) b, g_.GetPeakVolume ((ChannelType) 1), "B: " + m_);
					Assert.AreEqual ((float) -b, g_.GetVolumeAdjustment ((ChannelType) 1), "B: " + m_);
					Assert.AreEqual ((double) c, g_.GetPeakVolume ((ChannelType) 2), "C: " + m_);
					Assert.AreEqual ((float) -c, g_.GetVolumeAdjustment ((ChannelType) 2), "C: " + m_);
					Assert.AreEqual ((double) d, g_.GetPeakVolume ((ChannelType) 3), "D: " + m_);
					Assert.AreEqual ((float) -d, g_.GetVolumeAdjustment ((ChannelType) 3), "D: " + m_);
					Assert.AreEqual ((double) e, g_.GetPeakVolume ((ChannelType) 4), "E: " + m_);
					Assert.AreEqual ((float) -e, g_.GetVolumeAdjustment ((ChannelType) 4), "E: " + m_);
					Assert.AreEqual ((double) f, g_.GetPeakVolume ((ChannelType) 5), "F: " + m_);
					Assert.AreEqual ((float) -f, g_.GetVolumeAdjustment ((ChannelType) 5), "F: " + m_);
					Assert.AreEqual ((double) g, g_.GetPeakVolume ((ChannelType) 6), "G: " + m_);
					Assert.AreEqual ((float) -g, g_.GetVolumeAdjustment ((ChannelType) 6), "G: " + m_);
					Assert.AreEqual ((double) h, g_.GetPeakVolume ((ChannelType) 7), "H: " + m_);
					Assert.AreEqual ((float) -h, g_.GetVolumeAdjustment ((ChannelType) 7), "H: " + m_);
					Assert.AreEqual ((double) i, g_.GetPeakVolume ((ChannelType) 8), "I: " + m_);
					Assert.AreEqual ((float) -i, g_.GetVolumeAdjustment ((ChannelType) 8), "I: " + m_);
				});
			
			}}}}}}}}}
		}

		[Test]
		public void TestRelativeVolumeFrameWithBrokenPeakVolume ()
		{
			// RVA2 data found in the wild
			var data = new byte[] { 82, 86, 65, 50, 0, 0, 0, 12, 0, 0, 97, 108, 98, 117, 109, 0, 1, 255, 0, 200, 15, 116 };
			var frame = new RelativeVolumeFrame (data, 4);
			Assert.AreEqual ("album", frame.Identification);
			Assert.AreEqual (-256, frame.GetVolumeAdjustmentIndex (ChannelType.MasterVolume));
			Assert.AreEqual (0, frame.GetPeakVolumeIndex (ChannelType.MasterVolume));
		}

		[Test]
		public void TestSynchronisedLyricsFrame ()
		{
			string lang = "ENG";
			SynchedText [] text = new SynchedText [] {
				new SynchedText (0, "Curtain Opens"),
				new SynchedText (1000, "Lights"),
				new SynchedText (2000, "Romeo Enters"),
				new SynchedText (120000, "Juliet Enters")
			};
			
			SynchronisedLyricsFrame frame = new SynchronisedLyricsFrame (val_sing, lang, SynchedTextType.Events);
			frame.Format = TimestampFormat.AbsoluteMilliseconds;
			frame.Text = text;
			
			FrameTest (frame, 2,
				delegate (Frame f, StringType e) {
					(f as SynchronisedLyricsFrame).TextEncoding = e;
				},
				
				delegate (ByteVector d, byte v) {
					return new SynchronisedLyricsFrame (d, v);
				},
				
				delegate (Frame f, string m) {
					SynchronisedLyricsFrame g = (f as SynchronisedLyricsFrame);
					Assert.AreEqual (val_sing, g.Description, m);
					Assert.AreEqual (lang, g.Language, m);
					Assert.AreEqual (SynchedTextType.Events, g.Type, m);
					Assert.AreEqual (TimestampFormat.AbsoluteMilliseconds, g.Format, m);
					Assert.AreEqual (text.Length, g.Text.Length, m);
					for (int i = 0; i < text.Length; i ++) {
						Assert.AreEqual (text [i].Time, g.Text [i].Time, m);
						Assert.AreEqual (text [i].Text, g.Text [i].Text, m);
					}
				});
		}
		
		[Test]
		public void TestTermsOfUseFrame ()
		{
			string lang = "ENG";
			TermsOfUseFrame frame = new TermsOfUseFrame (lang);
			frame.Text = val_sing;
			
			FrameTest (frame, 4,
				delegate (Frame f, StringType e) {
					(f as TermsOfUseFrame).TextEncoding = e;
				},
				
				delegate (ByteVector d, byte v) {
					return new TermsOfUseFrame (d, v);
				},
				
				delegate (Frame f, string m) {
					TermsOfUseFrame g = (f as TermsOfUseFrame);
					Assert.AreEqual (lang, g.Language, m);
					Assert.AreEqual (val_sing, g.Text, m);
				});
		}
		
		[Test]
		public void TestTextInformationFrame ()
		{
			ByteVector id = "TPE2";
			TextInformationFrame frame = new TextInformationFrame (id);
			frame.Text = val_mult;
			
			FrameTest (frame, 2,
				delegate (Frame f, StringType e) {
					(f as TextInformationFrame).TextEncoding = e;
				},
				
				delegate (ByteVector d, byte v) {
					return new TextInformationFrame (d, v);
				},
				
				delegate (Frame f, string m) {
					TextInformationFrame g = (f as TextInformationFrame);
					Assert.AreEqual (id, g.FrameId, m);
					Assert.AreEqual (val_mult.Length, g.Text.Length, m);
					for (int i = 0; i < val_mult.Length; i ++) {
						Assert.AreEqual (val_mult [i], g.Text [i], m);
					}
				});
		}
		
		[Test]
		public void TestUserTextInformationFrame ()
		{
			UserTextInformationFrame frame = new UserTextInformationFrame (val_sing);
			frame.Text = val_mult;
			
			FrameTest (frame, 2,
				delegate (Frame f, StringType e) {
					(f as UserTextInformationFrame).TextEncoding = e;
				},
				
				delegate (ByteVector d, byte v) {
					return new UserTextInformationFrame (d, v);
				},
				
				delegate (Frame f, string m) {
					UserTextInformationFrame g = (f as UserTextInformationFrame);
					Assert.AreEqual (val_sing, g.Description, m);
					Assert.AreEqual (val_mult.Length, g.Text.Length, m);
					for (int i = 0; i < val_mult.Length; i ++) {
						Assert.AreEqual (val_mult [i], g.Text [i], m);
					}
				});
		}
		
		[Test]
		public void TestUniqueFileIdentifierFrame ()
		{
			ByteVector data = val_sing;
			data.Add (data); data.Add (data); data.Add (data);
			data.Add (data); data.Add (data); data.Add (data);
			data.Add (data); data.Add (data); data.Add (data);
			data.Add (data); data.Add (data); data.Add (data);
			// data.Add (data); data.Add (data); data.Add (data);
			
			UniqueFileIdentifierFrame frame = new UniqueFileIdentifierFrame (val_sing);
			frame.Identifier = data;
			
			FrameTest (frame, 2, null,
				delegate (ByteVector d, byte v) {
					return new UniqueFileIdentifierFrame (d, v);
				},
				
				delegate (Frame f, string m) {
					UniqueFileIdentifierFrame g = (f as UniqueFileIdentifierFrame);
					Assert.AreEqual (val_sing, g.Owner, m);
					Assert.AreEqual (data, g.Identifier, m);
				});
		}
		
		[Test]
		public void TestUnknownFrame ()
		{
			ByteVector id = "XXXX";
			ByteVector data = val_sing;
			data.Add (data); data.Add (data); data.Add (data);
			data.Add (data); data.Add (data); data.Add (data);
			data.Add (data); data.Add (data); data.Add (data);
			data.Add (data); data.Add (data); data.Add (data);
			// data.Add (data); data.Add (data); data.Add (data);
			
			UnknownFrame frame = new UnknownFrame (id, data);
			
			FrameTest (frame, 3, null,
				delegate (ByteVector d, byte v) {
					return new UnknownFrame (d, v);
				},
				
				delegate (Frame f, string m) {
					UnknownFrame g = (f as UnknownFrame);
					Assert.AreEqual (id, g.FrameId, m);
					Assert.AreEqual (data, g.Data, m);
				});
		}
		
		[Test]
		public void TestUnsynchronisedLyricsFrame ()
		{
			string desc = "description";
			string lang = "ENG";
			UnsynchronisedLyricsFrame frame = new UnsynchronisedLyricsFrame (desc, lang);
			frame.Text = val_sing;
			
			FrameTest (frame, 2,
				delegate (Frame f, StringType e) {
					(f as UnsynchronisedLyricsFrame).TextEncoding = e;
				},
				
				delegate (ByteVector d, byte v) {
					return new UnsynchronisedLyricsFrame (d, v);
				},
				
				delegate (Frame f, string m) {
					UnsynchronisedLyricsFrame g = (f as UnsynchronisedLyricsFrame);
					Assert.AreEqual (desc, g.Description, m);
					Assert.AreEqual (lang, g.Language, m);
					Assert.AreEqual (val_sing, g.Text, m);
				});
		}

		[Test]
		public void TestEventTimeCodesFrame()
		{
			var events = new List<EventTimeCode> {
				new EventTimeCode(EventType.IntroStart, 5000),
				new EventTimeCode(EventType.IntroEnd, 15000),
			};

			var frame = new EventTimeCodesFrame(TimestampFormat.AbsoluteMilliseconds);
			frame.Events = events;

			FrameTest(frame, 2,
				delegate (Frame f, StringType e) {
				},

				delegate (ByteVector d, byte v) {
					return new EventTimeCodesFrame(d, v);
				},

				delegate (Frame f, string m) {
					var g = (f as EventTimeCodesFrame);
					Assert.AreEqual(TimestampFormat.AbsoluteMilliseconds, g.TimestampFormat, m);
					for (int i = 0; i < events.Count ; i++)
					{
						Assert.AreEqual(events[i].Time, g.Events[i].Time, m);
					}
				});
		}

		delegate void TagTestFunc (Id3v2.Tag tag, string msg);
		
		void TagTestWithSave (ref Id3v2.Tag tag,
		                      TagTestFunc testFunc)
		{
			testFunc (tag, "Before Save");
			for (byte version = 2; version <= 4; version ++) {
				tag.Version = version;
				tag = new Id3v2.Tag (tag.Render ());
				testFunc (tag, "After Save, Version: " + version);
				tag = tag.Clone ();
				testFunc (tag, "After Clone, Version: " + version);
				Id3v2.Tag tmp = new Id3v2.Tag ();
				tag.CopyTo (tmp, true);
				tag = tmp;
				testFunc (tag, "After CopyTo(true), Version: " + version);
				tmp = new Id3v2.Tag ();
				tag.CopyTo (tmp, false);
				tag = tmp;
				testFunc (tag, "After CopyTo(false), Version: " + version);
			}
		}
		
		delegate void FrameTestFunc (Frame frame, string msg);
		
		delegate void SetEncodingFunc (Frame frame,
		                               StringType encoding);
		
		delegate Frame CreateFrameFunc (ByteVector data,
		                                byte version);
		
		void FrameTest (Frame frame, byte minVersion,
		                SetEncodingFunc setEncFunc,
		                CreateFrameFunc createFunc,
		                FrameTestFunc testFunc)
		{
			testFunc (frame, "Beginning");
			for (byte version = minVersion; version <= 4;
				version ++) {
				for (int encoding = 0;
					encoding < (setEncFunc != null ? 5 : 1);
					encoding ++) {
					
					if (setEncFunc != null)
						setEncFunc (frame,
							(StringType) encoding);
					
					ByteVector tmp = frame.Render (version);
					//Extras.DumpHex (tmp.Data);
					frame = createFunc (tmp, version);
					testFunc (frame, "Render: Version " + version +
						"; Encoding " + (StringType)
						encoding);
					frame = frame.Clone ();
					testFunc (frame, "Clone: Version " + version +
						"; Encoding " + (StringType)
						encoding);
				}
			}
		}
	}
}
