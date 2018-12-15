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
	public class JpegBGO624097Test
	{
		[Test]
		public void Test ()
		{
			ImageTest.Run ("sample_bgo624097.jpg",
				false,
				new JpegBGO624097TestInvariantValidator ()
			);
		}
	}

	public class JpegBGO624097TestInvariantValidator : IMetadataInvariantValidator
	{
		public void ValidateMetadataInvariants (Image.File file)
		{
			Assert.IsNotNull (file);
			//  ---------- Start of IFD tests ----------

			var tag = file.GetTag (TagTypes.TiffIFD) as IFDTag;
			Assert.IsNotNull (tag, "IFD tag not found");

			var structure = tag.Structure;

			// Image.0x010F (Make/Ascii/18) "NIKON CORPORATION"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.Make);
				Assert.IsNotNull (entry, "Entry 0x010F missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("NIKON CORPORATION", (entry as StringIFDEntry).Value);
			}
			// Image.0x0110 (Model/Ascii/10) "NIKON D90"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.Model);
				Assert.IsNotNull (entry, "Entry 0x0110 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("NIKON D90", (entry as StringIFDEntry).Value);
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
			// Image.0x0131 (Software/Ascii/21) "f-spot version 0.7.1"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.Software);
				Assert.IsNotNull (entry, "Entry 0x0131 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("f-spot version 0.7.1", (entry as StringIFDEntry).Value);
			}
			// Image.0x0132 (DateTime/Ascii/20) "2010:07:01 22:19:26"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.DateTime);
				Assert.IsNotNull (entry, "Entry 0x0132 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("2010:07:01 22:19:26", (entry as StringIFDEntry).Value);
			}
			// Image.0x0213 (YCbCrPositioning/Short/1) "2"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.YCbCrPositioning);
				Assert.IsNotNull (entry, "Entry 0x0213 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (2, (entry as ShortIFDEntry).Value);
			}
			// Image.0x8769 (ExifTag/SubIFD/1) "232"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.ExifIFD);
				Assert.IsNotNull (entry, "Entry 0x8769 missing in IFD 0");
				Assert.IsNotNull (entry as SubIFDEntry, "Entry is not a sub IFD!");
			}

			var exif = structure.GetEntry (0, (ushort) IFDEntryTag.ExifIFD) as SubIFDEntry;
			Assert.IsNotNull (exif, "Exif tag not found");
			var exif_structure = exif.Structure;

			// Photo.0x829A (ExposureTime/Rational/1) "10/450"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ExposureTime);
				Assert.IsNotNull (entry, "Entry 0x829A missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (10, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (450, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Photo.0x829D (FNumber/Rational/1) "33/10"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.FNumber);
				Assert.IsNotNull (entry, "Entry 0x829D missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (33, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (10, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Photo.0x8822 (ExposureProgram/Short/1) "3"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ExposureProgram);
				Assert.IsNotNull (entry, "Entry 0x8822 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (3, (entry as ShortIFDEntry).Value);
			}
			// Photo.0x8827 (ISOSpeedRatings/Short/1) "200"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ISOSpeedRatings);
				Assert.IsNotNull (entry, "Entry 0x8827 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (200, (entry as ShortIFDEntry).Value);
			}
			// Photo.0x9000 (ExifVersion/Undefined/4) "48 50 50 49"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ExifVersion);
				Assert.IsNotNull (entry, "Entry 0x9000 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 48, 50, 50, 49 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Photo.0x9003 (DateTimeOriginal/Ascii/20) "2010:06:28 20:05:39"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.DateTimeOriginal);
				Assert.IsNotNull (entry, "Entry 0x9003 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("2010:06:28 20:05:39", (entry as StringIFDEntry).Value);
			}
			// Photo.0x9004 (DateTimeDigitized/Ascii/20) "2010:06:28 20:05:39"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.DateTimeDigitized);
				Assert.IsNotNull (entry, "Entry 0x9004 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("2010:06:28 20:05:39", (entry as StringIFDEntry).Value);
			}
			// Photo.0x9101 (ComponentsConfiguration/Undefined/4) "1 2 3 0"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ComponentsConfiguration);
				Assert.IsNotNull (entry, "Entry 0x9101 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 1, 2, 3, 0 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Photo.0x9102 (CompressedBitsPerPixel/Rational/1) "4/1"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.CompressedBitsPerPixel);
				Assert.IsNotNull (entry, "Entry 0x9102 missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (4, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (1, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Photo.0x9204 (ExposureBiasValue/SRational/1) "0/6"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ExposureBiasValue);
				Assert.IsNotNull (entry, "Entry 0x9204 missing in IFD 0");
				Assert.IsNotNull (entry as SRationalIFDEntry, "Entry is not a srational!");
				Assert.AreEqual (0, (entry as SRationalIFDEntry).Value.Numerator);
				Assert.AreEqual (6, (entry as SRationalIFDEntry).Value.Denominator);
			}
			// Photo.0x9205 (MaxApertureValue/Rational/1) "16/10"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.MaxApertureValue);
				Assert.IsNotNull (entry, "Entry 0x9205 missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (16, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (10, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Photo.0x9207 (MeteringMode/Short/1) "5"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.MeteringMode);
				Assert.IsNotNull (entry, "Entry 0x9207 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (5, (entry as ShortIFDEntry).Value);
			}
			// Photo.0x9208 (LightSource/Short/1) "0"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.LightSource);
				Assert.IsNotNull (entry, "Entry 0x9208 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Photo.0x9209 (Flash/Short/1) "0"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.Flash);
				Assert.IsNotNull (entry, "Entry 0x9209 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Photo.0x920A (FocalLength/Rational/1) "350/10"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.FocalLength);
				Assert.IsNotNull (entry, "Entry 0x920A missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (350, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (10, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Photo.0x927C (MakerNote/MakerNote/9191) "(Value ommitted)"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.MakerNote);
				Assert.IsNotNull (entry, "Entry 0x927C missing in IFD 0");
				Assert.IsNotNull (entry as MakernoteIFDEntry, "Entry is not a makernote IFD!");
			}

			var makernote = exif_structure.GetEntry (0, (ushort) ExifEntryTag.MakerNote) as MakernoteIFDEntry;
			Assert.IsNotNull (makernote, "MakerNote tag not found");
			var makernote_structure = makernote.Structure;


			Assert.AreEqual (MakernoteType.Nikon3, makernote.MakernoteType);

			// Nikon3.0x0001 (Version/Undefined/4) "48 50 49 48"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) Nikon3MakerNoteEntryTag.Version);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 48, 50, 49, 48 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Nikon3.0x0002 (ISOSpeed/Short/2) "0 200"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) Nikon3MakerNoteEntryTag.ISOSpeed);
				Assert.IsNotNull (entry, "Entry 0x0002 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 0, 200 }, (entry as ShortArrayIFDEntry).Values);
			}
			// Nikon3.0x0004 (Quality/Ascii/8) "FINE   "
			{
				var entry = makernote_structure.GetEntry (0, (ushort) Nikon3MakerNoteEntryTag.Quality);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("FINE   ", (entry as StringIFDEntry).Value);
			}
			// Nikon3.0x0005 (WhiteBalance/Ascii/13) "AUTO        "
			{
				var entry = makernote_structure.GetEntry (0, (ushort) Nikon3MakerNoteEntryTag.WhiteBalance);
				Assert.IsNotNull (entry, "Entry 0x0005 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("AUTO        ", (entry as StringIFDEntry).Value);
			}
			// Nikon3.0x0007 (Focus/Ascii/7) "AF-S  "
			{
				var entry = makernote_structure.GetEntry (0, (ushort) Nikon3MakerNoteEntryTag.Focus);
				Assert.IsNotNull (entry, "Entry 0x0007 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("AF-S  ", (entry as StringIFDEntry).Value);
			}
			// Nikon3.0x0008 (FlashSetting/Ascii/13) "NORMAL      "
			{
				var entry = makernote_structure.GetEntry (0, (ushort) Nikon3MakerNoteEntryTag.FlashSetting);
				Assert.IsNotNull (entry, "Entry 0x0008 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("NORMAL      ", (entry as StringIFDEntry).Value);
			}
			// Nikon3.0x0009 (FlashDevice/Ascii/20) "                   "
			{
				var entry = makernote_structure.GetEntry (0, (ushort) Nikon3MakerNoteEntryTag.FlashDevice);
				Assert.IsNotNull (entry, "Entry 0x0009 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("                   ", (entry as StringIFDEntry).Value);
			}
			// Nikon3.0x000B (WhiteBalanceBias/SShort/2) "0 0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) Nikon3MakerNoteEntryTag.WhiteBalanceBias);
				Assert.IsNotNull (entry, "Entry 0x000B missing in IFD 0");
				Assert.IsNotNull (entry as SShortArrayIFDEntry, "Entry is not a signed short array!");
				Assert.AreEqual (new short [] { 0, 0 }, (entry as SShortArrayIFDEntry).Values);
			}
			// Nikon3.0x000C (WB_RBLevels/Rational/4) "522/256 290/256 256/256 256/256"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) Nikon3MakerNoteEntryTag.WB_RBLevels);
				Assert.IsNotNull (entry, "Entry 0x000C missing in IFD 0");
				Assert.IsNotNull (entry as RationalArrayIFDEntry, "Entry is not a rational array!");
				var parts = (entry as RationalArrayIFDEntry).Values;
				Assert.AreEqual (4, parts.Length);
				Assert.AreEqual (522, parts[0].Numerator);
				Assert.AreEqual (256, parts[0].Denominator);
				Assert.AreEqual (290, parts[1].Numerator);
				Assert.AreEqual (256, parts[1].Denominator);
				Assert.AreEqual (256, parts[2].Numerator);
				Assert.AreEqual (256, parts[2].Denominator);
				Assert.AreEqual (256, parts[3].Numerator);
				Assert.AreEqual (256, parts[3].Denominator);
			}
			// Nikon3.0x000D (ProgramShift/Undefined/4) "0 1 6 0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) Nikon3MakerNoteEntryTag.ProgramShift);
				Assert.IsNotNull (entry, "Entry 0x000D missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 0, 1, 6, 0 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Nikon3.0x000E (ExposureDiff/Undefined/4) "0 1 12 0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) Nikon3MakerNoteEntryTag.ExposureDiff);
				Assert.IsNotNull (entry, "Entry 0x000E missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 0, 1, 12, 0 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Nikon3.0x0011 (Preview/SubIFD/1) "9210"
			//   --> Removed because we don't parse this one, it's corrupt.
			// Nikon3.0x0012 (FlashComp/Undefined/4) "244 1 6 0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) Nikon3MakerNoteEntryTag.FlashComp);
				Assert.IsNotNull (entry, "Entry 0x0012 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 244, 1, 6, 0 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Nikon3.0x0013 (ISOSettings/Short/2) "0 200"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) Nikon3MakerNoteEntryTag.ISOSettings);
				Assert.IsNotNull (entry, "Entry 0x0013 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 0, 200 }, (entry as ShortArrayIFDEntry).Values);
			}
			// Nikon3.0x0016 (ImageBoundary/Short/4) "0 0 4288 2848"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) Nikon3MakerNoteEntryTag.ImageBoundary);
				Assert.IsNotNull (entry, "Entry 0x0016 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 0, 0, 4288, 2848 }, (entry as ShortArrayIFDEntry).Values);
			}
			// Nikon3.0x0017 (0x0017/Undefined/4) "0 1 6 0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) Nikon3MakerNoteEntryTag.Unknown23);
				Assert.IsNotNull (entry, "Entry 0x0017 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 0, 1, 6, 0 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Nikon3.0x0018 (FlashBracketComp/Undefined/4) "0 1 6 0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) Nikon3MakerNoteEntryTag.FlashBracketComp);
				Assert.IsNotNull (entry, "Entry 0x0018 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 0, 1, 6, 0 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Nikon3.0x0019 (ExposureBracketComp/SRational/1) "0/6"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) Nikon3MakerNoteEntryTag.ExposureBracketComp);
				Assert.IsNotNull (entry, "Entry 0x0019 missing in IFD 0");
				Assert.IsNotNull (entry as SRationalIFDEntry, "Entry is not a srational!");
				Assert.AreEqual (0, (entry as SRationalIFDEntry).Value.Numerator);
				Assert.AreEqual (6, (entry as SRationalIFDEntry).Value.Denominator);
			}
			// Nikon3.0x001B (CropHiSpeed/Short/7) "0 4352 2868 4352 2868 0 0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) Nikon3MakerNoteEntryTag.CropHiSpeed);
				Assert.IsNotNull (entry, "Entry 0x001B missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 0, 4352, 2868, 4352, 2868, 0, 0 }, (entry as ShortArrayIFDEntry).Values);
			}
			// Nikon3.0x001C (0x001c/Undefined/3) "0 1 6"
			{
				// TODO: Unknown IFD tag: Nikon3 / 0x001C
				var entry = makernote_structure.GetEntry (0, (ushort) 0x001C);
				Assert.IsNotNull (entry, "Entry 0x001C missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 0, 1, 6 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Nikon3.0x001D (SerialNumber/Ascii/8) "6004417"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) Nikon3MakerNoteEntryTag.SerialNumber);
				Assert.IsNotNull (entry, "Entry 0x001D missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("6004417", (entry as StringIFDEntry).Value);
			}
			// Nikon3.0x001E (ColorSpace/Short/1) "1"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) Nikon3MakerNoteEntryTag.ColorSpace);
				Assert.IsNotNull (entry, "Entry 0x001E missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1, (entry as ShortIFDEntry).Value);
			}
			// Nikon3.0x0022 (ActiveDLighting/Short/1) "65535"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) Nikon3MakerNoteEntryTag.ActiveDLighting);
				Assert.IsNotNull (entry, "Entry 0x0022 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (65535, (entry as ShortIFDEntry).Value);
			}
			// Nikon3.0x002C (0x002c/Undefined/94) "48 49 48 48 5 0 1 100 0 236 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0"
			{
				// TODO: Unknown IFD tag: Nikon3 / 0x002C
				var entry = makernote_structure.GetEntry (0, (ushort) 0x002C);
				Assert.IsNotNull (entry, "Entry 0x002C missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 48, 49, 48, 48, 5, 0, 1, 100, 0, 236, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Nikon3.0x0083 (LensType/Byte/1) "6"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) Nikon3MakerNoteEntryTag.LensType);
				Assert.IsNotNull (entry, "Entry 0x0083 missing in IFD 0");
				Assert.IsNotNull (entry as ByteIFDEntry, "Entry is not a byte!");
				Assert.AreEqual (6, (entry as ByteIFDEntry).Value);
			}
			// Nikon3.0x0084 (Lens/Rational/4) "350/10 350/10 18/10 18/10"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) Nikon3MakerNoteEntryTag.Lens);
				Assert.IsNotNull (entry, "Entry 0x0084 missing in IFD 0");
				Assert.IsNotNull (entry as RationalArrayIFDEntry, "Entry is not a rational array!");
				var parts = (entry as RationalArrayIFDEntry).Values;
				Assert.AreEqual (4, parts.Length);
				Assert.AreEqual (350, parts[0].Numerator);
				Assert.AreEqual (10, parts[0].Denominator);
				Assert.AreEqual (350, parts[1].Numerator);
				Assert.AreEqual (10, parts[1].Denominator);
				Assert.AreEqual (18, parts[2].Numerator);
				Assert.AreEqual (10, parts[2].Denominator);
				Assert.AreEqual (18, parts[3].Numerator);
				Assert.AreEqual (10, parts[3].Denominator);
			}
			// Nikon3.0x0087 (FlashMode/Byte/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) Nikon3MakerNoteEntryTag.FlashMode);
				Assert.IsNotNull (entry, "Entry 0x0087 missing in IFD 0");
				Assert.IsNotNull (entry as ByteIFDEntry, "Entry is not a byte!");
				Assert.AreEqual (0, (entry as ByteIFDEntry).Value);
			}
			// Nikon3.0x0089 (ShootingMode/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) Nikon3MakerNoteEntryTag.ShootingMode);
				Assert.IsNotNull (entry, "Entry 0x0089 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Nikon3.0x008A (AutoBracketRelease/Short/1) "1"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) Nikon3MakerNoteEntryTag.AutoBracketRelease);
				Assert.IsNotNull (entry, "Entry 0x008A missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1, (entry as ShortIFDEntry).Value);
			}
			// Nikon3.0x008B (LensFStops/Undefined/4) "88 1 12 0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) Nikon3MakerNoteEntryTag.LensFStops);
				Assert.IsNotNull (entry, "Entry 0x008B missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 88, 1, 12, 0 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Nikon3.0x0095 (NoiseReduction/Ascii/5) "OFF "
			{
				var entry = makernote_structure.GetEntry (0, (ushort) Nikon3MakerNoteEntryTag.NoiseReduction);
				Assert.IsNotNull (entry, "Entry 0x0095 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("OFF ", (entry as StringIFDEntry).Value);
			}
			// Nikon3.0x0097 (ColorBalance/Undefined/1302) "(Value ommitted)"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) Nikon3MakerNoteEntryTag.ColorBalance);
				Assert.IsNotNull (entry, "Entry 0x0097 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var parsed_hash = Utils.Md5Encode (parsed_bytes);
				Assert.AreEqual ("7c55759a885d8e977bc21e1a1a4bb011", parsed_hash);
				Assert.AreEqual (1302, parsed_bytes.Length);
			}
			// Nikon3.0x009E (RetouchHistory/Short/10) "0 0 0 0 0 0 0 0 0 0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) Nikon3MakerNoteEntryTag.RetouchHistory);
				Assert.IsNotNull (entry, "Entry 0x009E missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, (entry as ShortArrayIFDEntry).Values);
			}
			// Nikon3.0x00A2 (ImageDataSize/Long/1) "4020390"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) Nikon3MakerNoteEntryTag.ImageDataSize);
				Assert.IsNotNull (entry, "Entry 0x00A2 missing in IFD 0");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (4020390, (entry as LongIFDEntry).Value);
			}
			// Nikon3.0x00A3 (0x00a3/Byte/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) Nikon3MakerNoteEntryTag.Unknown163);
				Assert.IsNotNull (entry, "Entry 0x00A3 missing in IFD 0");
				Assert.IsNotNull (entry as ByteIFDEntry, "Entry is not a byte!");
				Assert.AreEqual (0, (entry as ByteIFDEntry).Value);
			}
			// Nikon3.0x00A7 (ShutterCount/Long/1) "6292"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) Nikon3MakerNoteEntryTag.ShutterCount);
				Assert.IsNotNull (entry, "Entry 0x00A7 missing in IFD 0");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (6292, (entry as LongIFDEntry).Value);
			}
			// Nikon3.0x00A8 (FlashInfo/Undefined/22) "48 49 48 51 0 0 0 0 0 0 0 255 0 0 0 0 0 0 0 0 0 0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) Nikon3MakerNoteEntryTag.FlashInfo);
				Assert.IsNotNull (entry, "Entry 0x00A8 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 48, 49, 48, 51, 0, 0, 0, 0, 0, 0, 0, 255, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Nikon3.0x00AB (VariProgram/Ascii/16) "               "
			{
				var entry = makernote_structure.GetEntry (0, (ushort) Nikon3MakerNoteEntryTag.VariProgram);
				Assert.IsNotNull (entry, "Entry 0x00AB missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("               ", (entry as StringIFDEntry).Value);
			}
			// Nikon3.0x00B0 (MultiExposure/Undefined/16) "48 49 48 48 0 0 0 0 0 0 0 0 0 0 0 0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) Nikon3MakerNoteEntryTag.MultiExposure);
				Assert.IsNotNull (entry, "Entry 0x00B0 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 48, 49, 48, 48, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Nikon3.0x00B1 (HighISONoiseReduction/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) Nikon3MakerNoteEntryTag.HighISONoiseReduction);
				Assert.IsNotNull (entry, "Entry 0x00B1 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Nikon3.0x00B6 (0x00b6/Undefined/8) "7 218 6 28 20 5 31 0"
			{
				// TODO: Unknown IFD tag: Nikon3 / 0x00B6
				var entry = makernote_structure.GetEntry (0, (ushort) 0x00B6);
				Assert.IsNotNull (entry, "Entry 0x00B6 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 7, 218, 6, 28, 20, 5, 31, 0 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Nikon3.0x00B7 (AFInfo2/Undefined/30) "48 49 48 48 0 10 2 1 1 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) Nikon3MakerNoteEntryTag.AFInfo2);
				Assert.IsNotNull (entry, "Entry 0x00B7 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 48, 49, 48, 48, 0, 10, 2, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Nikon3.0x00B8 (FileInfo/Undefined/172) "48 49 48 48 0 0 0 100 22 226 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) Nikon3MakerNoteEntryTag.FileInfo);
				Assert.IsNotNull (entry, "Entry 0x00B8 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 48, 49, 48, 48, 0, 0, 0, 100, 22, 226, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Nikon3.0x00BB (0x00bb/Undefined/6) "48 49 48 48 255 0"
			{
				// TODO: Unknown IFD tag: Nikon3 / 0x00BB
				var entry = makernote_structure.GetEntry (0, (ushort) 0x00BB);
				Assert.IsNotNull (entry, "Entry 0x00BB missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 48, 49, 48, 48, 255, 0 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Photo.0x9286 (UserComment/UserComment/8) "charset="Ascii" "
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.UserComment);
				Assert.IsNotNull (entry, "Entry 0x9286 missing in IFD 0");
				Assert.IsNotNull (entry as UserCommentIFDEntry, "Entry is not a user comment!");
				Assert.AreEqual ("", (entry as UserCommentIFDEntry).Value.Trim ());
			}
			// Photo.0x9290 (SubSecTime/Ascii/3) "00"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.SubsecTime);
				Assert.IsNotNull (entry, "Entry 0x9290 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("00", (entry as StringIFDEntry).Value);
			}
			// Photo.0x9291 (SubSecTimeOriginal/Ascii/3) "00"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.SubsecTimeOriginal);
				Assert.IsNotNull (entry, "Entry 0x9291 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("00", (entry as StringIFDEntry).Value);
			}
			// Photo.0x9292 (SubSecTimeDigitized/Ascii/3) "00"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.SubsecTimeDigitized);
				Assert.IsNotNull (entry, "Entry 0x9292 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("00", (entry as StringIFDEntry).Value);
			}
			// Photo.0xA000 (FlashpixVersion/Undefined/4) "48 49 48 48"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.FlashpixVersion);
				Assert.IsNotNull (entry, "Entry 0xA000 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 48, 49, 48, 48 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Photo.0xA001 (ColorSpace/Short/1) "1"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ColorSpace);
				Assert.IsNotNull (entry, "Entry 0xA001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1, (entry as ShortIFDEntry).Value);
			}
			// Photo.0xA002 (PixelXDimension/Short/1) "4288"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.PixelXDimension);
				Assert.IsNotNull (entry, "Entry 0xA002 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (4288, (entry as ShortIFDEntry).Value);
			}
			// Photo.0xA003 (PixelYDimension/Short/1) "2848"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.PixelYDimension);
				Assert.IsNotNull (entry, "Entry 0xA003 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (2848, (entry as ShortIFDEntry).Value);
			}
			// Photo.0xA005 (InteroperabilityTag/SubIFD/1) "10022"
			{
				var entry = exif_structure.GetEntry (0, (ushort) IFDEntryTag.InteroperabilityIFD);
				Assert.IsNotNull (entry, "Entry 0xA005 missing in IFD 0");
				Assert.IsNotNull (entry as SubIFDEntry, "Entry is not a sub IFD!");
			}

			var iop = exif_structure.GetEntry (0, (ushort) IFDEntryTag.InteroperabilityIFD) as SubIFDEntry;
			Assert.IsNotNull (iop, "Iop tag not found");
			var iop_structure = iop.Structure;

			// Iop.0x0001 (InteroperabilityIndex/Ascii/4) "R98"
			{
				var entry = iop_structure.GetEntry (0, (ushort) IOPEntryTag.InteroperabilityIndex);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("R98", (entry as StringIFDEntry).Value);
			}
			// Iop.0x0002 (InteroperabilityVersion/Undefined/4) "48 49 48 48"
			{
				var entry = iop_structure.GetEntry (0, (ushort) IOPEntryTag.InteroperabilityVersion);
				Assert.IsNotNull (entry, "Entry 0x0002 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 48, 49, 48, 48 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Photo.0xA217 (SensingMethod/Short/1) "2"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.SensingMethod);
				Assert.IsNotNull (entry, "Entry 0xA217 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (2, (entry as ShortIFDEntry).Value);
			}
			// Photo.0xA300 (FileSource/Undefined/1) "3"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.FileSource);
				Assert.IsNotNull (entry, "Entry 0xA300 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 3 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Photo.0xA301 (SceneType/Undefined/1) "1"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.SceneType);
				Assert.IsNotNull (entry, "Entry 0xA301 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 1 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Photo.0xA302 (CFAPattern/Undefined/8) "0 2 0 2 1 2 0 1"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.CFAPattern2);
				Assert.IsNotNull (entry, "Entry 0xA302 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 0, 2, 0, 2, 1, 2, 0, 1 };
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
			// Photo.0xA404 (DigitalZoomRatio/Rational/1) "1/1"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.DigitalZoomRatio);
				Assert.IsNotNull (entry, "Entry 0xA404 missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (1, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (1, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Photo.0xA405 (FocalLengthIn35mmFilm/Short/1) "52"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.FocalLengthIn35mmFilm);
				Assert.IsNotNull (entry, "Entry 0xA405 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (52, (entry as ShortIFDEntry).Value);
			}
			// Photo.0xA406 (SceneCaptureType/Short/1) "0"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.SceneCaptureType);
				Assert.IsNotNull (entry, "Entry 0xA406 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Photo.0xA407 (GainControl/Short/1) "0"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.GainControl);
				Assert.IsNotNull (entry, "Entry 0xA407 missing in IFD 0");
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
			// Photo.0xA40C (SubjectDistanceRange/Short/1) "0"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.SubjectDistanceRange);
				Assert.IsNotNull (entry, "Entry 0xA40C missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Image.0x8825 (GPSTag/SubIFD/1) "10052"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.GPSIFD);
				Assert.IsNotNull (entry, "Entry 0x8825 missing in IFD 0");
				Assert.IsNotNull (entry as SubIFDEntry, "Entry is not a sub IFD!");
			}

			var gps = structure.GetEntry (0, (ushort) IFDEntryTag.GPSIFD) as SubIFDEntry;
			Assert.IsNotNull (gps, "GPS tag not found");
			var gps_structure = gps.Structure;

			// GPSInfo.0x0000 (GPSVersionID/Byte/4) "2 2 0 0"
			{
				var entry = gps_structure.GetEntry (0, (ushort) GPSEntryTag.GPSVersionID);
				Assert.IsNotNull (entry, "Entry 0x0000 missing in IFD 0");
				Assert.IsNotNull (entry as ByteVectorIFDEntry, "Entry is not a byte array!");
				var parsed_bytes = (entry as ByteVectorIFDEntry).Data.Data;
				var bytes = new byte [] { 2, 2, 0, 0 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Thumbnail.0x0103 (Compression/Short/1) "6"
			{
				var entry = structure.GetEntry (1, (ushort) IFDEntryTag.Compression);
				Assert.IsNotNull (entry, "Entry 0x0103 missing in IFD 1");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (6, (entry as ShortIFDEntry).Value);
			}
			// Thumbnail.0x011A (XResolution/Rational/1) "300/1"
			{
				var entry = structure.GetEntry (1, (ushort) IFDEntryTag.XResolution);
				Assert.IsNotNull (entry, "Entry 0x011A missing in IFD 1");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (300, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (1, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Thumbnail.0x011B (YResolution/Rational/1) "300/1"
			{
				var entry = structure.GetEntry (1, (ushort) IFDEntryTag.YResolution);
				Assert.IsNotNull (entry, "Entry 0x011B missing in IFD 1");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (300, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (1, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Thumbnail.0x0128 (ResolutionUnit/Short/1) "2"
			{
				var entry = structure.GetEntry (1, (ushort) IFDEntryTag.ResolutionUnit);
				Assert.IsNotNull (entry, "Entry 0x0128 missing in IFD 1");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (2, (entry as ShortIFDEntry).Value);
			}
			// Thumbnail.0x0201 (JPEGInterchangeFormat/ThumbnailDataIFD/1) "10176"
			{
				var entry = structure.GetEntry (1, (ushort) IFDEntryTag.JPEGInterchangeFormat);
				Assert.IsNotNull (entry, "Entry 0x0201 missing in IFD 1");
				Assert.IsNotNull (entry as ThumbnailDataIFDEntry, "Entry is not a thumbnail IFD!");
			}
			// Thumbnail.0x0202 (JPEGInterchangeFormatLength/Long/1) "9661"
			{
				var entry = structure.GetEntry (1, (ushort) IFDEntryTag.JPEGInterchangeFormatLength);
				Assert.IsNotNull (entry, "Entry 0x0202 missing in IFD 1");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (9661, (entry as LongIFDEntry).Value);
			}
			// Thumbnail.0x0213 (YCbCrPositioning/Short/1) "2"
			{
				var entry = structure.GetEntry (1, (ushort) IFDEntryTag.YCbCrPositioning);
				Assert.IsNotNull (entry, "Entry 0x0213 missing in IFD 1");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (2, (entry as ShortIFDEntry).Value);
			}

			//  ---------- End of IFD tests ----------

		}
	}
}
