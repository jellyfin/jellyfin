using System;
using NUnit.Framework;
using TagLib;

namespace TagLib.Tests.FileFormats
{
	[TestFixture]
	public class M4vFormatTest : IFormatTest
	{
		ReadOnlyByteVector BOXTYPE_LDES = "ldes"; // long description
		ReadOnlyByteVector BOXTYPE_TVSH = "tvsh"; // TV Show or series
		ReadOnlyByteVector BOXTYPE_PURD = "purd"; // purchase date

		private const string LONG_DESC = "American comedy luminaries talk about the influence of Monty Python.";
		private const string PURD_DATE = "2009-01-26 08:14:10";
		private const string TV_SHOW = "Ask An Astronomer";

		private readonly string sample_file = TestPath.Samples + "sample.m4v";
		private readonly string tmp_file = TestPath.Samples + "tmpwrite.m4v";
		private File file;

		[OneTimeSetUp]
		public void Init ()
		{
			file = File.Create (sample_file);
		}

		[Test]
		public void ReadAudioProperties ()
		{
			// Despite the method name, we're reading the video properties here
			Assert.AreEqual (632, file.Properties.VideoWidth);
			Assert.AreEqual (472, file.Properties.VideoHeight);
		}

		[Test]
		public void ReadTags ()
		{
			bool gotLongDesc = false;
			bool gotPurdDate = false;

			Assert.AreEqual ("Will Yapp", file.Tag.FirstPerformer);
			Assert.AreEqual ("Why I Love Monty Python", file.Tag.Title);
			Assert.AreEqual (2008, file.Tag.Year);

			// Test Apple tags
			Mpeg4.AppleTag tag = (Mpeg4.AppleTag) file.GetTag (TagTypes.Apple, false);
			Assert.IsNotNull (tag);

			foreach (Mpeg4.AppleDataBox adbox in tag.DataBoxes (new ReadOnlyByteVector[] {BOXTYPE_LDES})) {
				Assert.AreEqual (LONG_DESC, adbox.Text);
				gotLongDesc = true;
			}

			foreach (Mpeg4.AppleDataBox adbox in tag.DataBoxes (new ReadOnlyByteVector[] {BOXTYPE_PURD})) {
				Assert.AreEqual(PURD_DATE, adbox.Text);
				gotPurdDate = true;
			}

			Assert.IsTrue (gotLongDesc);
			Assert.IsTrue (gotPurdDate);
		}

		[Test]
		public void WriteAppleTags ()
		{
			if (System.IO.File.Exists (tmp_file))
				System.IO.File.Delete (tmp_file);

			System.IO.File.Copy (sample_file, tmp_file);

			File tmp = File.Create (tmp_file);
			Mpeg4.AppleTag tag = (Mpeg4.AppleTag) tmp.GetTag (TagTypes.Apple, false);
			SetTags (tag);
			tmp.Save ();

			tmp = File.Create (tmp_file);
			tag = (Mpeg4.AppleTag) tmp.GetTag (TagTypes.Apple, false);
			CheckTags (tag);
		}

		[Test]
		[Ignore("PictureLazy not supported yet")]
		public void WriteStandardPicturesLazy()
		{
			StandardTests.WriteStandardPictures(sample_file, tmp_file, ReadStyle.PictureLazy);
		}


		[Test]
		public void TestCorruptionResistance ()
		{
		}

		private void SetTags (Mpeg4.AppleTag tag)
		{
			tag.Title = "TEST title";
			tag.Performers = new string[] {"TEST performer 1", "TEST performer 2"};
			tag.Comment = "TEST comment";
			tag.Copyright = "TEST copyright";
			tag.Genres = new string [] {"TEST genre 1", "TEST genre 2"};
			tag.Year = 1999;

			Mpeg4.AppleTag atag = (Mpeg4.AppleTag)tag;
			Assert.IsNotNull(atag);

			Mpeg4.AppleDataBox newbox1 = new Mpeg4.AppleDataBox (
				ByteVector.FromString ("TEST Long Description", StringType.UTF8),
				(int) Mpeg4.AppleDataBox.FlagType.ContainsText);
			Mpeg4.AppleDataBox newbox2 = new Mpeg4.AppleDataBox (
				ByteVector.FromString ("TEST TV Show", StringType.UTF8),
				(int) Mpeg4.AppleDataBox.FlagType.ContainsText);
			atag.SetData (BOXTYPE_LDES, new Mpeg4.AppleDataBox[] {newbox1});
			atag.SetData (BOXTYPE_TVSH, new Mpeg4.AppleDataBox[] {newbox2});
		}

		private void CheckTags (Mpeg4.AppleTag tag)
		{
			Assert.AreEqual ("TEST title", tag.Title);
			Assert.AreEqual ("TEST performer 1; TEST performer 2", tag.JoinedPerformers);
			Assert.AreEqual ("TEST comment", tag.Comment);
			Assert.AreEqual ("TEST copyright", tag.Copyright);
			Assert.AreEqual ("TEST genre 1; TEST genre 2", tag.JoinedGenres);
			Assert.AreEqual (1999, tag.Year);

			Mpeg4.AppleTag atag = (Mpeg4.AppleTag) tag;
			Assert.IsNotNull (atag);

			foreach (Mpeg4.AppleDataBox adbox in atag.DataBoxes (new ReadOnlyByteVector[] {BOXTYPE_LDES})) {
				Assert.AreEqual ("TEST Long Description", adbox.Text);
			}

			foreach (Mpeg4.AppleDataBox adbox in atag.DataBoxes (new ReadOnlyByteVector[] {BOXTYPE_TVSH})) {
				Assert.AreEqual ("TEST TV Show", adbox.Text);
			}
		}
	}
}
