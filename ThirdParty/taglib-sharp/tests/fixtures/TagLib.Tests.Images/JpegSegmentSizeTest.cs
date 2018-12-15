using System;
using NUnit.Framework;
using TagLib;
using TagLib.IFD;
using TagLib.IFD.Entries;
using TagLib.Jpeg;
using TagLib.Xmp;

namespace TagLib.Tests.Images
{
	[TestFixture]
	public class JpegSegmentSizeTest
	{
		private static string sample_file = TestPath.Samples + "sample.jpg";
		private static string tmp_file = TestPath.Samples + "tmpwrite_exceed_segment_size.jpg";

		private static int max_segment_size = 0xFFFF;

		private TagTypes contained_types =
				TagTypes.JpegComment |
				TagTypes.TiffIFD |
				TagTypes.XMP;


		private string CreateDataString (int min_size)
		{
			string src = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

			ByteVector data = new ByteVector ();

			for (int i = 0; data.Count < min_size; i++)
			{
				int index = i % src.Length;
				data.Add (src.Substring (index, src.Length - index));
			}

			return data.ToString ();
		}

		[Test]
		public void ExifExceed ()
		{
			File tmp = Utils.CreateTmpFile (sample_file, tmp_file) as File;
			CheckTags (tmp);

			var exif_tag = tmp.GetTag (TagTypes.TiffIFD) as IFDTag;

			Assert.IsNotNull (exif_tag, "exif tag");

			// ensure data is big enough
			exif_tag.Comment = CreateDataString (max_segment_size);

			Assert.IsFalse (SaveFile (tmp), "file with exceed exif segment saved");
		}

		[Test]
		public void XmpExceed ()
		{
			File tmp = Utils.CreateTmpFile (sample_file, tmp_file) as File;
			CheckTags (tmp);

			var xmp_tag = tmp.GetTag (TagTypes.XMP) as XmpTag;

			Assert.IsNotNull (xmp_tag, "xmp tag");

			// ensure data is big enough
			xmp_tag.Comment = CreateDataString (max_segment_size);

			Assert.IsFalse (SaveFile (tmp), "file with exceed xmp segment saved");
		}

		[Test]
		public void JpegCommentExceed ()
		{
			File tmp = Utils.CreateTmpFile (sample_file, tmp_file) as File;
			CheckTags (tmp);

			var com_tag = tmp.GetTag (TagTypes.JpegComment) as JpegCommentTag;

			Assert.IsNotNull (com_tag, "comment tag");

			// ensure data is big enough
			com_tag.Comment = CreateDataString (max_segment_size);

			Assert.IsFalse (SaveFile (tmp), "file with exceed comment segment saved");
		}

		private void CheckTags (File file) {
			Assert.IsTrue (file is Jpeg.File, "not a Jpeg file");

			Assert.AreEqual (contained_types, file.TagTypes);
			Assert.AreEqual (contained_types, file.TagTypesOnDisk);
		}

		private bool SaveFile (File file)
		{
			try {
				file.Save ();
			} catch (Exception) {
				return false;
			}

			return true;
		}
	}
}
