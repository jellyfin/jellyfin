using System;
using System.Security.Cryptography;
using NUnit.Framework;
using TagLib.Riff;

namespace TagLib.Tests.TaggingFormats
{   
	[TestFixture]
	public class InfoTagTest
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
			Riff.InfoTag tag = new Riff.InfoTag ();
			
			TagTestWithSave (ref tag, delegate (Riff.InfoTag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.Title, "Initial (Null): " + m);
			});
			
			tag.Title = val_sing;
			
			TagTestWithSave (ref tag, delegate (Riff.InfoTag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.Title, "Value Set (!Null): " + m);
			});
			
			tag.Title = string.Empty;
			
			TagTestWithSave (ref tag, delegate (Riff.InfoTag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.Title, "Value Cleared (Null): " + m);
			});

		}

		[Test]
		public void TestDescription()
		{
			Riff.InfoTag tag = new Riff.InfoTag();

			TagTestWithSave(ref tag, delegate (Riff.InfoTag t, string m) {
				Assert.IsTrue(t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull(t.Description, "Initial (Null): " + m);
			});

			tag.Description = val_sing;

			TagTestWithSave(ref tag, delegate (Riff.InfoTag t, string m) {
				Assert.IsFalse(t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual(val_sing, t.Description, "Value Set (!Null): " + m);
			});

			tag.Description = string.Empty;

			TagTestWithSave(ref tag, delegate (Riff.InfoTag t, string m) {
				Assert.IsTrue(t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull(t.Description, "Value Cleared (Null): " + m);
			});

		}

		[Test]
		public void TestAlbum()
		{
			Riff.InfoTag tag = new Riff.InfoTag();

			TagTestWithSave(ref tag, delegate (Riff.InfoTag t, string m) {
				Assert.IsTrue(t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull(t.Album, "Initial (Null): " + m);
			});

			tag.Album = val_sing;

			TagTestWithSave(ref tag, delegate (Riff.InfoTag t, string m) {
				Assert.IsFalse(t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual(val_sing, t.Album, "Value Set (!Null): " + m);
			});

			tag.Album = string.Empty;

			TagTestWithSave(ref tag, delegate (Riff.InfoTag t, string m) {
				Assert.IsTrue(t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull(t.Album, "Value Cleared (Null): " + m);
			});

		}


		[Test]
		public void TestConductor()
		{
			Riff.InfoTag tag = new Riff.InfoTag();

			TagTestWithSave(ref tag, delegate (Riff.InfoTag t, string m) {
				Assert.IsTrue(t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull(t.Conductor, "Initial (Null): " + m);
			});

			tag.Conductor = val_sing;

			TagTestWithSave(ref tag, delegate (Riff.InfoTag t, string m) {
				Assert.IsFalse(t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual(val_sing, t.Conductor, "Value Set (!Null): " + m);
			});

			tag.Conductor = string.Empty;

			TagTestWithSave(ref tag, delegate (Riff.InfoTag t, string m) {
				Assert.IsTrue(t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull(t.Conductor, "Value Cleared (Null): " + m);
			});

		}


		[Test]
		public void TestPerformers ()
		{
			Riff.InfoTag tag = new Riff.InfoTag ();
			
			TagTestWithSave (ref tag, delegate (Riff.InfoTag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.AreEqual (0, t.Performers.Length, "Initial (Zero): " + m);
			});
			
			tag.Performers = val_mult;
			
			TagTestWithSave (ref tag, delegate (Riff.InfoTag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_mult.Length, t.Performers.Length, "Value Set: " + m);
				for (int i = 0; i < val_mult.Length; i ++) {
					Assert.AreEqual (val_mult [i], t.Performers [i], "Value Set: " + m);
				}
			});
			
			tag.Performers = new string [0];
			
			TagTestWithSave (ref tag, delegate (Riff.InfoTag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.AreEqual (0, t.Performers.Length, "Value Cleared (Zero): " + m);
			});
		}
		
		[Test]
		public void TestAlbumArtists ()
		{
			Riff.InfoTag tag = new Riff.InfoTag ();
			
			TagTestWithSave (ref tag, delegate (Riff.InfoTag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.AreEqual (0, t.AlbumArtists.Length, "Initial (Zero): " + m);
			});
			
			tag.AlbumArtists = val_mult;
			
			TagTestWithSave (ref tag, delegate (Riff.InfoTag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_mult.Length, t.AlbumArtists.Length, "Value Set: " + m);
				for (int i = 0; i < val_mult.Length; i ++) {
					Assert.AreEqual (val_mult [i], t.AlbumArtists [i], "Value Set: " + m);
				}
			});
			
			tag.AlbumArtists = new string [0];
			
			TagTestWithSave (ref tag, delegate (Riff.InfoTag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.AreEqual (0, t.AlbumArtists.Length, "Value Cleared (Zero): " + m);
			});
		}
		
		[Test]
		public void TestComposers ()
		{
			Riff.InfoTag tag = new Riff.InfoTag ();
			
			TagTestWithSave (ref tag, delegate (Riff.InfoTag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.AreEqual (0, t.Composers.Length, "Initial (Zero): " + m);
			});
			
			tag.Composers = val_mult;
			
			TagTestWithSave (ref tag, delegate (Riff.InfoTag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_mult.Length, t.Composers.Length, "Value Set: " + m);
				for (int i = 0; i < val_mult.Length; i ++) {
					Assert.AreEqual (val_mult [i], t.Composers [i], "Value Set: " + m);
				}
			});
			
			tag.Composers = new string [0];
			
			TagTestWithSave (ref tag, delegate (Riff.InfoTag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.AreEqual (0, t.Composers.Length, "Value Cleared (Zero): " + m);
			});
		}
		
		[Test]
		public void TestComment ()
		{
			Riff.InfoTag tag = new Riff.InfoTag ();

			TagTestWithSave (ref tag, delegate (Riff.InfoTag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.Comment, "Initial (Null): " + m);
			});
			
			tag.Comment = val_sing;
			
			TagTestWithSave (ref tag, delegate (Riff.InfoTag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.Comment, "Value Set (!Null): " + m);
			});
			
			tag.Comment = string.Empty;

			TagTestWithSave (ref tag, delegate (Riff.InfoTag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.Comment, "Value Cleared (Null): " + m);
			});
		}
		
		[Test]
		public void TestGenres ()
		{
			Riff.InfoTag tag = new Riff.InfoTag ();

			TagTestWithSave (ref tag, delegate (Riff.InfoTag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.AreEqual (0, t.Genres.Length, "Initial (Zero): " + m);
			});
			
			tag.Genres = val_gnre;
			
			TagTestWithSave (ref tag, delegate (Riff.InfoTag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_gnre.Length, t.Genres.Length, "Value Set: " + m);
				for (int i = 0; i < val_gnre.Length; i ++) {
					Assert.AreEqual (val_gnre [i], t.Genres [i], "Value Set: " + m);
				}
			});
			
			tag.Genres = val_mult;
			
			TagTestWithSave (ref tag, delegate (Riff.InfoTag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_mult.Length, t.Genres.Length, "Value Set: " + m);
				for (int i = 0; i < val_mult.Length; i ++) {
					Assert.AreEqual (val_mult [i], t.Genres [i], "Value Set: " + m);
				}
			});
			
			tag.Genres = new string [0];

			TagTestWithSave (ref tag, delegate (Riff.InfoTag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.AreEqual (0, t.Genres.Length, "Value Cleared (Zero): " + m);
			});
		}
		
		[Test]
		public void TestYear ()
		{
			Riff.InfoTag tag = new Riff.InfoTag ();

			TagTestWithSave (ref tag, delegate (Riff.InfoTag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.AreEqual (0, tag.Year, "Initial (Zero): " + m);
			});
			
			tag.Year = 1999;
			
			TagTestWithSave (ref tag, delegate (Riff.InfoTag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (1999, tag.Year, "Value Set: " + m);
			});
			
			tag.Year = 0;

			TagTestWithSave (ref tag, delegate (Riff.InfoTag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.AreEqual (0, t.Year, "Value Cleared (Zero): " + m);
			});
		}
		
		[Test]
		public void TestTrack ()
		{
			Riff.InfoTag tag = new Riff.InfoTag ();

			TagTestWithSave (ref tag, delegate (Riff.InfoTag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.AreEqual (0, tag.Track, "Initial (Zero): " + m);
			});
			
			tag.Track = 199;
			
			TagTestWithSave (ref tag, delegate (Riff.InfoTag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (199, tag.Track, "Value Set: " + m);
			});
			
			tag.Track = 0;

			TagTestWithSave (ref tag, delegate (Riff.InfoTag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.AreEqual (0, t.Track, "Value Cleared (Zero): " + m);
			});
		}
		
		[Test]
		public void TestTrackCount ()
		{
			Riff.InfoTag tag = new Riff.InfoTag ();

			TagTestWithSave (ref tag, delegate (Riff.InfoTag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.AreEqual (0, tag.TrackCount, "Initial (Zero): " + m);
			});
			
			tag.TrackCount = 199;
			
			TagTestWithSave (ref tag, delegate (Riff.InfoTag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (199, tag.TrackCount, "Value Set: " + m);
			});
			
			tag.TrackCount = 0;

			TagTestWithSave (ref tag, delegate (Riff.InfoTag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.AreEqual (0, t.TrackCount, "Value Cleared (Zero): " + m);
			});
		}
		
		[Test]
		public void TestCopyright ()
		{
			Riff.InfoTag tag = new Riff.InfoTag ();

			TagTestWithSave (ref tag, delegate (Riff.InfoTag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Initial (IsEmpty): " + m);
				Assert.IsNull (t.Copyright, "Initial (Null): " + m);
			});
			
			tag.Copyright = val_sing;
			
			TagTestWithSave (ref tag, delegate (Riff.InfoTag t, string m) {
				Assert.IsFalse (t.IsEmpty, "Value Set (!IsEmpty): " + m);
				Assert.AreEqual (val_sing, t.Copyright, "Value Set (!Null): " + m);
			});
			
			tag.Copyright = string.Empty;

			TagTestWithSave (ref tag, delegate (Riff.InfoTag t, string m) {
				Assert.IsTrue (t.IsEmpty, "Value Cleared (IsEmpty): " + m);
				Assert.IsNull (t.Copyright, "Value Cleared (Null): " + m);
			});
		}
			
		[Test]
		public void TestClear ()
		{
			Riff.InfoTag tag = new Riff.InfoTag ();
			
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
			tag.Pictures = new Picture [] {new Picture (TestPath.Covers + "sample_a.png")};
			
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
		
		private delegate void TagTestFunc (Riff.InfoTag tag, string msg);
		
		private void TagTestWithSave (ref Riff.InfoTag tag,
		                              TagTestFunc testFunc)
		{
			testFunc (tag, "Before Save");
			//Extras.DumpHex (tag.Render ().Data);
			tag = new Riff.InfoTag (tag.Render ());
			testFunc (tag, "After Save");
		}
	}
}
