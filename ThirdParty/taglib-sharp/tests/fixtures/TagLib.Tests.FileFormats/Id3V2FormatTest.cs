using System;
using NUnit.Framework;
using TagLib;

namespace TagLib.Tests.FileFormats
{   
	[TestFixture]
	public class Id3V2FormatTest : IFormatTest
	{
		private static string sample_file = TestPath.Samples + "sample_v2_only.mp3";
		private static string corrupt_file = TestPath.Samples + "corrupt/null_title_v2.mp3";
		private static string tmp_file = TestPath.Samples + "tmpwrite_v2_only.mp3";
		private static string ext_header_file = TestPath.Samples + "sample_v2_3_ext_header.mp3";
		private File file;
		
		[OneTimeSetUp]
		public void Init()
		{
			file = File.Create(sample_file);
		}
	
		[Test]
		public void ReadAudioProperties()
		{
			Assert.AreEqual(44100, file.Properties.AudioSampleRate);
			Assert.AreEqual(1, file.Properties.Duration.Seconds);
		}
		
		[Test]
		public void ReadTags()
		{
			Assert.AreEqual("MP3 album", file.Tag.Album);
			Assert.AreEqual("MP3 artist", file.Tag.FirstPerformer);
			Assert.AreEqual("MP3 comment", file.Tag.Comment);
			Assert.AreEqual("Acid Punk", file.Tag.FirstGenre);
			Assert.AreEqual("MP3 title", file.Tag.Title);
			Assert.AreEqual(6, file.Tag.Track);
			Assert.AreEqual(7, file.Tag.TrackCount);
			Assert.AreEqual(1234, file.Tag.Year);
		}

		[Test]
		public void MultiGenresTest()
		{
			string inFile = TestPath.Samples + "sample.mp3";
			string tempFile = TestPath.Samples + "tmpwrite.mp3";

			File rgFile = File.Create(inFile);
			var tag = rgFile.Tag;
			var genres = tag.Genres;

			Assert.AreEqual(3, genres.Length);
			Assert.AreEqual("Genre 1", genres[0]);
			Assert.AreEqual("Genre 2", genres[1]);
			Assert.AreEqual("Genre 3", genres[2]);

			rgFile.Dispose();
			System.IO.File.Delete(tempFile);
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
		public void WriteExtendedTags()
		{
			ExtendedTests.WriteExtendedTags(sample_file, tmp_file);
		}

		[Test] // http://bugzilla.gnome.org/show_bug.cgi?id=558123
		public void TestTruncateOnNull ()
		{
			if (System.IO.File.Exists (tmp_file)) {
				System.IO.File.Delete (tmp_file);
			}
			
			System.IO.File.Copy (corrupt_file, tmp_file);
			File tmp = File.Create (tmp_file);
			
			Assert.AreEqual ("T", tmp.Tag.Title);
		}
		
		[Test]
		public void TestCorruptionResistance()
		{
		}

		[Test]
		public void TestExtendedHeaderSize()
		{
			// bgo#604488
			var file = File.Create (ext_header_file);
			Assert.AreEqual ("Title v2", file.Tag.Title);
		}

		[Test]
		public void URLLinkFrameTest()
		{
			string tempFile = TestPath.Samples + "tmpwrite_sample_v2_only.mp3";

			System.IO.File.Copy(sample_file, tempFile, true);

			File urlLinkFile = File.Create(tempFile);
			var id3v2tag = urlLinkFile.GetTag(TagTypes.Id3v2) as TagLib.Id3v2.Tag;
			id3v2tag.SetTextFrame("WCOM", "www.commercial.com");
			id3v2tag.SetTextFrame("WCOP", "www.copyright.com");
			id3v2tag.SetTextFrame("WOAF", "www.official-audio.com");
			id3v2tag.SetTextFrame("WOAR", "www.official-artist.com");
			id3v2tag.SetTextFrame("WOAS", "www.official-audio-source.com");
			id3v2tag.SetTextFrame("WORS", "www.official-internet-radio.com");
			id3v2tag.SetTextFrame("WPAY", "www.payment.com");
			id3v2tag.SetTextFrame("WPUB", "www.official-publisher.com");
			urlLinkFile.Save();
			urlLinkFile.Dispose();

			urlLinkFile = File.Create(tempFile);
			id3v2tag = urlLinkFile.GetTag(TagTypes.Id3v2) as TagLib.Id3v2.Tag;
			Assert.AreEqual("www.commercial.com", id3v2tag.GetTextAsString("WCOM"));
			Assert.AreEqual("www.copyright.com", id3v2tag.GetTextAsString("WCOP"));
			Assert.AreEqual("www.official-audio.com", id3v2tag.GetTextAsString("WOAF"));
			Assert.AreEqual("www.official-artist.com", id3v2tag.GetTextAsString("WOAR"));
			Assert.AreEqual("www.official-audio-source.com", id3v2tag.GetTextAsString("WOAS"));
			Assert.AreEqual("www.official-internet-radio.com", id3v2tag.GetTextAsString("WORS"));
			Assert.AreEqual("www.payment.com", id3v2tag.GetTextAsString("WPAY"));
			Assert.AreEqual("www.official-publisher.com", id3v2tag.GetTextAsString("WPUB"));
			urlLinkFile.Dispose();

			System.IO.File.Delete(tempFile);
		}
	}
}
