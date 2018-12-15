using System;
using NUnit.Framework;
using TagLib;
using TagLib.IFD;
using TagLib.IFD.Entries;
using TagLib.IFD.Tags;
using TagLib.Jpeg;
using TagLib.Xmp;

namespace TagLib.Tests.Images
{
	[TestFixture]
	public class JpegTangledTest
	{
		private static int count = 6;

		private static string sample_file = TestPath.Samples + "sample_tangled{0}.jpg";
		private static string tmp_file = TestPath.Samples + "tmpwrite_tangled{0}.jpg";

		private static TagTypes[] contained_types = new TagTypes[] {
				TagTypes.JpegComment | TagTypes.TiffIFD | TagTypes.XMP,
				TagTypes.JpegComment | TagTypes.TiffIFD,
				TagTypes.JpegComment | TagTypes.TiffIFD | TagTypes.XMP,
				TagTypes.JpegComment | TagTypes.XMP,
				TagTypes.JpegComment | TagTypes.XMP,
				TagTypes.JpegComment
		};

		private File[] files;

		private static string GetSampleFilename (int i)
		{
			return String.Format (sample_file, i + 1);
		}

		private static string GetTmpFilename (int i)
		{
			return String.Format (tmp_file, i + 1);
		}

		[OneTimeSetUp]
		public void Init () {
			files = new File[count];

			for (int i = 0; i < count; i++)
				files[i] = File.Create (GetSampleFilename (i));
		}

		[Test]
		public void JpegRead () {
			for (int i = 0; i < count; i++)
				CheckTags (files[i], i);
		}

		[Test]
		public void ExifRead () {
			for (int i = 0; i < count; i++)
				if ((TagTypes.TiffIFD & contained_types[i]) != 0)
					CheckExif (files[i], i);
		}

		[Test]
		public void XmpRead () {
			for (int i = 0; i < count; i++)
				if ((TagTypes.XMP & contained_types[i]) != 0)
					CheckXmp (files[i], i);
		}

		[Test]
		public void JpegCommentRead () {
			for (int i = 0; i < count; i++)
				if ((TagTypes.JpegComment & contained_types[i]) != 0)
					CheckJpegComment (files[i], i);
		}

		[Test]
		public void Rewrite () {

			for (int i = 0; i < count; i++)  {
				File tmp = Utils.CreateTmpFile (GetSampleFilename (i), GetTmpFilename (i));

				tmp.Save ();

				tmp = File.Create (GetTmpFilename (i));

				if ((TagTypes.TiffIFD & contained_types[i]) != 0)
					CheckExif (tmp, i);

				if ((TagTypes.XMP & contained_types[i]) != 0)
					CheckXmp (tmp, i);

				if ((TagTypes.JpegComment & contained_types[i]) != 0)
					CheckJpegComment (tmp, i);
			}
		}

		[Test]
		public void AddExif ()
		{
			for (int i = 0; i < count; i++)
				AddImageMetadataTests.AddExifTest (GetSampleFilename (i),
				                                   GetTmpFilename (i),
				                                   (TagTypes.TiffIFD & contained_types[i]) != 0);
		}

		[Test]
		public void AddGPS ()
		{
			for (int i = 0; i < count; i++)
				AddImageMetadataTests.AddGPSTest (GetSampleFilename (i),
				                                  GetTmpFilename (i),
				                                  (TagTypes.TiffIFD & contained_types[i]) != 0);
		}

		[Test]
		public void AddXMP1 ()
		{
			for (int i = 0; i < count; i++)
				AddImageMetadataTests.AddXMPTest1 (GetSampleFilename (i),
				                                  GetTmpFilename (i),
				                                  (TagTypes.XMP & contained_types[i]) != 0);
		}

		[Test]
		public void AddXMP2 ()
		{
			for (int i = 0; i < count; i++)
				AddImageMetadataTests.AddXMPTest2 (GetSampleFilename (i),
				                                  GetTmpFilename (i),
				                                  (TagTypes.XMP & contained_types[i]) != 0);
		}

		public void CheckTags (File file, int i) {
			Assert.IsTrue (file is Jpeg.File, String.Format ("not a Jpeg file: index {0}", i));

			Assert.AreEqual (contained_types[i], file.TagTypes, String.Format ("index {0}", i));
			Assert.AreEqual (contained_types[i], file.TagTypesOnDisk, String.Format ("index {0}", i));
		}

		public void CheckExif (File file, int i) {
			var tag = file.GetTag (TagTypes.TiffIFD) as IFDTag;
			Assert.IsNotNull (tag, String.Format ("Tiff Tag not contained: index {0}", i));

			var exif_ifd = tag.Structure.GetEntry(0, IFDEntryTag.ExifIFD) as SubIFDEntry;
			Assert.IsNotNull (exif_ifd, String.Format ("Exif SubIFD not contained: index {0}", i));

			Assert.AreEqual ("test comment", tag.Comment, String.Format ("index {0}", i));
		}

		public void CheckXmp (File file, int i) {
			var tag = file.GetTag (TagTypes.XMP) as XmpTag;
			Assert.IsNotNull (tag, String.Format ("XMP Tag not contained: index {0}", i));

			Assert.AreEqual ("test description", tag.Comment);
		}

		public void CheckJpegComment (File file, int i) {
			var tag = file.GetTag (TagTypes.JpegComment) as JpegCommentTag;
			Assert.IsNotNull (tag, String.Format ("JpegTag Tag not contained: index {0}", i));

			Assert.AreEqual ("Created with GIMP", tag.Comment, String.Format ("index {0}", i));
		}
	}
}
