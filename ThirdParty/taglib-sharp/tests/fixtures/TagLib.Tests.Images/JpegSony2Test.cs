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
	public class JpegSony2Test
	{
		private static string sample_file = TestPath.Samples + "sample_sony2.jpg";
		private static string tmp_file = TestPath.Samples + "tmpwrite_sony2.jpg";

		private TagTypes contained_types = TagTypes.TiffIFD;

		private File file;

		[OneTimeSetUp]
		public void Init ()
		{
			file = File.Create (sample_file);
		}

		[Test]
		public void JpegRead ()
		{
			CheckTags (file);
		}

		[Test]
		public void ExifRead ()
		{
			CheckExif (file);
		}

		[Test]
		public void MakernoteRead ()
		{
			CheckMakerNote (file);
		}

		[Test]
		public void Rewrite ()
		{
			File tmp = Utils.CreateTmpFile (sample_file, tmp_file);
			tmp.Save ();

			tmp = File.Create (tmp_file);

			CheckTags (tmp);
			CheckExif (tmp);
			CheckMakerNote (tmp);
			CheckProperties (tmp);
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

		public void CheckTags (File file)
		{
			Assert.IsTrue (file is Jpeg.File, "not a Jpeg file");

			Assert.AreEqual (contained_types, file.TagTypes);
			Assert.AreEqual (contained_types, file.TagTypesOnDisk);
		}

		public void CheckExif (File file)
		{
			var tag = file.GetTag (TagTypes.TiffIFD) as IFDTag;

			Assert.IsNotNull (tag, "tag");

			var exif_ifd = tag.Structure.GetEntry(0, IFDEntryTag.ExifIFD) as SubIFDEntry;
			Assert.IsNotNull (exif_ifd, "Exif IFD");

			Assert.AreEqual ("SONY ", tag.Make);
			Assert.AreEqual ("DSLR-A700", tag.Model);
			Assert.AreEqual (400, tag.ISOSpeedRatings, "ISOSpeedRatings");
			Assert.AreEqual (1.0d/125.0d, tag.ExposureTime);
			Assert.AreEqual (5.6d, tag.FNumber);
			Assert.AreEqual (70.0d, tag.FocalLength);
			Assert.AreEqual (new DateTime (2009, 11, 06, 20, 56, 07), tag.DateTime);
			Assert.AreEqual (new DateTime (2009, 11, 06, 20, 56, 07), tag.DateTimeDigitized);
			Assert.AreEqual (new DateTime (2009, 11, 06, 20, 56, 07), tag.DateTimeOriginal);
		}

		public void CheckMakerNote (File file)
		{
			IFDTag tag = file.GetTag (TagTypes.TiffIFD) as IFDTag;
			Assert.IsNotNull (tag, "tag");

			var makernote_ifd =
				tag.ExifIFD.GetEntry (0, (ushort) ExifEntryTag.MakerNote) as MakernoteIFDEntry;

			Assert.IsNotNull (makernote_ifd, "makernote ifd");
			Assert.AreEqual (MakernoteType.Sony, makernote_ifd.MakernoteType);

			var structure = makernote_ifd.Structure;
			Assert.IsNotNull (structure, "structure");
			//Tag info from http://www.sno.phy.queensu.ca/~phil/exiftool/TagNames/Sony.html
			//0x0102: image quality
			{
				var entry = structure.GetEntry (0, 0x0102) as LongIFDEntry;
				Assert.IsNotNull (entry, "entry 0x0102");
				Assert.AreEqual (5, entry.Value);
			}
			{
				var entry = structure.GetEntry (0, 0x0104) as SRationalIFDEntry;
				Assert.IsNotNull (entry, "entry 0x0115");
				Assert.AreEqual (0.0d, (double) entry.Value);
			}
			//0x0115: white balance
			{
				var entry = structure.GetEntry (0, 0x0115) as LongIFDEntry;
				Assert.IsNotNull (entry, "entry 0x0115");
				Assert.AreEqual (80, entry.Value);
			}
			//0xb026: image stabilizer
			{
				var entry = structure.GetEntry (0, 0xb026) as LongIFDEntry;
				Assert.IsNotNull (entry, "entry 0xb026");
				Assert.AreEqual (1, entry.Value);
			}
		}

		public void CheckProperties (File file)
		{
			Assert.AreEqual (4272, file.Properties.PhotoWidth);
			Assert.AreEqual (2848, file.Properties.PhotoHeight);
			Assert.AreEqual (99, file.Properties.PhotoQuality);
		}
	}
}
