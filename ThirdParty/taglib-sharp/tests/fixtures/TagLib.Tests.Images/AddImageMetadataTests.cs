using System;
using NUnit.Framework;

using TagLib;
using TagLib.IFD;
using TagLib.IFD.Entries;
using TagLib.Xmp;

namespace TagLib.Tests.Images
{
	public static class AddImageMetadataTests
	{
		public static string test_comment = "This is a TagLib# &Test?Comment%$@_ ";
		public static readonly DateTime date_time = new DateTime (2009, 10, 15, 12, 12, 59);

		public static readonly string[] keywords = new string[] {"keyword 1", "§$&§%", "99 dsf"};

		public static void AddExifTest (string sample_file, string tmp_file, bool contains_exif)
		{
			File file = Utils.CreateTmpFile (sample_file, tmp_file);
			IFDTag exif_tag;

			if (! contains_exif) {
				exif_tag = file.GetTag (TagTypes.TiffIFD, false) as IFDTag;
				Assert.IsNull (exif_tag, "Tiff Tag contained");
			}

			exif_tag = file.GetTag (TagTypes.TiffIFD, true) as IFDTag;
			Assert.IsNotNull (exif_tag, "Tiff Tag not created");

			exif_tag.Comment = test_comment;
			exif_tag.DateTime = date_time;
			exif_tag.DateTimeDigitized = date_time;

			Assert.AreEqual (test_comment, exif_tag.Comment);
			Assert.AreEqual (date_time, exif_tag.DateTime);
			Assert.AreEqual (date_time, exif_tag.DateTimeDigitized);
			Assert.AreEqual (date_time, exif_tag.DateTimeOriginal);

			// Store and reload file
			file.Save ();
			file = File.Create (tmp_file);

			exif_tag = file.GetTag (TagTypes.TiffIFD, false) as IFDTag;
			Assert.IsNotNull (exif_tag, "Tiff Tag not read");

			Assert.AreEqual (test_comment, exif_tag.Comment);
			Assert.AreEqual (date_time, exif_tag.DateTime);
			Assert.AreEqual (date_time, exif_tag.DateTimeDigitized);
			Assert.AreEqual (date_time, exif_tag.DateTimeOriginal);

		}

		public static void AddGPSTest (string sample_file, string tmp_file, bool contains_tiff)
		{
			AddGPSTest (sample_file, tmp_file, contains_tiff, +53.231d, +168.19823d, 40.0d);
			AddGPSTest (sample_file, tmp_file, contains_tiff, -21.342d, +88.18232d, -39.0d);
			AddGPSTest (sample_file, tmp_file, contains_tiff, +75.12931d, -8.98712d, -10.0d);
			AddGPSTest (sample_file, tmp_file, contains_tiff, -42.1023d, -113.12432d, 1920.0d);
			AddGPSTest (sample_file, tmp_file, contains_tiff, -87.23d, +23.9743d, 0.0000123d);
			AddGPSTest (sample_file, tmp_file, contains_tiff, +72.123d, +17.432d, -0.0000089d);
		}

		public static void AddXMPTest1 (string sample_file, string tmp_file, bool contains_xmp)
		{
			File file = Utils.CreateTmpFile (sample_file, tmp_file);
			XmpTag xmp_tag;

			if (! contains_xmp) {
				xmp_tag = file.GetTag (TagTypes.XMP, false) as XmpTag;
				Assert.IsNull (xmp_tag, "XMP Tag contained");
			}

			xmp_tag = file.GetTag (TagTypes.XMP, true) as XmpTag;
			Assert.IsNotNull (xmp_tag, "XMP Tag not created");

			xmp_tag.Keywords = keywords;
			xmp_tag.Comment = test_comment;
			xmp_tag.Software = null;

			Assert.AreEqual (keywords, xmp_tag.Keywords);
			Assert.AreEqual (test_comment, xmp_tag.Comment);
			Assert.AreEqual (null, xmp_tag.Software);

			// Store and reload file
			file.Save ();
			file = File.Create (tmp_file);

			xmp_tag = file.GetTag (TagTypes.XMP, false) as XmpTag;
			Assert.IsNotNull (xmp_tag, "XMP Tag not read");

			Assert.AreEqual (keywords, xmp_tag.Keywords);
			Assert.AreEqual (test_comment, xmp_tag.Comment);
			Assert.AreEqual (null, xmp_tag.Software);
		}

		public static void AddXMPTest2 (string sample_file, string tmp_file, bool contains_xmp)
		{
			File file = Utils.CreateTmpFile (sample_file, tmp_file);
			XmpTag xmp_tag;

			if (! contains_xmp) {
				xmp_tag = file.GetTag (TagTypes.XMP, false) as XmpTag;
				Assert.IsNull (xmp_tag, "XMP Tag contained");
			}

			xmp_tag = file.GetTag (TagTypes.XMP, true) as XmpTag;
			Assert.IsNotNull (xmp_tag, "XMP Tag not created");

			xmp_tag.Keywords = null;
			xmp_tag.Comment = null;
			xmp_tag.Software = test_comment;

			Assert.AreEqual (new string [] {}, xmp_tag.Keywords);
			Assert.AreEqual (null, xmp_tag.Comment);
			Assert.AreEqual (test_comment, xmp_tag.Software);

			// Store and reload file
			file.Save ();
			file = File.Create (tmp_file);

			xmp_tag = file.GetTag (TagTypes.XMP, false) as XmpTag;
			Assert.IsNotNull (xmp_tag, "XMP Tag not read");

			Assert.AreEqual (new string [] {}, xmp_tag.Keywords);
			Assert.AreEqual (null, xmp_tag.Comment);
			Assert.AreEqual (test_comment, xmp_tag.Software);
		}

		public static void AddAllTest (string sample_file, string tmp_file)
		{
			Image.File file = Utils.CreateTmpFile (sample_file, tmp_file) as Image.File;

			Assert.IsNotNull (file, "file");

			// ensure all tags are present
			file.GetTag (TagTypes.XMP, true);
			file.GetTag (TagTypes.TiffIFD, true);

			file.ImageTag.Comment = test_comment;
			file.ImageTag.Keywords = keywords;
			file.ImageTag.Rating = 4;
			file.ImageTag.DateTime = date_time;
			file.ImageTag.Latitude = 3.0;
			file.ImageTag.Longitude = 3.0;
			file.ImageTag.Altitude = 3.0;

			Assert.AreEqual (test_comment, file.ImageTag.Comment);
			Assert.AreEqual (keywords, file.ImageTag.Keywords);
			Assert.AreEqual (4, file.ImageTag.Rating);
			Assert.AreEqual (date_time, file.ImageTag.DateTime);
			Assert.AreEqual (3.0, file.ImageTag.Latitude);
			Assert.AreEqual (3.0, file.ImageTag.Longitude);
			Assert.AreEqual (3.0, file.ImageTag.Altitude);

			// Store and reload file
			file.Save ();
			file = File.Create (tmp_file) as Image.File;

			Assert.IsNotNull (file, "tmp file");

			Assert.AreEqual (test_comment, file.ImageTag.Comment);
			Assert.AreEqual (keywords, file.ImageTag.Keywords);
			Assert.AreEqual (4, file.ImageTag.Rating);
			Assert.AreEqual (date_time, file.ImageTag.DateTime);
			Assert.AreEqual (3.0, file.ImageTag.Latitude);
			Assert.AreEqual (3.0, file.ImageTag.Longitude);
			Assert.AreEqual (3.0, file.ImageTag.Altitude);

			var xmp_tag = file.GetTag (TagTypes.XMP, false) as XmpTag;
			Assert.IsNotNull (xmp_tag, "XMP Tag not read");
			Assert.AreEqual (test_comment, xmp_tag.Comment);
			Assert.AreEqual (keywords, xmp_tag.Keywords);
			Assert.AreEqual (4, xmp_tag.Rating);

			var ifd_tag = file.GetTag (TagTypes.TiffIFD, false) as IFDTag;
			Assert.IsNotNull (ifd_tag, "Tiff Tag not read");
			Assert.AreEqual (test_comment, ifd_tag.Comment);
			Assert.AreEqual (date_time, ifd_tag.DateTime);
			Assert.AreEqual (3.0, ifd_tag.Latitude);
			Assert.AreEqual (3.0, ifd_tag.Longitude);
			Assert.AreEqual (3.0, ifd_tag.Altitude);
		}

		private static void AddGPSTest (string sample_file, string tmp_file, bool contains_tiff, double latitude, double longitude, double altitude)
		{
			File file = Utils.CreateTmpFile (sample_file, tmp_file);
			IFDTag ifd;

			if (! contains_tiff) {
				ifd = file.GetTag (TagTypes.TiffIFD, false) as IFDTag;
				Assert.IsNull (ifd, "Tiff IFD not contained");
			}

			ifd = file.GetTag (TagTypes.TiffIFD, true) as IFDTag;
			Assert.IsNotNull (ifd, "Tiff IFD not created");

			ifd.Latitude = latitude;
			ifd.Longitude = longitude;
			ifd.Altitude = altitude;

			Assert.IsNotNull (ifd.Latitude, "Latitude");
			Assert.IsNotNull (ifd.Longitude, "Longitude");
			Assert.IsNotNull (ifd.Altitude, "Altitude");
			AssertEqualDouble (latitude, ifd.Latitude.Value, 0.00000001);
			AssertEqualDouble (longitude, ifd.Longitude.Value, 0.00000001);
			AssertEqualDouble (altitude, ifd.Altitude.Value, 0.00000001);

			// Store and reload file
			file.Save ();
			file = File.Create (tmp_file);

			ifd = file.GetTag (TagTypes.TiffIFD, false) as IFDTag;
			Assert.IsNotNull (ifd, "Tiff IFD not read");

			Assert.IsNotNull (ifd.Latitude, "Latitude");
			Assert.IsNotNull (ifd.Longitude, "Longitude");
			Assert.IsNotNull (ifd.Altitude, "Altitude");
			AssertEqualDouble (latitude, ifd.Latitude.Value, 0.00000001);
			AssertEqualDouble (longitude, ifd.Longitude.Value, 0.00000001);
			AssertEqualDouble (altitude, ifd.Altitude.Value, 0.00000001);
		}

		private static void AssertEqualDouble (double d1, double d2, double acc)
		{
			Assert.Less (d1 - acc, d2);
			Assert.Greater (d1 + acc, d2);
		}
	}
}
