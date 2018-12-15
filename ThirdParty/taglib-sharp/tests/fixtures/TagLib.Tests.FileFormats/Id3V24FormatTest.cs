using System;
using NUnit.Framework;
using TagLib;

namespace TagLib.Tests.FileFormats
{
	[TestFixture]
	public class Id3V24FormatTest : IFormatTest
	{
		private readonly string sample_file = TestPath.Samples + "sample_v2_4_unsynch.mp3";
		private readonly string tmp_file = TestPath.Samples + "tmpwrite_v2_4_unsynch.mp3";
		private File file;

		[OneTimeSetUp]
		public void Init ()
		{
			file = File.Create (sample_file);
		}

		[Test]
		public void ReadAudioProperties ()
		{
			Assert.AreEqual (44100, file.Properties.AudioSampleRate);
			Assert.AreEqual (1, file.Properties.Duration.Seconds);
		}

		[Test]
		public void ReadTags ()
		{
			Assert.AreEqual ("MP3 album", file.Tag.Album);
			Assert.IsTrue (file.Tag.Comment.StartsWith ("MP3 comment"));
			CollectionAssert.AreEqual (file.Tag.Genres, new [] { "Acid Punk" });
			CollectionAssert.AreEqual (file.Tag.Performers, new [] {
				"MP3 artist unicode (\u1283\u12ed\u120c \u1308\u1265\u1228\u1225\u120b\u1234)" });
			CollectionAssert.AreEqual (file.Tag.Composers, new [] { "MP3 composer" });
			Assert.AreEqual ("MP3 title unicode (\u12a2\u1275\u12ee\u1335\u12eb)", file.Tag.Title);
			Assert.AreEqual (6, file.Tag.Track);
			Assert.AreEqual (7, file.Tag.TrackCount);
			Assert.AreEqual (1234, file.Tag.Year);
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

		public void TestCorruptionResistance ()
		{
		}

		[Test]
		public void ReplayGainTest()
		{
			string inFile = TestPath.Samples + "sample_replaygain.mp3";
			string tempFile = TestPath.Samples + "tmpwrite_sample_replaygain.mp3";

			File rgFile = File.Create(inFile);
			Assert.AreEqual(2.22d, rgFile.Tag.ReplayGainTrackGain);
			Assert.AreEqual(0.418785d, rgFile.Tag.ReplayGainTrackPeak);
			Assert.AreEqual(2.32d, rgFile.Tag.ReplayGainAlbumGain);
			Assert.AreEqual(0.518785d, rgFile.Tag.ReplayGainAlbumPeak);
			rgFile.Dispose();

			System.IO.File.Copy(inFile, tempFile, true);

			rgFile = File.Create(tempFile);
			rgFile.Tag.ReplayGainTrackGain = -1;
			rgFile.Tag.ReplayGainTrackPeak = 1;
			rgFile.Tag.ReplayGainAlbumGain = 2;
			rgFile.Tag.ReplayGainAlbumPeak = 0;
			rgFile.Save();
			rgFile.Dispose();

			rgFile = File.Create(tempFile);
			Assert.AreEqual(-1d, rgFile.Tag.ReplayGainTrackGain);
			Assert.AreEqual(1d, rgFile.Tag.ReplayGainTrackPeak);
			Assert.AreEqual(2d, rgFile.Tag.ReplayGainAlbumGain);
			Assert.AreEqual(0d, rgFile.Tag.ReplayGainAlbumPeak);
			rgFile.Tag.ReplayGainTrackGain = double.NaN;
			rgFile.Tag.ReplayGainTrackPeak = double.NaN;
			rgFile.Tag.ReplayGainAlbumGain = double.NaN;
			rgFile.Tag.ReplayGainAlbumPeak = double.NaN;
			rgFile.Save();
			rgFile.Dispose();
			
			rgFile = File.Create(tempFile);
			Assert.AreEqual(double.NaN, rgFile.Tag.ReplayGainTrackGain);
			Assert.AreEqual(double.NaN, rgFile.Tag.ReplayGainTrackPeak);
			Assert.AreEqual(double.NaN, rgFile.Tag.ReplayGainAlbumGain);
			Assert.AreEqual(double.NaN, rgFile.Tag.ReplayGainAlbumPeak);
			rgFile.Dispose();

			System.IO.File.Delete(tempFile);
		}

		[Test]
		public void URLLinkFrameTest()
		{
			string tempFile = TestPath.Samples + "tmpwrite_urllink_v2_4_unsynch.mp3";

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
