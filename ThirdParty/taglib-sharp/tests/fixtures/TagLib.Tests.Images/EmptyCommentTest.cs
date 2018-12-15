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
	public class EmptyCommentTest
	{
		[Test]
		public void Test ()
		{
			ImageTest.Run ("sample_emptycomment.jpg",
				new EmptyCommentTestInvariantValidator (),
				NoModificationValidator.Instance
			);
		}
	}

	public class EmptyCommentTestInvariantValidator : IMetadataInvariantValidator
	{
		public void ValidateMetadataInvariants (Image.File file)
		{
			Assert.IsNotNull (file);
			//  ---------- Start of IFD tests ----------

			var tag = file.GetTag (TagTypes.TiffIFD) as IFDTag;
			Assert.IsNotNull (tag, "IFD tag not found");

			var structure = tag.Structure;

			// Image.0x010E (ImageDescription/Ascii/32) "OLYMPUS DIGITAL CAMERA         "
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.ImageDescription);
				Assert.IsNotNull (entry, "Entry 0x010E missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("OLYMPUS DIGITAL CAMERA         ", (entry as StringIFDEntry).Value);
			}
			// Image.0x010F (Make/Ascii/24) "OLYMPUS OPTICAL CO.,LTD"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.Make);
				Assert.IsNotNull (entry, "Entry 0x010F missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("OLYMPUS OPTICAL CO.,LTD", (entry as StringIFDEntry).Value);
			}
			// Image.0x0110 (Model/Ascii/13) "E-10        "
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.Model);
				Assert.IsNotNull (entry, "Entry 0x0110 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("E-10        ", (entry as StringIFDEntry).Value);
			}
			// Image.0x0112 (Orientation/Short/1) "1"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.Orientation);
				Assert.IsNotNull (entry, "Entry 0x0112 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1, (entry as ShortIFDEntry).Value);
			}
			// Image.0x011A (XResolution/Rational/1) "144/1"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.XResolution);
				Assert.IsNotNull (entry, "Entry 0x011A missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (144, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (1, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Image.0x011B (YResolution/Rational/1) "144/1"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.YResolution);
				Assert.IsNotNull (entry, "Entry 0x011B missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (144, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (1, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Image.0x0128 (ResolutionUnit/Short/1) "2"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.ResolutionUnit);
				Assert.IsNotNull (entry, "Entry 0x0128 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (2, (entry as ShortIFDEntry).Value);
			}
			// Image.0x0131 (Software/Ascii/32) "42-0119                        "
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.Software);
				Assert.IsNotNull (entry, "Entry 0x0131 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("42-0119                        ", (entry as StringIFDEntry).Value);
			}
			// Image.0x0132 (DateTime/Ascii/20) "2005:06:01 12:21:45"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.DateTime);
				Assert.IsNotNull (entry, "Entry 0x0132 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("2005:06:01 12:21:45", (entry as StringIFDEntry).Value);
			}
			// Image.0x0213 (YCbCrPositioning/Short/1) "2"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.YCbCrPositioning);
				Assert.IsNotNull (entry, "Entry 0x0213 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (2, (entry as ShortIFDEntry).Value);
			}
			// Image.0x8769 (ExifTag/SubIFD/1) "283"
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
			// Photo.0x829D (FNumber/Rational/1) "36/10"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.FNumber);
				Assert.IsNotNull (entry, "Entry 0x829D missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (36, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (10, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Photo.0x8822 (ExposureProgram/Short/1) "2"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ExposureProgram);
				Assert.IsNotNull (entry, "Entry 0x8822 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (2, (entry as ShortIFDEntry).Value);
			}
			// Photo.0x8827 (ISOSpeedRatings/Short/1) "80"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ISOSpeedRatings);
				Assert.IsNotNull (entry, "Entry 0x8827 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (80, (entry as ShortIFDEntry).Value);
			}
			// Photo.0x9000 (ExifVersion/Undefined/4) "48 50 49 48"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ExifVersion);
				Assert.IsNotNull (entry, "Entry 0x9000 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 48, 50, 49, 48 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Photo.0x9003 (DateTimeOriginal/Ascii/20) "2005:06:01 12:21:45"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.DateTimeOriginal);
				Assert.IsNotNull (entry, "Entry 0x9003 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("2005:06:01 12:21:45", (entry as StringIFDEntry).Value);
			}
			// Photo.0x9004 (DateTimeDigitized/Ascii/20) "2005:06:01 12:21:45"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.DateTimeDigitized);
				Assert.IsNotNull (entry, "Entry 0x9004 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("2005:06:01 12:21:45", (entry as StringIFDEntry).Value);
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
			// Photo.0x9204 (ExposureBiasValue/SRational/1) "0/10"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ExposureBiasValue);
				Assert.IsNotNull (entry, "Entry 0x9204 missing in IFD 0");
				Assert.IsNotNull (entry as SRationalIFDEntry, "Entry is not a srational!");
				Assert.AreEqual (0, (entry as SRationalIFDEntry).Value.Numerator);
				Assert.AreEqual (10, (entry as SRationalIFDEntry).Value.Denominator);
			}
			// Photo.0x9205 (MaxApertureValue/Rational/1) "206/100"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.MaxApertureValue);
				Assert.IsNotNull (entry, "Entry 0x9205 missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (206, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (100, (entry as RationalIFDEntry).Value.Denominator);
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
			// Photo.0x920A (FocalLength/Rational/1) "170/10"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.FocalLength);
				Assert.IsNotNull (entry, "Entry 0x920A missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (170, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (10, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Photo.0x927C (MakerNote/MakerNote/758) "(Value ommitted)"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.MakerNote);
				Assert.IsNotNull (entry, "Entry 0x927C missing in IFD 0");
				Assert.IsNotNull (entry as MakernoteIFDEntry, "Entry is not a makernote IFD!");
			}

			var makernote = exif_structure.GetEntry (0, (ushort) ExifEntryTag.MakerNote) as MakernoteIFDEntry;
			Assert.IsNotNull (makernote, "MakerNote tag not found");
			var makernote_structure = makernote.Structure;

			// Olympus.0x0200 (SpecialMode/Long/3) "3631961967 3583981211 265841571"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) OlympusMakerNoteEntryTag.SpecialMode);
				Assert.IsNotNull (entry, "Entry 0x0200 missing in IFD 0");
				Assert.IsNotNull (entry as LongArrayIFDEntry, "Entry is not a long array!");
				Assert.AreEqual (new long [] { 3631961967, 3583981211, 265841571 }, (entry as LongArrayIFDEntry).Values);
			}
			// Olympus.0x0201 (Quality/Short/1) "2"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) OlympusMakerNoteEntryTag.Quality);
				Assert.IsNotNull (entry, "Entry 0x0201 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (2, (entry as ShortIFDEntry).Value);
			}
			// Olympus.0x0202 (Macro/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) OlympusMakerNoteEntryTag.Macro);
				Assert.IsNotNull (entry, "Entry 0x0202 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Olympus.0x0203 (BWMode/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) OlympusMakerNoteEntryTag.BWMode);
				Assert.IsNotNull (entry, "Entry 0x0203 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Olympus.0x0204 (DigitalZoom/Rational/1) "149128747/4155087494"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) OlympusMakerNoteEntryTag.DigitalZoom);
				Assert.IsNotNull (entry, "Entry 0x0204 missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (149128747, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (4155087494, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Olympus.0x0205 (FocalPlaneDiagonal/Rational/1) "2670046643/2671390721"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) OlympusMakerNoteEntryTag.FocalPlaneDiagonal);
				Assert.IsNotNull (entry, "Entry 0x0205 missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (2670046643, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (2671390721, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Olympus.0x0206 (LensDistortionParams/SShort/6) "-5164 6906 7757 835 -25578 -17709"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) OlympusMakerNoteEntryTag.LensDistortionParams);
				Assert.IsNotNull (entry, "Entry 0x0206 missing in IFD 0");
				Assert.IsNotNull (entry as SShortArrayIFDEntry, "Entry is not a signed short array!");
				Assert.AreEqual (new short [] { -5164, 6906, 7757, 835, -25578, -17709 }, (entry as SShortArrayIFDEntry).Values);
			}
			// Olympus.0x0209 (CameraID/Undefined/32) "0 12 250 125 107 143 63 12 180 187 233 167 69 212 102 54 109 135 69 0 124 188 228 12 247 227 20 158 30 73 166 135"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) OlympusMakerNoteEntryTag.CameraID);
				Assert.IsNotNull (entry, "Entry 0x0209 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 0, 12, 250, 125, 107, 143, 63, 12, 180, 187, 233, 167, 69, 212, 102, 54, 109, 135, 69, 0, 124, 188, 228, 12, 247, 227, 20, 158, 30, 73, 166, 135 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Olympus.0x1000 (ShutterSpeed/SRational/1) "764354177/-942049472"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) OlympusMakerNoteEntryTag.ShutterSpeed);
				Assert.IsNotNull (entry, "Entry 0x1000 missing in IFD 0");
				Assert.IsNotNull (entry as SRationalIFDEntry, "Entry is not a srational!");
				Assert.AreEqual (764354177, (entry as SRationalIFDEntry).Value.Numerator);
				Assert.AreEqual (-942049472, (entry as SRationalIFDEntry).Value.Denominator);
			}
			// Olympus.0x1001 (ISOSpeed/SRational/1) "1239879654/638047914"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) OlympusMakerNoteEntryTag.ISOSpeed);
				Assert.IsNotNull (entry, "Entry 0x1001 missing in IFD 0");
				Assert.IsNotNull (entry as SRationalIFDEntry, "Entry is not a srational!");
				Assert.AreEqual (1239879654, (entry as SRationalIFDEntry).Value.Numerator);
				Assert.AreEqual (638047914, (entry as SRationalIFDEntry).Value.Denominator);
			}
			// Olympus.0x1002 (ApertureValue/SRational/1) "-1765853430/1703491222"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) OlympusMakerNoteEntryTag.ApertureValue);
				Assert.IsNotNull (entry, "Entry 0x1002 missing in IFD 0");
				Assert.IsNotNull (entry as SRationalIFDEntry, "Entry is not a srational!");
				Assert.AreEqual (-1765853430, (entry as SRationalIFDEntry).Value.Numerator);
				Assert.AreEqual (1703491222, (entry as SRationalIFDEntry).Value.Denominator);
			}
			// Olympus.0x1003 (Brightness/SRational/1) "-816914756/1974393359"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) OlympusMakerNoteEntryTag.Brightness);
				Assert.IsNotNull (entry, "Entry 0x1003 missing in IFD 0");
				Assert.IsNotNull (entry as SRationalIFDEntry, "Entry is not a srational!");
				Assert.AreEqual (-816914756, (entry as SRationalIFDEntry).Value.Numerator);
				Assert.AreEqual (1974393359, (entry as SRationalIFDEntry).Value.Denominator);
			}
			// Olympus.0x1004 (FlashMode/Short/1) "3"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) OlympusMakerNoteEntryTag.FlashMode);
				Assert.IsNotNull (entry, "Entry 0x1004 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (3, (entry as ShortIFDEntry).Value);
			}
			// Olympus.0x1005 (FlashDevice/Short/2) "0 0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) OlympusMakerNoteEntryTag.FlashDevice);
				Assert.IsNotNull (entry, "Entry 0x1005 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 0, 0 }, (entry as ShortArrayIFDEntry).Values);
			}
			// Olympus.0x1006 (Bracket/SRational/1) "335386020/1890807289"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) OlympusMakerNoteEntryTag.Bracket);
				Assert.IsNotNull (entry, "Entry 0x1006 missing in IFD 0");
				Assert.IsNotNull (entry as SRationalIFDEntry, "Entry is not a srational!");
				Assert.AreEqual (335386020, (entry as SRationalIFDEntry).Value.Numerator);
				Assert.AreEqual (1890807289, (entry as SRationalIFDEntry).Value.Denominator);
			}
			// Olympus.0x1007 (SensorTemperature/SShort/1) "23"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) OlympusMakerNoteEntryTag.SensorTemperature);
				Assert.IsNotNull (entry, "Entry 0x1007 missing in IFD 0");
				Assert.IsNotNull (entry as SShortIFDEntry, "Entry is not a signed short!");
				Assert.AreEqual (23, (entry as SShortIFDEntry).Value);
			}
			// Olympus.0x1008 (LensTemperature/SShort/1) "22"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) OlympusMakerNoteEntryTag.LensTemperature);
				Assert.IsNotNull (entry, "Entry 0x1008 missing in IFD 0");
				Assert.IsNotNull (entry as SShortIFDEntry, "Entry is not a signed short!");
				Assert.AreEqual (22, (entry as SShortIFDEntry).Value);
			}
			// Olympus.0x1009 (0x1009/Short/1) "0"
			{
				// TODO: Unknown IFD tag: Olympus / 0x1009
				var entry = makernote_structure.GetEntry (0, (ushort) 0x1009);
				Assert.IsNotNull (entry, "Entry 0x1009 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Olympus.0x100A (0x100a/Short/1) "0"
			{
				// TODO: Unknown IFD tag: Olympus / 0x100A
				var entry = makernote_structure.GetEntry (0, (ushort) 0x100A);
				Assert.IsNotNull (entry, "Entry 0x100A missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Olympus.0x100B (FocusMode/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) OlympusMakerNoteEntryTag.FocusMode);
				Assert.IsNotNull (entry, "Entry 0x100B missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Olympus.0x100C (FocusDistance/Rational/1) "1973778547/1270124588"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) OlympusMakerNoteEntryTag.FocusDistance);
				Assert.IsNotNull (entry, "Entry 0x100C missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (1973778547, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (1270124588, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Olympus.0x100D (Zoom/Short/1) "12"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) OlympusMakerNoteEntryTag.Zoom);
				Assert.IsNotNull (entry, "Entry 0x100D missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (12, (entry as ShortIFDEntry).Value);
			}
			// Olympus.0x100E (MacroFocus/Short/1) "315"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) OlympusMakerNoteEntryTag.MacroFocus);
				Assert.IsNotNull (entry, "Entry 0x100E missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (315, (entry as ShortIFDEntry).Value);
			}
			// Olympus.0x100F (SharpnessFactor/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) OlympusMakerNoteEntryTag.SharpnessFactor);
				Assert.IsNotNull (entry, "Entry 0x100F missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Olympus.0x1010 (FlashChargeLevel/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) OlympusMakerNoteEntryTag.FlashChargeLevel);
				Assert.IsNotNull (entry, "Entry 0x1010 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Olympus.0x1011 (ColorMatrix/Short/9) "12808 6873 28211 62318 6183 5428 43493 44469 45896"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) OlympusMakerNoteEntryTag.ColorMatrix);
				Assert.IsNotNull (entry, "Entry 0x1011 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 12808, 6873, 28211, 62318, 6183, 5428, 43493, 44469, 45896 }, (entry as ShortArrayIFDEntry).Values);
			}
			// Olympus.0x1012 (BlackLevel/Short/4) "37164 37255 16551 9228"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) OlympusMakerNoteEntryTag.BlackLevel);
				Assert.IsNotNull (entry, "Entry 0x1012 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 37164, 37255, 16551, 9228 }, (entry as ShortArrayIFDEntry).Values);
			}
			// Olympus.0x1013 (0x1013/Short/1) "0"
			{
				// TODO: Unknown IFD tag: Olympus / 0x1013
				var entry = makernote_structure.GetEntry (0, (ushort) 0x1013);
				Assert.IsNotNull (entry, "Entry 0x1013 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Olympus.0x1014 (0x1014/Short/1) "0"
			{
				// TODO: Unknown IFD tag: Olympus / 0x1014
				var entry = makernote_structure.GetEntry (0, (ushort) 0x1014);
				Assert.IsNotNull (entry, "Entry 0x1014 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Olympus.0x1015 (WhiteBalance/Short/2) "1 0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) OlympusMakerNoteEntryTag.WhiteBalance);
				Assert.IsNotNull (entry, "Entry 0x1015 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 1, 0 }, (entry as ShortArrayIFDEntry).Values);
			}
			// Olympus.0x1016 (0x1016/Short/1) "0"
			{
				// TODO: Unknown IFD tag: Olympus / 0x1016
				var entry = makernote_structure.GetEntry (0, (ushort) 0x1016);
				Assert.IsNotNull (entry, "Entry 0x1016 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Olympus.0x1017 (RedBalance/Short/2) "394 64"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) OlympusMakerNoteEntryTag.RedBalance);
				Assert.IsNotNull (entry, "Entry 0x1017 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 394, 64 }, (entry as ShortArrayIFDEntry).Values);
			}
			// Olympus.0x1018 (BlueBalance/Short/2) "406 64"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) OlympusMakerNoteEntryTag.BlueBalance);
				Assert.IsNotNull (entry, "Entry 0x1018 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 406, 64 }, (entry as ShortArrayIFDEntry).Values);
			}
			// Olympus.0x1019 (0x1019/Short/1) "0"
			{
				// TODO: Unknown IFD tag: Olympus / 0x1019
				var entry = makernote_structure.GetEntry (0, (ushort) 0x1019);
				Assert.IsNotNull (entry, "Entry 0x1019 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Olympus.0x101A (SerialNumber2/Ascii/32) "Fz�2�T6^c8m�)ǿ�"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) OlympusMakerNoteEntryTag.SerialNumber2);
				Assert.IsNotNull (entry, "Entry 0x101A missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("Fz�2�T6^c8m�)ǿ�", (entry as StringIFDEntry).Value);
			}
			// Olympus.0x101B (0x101b/Long/1) "0"
			{
				// TODO: Unknown IFD tag: Olympus / 0x101B
				var entry = makernote_structure.GetEntry (0, (ushort) 0x101B);
				Assert.IsNotNull (entry, "Entry 0x101B missing in IFD 0");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (0, (entry as LongIFDEntry).Value);
			}
			// Olympus.0x101C (0x101c/Long/1) "0"
			{
				// TODO: Unknown IFD tag: Olympus / 0x101C
				var entry = makernote_structure.GetEntry (0, (ushort) 0x101C);
				Assert.IsNotNull (entry, "Entry 0x101C missing in IFD 0");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (0, (entry as LongIFDEntry).Value);
			}
			// Olympus.0x101D (0x101d/Long/1) "0"
			{
				// TODO: Unknown IFD tag: Olympus / 0x101D
				var entry = makernote_structure.GetEntry (0, (ushort) 0x101D);
				Assert.IsNotNull (entry, "Entry 0x101D missing in IFD 0");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (0, (entry as LongIFDEntry).Value);
			}
			// Olympus.0x101E (0x101e/Long/1) "0"
			{
				// TODO: Unknown IFD tag: Olympus / 0x101E
				var entry = makernote_structure.GetEntry (0, (ushort) 0x101E);
				Assert.IsNotNull (entry, "Entry 0x101E missing in IFD 0");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (0, (entry as LongIFDEntry).Value);
			}
			// Olympus.0x101F (0x101f/Long/1) "0"
			{
				// TODO: Unknown IFD tag: Olympus / 0x101F
				var entry = makernote_structure.GetEntry (0, (ushort) 0x101F);
				Assert.IsNotNull (entry, "Entry 0x101F missing in IFD 0");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (0, (entry as LongIFDEntry).Value);
			}
			// Olympus.0x1020 (0x1020/Long/1) "0"
			{
				// TODO: Unknown IFD tag: Olympus / 0x1020
				var entry = makernote_structure.GetEntry (0, (ushort) 0x1020);
				Assert.IsNotNull (entry, "Entry 0x1020 missing in IFD 0");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (0, (entry as LongIFDEntry).Value);
			}
			// Olympus.0x1021 (0x1021/Long/1) "0"
			{
				// TODO: Unknown IFD tag: Olympus / 0x1021
				var entry = makernote_structure.GetEntry (0, (ushort) 0x1021);
				Assert.IsNotNull (entry, "Entry 0x1021 missing in IFD 0");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (0, (entry as LongIFDEntry).Value);
			}
			// Olympus.0x1022 (0x1022/Long/1) "0"
			{
				// TODO: Unknown IFD tag: Olympus / 0x1022
				var entry = makernote_structure.GetEntry (0, (ushort) 0x1022);
				Assert.IsNotNull (entry, "Entry 0x1022 missing in IFD 0");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (0, (entry as LongIFDEntry).Value);
			}
			// Olympus.0x1023 (FlashBias/SRational/1) "-1152340400/689675175"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) OlympusMakerNoteEntryTag.FlashBias);
				Assert.IsNotNull (entry, "Entry 0x1023 missing in IFD 0");
				Assert.IsNotNull (entry as SRationalIFDEntry, "Entry is not a srational!");
				Assert.AreEqual (-1152340400, (entry as SRationalIFDEntry).Value.Numerator);
				Assert.AreEqual (689675175, (entry as SRationalIFDEntry).Value.Denominator);
			}
			// Olympus.0x1024 (0x1024/Short/1) "0"
			{
				// TODO: Unknown IFD tag: Olympus / 0x1024
				var entry = makernote_structure.GetEntry (0, (ushort) 0x1024);
				Assert.IsNotNull (entry, "Entry 0x1024 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Olympus.0x1025 (0x1025/SRational/1) "-1727258578/1939699"
			{
				// TODO: Unknown IFD tag: Olympus / 0x1025
				var entry = makernote_structure.GetEntry (0, (ushort) 0x1025);
				Assert.IsNotNull (entry, "Entry 0x1025 missing in IFD 0");
				Assert.IsNotNull (entry as SRationalIFDEntry, "Entry is not a srational!");
				Assert.AreEqual (-1727258578, (entry as SRationalIFDEntry).Value.Numerator);
				Assert.AreEqual (1939699, (entry as SRationalIFDEntry).Value.Denominator);
			}
			// Olympus.0x1026 (ExternalFlashBounce/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) OlympusMakerNoteEntryTag.ExternalFlashBounce);
				Assert.IsNotNull (entry, "Entry 0x1026 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Olympus.0x1027 (ExternalFlashZoom/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) OlympusMakerNoteEntryTag.ExternalFlashZoom);
				Assert.IsNotNull (entry, "Entry 0x1027 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Olympus.0x1028 (ExternalFlashMode/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) OlympusMakerNoteEntryTag.ExternalFlashMode);
				Assert.IsNotNull (entry, "Entry 0x1028 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Olympus.0x1029 (Contrast/Short/1) "1"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) OlympusMakerNoteEntryTag.Contrast);
				Assert.IsNotNull (entry, "Entry 0x1029 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1, (entry as ShortIFDEntry).Value);
			}
			// Olympus.0x102A (SharpnessFactor/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) OlympusMakerNoteEntryTag.SharpnessFactor);
				Assert.IsNotNull (entry, "Entry 0x102A missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Olympus.0x102B (ColorControl/Short/6) "56024 63818 42376 12 65280 14993"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) OlympusMakerNoteEntryTag.ColorControl);
				Assert.IsNotNull (entry, "Entry 0x102B missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 56024, 63818, 42376, 12, 65280, 14993 }, (entry as ShortArrayIFDEntry).Values);
			}
			// Olympus.0x102C (ValidBits/Short/2) "10 0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) OlympusMakerNoteEntryTag.ValidBits);
				Assert.IsNotNull (entry, "Entry 0x102C missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 10, 0 }, (entry as ShortArrayIFDEntry).Values);
			}
			// Olympus.0x102D (CoringFilter/Short/1) "1792"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) OlympusMakerNoteEntryTag.CoringFilter);
				Assert.IsNotNull (entry, "Entry 0x102D missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1792, (entry as ShortIFDEntry).Value);
			}
			// Olympus.0x102E (ImageWidth/Long/1) "2240"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) OlympusMakerNoteEntryTag.ImageWidth);
				Assert.IsNotNull (entry, "Entry 0x102E missing in IFD 0");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (2240, (entry as LongIFDEntry).Value);
			}
			// Olympus.0x102F (ImageHeight/Long/1) "1680"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) OlympusMakerNoteEntryTag.ImageHeight);
				Assert.IsNotNull (entry, "Entry 0x102F missing in IFD 0");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (1680, (entry as LongIFDEntry).Value);
			}
			// Olympus.0x1030 (0x1030/Short/1) "0"
			{
				// TODO: Unknown IFD tag: Olympus / 0x1030
				var entry = makernote_structure.GetEntry (0, (ushort) 0x1030);
				Assert.IsNotNull (entry, "Entry 0x1030 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Olympus.0x1031 (0x1031/Long/8) "456164396 3548531428 3220502079 1524028172 379014428 2764611586 157170167 2795214894"
			{
				// TODO: Unknown IFD tag: Olympus / 0x1031
				var entry = makernote_structure.GetEntry (0, (ushort) 0x1031);
				Assert.IsNotNull (entry, "Entry 0x1031 missing in IFD 0");
				Assert.IsNotNull (entry as LongArrayIFDEntry, "Entry is not a long array!");
				Assert.AreEqual (new long [] { 456164396, 3548531428, 3220502079, 1524028172, 379014428, 2764611586, 157170167, 2795214894 }, (entry as LongArrayIFDEntry).Values);
			}
			// Olympus.0x1032 (0x1032/Short/1) "0"
			{
				// TODO: Unknown IFD tag: Olympus / 0x1032
				var entry = makernote_structure.GetEntry (0, (ushort) 0x1032);
				Assert.IsNotNull (entry, "Entry 0x1032 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Olympus.0x1033 (0x1033/Long/720) "(Value ommitted)"
			{
				// TODO: Unknown IFD tag: Olympus / 0x1033
				var entry = makernote_structure.GetEntry (0, (ushort) 0x1033);
				Assert.IsNotNull (entry, "Entry 0x1033 missing in IFD 0");
				Assert.IsNotNull (entry as LongArrayIFDEntry, "Entry is not a long array!");
				Assert.AreEqual (new long [] { 3775173325, 331394186, 3281099885, 415588571, 2426091196, 4212128228, 2683815189, 1139572724, 4030669310, 877947356, 2639143467, 2070649171, 3464800745, 2355308503, 896666372, 2992184398, 3730480224, 2655348427, 1949476168, 3733186084, 3803063343, 1935747103, 863891065, 950703926, 3454560335, 246222244, 2701674380, 2626819853, 1517382345, 631836739, 3191202963, 2244309622, 4061500464, 3795641112, 3488045928, 4073115847, 206280249, 180220757, 1384811907, 1829369069, 1109751634, 1489686415, 795756839, 2330928112, 1954541719, 1050110334, 4136748398, 3006817757, 2520305159, 52224943, 357548638, 344415160, 444902544, 3345558068, 2357688060, 478163408, 4057978557, 1726171778, 1503902237, 1431841902, 2857677284, 383985415, 3060510357, 137743569, 3578698861, 1989039966, 4039826655, 3911638537, 2763491464, 3830490341, 30003179, 1568303497, 96509462, 3663574834, 2445337372, 1671773917, 3870788149, 3180420722, 958628062, 4114355108, 3246569418, 2803863317, 3540534829, 3202809588, 1533782330, 4239098617, 514240629, 971232164, 853358834, 1283694511, 1253115388, 2472364826, 779510812, 4252064062, 617834197, 886270385, 1435383474, 306235171, 4071255784, 3733273428, 1248774535, 2293129042, 3538821809, 2107986055, 2426153512, 3841867485, 3953308633, 93085972, 303122252, 1737149109, 3551364500, 1991385786, 4008910711, 1000320639, 248830453, 663092370, 857745928, 519962368, 1246447478, 960995292, 2395515671, 2284662510, 2947868834, 725462092, 3905392069, 1309211903, 11358053, 1897312037, 3617460640, 286824565, 3717300749, 634182270, 2405626554, 2828166692, 4063705886, 3164565442, 2815462827, 1495323558, 3073446409, 2378682259, 3475455354, 3569765116, 3084153749, 1045915133, 1966628744, 3725805740, 3975050445, 1009291048, 3943325923, 2352086136, 3485301028, 4049467781, 3537270455, 1275498529, 557055458, 956099561, 4249183160, 2309844716, 2778606465, 3846149856, 12499420, 238984407, 2532857668, 2197013598, 804321884, 144928977, 2855873620, 2281249806, 4219448679, 1385329594, 1022006511, 731802911, 3440968008, 1886979327, 4891944, 220399553, 3345356253, 4174365043, 2018366039, 2444127588, 2998982, 319678932, 3804024282, 1605758496, 2409348896, 4082713243, 3149825546, 257117378, 1423677646, 918445078, 538044755, 4189203334, 2438284870, 3707218137, 3526673509, 2764685341, 842901782, 1360214240, 2625413363, 1488383078, 1783474582, 574180118, 957116801, 3982127453, 1142258920, 3654823687, 2460472237, 1936461882, 3369206365, 1191422014, 4117591412, 4074122115, 3109384287, 523303506, 3849273651, 1273874943, 4106751, 8245592, 2137960959, 10384255, 3745680047, 1690723657, 2791096853, 522275589, 3721868752, 3916829380, 3230222866, 941161411, 3174521544, 814252043, 3077969124, 1946091754, 2863881436, 2964807753, 1411121274, 528140915, 2638785300, 3677875444, 3635265936, 3978885320, 1849806159, 243478179, 2125519067, 2844901664, 1862274165, 3870453029, 2512521111, 2571710137, 3474580457, 1587667181, 349930708, 4243941477, 3391780643, 953056116, 2846183026, 2875640741, 121198828, 880621921, 3533060142, 414393892, 1998371715, 2194136864, 4267008734, 3874938269, 2789483377, 1571453246, 3802387486, 725309342, 13748387, 587435643, 3820843226, 4019105190, 1228200375, 2307940767, 22307953, 3355656014, 735354184, 3253451243, 1098071646, 3886210703, 3059286596, 2298298567, 3455735149, 728513121, 1923909643, 829888551, 2457062777, 3026131565, 873277805, 615607209, 3797030060, 2217472625, 2518720464, 3608915068, 1121352789, 1628814530, 1210156212, 2990296562, 1674609209, 3850844643, 347972201, 1293494708, 3104765893, 3191591924, 1221754143, 1445933902, 3151901220, 1073750159, 1189860149, 3614038741, 3334332891, 1002005589, 4218269810, 1612240968, 3826029512, 3925351161, 920841819, 4098908994, 3952431243, 2193874717, 1224652267, 3741569876, 3566663381, 1966695089, 3051022036, 2729454809, 3826387231, 2007025489, 1461236819, 2004314019, 3316145351, 1882032187, 3582125300, 3239923637, 1959942491, 3445907172, 2380362446, 238028788, 2887365782, 2203530053, 1996384606, 2266561707, 3485345016, 2948905533, 3961146261, 1528542244, 3406593851, 685872374, 4278253046, 3868021367, 1186292893, 2624862959, 501840442, 1746070032, 1571043249, 3328478716, 4025854471, 735551034, 2078456207, 54752125, 2003793264, 1653477952, 2386820786, 1175089231, 1052681149, 2046145351, 2067477282, 2391059376, 32624143, 1582257103, 655743194, 1554209882, 1288877073, 1122337962, 1257866368, 1415817952, 332381554, 3452029257, 2694878458, 3933928869, 4217180443, 1180010815, 822346305, 3354699721, 1738922218, 3218765966, 1003199920, 3141206111, 1748561918, 2131389853, 2499481989, 3295833921, 3049728899, 4259315456, 1608998479, 3811298251, 1864054173, 3418958934, 2705604170, 1637104258, 1501162386, 1205982906, 710176909, 3983747512, 3739852279, 3292220347, 3742096732, 2862759312, 2031422318, 213612910, 3626004167, 1593770119, 2167774242, 3525925413, 743340264, 1810402582, 1507418438, 386321474, 2818384646, 1115316657, 3345995452, 2682422933, 1839659156, 1197522509, 2895162233, 1790913646, 1942025416, 432360788, 3672524582, 647727702, 1729933078, 3787000006, 14277611, 1337492189, 654731638, 4007741468, 1743477164, 3527635089, 4209250831, 1129223908, 270704066, 2742033495, 1059836130, 2980237618, 932584740, 3239586361, 894748850, 1221675648, 983549071, 2056665442, 75770375, 3713525663, 2805287497, 1628911725, 3229729576, 3462941600, 335089919, 9520439, 1627323262, 2147314216, 3138098656, 3578761932, 3745082368, 1223178669, 3364095698, 400350109, 2383759140, 2069307537, 4181731475, 59539803, 4137129724, 2238549673, 4071854478, 707515062, 2972291932, 1216931348, 1872261144, 535256713, 3771667306, 2596707102, 2463154864, 1529529559, 2075243898, 28777655, 3194734941, 2524538515, 2442392290, 3175334002, 3914528890, 1477894309, 3626487439, 3422589342, 263767401, 1243009256, 3259430188, 3717130075, 2465153776, 3989107038, 592118383, 48818034, 2394881792, 1257299275, 1586950743, 2531797860, 3854340482, 3275602229, 3496613559, 372750192, 4014566510, 1498710934, 1610742970, 2452327636, 1205448494, 546009633, 3242255342, 2211979119, 1792174413, 3723771241, 1380998358, 1339009362, 1807072658, 589562758, 1851623437, 3892919423, 707103453, 1814337486, 1221535075, 1438702993, 2449348003, 2699612875, 876041050, 2782767915, 497874976, 2364807452, 2105371229, 1895397169, 3476983030, 724173587, 3963567932, 271594042, 235927635, 2001564316, 1934839114, 3684217308, 1515937094, 3605852707, 2251170658, 2803725941, 3922984074, 3689989793, 2842229457, 3572788185, 747307690, 2890082596, 1735069419, 2673862489, 376450971, 739075725, 620504008, 1253337044, 1664656569, 393319074, 1034657335, 314839494, 1124086334, 2497411025, 2440471929, 493195205, 2400466824, 2780795848, 3132723524, 251955707, 3181441024, 404783695, 553119573, 2086003437, 1398790051, 3116325712, 2445697673, 588014421, 3930602867, 2661726392, 1708882114, 1407562496, 29826773, 2931185484, 3008306305, 1616197345, 2425819710, 3502537459, 3610822251, 1504114648, 3614810404, 2010339730, 2523685705, 1991032401, 3228944236, 4216806693, 3452230271, 4133855814, 280111159, 229951279, 2786047870, 1177668979, 2050781976, 2924927304, 3106567848, 1574688134, 410857551, 1035593405, 1673723396, 2625130316, 3622080089, 2942539111, 1266141133, 130682732, 2519347769, 3343565713, 3387691188, 1952868228, 3347590318, 2921208213, 595622159, 18552217, 2130051272, 132103511, 452373986, 1863437169, 1671902359, 1453999249, 2113290264, 2393053503, 3876970314, 2939598562, 3715177440, 3826056960, 268041387, 1464382386, 3517963617, 2367908193, 1183992928, 3138548303, 3107563241, 699291569, 3165247787, 2056633395, 2916739702, 4074495676, 2526515357, 2988652230, 132109306, 1469053183, 5225170, 2085188971, 889127018, 3888940638, 3847983504, 975169494, 3136260510, 473095709, 1791367526, 1731946053, 1111546559, 2133041344, 3841796995, 3621893813, 3096817121, 826460850, 3332475178, 2896265252, 4123423044, 4002884423, 2640169830, 1707419549, 2510124700, 1728292196, 1599714482, 613529320, 2737077610, 3932092850, 849885534, 3295832571, 3784470553, 1492881055, 1255028611, 3117001316, 1456598365, 2327941115, 1669725952, 3956597465, 1363364074, 1927034425, 528834525, 180548012, 705291290, 2794540851, 2854276201, 3358311708, 3608245198, 3053387934, 4278253801, 2729179507, 4278243079, 4278238935, 4230521749, 1998160133, 3109286658, 1030908402, 294781148, 1198157351, 57886400, 2111979744, 179352007, 839276880, 1245675660, 334802205, 3996254404, 1175431652, 1959094888, 3601544660 }, (entry as LongArrayIFDEntry).Values);
			}
			// Olympus.0x1034 (CompressionRatio/Rational/1) "3531718916/243211988"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) OlympusMakerNoteEntryTag.CompressionRatio);
				Assert.IsNotNull (entry, "Entry 0x1034 missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (3531718916, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (243211988, (entry as RationalIFDEntry).Value.Denominator);
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
			// Photo.0xA002 (PixelXDimension/Short/1) "2240"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.PixelXDimension);
				Assert.IsNotNull (entry, "Entry 0xA002 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (2240, (entry as ShortIFDEntry).Value);
			}
			// Photo.0xA003 (PixelYDimension/Short/1) "1680"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.PixelYDimension);
				Assert.IsNotNull (entry, "Entry 0xA003 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1680, (entry as ShortIFDEntry).Value);
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
			// Photo.0xA005 (InteroperabilityTag/SubIFD/1) "1504"
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
			// Thumbnail.0x0201 (JPEGInterchangeFormat/ThumbnailDataIFD/1) "1628"
			{
				var entry = structure.GetEntry (1, (ushort) IFDEntryTag.JPEGInterchangeFormat);
				Assert.IsNotNull (entry, "Entry 0x0201 missing in IFD 1");
				Assert.IsNotNull (entry as ThumbnailDataIFDEntry, "Entry is not a thumbnail IFD!");
			}
			// Thumbnail.0x0202 (JPEGInterchangeFormatLength/Long/1) "11447"
			{
				var entry = structure.GetEntry (1, (ushort) IFDEntryTag.JPEGInterchangeFormatLength);
				Assert.IsNotNull (entry, "Entry 0x0202 missing in IFD 1");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (11447, (entry as LongIFDEntry).Value);
			}

			//  ---------- End of IFD tests ----------

		}
	}
}
