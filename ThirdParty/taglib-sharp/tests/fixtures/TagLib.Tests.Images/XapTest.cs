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
	public class XapTest
	{
		[Test]
		public void Test ()
		{
			ImageTest.Run ("sample_xap.jpg",
				new XapTestInvariantValidator (),
				new NoModificationValidator (),
				new CommentModificationValidator ("Communications"),
				new TagCommentModificationValidator ("Communications", TagTypes.TiffIFD, true),
				new TagCommentModificationValidator ("Communications", TagTypes.XMP, true),
				new KeywordsModificationValidator (new string [] { "Communications" }),
				new TagKeywordsModificationValidator (new string [] { "Communications" }, TagTypes.XMP, true)
			);
		}
	}

	public class XapTestInvariantValidator : IMetadataInvariantValidator
	{
		public void ValidateMetadataInvariants (Image.File file)
		{
			Assert.IsNotNull (file);
			//  ---------- Start of IFD tests ----------

			var tag = file.GetTag (TagTypes.TiffIFD) as IFDTag;
			Assert.IsNotNull (tag, "IFD tag not found");

			var structure = tag.Structure;

			// Image.0x010E (ImageDescription/Ascii/15) "Communications"
			//  --> Test removed because of CommentModificationValidator, value is checked there.
			// Image.0x010F (Make/Ascii/9) "FUJIFILM"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.Make);
				Assert.IsNotNull (entry, "Entry 0x010F missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("FUJIFILM", (entry as StringIFDEntry).Value);
			}
			// Image.0x0110 (Model/Ascii/13) "FinePixS1Pro"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.Model);
				Assert.IsNotNull (entry, "Entry 0x0110 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("FinePixS1Pro", (entry as StringIFDEntry).Value);
			}
			// Image.0x0112 (Orientation/Short/1) "1"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.Orientation);
				Assert.IsNotNull (entry, "Entry 0x0112 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1, (entry as ShortIFDEntry).Value);
			}
			// Image.0x011A (XResolution/Rational/1) "300/1"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.XResolution);
				Assert.IsNotNull (entry, "Entry 0x011A missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (300, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (1, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Image.0x011B (YResolution/Rational/1) "300/1"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.YResolution);
				Assert.IsNotNull (entry, "Entry 0x011B missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (300, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (1, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Image.0x0128 (ResolutionUnit/Short/1) "2"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.ResolutionUnit);
				Assert.IsNotNull (entry, "Entry 0x0128 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (2, (entry as ShortIFDEntry).Value);
			}
			// Image.0x0131 (Software/Ascii/20) "Adobe Photoshop 7.0"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.Software);
				Assert.IsNotNull (entry, "Entry 0x0131 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("Adobe Photoshop 7.0", (entry as StringIFDEntry).Value);
			}
			// Image.0x0132 (DateTime/Ascii/20) "2002:07:19 13:28:10"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.DateTime);
				Assert.IsNotNull (entry, "Entry 0x0132 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("2002:07:19 13:28:10", (entry as StringIFDEntry).Value);
			}
			// Image.0x013B (Artist/Ascii/12) "Ian Britton"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.Artist);
				Assert.IsNotNull (entry, "Entry 0x013B missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("Ian Britton", (entry as StringIFDEntry).Value);
			}
			// Image.0x0213 (YCbCrPositioning/Short/1) "2"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.YCbCrPositioning);
				Assert.IsNotNull (entry, "Entry 0x0213 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (2, (entry as ShortIFDEntry).Value);
			}
			// Image.0x0214 (ReferenceBlackWhite/Rational/6) "0/1 255/1 128/1 255/1 128/1 255/1"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.ReferenceBlackWhite);
				Assert.IsNotNull (entry, "Entry 0x0214 missing in IFD 0");
				Assert.IsNotNull (entry as RationalArrayIFDEntry, "Entry is not a rational array!");
				var parts = (entry as RationalArrayIFDEntry).Values;
				Assert.AreEqual (6, parts.Length);
				Assert.AreEqual (0, parts[0].Numerator);
				Assert.AreEqual (1, parts[0].Denominator);
				Assert.AreEqual (255, parts[1].Numerator);
				Assert.AreEqual (1, parts[1].Denominator);
				Assert.AreEqual (128, parts[2].Numerator);
				Assert.AreEqual (1, parts[2].Denominator);
				Assert.AreEqual (255, parts[3].Numerator);
				Assert.AreEqual (1, parts[3].Denominator);
				Assert.AreEqual (128, parts[4].Numerator);
				Assert.AreEqual (1, parts[4].Denominator);
				Assert.AreEqual (255, parts[5].Numerator);
				Assert.AreEqual (1, parts[5].Denominator);
			}
			// Image.0x8298 (Copyright/Ascii/27) "ian Britton - FreeFoto.com"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.Copyright);
				Assert.IsNotNull (entry, "Entry 0x8298 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("ian Britton - FreeFoto.com", (entry as StringIFDEntry).Value);
			}
			// Image.0x8769 (ExifTag/SubIFD/1) "376"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.ExifIFD);
				Assert.IsNotNull (entry, "Entry 0x8769 missing in IFD 0");
				Assert.IsNotNull (entry as SubIFDEntry, "Entry is not a sub IFD!");
			}

			var exif = structure.GetEntry (0, (ushort) IFDEntryTag.ExifIFD) as SubIFDEntry;
			Assert.IsNotNull (exif, "Exif tag not found");
			var exif_structure = exif.Structure;

			// Photo.0x829D (FNumber/Rational/1) "1074135040/1677721600"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.FNumber);
				Assert.IsNotNull (entry, "Entry 0x829D missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (1074135040, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (1677721600, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Photo.0x8822 (ExposureProgram/Short/1) "4"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ExposureProgram);
				Assert.IsNotNull (entry, "Entry 0x8822 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (4, (entry as ShortIFDEntry).Value);
			}
			// Photo.0x8827 (ISOSpeedRatings/Short/1) "0"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ISOSpeedRatings);
				Assert.IsNotNull (entry, "Entry 0x8827 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Photo.0x9000 (ExifVersion/Undefined/4) "48 50 48 48 "
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ExifVersion);
				Assert.IsNotNull (entry, "Entry 0x9000 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var bytes = new byte [] { 48, 50, 48, 48 };
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Photo.0x9003 (DateTimeOriginal/Ascii/20) "2002:07:13 15:58:28"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.DateTimeOriginal);
				Assert.IsNotNull (entry, "Entry 0x9003 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("2002:07:13 15:58:28", (entry as StringIFDEntry).Value);
			}
			// Photo.0x9004 (DateTimeDigitized/Ascii/20) "2002:07:13 15:58:28"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.DateTimeDigitized);
				Assert.IsNotNull (entry, "Entry 0x9004 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("2002:07:13 15:58:28", (entry as StringIFDEntry).Value);
			}
			// Photo.0x9101 (ComponentsConfiguration/Undefined/4) "1 2 3 0 "
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ComponentsConfiguration);
				Assert.IsNotNull (entry, "Entry 0x9101 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var bytes = new byte [] { 1, 2, 3, 0 };
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Photo.0x9201 (ShutterSpeedValue/SRational/1) "1275068416/134217728"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ShutterSpeedValue);
				Assert.IsNotNull (entry, "Entry 0x9201 missing in IFD 0");
				Assert.IsNotNull (entry as SRationalIFDEntry, "Entry is not a srational!");
				Assert.AreEqual (1275068416, (entry as SRationalIFDEntry).Value.Numerator);
				Assert.AreEqual (134217728, (entry as SRationalIFDEntry).Value.Denominator);
			}
			// Photo.0x9202 (ApertureValue/Rational/1) "1610612736/201326592"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ApertureValue);
				Assert.IsNotNull (entry, "Entry 0x9202 missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (1610612736, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (201326592, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Photo.0x9203 (BrightnessValue/SRational/1) "436469760/1677721600"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.BrightnessValue);
				Assert.IsNotNull (entry, "Entry 0x9203 missing in IFD 0");
				Assert.IsNotNull (entry as SRationalIFDEntry, "Entry is not a srational!");
				Assert.AreEqual (436469760, (entry as SRationalIFDEntry).Value.Numerator);
				Assert.AreEqual (1677721600, (entry as SRationalIFDEntry).Value.Denominator);
			}
			// Photo.0x9204 (ExposureBiasValue/SRational/1) "-1090519041/1677721600"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ExposureBiasValue);
				Assert.IsNotNull (entry, "Entry 0x9204 missing in IFD 0");
				Assert.IsNotNull (entry as SRationalIFDEntry, "Entry is not a srational!");
				Assert.AreEqual (-1090519041, (entry as SRationalIFDEntry).Value.Numerator);
				Assert.AreEqual (1677721600, (entry as SRationalIFDEntry).Value.Denominator);
			}
			// Photo.0x9207 (MeteringMode/Short/1) "5"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.MeteringMode);
				Assert.IsNotNull (entry, "Entry 0x9207 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (5, (entry as ShortIFDEntry).Value);
			}
			// Photo.0x9209 (Flash/Short/1) "0"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.Flash);
				Assert.IsNotNull (entry, "Entry 0x9209 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Photo.0x920A (FocalLength/Rational/1) "0/16777216"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.FocalLength);
				Assert.IsNotNull (entry, "Entry 0x920A missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (0, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (16777216, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Photo.0xA000 (FlashpixVersion/Undefined/4) "48 49 48 48 "
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.FlashpixVersion);
				Assert.IsNotNull (entry, "Entry 0xA000 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var bytes = new byte [] { 48, 49, 48, 48 };
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Photo.0xA001 (ColorSpace/Short/1) "1"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ColorSpace);
				Assert.IsNotNull (entry, "Entry 0xA001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1, (entry as ShortIFDEntry).Value);
			}
			// Photo.0xA002 (PixelXDimension/Long/1) "2400"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.PixelXDimension);
				Assert.IsNotNull (entry, "Entry 0xA002 missing in IFD 0");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (2400, (entry as LongIFDEntry).Value);
			}
			// Photo.0xA003 (PixelYDimension/Long/1) "1600"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.PixelYDimension);
				Assert.IsNotNull (entry, "Entry 0xA003 missing in IFD 0");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (1600, (entry as LongIFDEntry).Value);
			}
			// Photo.0xA20E (FocalPlaneXResolution/Rational/1) "202178560/16777216"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.FocalPlaneXResolution);
				Assert.IsNotNull (entry, "Entry 0xA20E missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (202178560, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (16777216, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Photo.0xA20F (FocalPlaneYResolution/Rational/1) "202178560/16777216"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.FocalPlaneYResolution);
				Assert.IsNotNull (entry, "Entry 0xA20F missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (202178560, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (16777216, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Photo.0xA210 (FocalPlaneResolutionUnit/Short/1) "2"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.FocalPlaneResolutionUnit);
				Assert.IsNotNull (entry, "Entry 0xA210 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (2, (entry as ShortIFDEntry).Value);
			}
			// Photo.0xA217 (SensingMethod/Short/1) "2"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.SensingMethod);
				Assert.IsNotNull (entry, "Entry 0xA217 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (2, (entry as ShortIFDEntry).Value);
			}
			// Photo.0xA300 (FileSource/Undefined/1) "0 "
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.FileSource);
				Assert.IsNotNull (entry, "Entry 0xA300 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var bytes = new byte [] { 0 };
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Photo.0xA301 (SceneType/Undefined/1) "0 "
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.SceneType);
				Assert.IsNotNull (entry, "Entry 0xA301 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var bytes = new byte [] { 0 };
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Image.0x8825 (GPSTag/SubIFD/1) "776"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.GPSIFD);
				Assert.IsNotNull (entry, "Entry 0x8825 missing in IFD 0");
				Assert.IsNotNull (entry as SubIFDEntry, "Entry is not a sub IFD!");
			}

			var gps = structure.GetEntry (0, (ushort) IFDEntryTag.GPSIFD) as SubIFDEntry;
			Assert.IsNotNull (gps, "GPS tag not found");
			var gps_structure = gps.Structure;

			// GPSInfo.0x0000 (GPSVersionID/Byte/4) "2 0 0 0 "
			{
				var entry = gps_structure.GetEntry (0, (ushort) GPSEntryTag.GPSVersionID);
				Assert.IsNotNull (entry, "Entry 0x0000 missing in IFD 0");
				Assert.IsNotNull (entry as ByteVectorIFDEntry, "Entry is not a byte array!");
				var bytes = new byte [] { 2, 0, 0, 0 };
				var parsed_bytes = (entry as ByteVectorIFDEntry).Data.Data;
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// GPSInfo.0x0001 (GPSLatitudeRef/Ascii/2) "N"
			{
				var entry = gps_structure.GetEntry (0, (ushort) GPSEntryTag.GPSLatitudeRef);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("N", (entry as StringIFDEntry).Value);
			}
			// GPSInfo.0x0002 (GPSLatitude/Rational/3) "54/1 5938/100 0/1"
			{
				var entry = gps_structure.GetEntry (0, (ushort) GPSEntryTag.GPSLatitude);
				Assert.IsNotNull (entry, "Entry 0x0002 missing in IFD 0");
				Assert.IsNotNull (entry as RationalArrayIFDEntry, "Entry is not a rational array!");
				var parts = (entry as RationalArrayIFDEntry).Values;
				Assert.AreEqual (3, parts.Length);
				Assert.AreEqual (54, parts[0].Numerator);
				Assert.AreEqual (1, parts[0].Denominator);
				Assert.AreEqual (5938, parts[1].Numerator);
				Assert.AreEqual (100, parts[1].Denominator);
				Assert.AreEqual (0, parts[2].Numerator);
				Assert.AreEqual (1, parts[2].Denominator);
			}
			// GPSInfo.0x0003 (GPSLongitudeRef/Ascii/2) "W"
			{
				var entry = gps_structure.GetEntry (0, (ushort) GPSEntryTag.GPSLongitudeRef);
				Assert.IsNotNull (entry, "Entry 0x0003 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("W", (entry as StringIFDEntry).Value);
			}
			// GPSInfo.0x0004 (GPSLongitude/Rational/3) "1/1 5485/100 0/1"
			{
				var entry = gps_structure.GetEntry (0, (ushort) GPSEntryTag.GPSLongitude);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as RationalArrayIFDEntry, "Entry is not a rational array!");
				var parts = (entry as RationalArrayIFDEntry).Values;
				Assert.AreEqual (3, parts.Length);
				Assert.AreEqual (1, parts[0].Numerator);
				Assert.AreEqual (1, parts[0].Denominator);
				Assert.AreEqual (5485, parts[1].Numerator);
				Assert.AreEqual (100, parts[1].Denominator);
				Assert.AreEqual (0, parts[2].Numerator);
				Assert.AreEqual (1, parts[2].Denominator);
			}
			// GPSInfo.0x0007 (GPSTimeStamp/Rational/3) "14/1 58/1 24/1"
			{
				var entry = gps_structure.GetEntry (0, (ushort) GPSEntryTag.GPSTimeStamp);
				Assert.IsNotNull (entry, "Entry 0x0007 missing in IFD 0");
				Assert.IsNotNull (entry as RationalArrayIFDEntry, "Entry is not a rational array!");
				var parts = (entry as RationalArrayIFDEntry).Values;
				Assert.AreEqual (3, parts.Length);
				Assert.AreEqual (14, parts[0].Numerator);
				Assert.AreEqual (1, parts[0].Denominator);
				Assert.AreEqual (58, parts[1].Numerator);
				Assert.AreEqual (1, parts[1].Denominator);
				Assert.AreEqual (24, parts[2].Numerator);
				Assert.AreEqual (1, parts[2].Denominator);
			}
			// GPSInfo.0x0012 (GPSMapDatum/Ascii/6) "WGS84"
			{
				var entry = gps_structure.GetEntry (0, (ushort) GPSEntryTag.GPSMapDatum);
				Assert.IsNotNull (entry, "Entry 0x0012 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("WGS84", (entry as StringIFDEntry).Value);
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
			// Thumbnail.0x0201 (JPEGInterchangeFormat/ThumbnailDataIFD/1) "1038"
			{
				var entry = structure.GetEntry (1, (ushort) IFDEntryTag.JPEGInterchangeFormat);
				Assert.IsNotNull (entry, "Entry 0x0201 missing in IFD 1");
				Assert.IsNotNull (entry as ThumbnailDataIFDEntry, "Entry is not a thumbnail IFD!");
			}
			// Thumbnail.0x0202 (JPEGInterchangeFormatLength/Long/1) "3662"
			{
				var entry = structure.GetEntry (1, (ushort) IFDEntryTag.JPEGInterchangeFormatLength);
				Assert.IsNotNull (entry, "Entry 0x0202 missing in IFD 1");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (3662, (entry as LongIFDEntry).Value);
			}

			//  ---------- End of IFD tests ----------


			//  ---------- Start of XMP tests ----------

			XmpTag xmp = file.GetTag (TagTypes.XMP) as XmpTag;
			// Xmp.photoshop.CaptionWriter (XmpText/11) "Ian Britton"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.PHOTOSHOP_NS, "CaptionWriter");
				Assert.IsNotNull (node);
				Assert.AreEqual ("Ian Britton", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.photoshop.Headline (XmpText/14) "Communications"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.PHOTOSHOP_NS, "Headline");
				Assert.IsNotNull (node);
				Assert.AreEqual ("Communications", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.photoshop.AuthorsPosition (XmpText/12) "Photographer"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.PHOTOSHOP_NS, "AuthorsPosition");
				Assert.IsNotNull (node);
				Assert.AreEqual ("Photographer", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.photoshop.Credit (XmpText/11) "Ian Britton"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.PHOTOSHOP_NS, "Credit");
				Assert.IsNotNull (node);
				Assert.AreEqual ("Ian Britton", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.photoshop.Source (XmpText/12) "FreeFoto.com"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.PHOTOSHOP_NS, "Source");
				Assert.IsNotNull (node);
				Assert.AreEqual ("FreeFoto.com", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.photoshop.City (XmpText/1) ""
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.PHOTOSHOP_NS, "City");
				Assert.IsNotNull (node);
				Assert.AreEqual ("", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.photoshop.State (XmpText/1) ""
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.PHOTOSHOP_NS, "State");
				Assert.IsNotNull (node);
				Assert.AreEqual ("", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.photoshop.Country (XmpText/14) "Ubited Kingdom"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.PHOTOSHOP_NS, "Country");
				Assert.IsNotNull (node);
				Assert.AreEqual ("Ubited Kingdom", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.photoshop.Category (XmpText/3) "BUS"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.PHOTOSHOP_NS, "Category");
				Assert.IsNotNull (node);
				Assert.AreEqual ("BUS", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.photoshop.DateCreated (XmpText/10) "2002-06-20"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.PHOTOSHOP_NS, "DateCreated");
				Assert.IsNotNull (node);
				Assert.AreEqual ("2002-06-20", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.photoshop.Urgency (XmpText/1) "5"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.PHOTOSHOP_NS, "Urgency");
				Assert.IsNotNull (node);
				Assert.AreEqual ("5", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.photoshop.SupplementalCategories (XmpBag/1) "Communications"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.PHOTOSHOP_NS, "SupplementalCategories");
				Assert.IsNotNull (node);
				Assert.AreEqual (XmpNodeType.Bag, node.Type);
				Assert.AreEqual ("", node.Value);
				Assert.AreEqual (1, node.Children.Count);
				Assert.AreEqual ("Communications", node.Children [0].Value);
			}
			// Xmp.xmpBJ.JobRef (XmpText/0) "type="Bag""
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.XAP_BJ_NS, "JobRef");
				Assert.IsNotNull (node);
				Assert.AreEqual (XmpNodeType.Bag, node.Type);
			}
			// Xmp.xmpBJ.JobRef[1] (XmpText/0) "type="Struct""
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.XAP_BJ_NS, "JobRef");
				Assert.IsNotNull (node);
				node = node.Children [0];
				Assert.IsNotNull (node);
				Assert.AreEqual (XmpNodeType.Struct, node.Type);
			}
			// Xmp.xmpBJ.JobRef[1]/stJob:name (XmpText/12) "Photographer"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.XAP_BJ_NS, "JobRef");
				Assert.IsNotNull (node);
				node = node.Children [0];
				Assert.IsNotNull (node);
				node = node.GetChild (XmpTag.JOB_NS, "name");
				Assert.IsNotNull (node);
				Assert.AreEqual ("Photographer", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.xmpMM.DocumentID (XmpText/58) "adobe:docid:photoshop:84d4dba8-9b11-11d6-895d-c4d063a70fb0"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.XAP_MM_NS, "DocumentID");
				Assert.IsNotNull (node);
				Assert.AreEqual ("adobe:docid:photoshop:84d4dba8-9b11-11d6-895d-c4d063a70fb0", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.xmpRights.WebStatement (XmpText/16) "www.freefoto.com"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.XAP_RIGHTS_NS, "WebStatement");
				Assert.IsNotNull (node);
				Assert.AreEqual ("www.freefoto.com", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.xmpRights.Marked (XmpText/4) "True"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.XAP_RIGHTS_NS, "Marked");
				Assert.IsNotNull (node);
				Assert.AreEqual ("True", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
			}
			// Xmp.dc.description (LangAlt/1) "lang="x-default" Communications"
			//  --> Test removed because of CommentModificationValidator, value is checked there.
			// Xmp.dc.creator (XmpSeq/1) "Ian Britton"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.DC_NS, "creator");
				Assert.IsNotNull (node);
				Assert.AreEqual (XmpNodeType.Seq, node.Type);
				Assert.AreEqual ("", node.Value);
				Assert.AreEqual (1, node.Children.Count);
				Assert.AreEqual ("Ian Britton", node.Children [0].Value);
			}
			// Xmp.dc.title (LangAlt/1) "lang="x-default" Communications"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.DC_NS, "title");
				Assert.IsNotNull (node);
				Assert.AreEqual ("x-default", node.Children [0].GetQualifier (XmpTag.XML_NS, "lang").Value);
				Assert.AreEqual ("Communications", node.Children [0].Value);
			}
			// Xmp.dc.rights (LangAlt/1) "lang="x-default" ian Britton - FreeFoto.com"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.DC_NS, "rights");
				Assert.IsNotNull (node);
				Assert.AreEqual ("x-default", node.Children [0].GetQualifier (XmpTag.XML_NS, "lang").Value);
				Assert.AreEqual ("ian Britton - FreeFoto.com", node.Children [0].Value);
			}
			// Xmp.dc.subject (XmpBag/1) "Communications"
			//  --> Test removed because of KeywordsModificationValidator, value is checked there.

			//  ---------- End of XMP tests ----------

		}
	}
}
