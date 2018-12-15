using System;
using NUnit.Framework;
using TagLib.IFD;
using TagLib.IFD.Entries;
using TagLib.IFD.Tags;
using TagLib.Xmp;
using TagLib.Tests.Images.Validators;

namespace TagLib.Tests.Images
{
	[TestFixture]
	public class PefPentaxKd10Test
	{
		[Test]
		public void Test ()
		{
			ImageTest.Run ("raw-samples/PEF", "RAW_PENTAX_KD10.PEF",
				false,
				new PefPentaxKd10TestInvariantValidator ()
			);
		}
	}

	public class PefPentaxKd10TestInvariantValidator : IMetadataInvariantValidator
	{
		public void ValidateMetadataInvariants (Image.File file)
		{
			Assert.IsNotNull (file);

			//  ---------- Start of ImageTag tests ----------

			var imagetag = file.ImageTag;
			Assert.IsNotNull (imagetag);
			Assert.AreEqual (String.Empty, imagetag.Comment, "Comment");
			Assert.AreEqual (new string [] {}, imagetag.Keywords, "Keywords");
			Assert.AreEqual (null, imagetag.Rating, "Rating");
			Assert.AreEqual (Image.ImageOrientation.LeftBottom, imagetag.Orientation, "Orientation");
			Assert.AreEqual ("K10D Ver 1.31          ", imagetag.Software, "Software");
			Assert.AreEqual (null, imagetag.Latitude, "Latitude");
			Assert.AreEqual (null, imagetag.Longitude, "Longitude");
			Assert.AreEqual (null, imagetag.Altitude, "Altitude");
			Assert.AreEqual ((double) 1/160, imagetag.ExposureTime, "ExposureTime");
			Assert.AreEqual (4.5, imagetag.FNumber, "FNumber");
			Assert.AreEqual (640, imagetag.ISOSpeedRatings, "ISOSpeedRatings");
			Assert.AreEqual (190, imagetag.FocalLength, "FocalLength");
			Assert.AreEqual (285, imagetag.FocalLengthIn35mmFilm, "FocalLengthIn35mmFilm");
			Assert.AreEqual ("PENTAX Corporation ", imagetag.Make, "Make");
			Assert.AreEqual ("PENTAX K10D        ", imagetag.Model, "Model");
			Assert.AreEqual (null, imagetag.Creator, "Creator");

			var properties = file.Properties;
			Assert.IsNotNull (properties);
			Assert.AreEqual (3936, properties.PhotoWidth, "PhotoWidth");
			Assert.AreEqual (2624, properties.PhotoHeight, "PhotoHeight");

			//  ---------- End of ImageTag tests ----------

			//  ---------- Start of IFD tests ----------

			var tag = file.GetTag (TagTypes.TiffIFD) as IFDTag;
			Assert.IsNotNull (tag, "IFD tag not found");

			var structure = tag.Structure;

			// Image.0x0100 (ImageWidth/Long/1) "3936"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.ImageWidth);
				Assert.IsNotNull (entry, "Entry 0x0100 missing in IFD 0");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (3936, (entry as LongIFDEntry).Value);
			}
			// Image.0x0101 (ImageLength/Long/1) "2624"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.ImageLength);
				Assert.IsNotNull (entry, "Entry 0x0101 missing in IFD 0");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (2624, (entry as LongIFDEntry).Value);
			}
			// Image.0x0102 (BitsPerSample/Short/1) "12"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.BitsPerSample);
				Assert.IsNotNull (entry, "Entry 0x0102 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (12, (entry as ShortIFDEntry).Value);
			}
			// Image.0x0103 (Compression/Short/1) "65535"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.Compression);
				Assert.IsNotNull (entry, "Entry 0x0103 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (65535, (entry as ShortIFDEntry).Value);
			}
			// Image.0x0106 (PhotometricInterpretation/Short/1) "32803"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.PhotometricInterpretation);
				Assert.IsNotNull (entry, "Entry 0x0106 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (32803, (entry as ShortIFDEntry).Value);
			}
			// Image.0x010F (Make/Ascii/20) "PENTAX Corporation "
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.Make);
				Assert.IsNotNull (entry, "Entry 0x010F missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("PENTAX Corporation ", (entry as StringIFDEntry).Value);
			}
			// Image.0x0110 (Model/Ascii/20) "PENTAX K10D        "
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.Model);
				Assert.IsNotNull (entry, "Entry 0x0110 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("PENTAX K10D        ", (entry as StringIFDEntry).Value);
			}
			// Image.0x0111 (StripOffsets/StripOffsets/1) "84700"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.StripOffsets);
				Assert.IsNotNull (entry, "Entry 0x0111 missing in IFD 0");
				Assert.IsNotNull (entry as StripOffsetsIFDEntry, "Entry is not a strip offsets entry!");
				Assert.AreEqual (1, (entry as StripOffsetsIFDEntry).Values.Length);
			}
			// Image.0x0112 (Orientation/Short/1) "8"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.Orientation);
				Assert.IsNotNull (entry, "Entry 0x0112 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (8, (entry as ShortIFDEntry).Value);
			}
			// Image.0x0115 (SamplesPerPixel/Short/1) "1"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.SamplesPerPixel);
				Assert.IsNotNull (entry, "Entry 0x0115 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1, (entry as ShortIFDEntry).Value);
			}
			// Image.0x0116 (RowsPerStrip/Long/1) "2624"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.RowsPerStrip);
				Assert.IsNotNull (entry, "Entry 0x0116 missing in IFD 0");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (2624, (entry as LongIFDEntry).Value);
			}
			// Image.0x0117 (StripByteCounts/Long/1) "9000666"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.StripByteCounts);
				Assert.IsNotNull (entry, "Entry 0x0117 missing in IFD 0");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (9000666, (entry as LongIFDEntry).Value);
			}
			// Image.0x011A (XResolution/Rational/1) "72/1"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.XResolution);
				Assert.IsNotNull (entry, "Entry 0x011A missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (72, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (1, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Image.0x011B (YResolution/Rational/1) "72/1"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.YResolution);
				Assert.IsNotNull (entry, "Entry 0x011B missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (72, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (1, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Image.0x011C (PlanarConfiguration/Short/1) "1"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.PlanarConfiguration);
				Assert.IsNotNull (entry, "Entry 0x011C missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1, (entry as ShortIFDEntry).Value);
			}
			// Image.0x0128 (ResolutionUnit/Short/1) "2"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.ResolutionUnit);
				Assert.IsNotNull (entry, "Entry 0x0128 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (2, (entry as ShortIFDEntry).Value);
			}
			// Image.0x0131 (Software/Ascii/24) "K10D Ver 1.31          "
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.Software);
				Assert.IsNotNull (entry, "Entry 0x0131 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("K10D Ver 1.31          ", (entry as StringIFDEntry).Value);
			}
			// Image.0x0132 (DateTime/Ascii/20) "2010:07:04 11:24:09"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.DateTime);
				Assert.IsNotNull (entry, "Entry 0x0132 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("2010:07:04 11:24:09", (entry as StringIFDEntry).Value);
			}
			// Image.0x8769 (ExifTag/SubIFD/1) "342"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.ExifIFD);
				Assert.IsNotNull (entry, "Entry 0x8769 missing in IFD 0");
				Assert.IsNotNull (entry as SubIFDEntry, "Entry is not a sub IFD!");
			}

			var exif = structure.GetEntry (0, (ushort) IFDEntryTag.ExifIFD) as SubIFDEntry;
			Assert.IsNotNull (exif, "Exif tag not found");
			var exif_structure = exif.Structure;

			// Photo.0x829A (ExposureTime/Rational/1) "1/160"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ExposureTime);
				Assert.IsNotNull (entry, "Entry 0x829A missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (1, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (160, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Photo.0x829D (FNumber/Rational/1) "45/10"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.FNumber);
				Assert.IsNotNull (entry, "Entry 0x829D missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (45, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (10, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Photo.0x8822 (ExposureProgram/Short/1) "4"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ExposureProgram);
				Assert.IsNotNull (entry, "Entry 0x8822 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (4, (entry as ShortIFDEntry).Value);
			}
			// Photo.0x8827 (ISOSpeedRatings/Short/1) "640"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ISOSpeedRatings);
				Assert.IsNotNull (entry, "Entry 0x8827 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (640, (entry as ShortIFDEntry).Value);
			}
			// Photo.0x9003 (DateTimeOriginal/Ascii/20) "2010:07:04 11:24:09"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.DateTimeOriginal);
				Assert.IsNotNull (entry, "Entry 0x9003 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("2010:07:04 11:24:09", (entry as StringIFDEntry).Value);
			}
			// Photo.0x9004 (DateTimeDigitized/Ascii/20) "2010:07:04 11:24:09"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.DateTimeDigitized);
				Assert.IsNotNull (entry, "Entry 0x9004 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("2010:07:04 11:24:09", (entry as StringIFDEntry).Value);
			}
			// Photo.0x9204 (ExposureBiasValue/SRational/1) "0/10"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ExposureBiasValue);
				Assert.IsNotNull (entry, "Entry 0x9204 missing in IFD 0");
				Assert.IsNotNull (entry as SRationalIFDEntry, "Entry is not a srational!");
				Assert.AreEqual (0, (entry as SRationalIFDEntry).Value.Numerator);
				Assert.AreEqual (10, (entry as SRationalIFDEntry).Value.Denominator);
			}
			// Photo.0x9207 (MeteringMode/Short/1) "2"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.MeteringMode);
				Assert.IsNotNull (entry, "Entry 0x9207 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (2, (entry as ShortIFDEntry).Value);
			}
			// Photo.0x9209 (Flash/Short/1) "16"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.Flash);
				Assert.IsNotNull (entry, "Entry 0x9209 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (16, (entry as ShortIFDEntry).Value);
			}
			// Photo.0x920A (FocalLength/Rational/1) "19000/100"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.FocalLength);
				Assert.IsNotNull (entry, "Entry 0x920A missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (19000, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (100, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Photo.0x927C (MakerNote/MakerNote/77824) "(Value ommitted)"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.MakerNote);
				Assert.IsNotNull (entry, "Entry 0x927C missing in IFD 0");
				Assert.IsNotNull (entry as MakernoteIFDEntry, "Entry is not a makernote IFD!");
			}

			var makernote = exif_structure.GetEntry (0, (ushort) ExifEntryTag.MakerNote) as MakernoteIFDEntry;
			Assert.IsNotNull (makernote, "MakerNote tag not found");
			var makernote_structure = makernote.Structure;

			// Pentax.0x0000 (Version/Byte/4) "3 0 0 0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.Version);
				Assert.IsNotNull (entry, "Entry 0x0000 missing in IFD 0");
				Assert.IsNotNull (entry as ByteVectorIFDEntry, "Entry is not a byte array!");
				var parsed_bytes = (entry as ByteVectorIFDEntry).Data.Data;
				var bytes = new byte [] { 3, 0, 0, 0 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Pentax.0x0001 (Mode/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.Mode);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Pentax.0x0002 (PreviewResolution/Short/2) "640 480"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.PreviewResolution);
				Assert.IsNotNull (entry, "Entry 0x0002 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 640, 480 }, (entry as ShortArrayIFDEntry).Values);
			}
			// Pentax.0x0003 (PreviewLength/Long/1) "24947"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.PreviewLength);
				Assert.IsNotNull (entry, "Entry 0x0003 missing in IFD 0");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (24947, (entry as LongIFDEntry).Value);
			}
			// Pentax.0x0004 (PreviewOffset/Long/1) "32520"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.PreviewOffset);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (32520, (entry as LongIFDEntry).Value);
			}
			// Pentax.0x0005 (ModelID/Long/1) "76830"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.ModelID);
				Assert.IsNotNull (entry, "Entry 0x0005 missing in IFD 0");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (76830, (entry as LongIFDEntry).Value);
			}
			// Pentax.0x0006 (Date/Undefined/4) "7 218 7 4"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.Date);
				Assert.IsNotNull (entry, "Entry 0x0006 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 7, 218, 7, 4 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Pentax.0x0007 (Time/Undefined/3) "11 24 9"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.Time);
				Assert.IsNotNull (entry, "Entry 0x0007 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 11, 24, 9 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Pentax.0x0008 (Quality/Short/1) "4"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.Quality);
				Assert.IsNotNull (entry, "Entry 0x0008 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (4, (entry as ShortIFDEntry).Value);
			}
			// Pentax.0x000C (Flash/Short/2) "1 63"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.Flash);
				Assert.IsNotNull (entry, "Entry 0x000C missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 1, 63 }, (entry as ShortArrayIFDEntry).Values);
			}
			// Pentax.0x000D (Focus/Short/1) "16"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.Focus);
				Assert.IsNotNull (entry, "Entry 0x000D missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (16, (entry as ShortIFDEntry).Value);
			}
			// Pentax.0x000E (AFPoint/Short/1) "65534"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.AFPoint);
				Assert.IsNotNull (entry, "Entry 0x000E missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (65534, (entry as ShortIFDEntry).Value);
			}
			// Pentax.0x0012 (ExposureTime/Long/1) "625"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.ExposureTime);
				Assert.IsNotNull (entry, "Entry 0x0012 missing in IFD 0");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (625, (entry as LongIFDEntry).Value);
			}
			// Pentax.0x0013 (FNumber/Short/1) "45"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.FNumber);
				Assert.IsNotNull (entry, "Entry 0x0013 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (45, (entry as ShortIFDEntry).Value);
			}
			// Pentax.0x0014 (ISO/Short/1) "14"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.ISO);
				Assert.IsNotNull (entry, "Entry 0x0014 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (14, (entry as ShortIFDEntry).Value);
			}
			// Pentax.0x0016 (ExposureCompensation/Short/1) "50"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.ExposureCompensation);
				Assert.IsNotNull (entry, "Entry 0x0016 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (50, (entry as ShortIFDEntry).Value);
			}
			// Pentax.0x0017 (MeteringMode/Short/1) "1"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.MeteringMode);
				Assert.IsNotNull (entry, "Entry 0x0017 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1, (entry as ShortIFDEntry).Value);
			}
			// Pentax.0x0018 (AutoBracketing/Short/2) "0 0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.AutoBracketing);
				Assert.IsNotNull (entry, "Entry 0x0018 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 0, 0 }, (entry as ShortArrayIFDEntry).Values);
			}
			// Pentax.0x0019 (WhiteBallance/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.WhiteBallance);
				Assert.IsNotNull (entry, "Entry 0x0019 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Pentax.0x001A (WhiteBallanceMode/Short/1) "8"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.WhiteBallanceMode);
				Assert.IsNotNull (entry, "Entry 0x001A missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (8, (entry as ShortIFDEntry).Value);
			}
			// Pentax.0x001D (FocalLength/Long/1) "19000"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.FocalLength);
				Assert.IsNotNull (entry, "Entry 0x001D missing in IFD 0");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (19000, (entry as LongIFDEntry).Value);
			}
			// Pentax.0x001F (Saturation/Short/1) "1"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.Saturation);
				Assert.IsNotNull (entry, "Entry 0x001F missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1, (entry as ShortIFDEntry).Value);
			}
			// Pentax.0x0020 (Contrast/Short/1) "1"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.Contrast);
				Assert.IsNotNull (entry, "Entry 0x0020 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1, (entry as ShortIFDEntry).Value);
			}
			// Pentax.0x0021 (Sharpness/Short/1) "1"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.Sharpness);
				Assert.IsNotNull (entry, "Entry 0x0021 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1, (entry as ShortIFDEntry).Value);
			}
			// Pentax.0x0022 (Location/Short/1) "1"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.Location);
				Assert.IsNotNull (entry, "Entry 0x0022 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1, (entry as ShortIFDEntry).Value);
			}
			// Pentax.0x0023 (Hometown/Short/1) "24"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.Hometown);
				Assert.IsNotNull (entry, "Entry 0x0023 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (24, (entry as ShortIFDEntry).Value);
			}
			// Pentax.0x0024 (Destination/Short/1) "24"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.Destination);
				Assert.IsNotNull (entry, "Entry 0x0024 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (24, (entry as ShortIFDEntry).Value);
			}
			// Pentax.0x0025 (HometownDST/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.HometownDST);
				Assert.IsNotNull (entry, "Entry 0x0025 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Pentax.0x0026 (DestinationDST/Short/1) "1"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.DestinationDST);
				Assert.IsNotNull (entry, "Entry 0x0026 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1, (entry as ShortIFDEntry).Value);
			}
			// Pentax.0x0027 (DSPFirmwareVersion/Undefined/4) "254 224 255 236"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.DSPFirmwareVersion);
				Assert.IsNotNull (entry, "Entry 0x0027 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 254, 224, 255, 236 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Pentax.0x0028 (CPUFirmwareVersion/Undefined/4) "254 224 255 236"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.CPUFirmwareVersion);
				Assert.IsNotNull (entry, "Entry 0x0028 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 254, 224, 255, 236 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Pentax.0x002D (EffectiveLV/Short/1) "9472"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.EffectiveLV);
				Assert.IsNotNull (entry, "Entry 0x002D missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (9472, (entry as ShortIFDEntry).Value);
			}
			// Pentax.0x0033 (PictureMode/Byte/3) "4 0 1"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.PictureMode);
				Assert.IsNotNull (entry, "Entry 0x0033 missing in IFD 0");
				Assert.IsNotNull (entry as ByteVectorIFDEntry, "Entry is not a byte array!");
				var parsed_bytes = (entry as ByteVectorIFDEntry).Data.Data;
				var bytes = new byte [] { 4, 0, 1 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Pentax.0x0034 (DriveMode/Byte/4) "0 0 0 0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.DriveMode);
				Assert.IsNotNull (entry, "Entry 0x0034 missing in IFD 0");
				Assert.IsNotNull (entry as ByteVectorIFDEntry, "Entry is not a byte array!");
				var parsed_bytes = (entry as ByteVectorIFDEntry).Data.Data;
				var bytes = new byte [] { 0, 0, 0, 0 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Pentax.0x0035 (0x0035/Short/2) "11894 7962"
			{
				// TODO: Unknown IFD tag: Pentax / 0x0035
				var entry = makernote_structure.GetEntry (0, (ushort) 0x0035);
				Assert.IsNotNull (entry, "Entry 0x0035 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 11894, 7962 }, (entry as ShortArrayIFDEntry).Values);
			}
			// Pentax.0x0037 (ColorSpace/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.ColorSpace);
				Assert.IsNotNull (entry, "Entry 0x0037 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Pentax.0x0038 (0x0038/Short/2) "8 8"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.ImageAreaOffset);
				Assert.IsNotNull (entry, "Entry 0x0038 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 8, 8 }, (entry as ShortArrayIFDEntry).Values);
			}
			// Pentax.0x0039 (0x0039/Short/2) "3872 2592"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.RawImageSize);
				Assert.IsNotNull (entry, "Entry 0x0039 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 3872, 2592 }, (entry as ShortArrayIFDEntry).Values);
			}
			// Pentax.0x003D (0x003d/Short/1) "8192"
			{
				// TODO: Unknown IFD tag: Pentax / 0x003D
				var entry = makernote_structure.GetEntry (0, (ushort) 0x003D);
				Assert.IsNotNull (entry, "Entry 0x003D missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (8192, (entry as ShortIFDEntry).Value);
			}
			// Pentax.0x003E (PreviewImageBorders/Byte/4) "26 26 0 0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.PreviewImageBorders);
				Assert.IsNotNull (entry, "Entry 0x003E missing in IFD 0");
				Assert.IsNotNull (entry as ByteVectorIFDEntry, "Entry is not a byte array!");
				var parsed_bytes = (entry as ByteVectorIFDEntry).Data.Data;
				var bytes = new byte [] { 26, 26, 0, 0 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Pentax.0x003F (LensType/Byte/3) "3 255 0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.LensType);
				Assert.IsNotNull (entry, "Entry 0x003F missing in IFD 0");
				Assert.IsNotNull (entry as ByteVectorIFDEntry, "Entry is not a byte array!");
				var parsed_bytes = (entry as ByteVectorIFDEntry).Data.Data;
				var bytes = new byte [] { 3, 255, 0 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Pentax.0x0047 (Temperature/SByte/1) "23"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.Temperature);
				Assert.IsNotNull (entry, "Entry 0x0047 missing in IFD 0");
				Assert.IsNotNull (entry as SByteIFDEntry, "Entry is not a signed byte!");
				Assert.AreEqual (23, (entry as SByteIFDEntry).Value);
			}
			// Pentax.0x0048 (AELock/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.AELock);
				Assert.IsNotNull (entry, "Entry 0x0048 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Pentax.0x0049 (NoiseReduction/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.NoiseReduction);
				Assert.IsNotNull (entry, "Entry 0x0049 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Pentax.0x004D (FlashExposureCompensation/SLong/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.FlashExposureCompensation);
				Assert.IsNotNull (entry, "Entry 0x004D missing in IFD 0");
				Assert.IsNotNull (entry as SLongIFDEntry, "Entry is not a signed long!");
				Assert.AreEqual (0, (entry as SLongIFDEntry).Value);
			}
			// Pentax.0x004F (ImageTone/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.ImageTone);
				Assert.IsNotNull (entry, "Entry 0x004F missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Pentax.0x0050 (ColorTemperature/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.ColorTemperature);
				Assert.IsNotNull (entry, "Entry 0x0050 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Pentax.0x0053 (0x0053/Undefined/4) "187 113 0 43"
			{
				// TODO: Unknown IFD tag: Pentax / 0x0053
				var entry = makernote_structure.GetEntry (0, (ushort) 0x0053);
				Assert.IsNotNull (entry, "Entry 0x0053 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 187, 113, 0, 43 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Pentax.0x0054 (0x0054/Undefined/4) "176 134 0 26"
			{
				// TODO: Unknown IFD tag: Pentax / 0x0054
				var entry = makernote_structure.GetEntry (0, (ushort) 0x0054);
				Assert.IsNotNull (entry, "Entry 0x0054 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 176, 134, 0, 26 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Pentax.0x0055 (0x0055/Undefined/4) "184 86 0 37"
			{
				// TODO: Unknown IFD tag: Pentax / 0x0055
				var entry = makernote_structure.GetEntry (0, (ushort) 0x0055);
				Assert.IsNotNull (entry, "Entry 0x0055 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 184, 86, 0, 37 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Pentax.0x0056 (0x0056/Undefined/4) "196 157 0 50"
			{
				// TODO: Unknown IFD tag: Pentax / 0x0056
				var entry = makernote_structure.GetEntry (0, (ushort) 0x0056);
				Assert.IsNotNull (entry, "Entry 0x0056 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 196, 157, 0, 50 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Pentax.0x0057 (0x0057/Undefined/4) "185 14 0 122"
			{
				// TODO: Unknown IFD tag: Pentax / 0x0057
				var entry = makernote_structure.GetEntry (0, (ushort) 0x0057);
				Assert.IsNotNull (entry, "Entry 0x0057 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 185, 14, 0, 122 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Pentax.0x0058 (0x0058/Undefined/4) "189 16 0 67"
			{
				// TODO: Unknown IFD tag: Pentax / 0x0058
				var entry = makernote_structure.GetEntry (0, (ushort) 0x0058);
				Assert.IsNotNull (entry, "Entry 0x0058 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 189, 16, 0, 67 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Pentax.0x0059 (0x0059/Undefined/4) "191 152 0 85"
			{
				// TODO: Unknown IFD tag: Pentax / 0x0059
				var entry = makernote_structure.GetEntry (0, (ushort) 0x0059);
				Assert.IsNotNull (entry, "Entry 0x0059 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 191, 152, 0, 85 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Pentax.0x005A (0x005a/Undefined/4) "186 154 0 0"
			{
				// TODO: Unknown IFD tag: Pentax / 0x005A
				var entry = makernote_structure.GetEntry (0, (ushort) 0x005A);
				Assert.IsNotNull (entry, "Entry 0x005A missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 186, 154, 0, 0 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Pentax.0x005C (ShakeReduction/Byte/4) "1 1 255 47"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.ShakeReduction);
				Assert.IsNotNull (entry, "Entry 0x005C missing in IFD 0");
				Assert.IsNotNull (entry as ByteVectorIFDEntry, "Entry is not a byte array!");
				var parsed_bytes = (entry as ByteVectorIFDEntry).Data.Data;
				var bytes = new byte [] { 1, 1, 255, 47 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Pentax.0x005D (ShutterCount/Undefined/4) "243 61 210 115"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.ShutterCount);
				Assert.IsNotNull (entry, "Entry 0x005D missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 243, 61, 210, 115 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Pentax.0x0062 (0x0062/Short/1) "1"
			{
				// TODO: Unknown IFD tag: Pentax / 0x0062
				var entry = makernote_structure.GetEntry (0, (ushort) 0x0062);
				Assert.IsNotNull (entry, "Entry 0x0062 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1, (entry as ShortIFDEntry).Value);
			}
			// Pentax.0x0200 (BlackPoint/Short/4) "0 0 0 0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.BlackPoint);
				Assert.IsNotNull (entry, "Entry 0x0200 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 0, 0, 0, 0 }, (entry as ShortArrayIFDEntry).Values);
			}
			// Pentax.0x0201 (WhitePoint/Short/4) "12960 8192 8192 9888"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.WhitePoint);
				Assert.IsNotNull (entry, "Entry 0x0201 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 12960, 8192, 8192, 9888 }, (entry as ShortArrayIFDEntry).Values);
			}
			// Pentax.0x0205 (ShotInfo/Undefined/23) "4 32 1 33 0 32 32 0 3 0 0 0 0 4 0 156 1 230 127 116 40 64 0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.ShotInfo);
				Assert.IsNotNull (entry, "Entry 0x0205 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 4, 32, 1, 33, 0, 32, 32, 0, 3, 0, 0, 0, 0, 4, 0, 156, 1, 230, 127, 116, 40, 64, 0 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Pentax.0x0206 (AEInfo/Undefined/16) "127 104 53 64 0 164 2 4 0 104 104 144 16 64 0 110"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.AEInfo);
				Assert.IsNotNull (entry, "Entry 0x0206 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 127, 104, 53, 64, 0, 164, 2, 4, 0, 104, 104, 144, 16, 64, 0, 110 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Pentax.0x0207 (LensInfo/Undefined/69) "131 0 0 255 0 40 148 106 65 69 6 238 65 78 153 80 40 1 73 107 251 255 255 255 0 0 69 6 238 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.LensInfo);
				Assert.IsNotNull (entry, "Entry 0x0207 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 131, 0, 0, 255, 0, 40, 148, 106, 65, 69, 6, 238, 65, 78, 153, 80, 40, 1, 73, 107, 251, 255, 255, 255, 0, 0, 69, 6, 238, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Pentax.0x0208 (FlashInfo/Undefined/27) "0 240 63 0 0 0 0 0 166 20 41 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.FlashInfo);
				Assert.IsNotNull (entry, "Entry 0x0208 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 0, 240, 63, 0, 0, 0, 0, 0, 166, 20, 41, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Pentax.0x0209 (AEMeteringSegments/Undefined/16) "114 114 113 104 114 105 111 110 112 107 108 107 110 105 107 109"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.AEMeteringSegments);
				Assert.IsNotNull (entry, "Entry 0x0209 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 114, 114, 113, 104, 114, 105, 111, 110, 112, 107, 108, 107, 110, 105, 107, 109 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Pentax.0x020A (FlashADump/Undefined/16) "0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.FlashADump);
				Assert.IsNotNull (entry, "Entry 0x020A missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Pentax.0x020B (FlashBDump/Undefined/16) "0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.FlashBDump);
				Assert.IsNotNull (entry, "Entry 0x020B missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Pentax.0x020D (WB_RGGBLevelsDaylight/Short/4) "13600 8192 8192 8765"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.WB_RGGBLevelsDaylight);
				Assert.IsNotNull (entry, "Entry 0x020D missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 13600, 8192, 8192, 8765 }, (entry as ShortArrayIFDEntry).Values);
			}
			// Pentax.0x020E (WB_RGGBLevelsShade/Short/4) "16128 8192 8192 6635"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.WB_RGGBLevelsShade);
				Assert.IsNotNull (entry, "Entry 0x020E missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 16128, 8192, 8192, 6635 }, (entry as ShortArrayIFDEntry).Values);
			}
			// Pentax.0x020F (WB_RGGBLevelsCloudy/Short/4) "14560 8192 8192 7782"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.WB_RGGBLevelsCloudy);
				Assert.IsNotNull (entry, "Entry 0x020F missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 14560, 8192, 8192, 7782 }, (entry as ShortArrayIFDEntry).Values);
			}
			// Pentax.0x0210 (WB_RGGBLevelsTungsten/Short/4) "8192 8192 8192 20971"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.WB_RGGBLevelsTungsten);
				Assert.IsNotNull (entry, "Entry 0x0210 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 8192, 8192, 8192, 20971 }, (entry as ShortArrayIFDEntry).Values);
			}
			// Pentax.0x0211 (WB_RGGBLevelsFluorescentD/Short/4) "17376 8192 8192 8847"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.WB_RGGBLevelsFluorescentD);
				Assert.IsNotNull (entry, "Entry 0x0211 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 17376, 8192, 8192, 8847 }, (entry as ShortArrayIFDEntry).Values);
			}
			// Pentax.0x0212 (WB_RGGBLevelsFluorescentN/Short/4) "14528 8192 8192 10076"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.WB_RGGBLevelsFluorescentN);
				Assert.IsNotNull (entry, "Entry 0x0212 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 14528, 8192, 8192, 10076 }, (entry as ShortArrayIFDEntry).Values);
			}
			// Pentax.0x0213 (WB_RGGBLevelsFluorescentW/Short/4) "13088 8192 8192 12206"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.WB_RGGBLevelsFluorescentW);
				Assert.IsNotNull (entry, "Entry 0x0213 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 13088, 8192, 8192, 12206 }, (entry as ShortArrayIFDEntry).Values);
			}
			// Pentax.0x0214 (WB_RGGBLevelsFlash/Short/4) "13632 8192 8192 8601"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.WB_RGGBLevelsFlash);
				Assert.IsNotNull (entry, "Entry 0x0214 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 13632, 8192, 8192, 8601 }, (entry as ShortArrayIFDEntry).Values);
			}
			// Pentax.0x0215 (CameraInfo/Long/5) "76830 20071221 2 1 8120852"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.CameraInfo);
				Assert.IsNotNull (entry, "Entry 0x0215 missing in IFD 0");
				Assert.IsNotNull (entry as LongArrayIFDEntry, "Entry is not a long array!");
				Assert.AreEqual (new long [] { 76830, 20071221, 2, 1, 8120852 }, (entry as LongArrayIFDEntry).Values);
			}
			// Pentax.0x0216 (BatteryInfo/Undefined/6) "2 68 177 173 181 166"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.BatteryInfo);
				Assert.IsNotNull (entry, "Entry 0x0216 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 2, 68, 177, 173, 181, 166 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Pentax.0x021A (0x021a/Undefined/38) "0 5 0 1 0 2 0 3 0 128 0 129 0 2 0 0 0 1 0 1 0 1 0 0 0 0 0 1 0 1 0 1 0 1 0 1 0 1"
			{
				// TODO: Unknown IFD tag: Pentax / 0x021A
				var entry = makernote_structure.GetEntry (0, (ushort) 0x021A);
				Assert.IsNotNull (entry, "Entry 0x021A missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 0, 5, 0, 1, 0, 2, 0, 3, 0, 128, 0, 129, 0, 2, 0, 0, 0, 1, 0, 1, 0, 1, 0, 0, 0, 0, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Pentax.0x021B (0x021b/Undefined/40) "0 0 0 2 49 32 240 224 254 0 252 128 44 64 247 64 255 160 247 224 40 128 54 96 238 128 251 32 251 64 49 192 243 0 255 96 246 32 42 128"
			{
				// TODO: Unknown IFD tag: Pentax / 0x021B
				var entry = makernote_structure.GetEntry (0, (ushort) 0x021B);
				Assert.IsNotNull (entry, "Entry 0x021B missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 0, 0, 0, 2, 49, 32, 240, 224, 254, 0, 252, 128, 44, 64, 247, 64, 255, 160, 247, 224, 40, 128, 54, 96, 238, 128, 251, 32, 251, 64, 49, 192, 243, 0, 255, 96, 246, 32, 42, 128 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Pentax.0x021C (0x021c/Undefined/18) "22 225 9 30 0 0 0 0 32 0 0 0 0 0 1 79 30 176"
			{
				// TODO: Unknown IFD tag: Pentax / 0x021C
				var entry = makernote_structure.GetEntry (0, (ushort) 0x021C);
				Assert.IsNotNull (entry, "Entry 0x021C missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 22, 225, 9, 30, 0, 0, 0, 0, 32, 0, 0, 0, 0, 0, 1, 79, 30, 176 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Pentax.0x021D (0x021d/Undefined/18) "54 128 238 192 250 192 250 64 53 224 239 224 0 128 245 0 42 128"
			{
				// TODO: Unknown IFD tag: Pentax / 0x021D
				var entry = makernote_structure.GetEntry (0, (ushort) 0x021D);
				Assert.IsNotNull (entry, "Entry 0x021D missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 54, 128, 238, 192, 250, 192, 250, 64, 53, 224, 239, 224, 0, 128, 245, 0, 42, 128 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Pentax.0x021E (0x021e/Undefined/8) "35 168 32 0 32 42 43 46"
			{
				// TODO: Unknown IFD tag: Pentax / 0x021E
				var entry = makernote_structure.GetEntry (0, (ushort) 0x021E);
				Assert.IsNotNull (entry, "Entry 0x021E missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 35, 168, 32, 0, 32, 42, 43, 46 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Pentax.0x021F (AFInfo/Undefined/12) "0 32 96 32 0 1 0 11 31 31 13 5"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.AFInfo);
				Assert.IsNotNull (entry, "Entry 0x021F missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 0, 32, 96, 32, 0, 1, 0, 11, 31, 31, 13, 5 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Pentax.0x0220 (0x0220/Undefined/53) "0 1 0 0 0 22 0 60 0 171 0 189 0 212 15 0 12 0 8 0 0 0 4 0 10 0 14 0 15 128 15 192 15 224 15 240 15 248 15 252 5 3 3 2 2 3 4 6 7 8 9 10 10"
			{
				// TODO: Unknown IFD tag: Pentax / 0x0220
				var entry = makernote_structure.GetEntry (0, (ushort) 0x0220);
				Assert.IsNotNull (entry, "Entry 0x0220 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 0, 1, 0, 0, 0, 22, 0, 60, 0, 171, 0, 189, 0, 212, 15, 0, 12, 0, 8, 0, 0, 0, 4, 0, 10, 0, 14, 0, 15, 128, 15, 192, 15, 224, 15, 240, 15, 248, 15, 252, 5, 3, 3, 2, 2, 3, 4, 6, 7, 8, 9, 10, 10 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Pentax.0x0221 (0x0221/Undefined/138) "0 17 187 113 0 0 52 163 36 46 198 2 0 0 27 131 78 17 197 128 0 0 29 86 72 112 196 234 0 0 31 69 67 61 196 74 0 0 33 84 62 112 195 140 0 0 35 133 57 245 194 196 0 0 37 216 53 212 193 212 0 0 40 79 49 253 192 188 0 0 42 233 46 107 191 124 0 0 45 166 43 30 190 0 0 0 48 126 40 17 188 62 0 0 51 116 37 61 186 14 0 0 54 124 34 158 183 92 0 0 57 139 32 56 179 226 0 0 60 150 30 5 175 60 0 0 63 139 28 5 168 182 0 0 66 88 26 53"
			{
				// TODO: Unknown IFD tag: Pentax / 0x0221
				var entry = makernote_structure.GetEntry (0, (ushort) 0x0221);
				Assert.IsNotNull (entry, "Entry 0x0221 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 0, 17, 187, 113, 0, 0, 52, 163, 36, 46, 198, 2, 0, 0, 27, 131, 78, 17, 197, 128, 0, 0, 29, 86, 72, 112, 196, 234, 0, 0, 31, 69, 67, 61, 196, 74, 0, 0, 33, 84, 62, 112, 195, 140, 0, 0, 35, 133, 57, 245, 194, 196, 0, 0, 37, 216, 53, 212, 193, 212, 0, 0, 40, 79, 49, 253, 192, 188, 0, 0, 42, 233, 46, 107, 191, 124, 0, 0, 45, 166, 43, 30, 190, 0, 0, 0, 48, 126, 40, 17, 188, 62, 0, 0, 51, 116, 37, 61, 186, 14, 0, 0, 54, 124, 34, 158, 183, 92, 0, 0, 57, 139, 32, 56, 179, 226, 0, 0, 60, 150, 30, 5, 175, 60, 0, 0, 63, 139, 28, 5, 168, 182, 0, 0, 66, 88, 26, 53 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Pentax.0x0222 (ColorInfo/Undefined/18) "32 131 31 100 31 125 32 156 33 72 32 246 31 51 31 10 0 0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PentaxMakerNoteEntryTag.ColorInfo);
				Assert.IsNotNull (entry, "Entry 0x0222 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 32, 131, 31, 100, 31, 125, 32, 156, 33, 72, 32, 246, 31, 51, 31, 10, 0, 0 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Pentax.0x0223 (0x0223/Undefined/198) "0 2 0 8 48 0 73 0 59 0 48 0 73 0 59 0 80 0 177 0 76 0 80 0 177 0 76 0 29 0 71 0 158 0 29 0 71 0 158 0 88 0 236 0 86 0 88 0 236 0 86 0 55 0 90 0 200 0 55 0 90 0 200 0 83 0 212 0 243 0 83 0 212 0 243 0 73 0 11 0 75 0 73 0 11 0 75 0 65 0 242 0 216 0 65 0 242 0 216 0 0 8 48 0 73 0 59 0 48 0 73 0 59 0 80 0 177 0 76 0 80 0 177 0 76 0 29 0 71 0 158 0 29 0 71 0 158 0 88 0 236 0 86 0 88 0 236 0 86 0 55 0 90 0 200 0 55 0 90 0 200 0 83 0 212 0 243 0 83 0 212 0 243 0 73 0 11 0 75 0 73 0 11 0 75 0 65 0 242 0 216 0 65 0 242 0 216 0"
			{
				// TODO: Unknown IFD tag: Pentax / 0x0223
				var entry = makernote_structure.GetEntry (0, (ushort) 0x0223);
				Assert.IsNotNull (entry, "Entry 0x0223 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 0, 2, 0, 8, 48, 0, 73, 0, 59, 0, 48, 0, 73, 0, 59, 0, 80, 0, 177, 0, 76, 0, 80, 0, 177, 0, 76, 0, 29, 0, 71, 0, 158, 0, 29, 0, 71, 0, 158, 0, 88, 0, 236, 0, 86, 0, 88, 0, 236, 0, 86, 0, 55, 0, 90, 0, 200, 0, 55, 0, 90, 0, 200, 0, 83, 0, 212, 0, 243, 0, 83, 0, 212, 0, 243, 0, 73, 0, 11, 0, 75, 0, 73, 0, 11, 0, 75, 0, 65, 0, 242, 0, 216, 0, 65, 0, 242, 0, 216, 0, 0, 8, 48, 0, 73, 0, 59, 0, 48, 0, 73, 0, 59, 0, 80, 0, 177, 0, 76, 0, 80, 0, 177, 0, 76, 0, 29, 0, 71, 0, 158, 0, 29, 0, 71, 0, 158, 0, 88, 0, 236, 0, 86, 0, 88, 0, 236, 0, 86, 0, 55, 0, 90, 0, 200, 0, 55, 0, 90, 0, 200, 0, 83, 0, 212, 0, 243, 0, 83, 0, 212, 0, 243, 0, 73, 0, 11, 0, 75, 0, 73, 0, 11, 0, 75, 0, 65, 0, 242, 0, 216, 0, 65, 0, 242, 0, 216, 0 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Pentax.0x0224 (0x0224/Undefined/8) "1 1 12 0 0 0 0 0"
			{
				// TODO: Unknown IFD tag: Pentax / 0x0224
				var entry = makernote_structure.GetEntry (0, (ushort) 0x0224);
				Assert.IsNotNull (entry, "Entry 0x0224 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 1, 1, 12, 0, 0, 0, 0, 0 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Pentax.0x03FF (0x03ff/Undefined/32) "0 6 0 7 0 5 0 7 0 37 11 107 0 37 105 191 0 0 46 118 35 144 31 234 32 21 110 65 0 0 0 8"
			{
				// TODO: Unknown IFD tag: Pentax / 0x03FF
				var entry = makernote_structure.GetEntry (0, (ushort) 0x03FF);
				Assert.IsNotNull (entry, "Entry 0x03FF missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 0, 6, 0, 7, 0, 5, 0, 7, 0, 37, 11, 107, 0, 37, 105, 191, 0, 0, 46, 118, 35, 144, 31, 234, 32, 21, 110, 65, 0, 0, 0, 8 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Pentax.0x0404 (0x0404/Undefined/8230) "(Value ommitted)"
			{
				// TODO: Unknown IFD tag: Pentax / 0x0404
				var entry = makernote_structure.GetEntry (0, (ushort) 0x0404);
				Assert.IsNotNull (entry, "Entry 0x0404 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var parsed_hash = Utils.Md5Encode (parsed_bytes);
				Assert.AreEqual ("660abf1742145f8052492ccbabf9ce03", parsed_hash);
				Assert.AreEqual (8230, parsed_bytes.Length);
			}
			// Pentax.0x0405 (0x0405/Undefined/21608) "(Value ommitted)"
			{
				// TODO: Unknown IFD tag: Pentax / 0x0405
				var entry = makernote_structure.GetEntry (0, (ushort) 0x0405);
				Assert.IsNotNull (entry, "Entry 0x0405 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var parsed_hash = Utils.Md5Encode (parsed_bytes);
				Assert.AreEqual ("b8893d586f313e16cbcdfd23bfaaa3ea", parsed_hash);
				Assert.AreEqual (21608, parsed_bytes.Length);
			}
			// Photo.0xA217 (SensingMethod/Short/1) "2"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.SensingMethod);
				Assert.IsNotNull (entry, "Entry 0xA217 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (2, (entry as ShortIFDEntry).Value);
			}
			// Photo.0xA302 (CFAPattern/Undefined/8) "0 2 0 2 0 1 1 2"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.CFAPattern2);
				Assert.IsNotNull (entry, "Entry 0xA302 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 0, 2, 0, 2, 0, 1, 1, 2 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Photo.0xA401 (CustomRendered/Short/1) "0"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.CustomRendered);
				Assert.IsNotNull (entry, "Entry 0xA401 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Photo.0xA402 (ExposureMode/Short/1) "0"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ExposureMode);
				Assert.IsNotNull (entry, "Entry 0xA402 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Photo.0xA403 (WhiteBalance/Short/1) "0"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.WhiteBalance);
				Assert.IsNotNull (entry, "Entry 0xA403 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Photo.0xA405 (FocalLengthIn35mmFilm/Short/1) "285"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.FocalLengthIn35mmFilm);
				Assert.IsNotNull (entry, "Entry 0xA405 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (285, (entry as ShortIFDEntry).Value);
			}
			// Photo.0xA406 (SceneCaptureType/Short/1) "0"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.SceneCaptureType);
				Assert.IsNotNull (entry, "Entry 0xA406 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Photo.0xA408 (Contrast/Short/1) "0"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.Contrast);
				Assert.IsNotNull (entry, "Entry 0xA408 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Photo.0xA409 (Saturation/Short/1) "0"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.Saturation);
				Assert.IsNotNull (entry, "Entry 0xA409 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Photo.0xA40A (Sharpness/Short/1) "0"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.Sharpness);
				Assert.IsNotNull (entry, "Entry 0xA40A missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Photo.0xA40C (SubjectDistanceRange/Short/1) "3"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.SubjectDistanceRange);
				Assert.IsNotNull (entry, "Entry 0xA40C missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (3, (entry as ShortIFDEntry).Value);
			}
			// Thumbnail.0x0100 (ImageWidth/Long/1) "160"
			{
				var entry = structure.GetEntry (1, (ushort) IFDEntryTag.ImageWidth);
				Assert.IsNotNull (entry, "Entry 0x0100 missing in IFD 1");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (160, (entry as LongIFDEntry).Value);
			}
			// Thumbnail.0x0101 (ImageLength/Long/1) "120"
			{
				var entry = structure.GetEntry (1, (ushort) IFDEntryTag.ImageLength);
				Assert.IsNotNull (entry, "Entry 0x0101 missing in IFD 1");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (120, (entry as LongIFDEntry).Value);
			}
			// Thumbnail.0x0103 (Compression/Short/1) "6"
			{
				var entry = structure.GetEntry (1, (ushort) IFDEntryTag.Compression);
				Assert.IsNotNull (entry, "Entry 0x0103 missing in IFD 1");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (6, (entry as ShortIFDEntry).Value);
			}
			// Thumbnail.0x011A (XResolution/Rational/1) "72/1"
			{
				var entry = structure.GetEntry (1, (ushort) IFDEntryTag.XResolution);
				Assert.IsNotNull (entry, "Entry 0x011A missing in IFD 1");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (72, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (1, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Thumbnail.0x011B (YResolution/Rational/1) "72/1"
			{
				var entry = structure.GetEntry (1, (ushort) IFDEntryTag.YResolution);
				Assert.IsNotNull (entry, "Entry 0x011B missing in IFD 1");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (72, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (1, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Thumbnail.0x0128 (ResolutionUnit/Short/1) "2"
			{
				var entry = structure.GetEntry (1, (ushort) IFDEntryTag.ResolutionUnit);
				Assert.IsNotNull (entry, "Entry 0x0128 missing in IFD 1");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (2, (entry as ShortIFDEntry).Value);
			}
			// Thumbnail.0x0201 (JPEGInterchangeFormat/ThumbnailDataIFD/1) "78752"
			{
				var entry = structure.GetEntry (1, (ushort) IFDEntryTag.JPEGInterchangeFormat);
				Assert.IsNotNull (entry, "Entry 0x0201 missing in IFD 1");
				Assert.IsNotNull (entry as ThumbnailDataIFDEntry, "Entry is not a thumbnail IFD!");
			}
			// Thumbnail.0x0202 (JPEGInterchangeFormatLength/Long/1) "5945"
			{
				var entry = structure.GetEntry (1, (ushort) IFDEntryTag.JPEGInterchangeFormatLength);
				Assert.IsNotNull (entry, "Entry 0x0202 missing in IFD 1");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (5945, (entry as LongIFDEntry).Value);
			}
			// Image2.0x0100 (ImageWidth/Long/1) "3872"
			{
				var entry = structure.GetEntry (2, (ushort) IFDEntryTag.ImageWidth);
				Assert.IsNotNull (entry, "Entry 0x0100 missing in IFD 2");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (3872, (entry as LongIFDEntry).Value);
			}
			// Image2.0x0101 (ImageLength/Long/1) "2592"
			{
				var entry = structure.GetEntry (2, (ushort) IFDEntryTag.ImageLength);
				Assert.IsNotNull (entry, "Entry 0x0101 missing in IFD 2");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (2592, (entry as LongIFDEntry).Value);
			}
			// Image2.0x0103 (Compression/Short/1) "6"
			{
				var entry = structure.GetEntry (2, (ushort) IFDEntryTag.Compression);
				Assert.IsNotNull (entry, "Entry 0x0103 missing in IFD 2");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (6, (entry as ShortIFDEntry).Value);
			}
			// Image2.0x011A (XResolution/Rational/1) "72/1"
			{
				var entry = structure.GetEntry (2, (ushort) IFDEntryTag.XResolution);
				Assert.IsNotNull (entry, "Entry 0x011A missing in IFD 2");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (72, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (1, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Image2.0x011B (YResolution/Rational/1) "72/1"
			{
				var entry = structure.GetEntry (2, (ushort) IFDEntryTag.YResolution);
				Assert.IsNotNull (entry, "Entry 0x011B missing in IFD 2");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (72, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (1, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Image2.0x0128 (ResolutionUnit/Short/1) "2"
			{
				var entry = structure.GetEntry (2, (ushort) IFDEntryTag.ResolutionUnit);
				Assert.IsNotNull (entry, "Entry 0x0128 missing in IFD 2");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (2, (entry as ShortIFDEntry).Value);
			}
			// Image2.0x0201 (JPEGInterchangeFormat/ThumbnailDataIFD/1) "9085368"
			{
				var entry = structure.GetEntry (2, (ushort) IFDEntryTag.JPEGInterchangeFormat);
				Assert.IsNotNull (entry, "Entry 0x0201 missing in IFD 2");
				Assert.IsNotNull (entry as ThumbnailDataIFDEntry, "Entry is not a thumbnail IFD!");
			}
			// Image2.0x0202 (JPEGInterchangeFormatLength/Long/1) "1240997"
			{
				var entry = structure.GetEntry (2, (ushort) IFDEntryTag.JPEGInterchangeFormatLength);
				Assert.IsNotNull (entry, "Entry 0x0202 missing in IFD 2");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (1240997, (entry as LongIFDEntry).Value);
			}

			//  ---------- End of IFD tests ----------

		}
	}
}
