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
	public class JpegNikon4Test
	{
		private static string sample_file = TestPath.Samples + "sample_nikon4.jpg";
		private static string tmp_file = TestPath.Samples + "tmpwrite_nikon4.jpg";

		private TagTypes contained_types = TagTypes.TiffIFD;

		private File file;

		[OneTimeSetUp]
		public void Init () {
			file = File.Create (sample_file);
		}

		[Test]
		public void JpegRead () {
			CheckTags (file);
		}

		[Test]
		public void ExifRead () {
			CheckExif (file);
		}

		[Test]
		public void MakernoteRead () {
			CheckMakerNote (file);
		}

		[Test]
		public void PropertiesRead () {
			CheckProperties (file);
		}

		[Test]
		public void Rewrite () {
			File tmp = Utils.CreateTmpFile (sample_file, tmp_file);
			tmp.Save ();

			tmp = File.Create (tmp_file);

			CheckTags (tmp);
			CheckExif (tmp);
			CheckMakerNote (tmp);
			CheckProperties(tmp);
		}

		[Test]
		public void AddExif ()
		{
			AddImageMetadataTests.AddExifTest (sample_file, tmp_file, true);
		}

		[Test]
		public void AddGPS ()
		{
			AddImageMetadataTests.AddGPSTest (sample_file, tmp_file, true);
		}

		[Test]
		public void AddXMP1 ()
		{
			AddImageMetadataTests.AddXMPTest1 (sample_file, tmp_file, false);
		}

		[Test]
		public void AddXMP2 ()
		{
			AddImageMetadataTests.AddXMPTest2 (sample_file, tmp_file, false);
		}

		public void CheckTags (File file) {
			Assert.IsTrue (file is Jpeg.File, "not a Jpeg file");

			Assert.AreEqual (contained_types, file.TagTypes);
			Assert.AreEqual (contained_types, file.TagTypesOnDisk);
		}

		public void CheckExif (File file) {
			var tag = file.GetTag (TagTypes.TiffIFD) as IFDTag;

			Assert.IsNotNull (tag, "tag");

			var exif_ifd = tag.Structure.GetEntry(0, IFDEntryTag.ExifIFD) as SubIFDEntry;
			Assert.IsNotNull (exif_ifd, "Exif IFD");

			Assert.AreEqual ("NIKON CORPORATION", tag.Make);
			Assert.AreEqual ("NIKON D80", tag.Model);
			Assert.AreEqual (200, tag.ISOSpeedRatings, "ISOSpeedRatings");
			Assert.AreEqual (1.0d/50.0d, tag.ExposureTime);
			Assert.AreEqual (22.0d, tag.FNumber);
			Assert.AreEqual (105.0d, tag.FocalLength);
			Assert.AreEqual (new DateTime (2009, 04, 01, 15, 08, 17), tag.DateTime);
			Assert.AreEqual (new DateTime (2009, 04, 01, 15, 08, 17), tag.DateTimeDigitized);
			Assert.AreEqual (new DateTime (2009, 04, 01, 15, 08, 17), tag.DateTimeOriginal);
		}


		public void CheckMakerNote (File file) {
			IFDTag tag = file.GetTag (TagTypes.TiffIFD) as IFDTag;
			Assert.IsNotNull (tag, "tag");

			var makernote_ifd =
				tag.ExifIFD.GetEntry (0, (ushort) ExifEntryTag.MakerNote) as MakernoteIFDEntry;

			Assert.IsNotNull (makernote_ifd, "makernote ifd");
			Assert.AreEqual (MakernoteType.Nikon3, makernote_ifd.MakernoteType);

			var structure = makernote_ifd.Structure;
			Assert.IsNotNull (structure, "structure");
			{
				var entry = structure.GetEntry (0, 0x01) as UndefinedIFDEntry;
				Assert.IsNotNull (entry);
				ByteVector read_bytes = entry.Data;
				ByteVector expected_bytes = new ByteVector (new byte [] {48, 50, 49, 48});

				Assert.AreEqual (expected_bytes.Count, read_bytes.Count);
				for (int i = 0; i < expected_bytes.Count; i++)
					Assert.AreEqual (expected_bytes[i], read_bytes[i]);
			}
			{
				var entry = structure.GetEntry (0, 0x03) as StringIFDEntry;
				Assert.IsNotNull (entry, "entry 0x03");
				Assert.AreEqual ("COLOR", entry.Value);
			}
			{
				var entry = structure.GetEntry (0, 0x05) as StringIFDEntry;
				Assert.IsNotNull (entry, "entry 0x05");
				Assert.AreEqual ("AUTO        ", entry.Value);
			}
			{
				var entry = structure.GetEntry (0, 0x09) as StringIFDEntry;
				Assert.IsNotNull (entry, "entry 0x09");
				Assert.AreEqual ("                   ", entry.Value);
			}
			{
				var entry = structure.GetEntry (0, 0x84) as RationalArrayIFDEntry;
				Assert.IsNotNull (entry, "entry 0x84");
				var values = entry.Values;

				Assert.IsNotNull (values, "values of entry 0x84");
				Assert.AreEqual (4, values.Length);
				Assert.AreEqual (1050.0d/10.0d, (double) values[0]);
				Assert.AreEqual (1050.0d/10.0d, (double) values[1]);
				Assert.AreEqual (28.0d/10.0d, (double) values[2]);
				Assert.AreEqual (28.0d/10.0d, (double) values[3]);
			}
		}

		public void CheckProperties (File file)
		{
			Assert.AreEqual (1200, file.Properties.PhotoWidth);
			Assert.AreEqual (803, file.Properties.PhotoHeight);
			Assert.AreEqual (91, file.Properties.PhotoQuality);
		}
	}
}
