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
	public class Cr2Canon400d1Test
	{
		[Test]
		public void Test ()
		{
			ImageTest.Run ("raw-samples/CR2", "sample_canon_400d1.cr2",
				false,
				new Cr2Canon400d1TestInvariantValidator ()
			);
		}
	}

	public class Cr2Canon400d1TestInvariantValidator : IMetadataInvariantValidator
	{
		public void ValidateMetadataInvariants (Image.File file)
		{
			Assert.IsNotNull (file);
			//  ---------- Start of IFD tests ----------

			var tag = file.GetTag (TagTypes.TiffIFD) as IFDTag;
			Assert.IsNotNull (tag, "IFD tag not found");

			var structure = tag.Structure;

			// Image.0x0100 (ImageWidth/Short/1) "3888"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.ImageWidth);
				Assert.IsNotNull (entry, "Entry 0x0100 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (3888, (entry as ShortIFDEntry).Value);
			}
			// Image.0x0101 (ImageLength/Short/1) "2592"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.ImageLength);
				Assert.IsNotNull (entry, "Entry 0x0101 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (2592, (entry as ShortIFDEntry).Value);
			}
			// Image.0x0102 (BitsPerSample/Short/3) "8 8 8"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.BitsPerSample);
				Assert.IsNotNull (entry, "Entry 0x0102 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 8, 8, 8 }, (entry as ShortArrayIFDEntry).Values);
			}
			// Image.0x0103 (Compression/Short/1) "6"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.Compression);
				Assert.IsNotNull (entry, "Entry 0x0103 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (6, (entry as ShortIFDEntry).Value);
			}
			// Image.0x010F (Make/Ascii/6) "Canon"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.Make);
				Assert.IsNotNull (entry, "Entry 0x010F missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("Canon", (entry as StringIFDEntry).Value);
			}
			// Image.0x0110 (Model/Ascii/23) "Canon EOS 400D DIGITAL"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.Model);
				Assert.IsNotNull (entry, "Entry 0x0110 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("Canon EOS 400D DIGITAL", (entry as StringIFDEntry).Value);
			}
			// Image.0x0111 (StripOffsets/StripOffsets/1) "82709"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.StripOffsets);
				Assert.IsNotNull (entry, "Entry 0x0111 missing in IFD 0");
				Assert.IsNotNull (entry as StripOffsetsIFDEntry, "Entry is not a strip offsets entry!");
				Assert.AreEqual (1, (entry as StripOffsetsIFDEntry).Values.Length);
			}
			// Image.0x0112 (Orientation/Short/1) "1"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.Orientation);
				Assert.IsNotNull (entry, "Entry 0x0112 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1, (entry as ShortIFDEntry).Value);
			}
			// Image.0x0117 (StripByteCounts/Long/1) "2373662"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.StripByteCounts);
				Assert.IsNotNull (entry, "Entry 0x0117 missing in IFD 0");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (2373662, (entry as LongIFDEntry).Value);
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
			// Image.0x0128 (ResolutionUnit/Short/1) "2"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.ResolutionUnit);
				Assert.IsNotNull (entry, "Entry 0x0128 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (2, (entry as ShortIFDEntry).Value);
			}
			// Image.0x0132 (DateTime/Ascii/20) "2010:02:15 12:10:55"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.DateTime);
				Assert.IsNotNull (entry, "Entry 0x0132 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("2010:02:15 12:10:55", (entry as StringIFDEntry).Value);
			}
			// Image.0x8769 (ExifTag/SubIFD/1) "270"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.ExifIFD);
				Assert.IsNotNull (entry, "Entry 0x8769 missing in IFD 0");
				Assert.IsNotNull (entry as SubIFDEntry, "Entry is not a sub IFD!");
			}

			var exif = structure.GetEntry (0, (ushort) IFDEntryTag.ExifIFD) as SubIFDEntry;
			Assert.IsNotNull (exif, "Exif tag not found");
			var exif_structure = exif.Structure;

			// Photo.0x829A (ExposureTime/Rational/1) "1/100"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ExposureTime);
				Assert.IsNotNull (entry, "Entry 0x829A missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (1, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (100, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Photo.0x829D (FNumber/Rational/1) "63/10"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.FNumber);
				Assert.IsNotNull (entry, "Entry 0x829D missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (63, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (10, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Photo.0x8822 (ExposureProgram/Short/1) "1"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ExposureProgram);
				Assert.IsNotNull (entry, "Entry 0x8822 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1, (entry as ShortIFDEntry).Value);
			}
			// Photo.0x8827 (ISOSpeedRatings/Short/1) "100"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ISOSpeedRatings);
				Assert.IsNotNull (entry, "Entry 0x8827 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (100, (entry as ShortIFDEntry).Value);
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
			// Photo.0x9003 (DateTimeOriginal/Ascii/20) "2010:02:15 12:10:55"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.DateTimeOriginal);
				Assert.IsNotNull (entry, "Entry 0x9003 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("2010:02:15 12:10:55", (entry as StringIFDEntry).Value);
			}
			// Photo.0x9004 (DateTimeDigitized/Ascii/20) "2010:02:15 12:10:55"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.DateTimeDigitized);
				Assert.IsNotNull (entry, "Entry 0x9004 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("2010:02:15 12:10:55", (entry as StringIFDEntry).Value);
			}
			// Photo.0x9101 (ComponentsConfiguration/Undefined/4) "0 0 0 0"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ComponentsConfiguration);
				Assert.IsNotNull (entry, "Entry 0x9101 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 0, 0, 0, 0 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Photo.0x9201 (ShutterSpeedValue/SRational/1) "435412/65536"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ShutterSpeedValue);
				Assert.IsNotNull (entry, "Entry 0x9201 missing in IFD 0");
				Assert.IsNotNull (entry as SRationalIFDEntry, "Entry is not a srational!");
				Assert.AreEqual (435412, (entry as SRationalIFDEntry).Value.Numerator);
				Assert.AreEqual (65536, (entry as SRationalIFDEntry).Value.Denominator);
			}
			// Photo.0x9202 (ApertureValue/Rational/1) "348042/65536"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ApertureValue);
				Assert.IsNotNull (entry, "Entry 0x9202 missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (348042, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (65536, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Photo.0x9204 (ExposureBiasValue/SRational/1) "0/3"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ExposureBiasValue);
				Assert.IsNotNull (entry, "Entry 0x9204 missing in IFD 0");
				Assert.IsNotNull (entry as SRationalIFDEntry, "Entry is not a srational!");
				Assert.AreEqual (0, (entry as SRationalIFDEntry).Value.Numerator);
				Assert.AreEqual (3, (entry as SRationalIFDEntry).Value.Denominator);
			}
			// Photo.0x9207 (MeteringMode/Short/1) "5"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.MeteringMode);
				Assert.IsNotNull (entry, "Entry 0x9207 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (5, (entry as ShortIFDEntry).Value);
			}
			// Photo.0x9209 (Flash/Short/1) "9"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.Flash);
				Assert.IsNotNull (entry, "Entry 0x9209 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (9, (entry as ShortIFDEntry).Value);
			}
			// Photo.0x920A (FocalLength/Rational/1) "25/1"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.FocalLength);
				Assert.IsNotNull (entry, "Entry 0x920A missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (25, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (1, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Photo.0x927C (MakerNote/MakerNote/76646) "(Value ommitted)"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.MakerNote);
				Assert.IsNotNull (entry, "Entry 0x927C missing in IFD 0");
				Assert.IsNotNull (entry as MakernoteIFDEntry, "Entry is not a makernote IFD!");
			}

			var makernote = exif_structure.GetEntry (0, (ushort) ExifEntryTag.MakerNote) as MakernoteIFDEntry;
			Assert.IsNotNull (makernote, "MakerNote tag not found");
			var makernote_structure = makernote.Structure;


			Assert.AreEqual (MakernoteType.Canon, makernote.MakernoteType);

			// CanonCs.0x0000 (0x0000/Short/1) "92"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (1 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (92, (entry as ShortArrayIFDEntry).Values [0]);
			}
			// CanonCs.0x0001 (Macro/Short/1) "2"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (2 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (2, (entry as ShortArrayIFDEntry).Values [1]);
			}
			// CanonCs.0x0002 (Selftimer/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (3 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [2]);
			}
			// CanonCs.0x0003 (Quality/Short/1) "4"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (4 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (4, (entry as ShortArrayIFDEntry).Values [3]);
			}
			// CanonCs.0x0004 (FlashMode/Short/1) "2"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (5 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (2, (entry as ShortArrayIFDEntry).Values [4]);
			}
			// CanonCs.0x0005 (DriveMode/Short/1) "1"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (6 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (1, (entry as ShortArrayIFDEntry).Values [5]);
			}
			// CanonCs.0x0006 (0x0006/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (7 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [6]);
			}
			// CanonCs.0x0007 (FocusMode/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (8 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [7]);
			}
			// CanonCs.0x0008 (0x0008/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (9 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [8]);
			}
			// CanonCs.0x0009 (0x0009/Short/1) "7"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (10 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (7, (entry as ShortArrayIFDEntry).Values [9]);
			}
			// CanonCs.0x000A (ImageSize/Short/1) "65535"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (11 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (65535, (entry as ShortArrayIFDEntry).Values [10]);
			}
			// CanonCs.0x000B (EasyMode/Short/1) "1"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (12 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (1, (entry as ShortArrayIFDEntry).Values [11]);
			}
			// CanonCs.0x000C (DigitalZoom/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (13 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [12]);
			}
			// CanonCs.0x000D (Contrast/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (14 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [13]);
			}
			// CanonCs.0x000E (Saturation/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (15 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [14]);
			}
			// CanonCs.0x000F (Sharpness/Short/1) "3"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (16 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (3, (entry as ShortArrayIFDEntry).Values [15]);
			}
			// CanonCs.0x0010 (ISOSpeed/Short/1) "32767"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (17 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (32767, (entry as ShortArrayIFDEntry).Values [16]);
			}
			// CanonCs.0x0011 (MeteringMode/Short/1) "3"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (18 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (3, (entry as ShortArrayIFDEntry).Values [17]);
			}
			// CanonCs.0x0012 (FocusType/Short/1) "2"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (19 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (2, (entry as ShortArrayIFDEntry).Values [18]);
			}
			// CanonCs.0x0013 (AFPoint/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (20 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [19]);
			}
			// CanonCs.0x0014 (ExposureProgram/Short/1) "4"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (21 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (4, (entry as ShortArrayIFDEntry).Values [20]);
			}
			// CanonCs.0x0015 (0x0015/Short/1) "65535"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (22 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (65535, (entry as ShortArrayIFDEntry).Values [21]);
			}
			// CanonCs.0x0016 (LensType/Short/1) "37"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (23 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (37, (entry as ShortArrayIFDEntry).Values [22]);
			}
			// CanonCs.0x0017 (Lens/Short/3) "50 17 1"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (26 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (50, (entry as ShortArrayIFDEntry).Values [23]);
				Assert.AreEqual (17, (entry as ShortArrayIFDEntry).Values [24]);
				Assert.AreEqual (1, (entry as ShortArrayIFDEntry).Values [25]);
			}
			// CanonCs.0x001A (MaxAperture/Short/1) "85"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (27 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (85, (entry as ShortArrayIFDEntry).Values [26]);
			}
			// CanonCs.0x001B (MinAperture/Short/1) "304"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (28 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (304, (entry as ShortArrayIFDEntry).Values [27]);
			}
			// CanonCs.0x001C (FlashActivity/Short/1) "141"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (29 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (141, (entry as ShortArrayIFDEntry).Values [28]);
			}
			// CanonCs.0x001D (FlashDetails/Short/1) "16392"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (30 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (16392, (entry as ShortArrayIFDEntry).Values [29]);
			}
			// CanonCs.0x001E (0x001e/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (31 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [30]);
			}
			// CanonCs.0x001F (0x001f/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (32 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [31]);
			}
			// CanonCs.0x0020 (FocusContinuous/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (33 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [32]);
			}
			// CanonCs.0x0021 (AESetting/Short/1) "65535"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (34 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (65535, (entry as ShortArrayIFDEntry).Values [33]);
			}
			// CanonCs.0x0022 (ImageStabilization/Short/1) "65535"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (35 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (65535, (entry as ShortArrayIFDEntry).Values [34]);
			}
			// CanonCs.0x0023 (DisplayAperture/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (36 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [35]);
			}
			// CanonCs.0x0024 (ZoomSourceWidth/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (37 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [36]);
			}
			// CanonCs.0x0025 (ZoomTargetWidth/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (38 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [37]);
			}
			// CanonCs.0x0026 (0x0026/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (39 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [38]);
			}
			// CanonCs.0x0027 (0x0027/Short/1) "65535"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (40 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (65535, (entry as ShortArrayIFDEntry).Values [39]);
			}
			// CanonCs.0x0028 (PhotoEffect/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (41 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [40]);
			}
			// CanonCs.0x0029 (0x0029/Short/1) "32767"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (42 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (32767, (entry as ShortArrayIFDEntry).Values [41]);
			}
			// CanonCs.0x002A (ColorTone/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (43 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [42]);
			}
			// CanonCs.0x002B (0x002b/Short/1) "32767"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (44 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (32767, (entry as ShortArrayIFDEntry).Values [43]);
			}
			// CanonCs.0x002C (0x002c/Short/1) "65535"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (45 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (65535, (entry as ShortArrayIFDEntry).Values [44]);
			}
			// CanonCs.0x002D (0x002d/Short/1) "65535"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (46 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (65535, (entry as ShortArrayIFDEntry).Values [45]);
			}
			// Canon.0x0002 (FocalLength/Short/4) "2 25 907 605"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.FocalLength);
				Assert.IsNotNull (entry, "Entry 0x0002 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 2, 25, 907, 605 }, (entry as ShortArrayIFDEntry).Values);
			}
			// Canon.0x0003 (0x0003/Short/4) "0 100 0 0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.Unknown3);
				Assert.IsNotNull (entry, "Entry 0x0003 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 0, 100, 0, 0 }, (entry as ShortArrayIFDEntry).Values);
			}
			// CanonSi.0x0000 (0x0000/Short/1) "68"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ShotInfo);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (1 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (68, (entry as ShortArrayIFDEntry).Values [0]);
			}
			// CanonSi.0x0001 (0x0001/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ShotInfo);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (2 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [1]);
			}
			// CanonSi.0x0002 (ISOSpeed/Short/1) "160"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ShotInfo);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (3 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (160, (entry as ShortArrayIFDEntry).Values [2]);
			}
			// CanonSi.0x0003 (0x0003/Short/1) "84"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ShotInfo);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (4 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (84, (entry as ShortArrayIFDEntry).Values [3]);
			}
			// CanonSi.0x0004 (TargetAperture/Short/1) "172"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ShotInfo);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (5 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (172, (entry as ShortArrayIFDEntry).Values [4]);
			}
			// CanonSi.0x0005 (TargetShutterSpeed/Short/1) "212"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ShotInfo);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (6 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (212, (entry as ShortArrayIFDEntry).Values [5]);
			}
			// CanonSi.0x0006 (0x0006/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ShotInfo);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (7 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [6]);
			}
			// CanonSi.0x0007 (WhiteBalance/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ShotInfo);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (8 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [7]);
			}
			// CanonSi.0x0008 (0x0008/Short/1) "3"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ShotInfo);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (9 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (3, (entry as ShortArrayIFDEntry).Values [8]);
			}
			// CanonSi.0x0009 (Sequence/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ShotInfo);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (10 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [9]);
			}
			// CanonSi.0x000A (0x000a/Short/1) "8"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ShotInfo);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (11 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (8, (entry as ShortArrayIFDEntry).Values [10]);
			}
			// CanonSi.0x000B (0x000b/Short/1) "8"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ShotInfo);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (12 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (8, (entry as ShortArrayIFDEntry).Values [11]);
			}
			// CanonSi.0x000C (0x000c/Short/1) "144"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ShotInfo);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (13 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (144, (entry as ShortArrayIFDEntry).Values [12]);
			}
			// CanonSi.0x000D (0x000d/Short/1) "65535"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ShotInfo);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (14 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (65535, (entry as ShortArrayIFDEntry).Values [13]);
			}
			// CanonSi.0x000E (AFPointUsed/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ShotInfo);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (15 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [14]);
			}
			// CanonSi.0x000F (FlashBias/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ShotInfo);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (16 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [15]);
			}
			// CanonSi.0x0010 (0x0010/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ShotInfo);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (17 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [16]);
			}
			// CanonSi.0x0011 (0x0011/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ShotInfo);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (18 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [17]);
			}
			// CanonSi.0x0012 (0x0012/Short/1) "1"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ShotInfo);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (19 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (1, (entry as ShortArrayIFDEntry).Values [18]);
			}
			// CanonSi.0x0013 (SubjectDistance/Short/1) "52"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ShotInfo);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (20 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (52, (entry as ShortArrayIFDEntry).Values [19]);
			}
			// CanonSi.0x0014 (0x0014/Short/1) "57"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ShotInfo);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (21 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (57, (entry as ShortArrayIFDEntry).Values [20]);
			}
			// CanonSi.0x0015 (ApertureValue/Short/1) "172"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ShotInfo);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (22 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (172, (entry as ShortArrayIFDEntry).Values [21]);
			}
			// CanonSi.0x0016 (ShutterSpeedValue/Short/1) "212"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ShotInfo);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (23 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (212, (entry as ShortArrayIFDEntry).Values [22]);
			}
			// CanonSi.0x0017 (0x0017/Short/1) "106"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ShotInfo);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (24 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (106, (entry as ShortArrayIFDEntry).Values [23]);
			}
			// CanonSi.0x0018 (0x0018/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ShotInfo);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (25 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [24]);
			}
			// CanonSi.0x0019 (0x0019/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ShotInfo);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (26 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [25]);
			}
			// CanonSi.0x001A (0x001a/Short/1) "252"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ShotInfo);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (27 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (252, (entry as ShortArrayIFDEntry).Values [26]);
			}
			// CanonSi.0x001B (0x001b/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ShotInfo);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (28 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [27]);
			}
			// CanonSi.0x001C (0x001c/Short/1) "65535"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ShotInfo);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (29 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (65535, (entry as ShortArrayIFDEntry).Values [28]);
			}
			// CanonSi.0x001D (0x001d/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ShotInfo);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (30 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [29]);
			}
			// CanonSi.0x001E (0x001e/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ShotInfo);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (31 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [30]);
			}
			// CanonSi.0x001F (0x001f/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ShotInfo);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (32 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [31]);
			}
			// CanonSi.0x0020 (0x0020/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ShotInfo);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (33 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [32]);
			}
			// CanonSi.0x0021 (0x0021/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ShotInfo);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (34 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [33]);
			}
			// Canon.0x0006 (ImageType/Ascii/32) "Canon EOS 400D DIGITAL"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ImageType);
				Assert.IsNotNull (entry, "Entry 0x0006 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("Canon EOS 400D DIGITAL", (entry as StringIFDEntry).Value);
			}
			// Canon.0x0007 (FirmwareVersion/Ascii/32) "Firmware 1.1.0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.FirmwareVersion);
				Assert.IsNotNull (entry, "Entry 0x0007 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("Firmware 1.1.0", (entry as StringIFDEntry).Value);
			}
			// Canon.0x0009 (OwnerName/Ascii/32) "Mike Gemuende"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.OwnerName);
				Assert.IsNotNull (entry, "Entry 0x0009 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("Mike Gemuende", (entry as StringIFDEntry).Value);
			}
			// Canon.0x000C (SerialNumber/Long/1) "630363764"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.SerialNumber);
				Assert.IsNotNull (entry, "Entry 0x000C missing in IFD 0");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (630363764, (entry as LongIFDEntry).Value);
			}
			// Canon.0x000D (0x000d/Undefined/1024) "(Value ommitted)"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.Unknown13);
				Assert.IsNotNull (entry, "Entry 0x000D missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var parsed_hash = Utils.Md5Encode (parsed_bytes);
				Assert.AreEqual ("0f343b0931126a20f133d67c2b018a3b", parsed_hash);
				Assert.AreEqual (1024, parsed_bytes.Length);
			}
			// CanonCf.0x0000 (0x0000/Short/1) "24"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CustomFunctions);
				Assert.IsNotNull (entry, "Entry 0x000F missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (1 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (24, (entry as ShortArrayIFDEntry).Values [0]);
			}
			// CanonCf.0x0001 (NoiseReduction/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CustomFunctions);
				Assert.IsNotNull (entry, "Entry 0x000F missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (2 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [1]);
			}
			// CanonCf.0x0002 (ShutterAeLock/Short/1) "256"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CustomFunctions);
				Assert.IsNotNull (entry, "Entry 0x000F missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (3 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (256, (entry as ShortArrayIFDEntry).Values [2]);
			}
			// CanonCf.0x0003 (MirrorLockup/Short/1) "512"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CustomFunctions);
				Assert.IsNotNull (entry, "Entry 0x000F missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (4 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (512, (entry as ShortArrayIFDEntry).Values [3]);
			}
			// CanonCf.0x0004 (ExposureLevelIncrements/Short/1) "768"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CustomFunctions);
				Assert.IsNotNull (entry, "Entry 0x000F missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (5 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (768, (entry as ShortArrayIFDEntry).Values [4]);
			}
			// CanonCf.0x0005 (AFAssist/Short/1) "1024"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CustomFunctions);
				Assert.IsNotNull (entry, "Entry 0x000F missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (6 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (1024, (entry as ShortArrayIFDEntry).Values [5]);
			}
			// CanonCf.0x0006 (FlashSyncSpeedAv/Short/1) "1280"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CustomFunctions);
				Assert.IsNotNull (entry, "Entry 0x000F missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (7 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (1280, (entry as ShortArrayIFDEntry).Values [6]);
			}
			// CanonCf.0x0007 (AEBSequence/Short/1) "1536"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CustomFunctions);
				Assert.IsNotNull (entry, "Entry 0x000F missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (8 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (1536, (entry as ShortArrayIFDEntry).Values [7]);
			}
			// CanonCf.0x0008 (ShutterCurtainSync/Short/1) "1792"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CustomFunctions);
				Assert.IsNotNull (entry, "Entry 0x000F missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (9 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (1792, (entry as ShortArrayIFDEntry).Values [8]);
			}
			// CanonCf.0x0009 (LensAFStopButton/Short/1) "2048"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CustomFunctions);
				Assert.IsNotNull (entry, "Entry 0x000F missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (10 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (2048, (entry as ShortArrayIFDEntry).Values [9]);
			}
			// CanonCf.0x000A (FillFlashAutoReduction/Short/1) "2304"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CustomFunctions);
				Assert.IsNotNull (entry, "Entry 0x000F missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (11 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (2304, (entry as ShortArrayIFDEntry).Values [10]);
			}
			// CanonCf.0x000B (MenuButtonReturn/Short/1) "2560"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CustomFunctions);
				Assert.IsNotNull (entry, "Entry 0x000F missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (12 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (2560, (entry as ShortArrayIFDEntry).Values [11]);
			}
			// Canon.0x0010 (ModelID/Long/1) "2147484214"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ModelID);
				Assert.IsNotNull (entry, "Entry 0x0010 missing in IFD 0");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (2147484214, (entry as LongIFDEntry).Value);
			}
			// CanonPi.0x0000 (0x0000/Short/1) "9"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.PictureInfo);
				Assert.IsNotNull (entry, "Entry 0x0012 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (1 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (9, (entry as ShortArrayIFDEntry).Values [0]);
			}
			// CanonPi.0x0001 (0x0001/Short/1) "9"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.PictureInfo);
				Assert.IsNotNull (entry, "Entry 0x0012 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (2 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (9, (entry as ShortArrayIFDEntry).Values [1]);
			}
			// CanonPi.0x0002 (ImageWidth/Short/1) "3888"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.PictureInfo);
				Assert.IsNotNull (entry, "Entry 0x0012 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (3 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (3888, (entry as ShortArrayIFDEntry).Values [2]);
			}
			// CanonPi.0x0003 (ImageHeight/Short/1) "2592"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.PictureInfo);
				Assert.IsNotNull (entry, "Entry 0x0012 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (4 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (2592, (entry as ShortArrayIFDEntry).Values [3]);
			}
			// CanonPi.0x0004 (ImageWidthAsShot/Short/1) "3504"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.PictureInfo);
				Assert.IsNotNull (entry, "Entry 0x0012 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (5 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (3504, (entry as ShortArrayIFDEntry).Values [4]);
			}
			// CanonPi.0x0005 (ImageHeightAsShot/Short/1) "2336"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.PictureInfo);
				Assert.IsNotNull (entry, "Entry 0x0012 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (6 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (2336, (entry as ShortArrayIFDEntry).Values [5]);
			}
			// CanonPi.0x0006 (0x0006/Short/1) "78"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.PictureInfo);
				Assert.IsNotNull (entry, "Entry 0x0012 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (7 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (78, (entry as ShortArrayIFDEntry).Values [6]);
			}
			// CanonPi.0x0007 (0x0007/Short/1) "78"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.PictureInfo);
				Assert.IsNotNull (entry, "Entry 0x0012 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (8 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (78, (entry as ShortArrayIFDEntry).Values [7]);
			}
			// CanonPi.0x0008 (0x0008/Short/1) "8"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.PictureInfo);
				Assert.IsNotNull (entry, "Entry 0x0012 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (9 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (8, (entry as ShortArrayIFDEntry).Values [8]);
			}
			// CanonPi.0x0009 (0x0009/Short/1) "64981"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.PictureInfo);
				Assert.IsNotNull (entry, "Entry 0x0012 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (10 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (64981, (entry as ShortArrayIFDEntry).Values [9]);
			}
			// CanonPi.0x000A (0x000a/Short/1) "571"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.PictureInfo);
				Assert.IsNotNull (entry, "Entry 0x0012 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (11 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (571, (entry as ShortArrayIFDEntry).Values [10]);
			}
			// CanonPi.0x000B (0x000b/Short/1) "64605"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.PictureInfo);
				Assert.IsNotNull (entry, "Entry 0x0012 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (12 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (64605, (entry as ShortArrayIFDEntry).Values [11]);
			}
			// CanonPi.0x000C (0x000c/Short/1) "8"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.PictureInfo);
				Assert.IsNotNull (entry, "Entry 0x0012 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (13 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (8, (entry as ShortArrayIFDEntry).Values [12]);
			}
			// CanonPi.0x000D (0x000d/Short/1) "947"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.PictureInfo);
				Assert.IsNotNull (entry, "Entry 0x0012 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (14 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (947, (entry as ShortArrayIFDEntry).Values [13]);
			}
			// CanonPi.0x000E (0x000e/Short/1) "64981"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.PictureInfo);
				Assert.IsNotNull (entry, "Entry 0x0012 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (15 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (64981, (entry as ShortArrayIFDEntry).Values [14]);
			}
			// CanonPi.0x000F (0x000f/Short/1) "571"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.PictureInfo);
				Assert.IsNotNull (entry, "Entry 0x0012 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (16 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (571, (entry as ShortArrayIFDEntry).Values [15]);
			}
			// CanonPi.0x0010 (0x0010/Short/1) "8"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.PictureInfo);
				Assert.IsNotNull (entry, "Entry 0x0012 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (17 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (8, (entry as ShortArrayIFDEntry).Values [16]);
			}
			// CanonPi.0x0011 (0x0011/Short/1) "504"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.PictureInfo);
				Assert.IsNotNull (entry, "Entry 0x0012 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (18 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (504, (entry as ShortArrayIFDEntry).Values [17]);
			}
			// CanonPi.0x0012 (0x0012/Short/1) "270"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.PictureInfo);
				Assert.IsNotNull (entry, "Entry 0x0012 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (19 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (270, (entry as ShortArrayIFDEntry).Values [18]);
			}
			// CanonPi.0x0013 (0x0013/Short/1) "270"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.PictureInfo);
				Assert.IsNotNull (entry, "Entry 0x0012 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (20 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (270, (entry as ShortArrayIFDEntry).Values [19]);
			}
			// CanonPi.0x0014 (0x0014/Short/1) "4"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.PictureInfo);
				Assert.IsNotNull (entry, "Entry 0x0012 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (21 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (4, (entry as ShortArrayIFDEntry).Values [20]);
			}
			// CanonPi.0x0015 (0x0015/Short/1) "4"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.PictureInfo);
				Assert.IsNotNull (entry, "Entry 0x0012 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (22 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (4, (entry as ShortArrayIFDEntry).Values [21]);
			}
			// CanonPi.0x0016 (AFPointsUsed/Short/1) "4"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.PictureInfo);
				Assert.IsNotNull (entry, "Entry 0x0012 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (23 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (4, (entry as ShortArrayIFDEntry).Values [22]);
			}
			// CanonPi.0x0017 (0x0017/Short/1) "65274"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.PictureInfo);
				Assert.IsNotNull (entry, "Entry 0x0012 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (24 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (65274, (entry as ShortArrayIFDEntry).Values [23]);
			}
			// CanonPi.0x0018 (0x0018/Short/1) "65274"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.PictureInfo);
				Assert.IsNotNull (entry, "Entry 0x0012 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (25 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (65274, (entry as ShortArrayIFDEntry).Values [24]);
			}
			// CanonPi.0x0019 (0x0019/Short/1) "65040"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.PictureInfo);
				Assert.IsNotNull (entry, "Entry 0x0012 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (26 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (65040, (entry as ShortArrayIFDEntry).Values [25]);
			}
			// CanonPi.0x001A (AFPointsUsed20D/Short/1) "16"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.PictureInfo);
				Assert.IsNotNull (entry, "Entry 0x0012 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (27 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (16, (entry as ShortArrayIFDEntry).Values [26]);
			}
			// CanonPi.0x001B (0x001b/Short/1) "65535"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.PictureInfo);
				Assert.IsNotNull (entry, "Entry 0x0012 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (28 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (65535, (entry as ShortArrayIFDEntry).Values [27]);
			}
			// Canon.0x0013 (0x0013/Short/4) "0 159 7 112"
			{
				// TODO: Unknown IFD tag: Canon / 0x0013
				var entry = makernote_structure.GetEntry (0, (ushort) 0x0013);
				Assert.IsNotNull (entry, "Entry 0x0013 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 0, 159, 7, 112 }, (entry as ShortArrayIFDEntry).Values);
			}
			// Canon.0x0015 (0x0015/Long/1) "2684354560"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.SerialNumberFormat);
				Assert.IsNotNull (entry, "Entry 0x0015 missing in IFD 0");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (2684354560, (entry as LongIFDEntry).Value);
			}
			// Canon.0x0019 (0x0019/Short/1) "1"
			{
				// TODO: Unknown IFD tag: Canon / 0x0019
				var entry = makernote_structure.GetEntry (0, (ushort) 0x0019);
				Assert.IsNotNull (entry, "Entry 0x0019 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1, (entry as ShortIFDEntry).Value);
			}
			// Canon.0x0083 (0x0083/Long/1) "0"
			{
				// TODO: Unknown IFD tag: Canon / 0x0083
				var entry = makernote_structure.GetEntry (0, (ushort) 0x0083);
				Assert.IsNotNull (entry, "Entry 0x0083 missing in IFD 0");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (0, (entry as LongIFDEntry).Value);
			}
			// CanonFi.0x0000 (0x0000/SShort/1) "34"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CanonFileInfo);
				Assert.IsNotNull (entry, "Entry 0x0093 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (1 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (34, (entry as ShortArrayIFDEntry).Values [0]);
			}
			// CanonFi.0x0001 (FileNumber/Long/1) "13676045"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CanonFileInfo);
				Assert.IsNotNull (entry, "Entry 0x0093 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (2 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (44557, (entry as ShortArrayIFDEntry).Values [1]);
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (3 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (208, (entry as ShortArrayIFDEntry).Values [2]);
			}
			// CanonFi.0x0003 (BracketMode/SShort/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CanonFileInfo);
				Assert.IsNotNull (entry, "Entry 0x0093 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (4 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [3]);
			}
			// CanonFi.0x0004 (BracketValue/SShort/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CanonFileInfo);
				Assert.IsNotNull (entry, "Entry 0x0093 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (5 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [4]);
			}
			// CanonFi.0x0005 (BracketShotNumber/SShort/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CanonFileInfo);
				Assert.IsNotNull (entry, "Entry 0x0093 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (6 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [5]);
			}
			// CanonFi.0x0006 (RawJpgQuality/SShort/1) "3"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CanonFileInfo);
				Assert.IsNotNull (entry, "Entry 0x0093 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (7 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (3, (entry as ShortArrayIFDEntry).Values [6]);
			}
			// CanonFi.0x0007 (RawJpgSize/SShort/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CanonFileInfo);
				Assert.IsNotNull (entry, "Entry 0x0093 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (8 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [7]);
			}
			// CanonFi.0x0008 (NoiseReduction/SShort/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CanonFileInfo);
				Assert.IsNotNull (entry, "Entry 0x0093 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (9 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [8]);
			}
			// CanonFi.0x0009 (WBBracketMode/SShort/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CanonFileInfo);
				Assert.IsNotNull (entry, "Entry 0x0093 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (10 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [9]);
			}
			// CanonFi.0x000A (0x000a/SShort/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CanonFileInfo);
				Assert.IsNotNull (entry, "Entry 0x0093 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (11 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [10]);
			}
			// CanonFi.0x000B (0x000b/SShort/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CanonFileInfo);
				Assert.IsNotNull (entry, "Entry 0x0093 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (12 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [11]);
			}
			// CanonFi.0x000C (WBBracketValueAB/SShort/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CanonFileInfo);
				Assert.IsNotNull (entry, "Entry 0x0093 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (13 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [12]);
			}
			// CanonFi.0x000D (WBBracketValueGM/SShort/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CanonFileInfo);
				Assert.IsNotNull (entry, "Entry 0x0093 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (14 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [13]);
			}
			// CanonFi.0x000E (FilterEffect/SShort/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CanonFileInfo);
				Assert.IsNotNull (entry, "Entry 0x0093 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (15 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [14]);
			}
			// CanonFi.0x000F (ToningEffect/SShort/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CanonFileInfo);
				Assert.IsNotNull (entry, "Entry 0x0093 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (16 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [15]);
			}
			// CanonFi.0x0010 (0x0010/SShort/1) "56"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CanonFileInfo);
				Assert.IsNotNull (entry, "Entry 0x0093 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (17 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (56, (entry as ShortArrayIFDEntry).Values [16]);
			}
			// Canon.0x0095 (0x0095/Ascii/64) ""
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.LensModel);
				Assert.IsNotNull (entry, "Entry 0x0095 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("", (entry as StringIFDEntry).Value.Trim ());
			}
			// Canon.0x0096 (0x0096/Ascii/16) "H0733598"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.SerialInfo);
				Assert.IsNotNull (entry, "Entry 0x0096 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("H0733598", (entry as StringIFDEntry).Value);
			}
			// Canon.0x0097 (0x0097/Undefined/1024) "(Value ommitted)"
			{
				// TODO: Unknown IFD tag: Canon / 0x0097
				var entry = makernote_structure.GetEntry (0, (ushort) 0x0097);
				Assert.IsNotNull (entry, "Entry 0x0097 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var parsed_hash = Utils.Md5Encode (parsed_bytes);
				Assert.AreEqual ("0f343b0931126a20f133d67c2b018a3b", parsed_hash);
				Assert.AreEqual (1024, parsed_bytes.Length);
			}
			// Canon.0x0098 (0x0098/Short/4) "0 0 0 0"
			{
				// TODO: Unknown IFD tag: Canon / 0x0098
				var entry = makernote_structure.GetEntry (0, (ushort) 0x0098);
				Assert.IsNotNull (entry, "Entry 0x0098 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 0, 0, 0, 0 }, (entry as ShortArrayIFDEntry).Values);
			}
			// Canon.0x00A0 (0x00a0/Short/14) "28 0 3 0 0 0 0 0 32768 5200 129 0 0 0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ProcessingInfo);
				Assert.IsNotNull (entry, "Entry 0x00A0 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 28, 0, 3, 0, 0, 0, 0, 0, 32768, 5200, 129, 0, 0, 0 }, (entry as ShortArrayIFDEntry).Values);
			}
			// Canon.0x00AA (0x00aa/Short/5) "10 483 1024 1024 651"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.MeasuredColor);
				Assert.IsNotNull (entry, "Entry 0x00AA missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 10, 483, 1024, 1024, 651 }, (entry as ShortArrayIFDEntry).Values);
			}
			// Canon.0x00B4 (0x00b4/Short/1) "1"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ColorSpace);
				Assert.IsNotNull (entry, "Entry 0x00B4 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1, (entry as ShortIFDEntry).Value);
			}
			// Canon.0x00D0 (0x00d0/Long/1) "10638427"
			{
				// TODO: Unknown IFD tag: Canon / 0x00D0
				var entry = makernote_structure.GetEntry (0, (ushort) 0x00D0);
				Assert.IsNotNull (entry, "Entry 0x00D0 missing in IFD 0");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (10638427, (entry as LongIFDEntry).Value);
			}
			// Canon.0x00E0 (0x00e0/Short/17) "34 3948 2622 1 1 52 23 3939 2614 0 0 0 0 0 0 0 0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.SensorInfo);
				Assert.IsNotNull (entry, "Entry 0x00E0 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 34, 3948, 2622, 1, 1, 52, 23, 3939, 2614, 0, 0, 0, 0, 0, 0, 0, 0 }, (entry as ShortArrayIFDEntry).Values);
			}
			// Canon.0x4001 (0x4001/Short/796) "(Value ommitted)"
			{
				// TODO: Unknown IFD tag: Canon / 0x4001
				var entry = makernote_structure.GetEntry (0, (ushort) 0x4001);
				Assert.IsNotNull (entry, "Entry 0x4001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 1, 863, 1024, 1024, 365, 570, 1024, 1024, 525, 372, 1024, 1024, 767, 224, 284, 285, 101, 165, 320, 320, 164, 117, 345, 345, 256, 0, 0, 146, 143, 149, 0, 427, 1030, 1024, 617, 231, 48, 48, 4, 422, 811, 813, 124, 27, 240, 239, 391, 427, 1006, 1003, 609, 236, 51, 51, 4, 418, 785, 787, 114, 29, 235, 233, 390, 2608, 1024, 1024, 1413, 6137, 2608, 1024, 1024, 1413, 6137, 2606, 1022, 1025, 1412, 6137, 2341, 1024, 1024, 1560, 5200, 2731, 1024, 1024, 1304, 7000, 2539, 1024, 1024, 1419, 6000, 1689, 1102, 1102, 2606, 3200, 2000, 1043, 1043, 2189, 3769, 2341, 1024, 1024, 1560, 5189, 2781, 1024, 1024, 1367, 6689, 512, 1024, 1024, 8191, 4799, 512, 1024, 1024, 8191, 4799, 512, 1024, 1024, 8191, 4799, 1613, 1052, 1052, 1823, 3689, 65155, 336, 953, 10900, 65173, 344, 928, 10000, 65219, 362, 867, 8300, 65268, 384, 804, 7000, 65326, 413, 739, 6000, 65355, 429, 708, 5600, 65390, 448, 672, 5200, 65440, 479, 624, 4700, 65500, 521, 572, 4200, 27, 568, 523, 3800, 84, 613, 481, 3500, 151, 668, 433, 3200, 199, 713, 403, 3000, 257, 770, 369, 2800, 382, 925, 305, 2400, 500, 2070, 2085, 256, 255, 256, 255, 0, 0, 0, 25, 21, 18, 25, 20, 23, 23, 21, 23, 23, 0, 0, 0, 0, 0, 0, 27, 25, 19, 19, 18, 19, 20, 20, 20, 0, 25, 23, 0, 0, 0, 24, 41, 21, 17, 19, 17, 18, 17, 18, 18, 0, 0, 0, 0, 14, 19, 31, 44, 20, 18, 19, 16, 17, 17, 16, 16, 17, 20, 0, 0, 0, 0, 0, 47, 40, 31, 42, 33, 37, 38, 35, 38, 33, 0, 0, 0, 0, 0, 0, 63, 56, 41, 43, 38, 40, 40, 40, 46, 0, 41, 33, 0, 0, 0, 56, 103, 49, 40, 43, 39, 40, 39, 39, 38, 0, 0, 0, 0, 32, 44, 79, 112, 47, 42, 44, 38, 41, 39, 39, 39, 38, 44, 0, 0, 0, 0, 0, 51, 41, 33, 45, 36, 39, 38, 34, 35, 33, 0, 0, 0, 0, 0, 0, 62, 55, 40, 41, 37, 37, 37, 35, 36, 0, 37, 32, 0, 0, 0, 60, 102, 48, 39, 41, 37, 38, 34, 34, 32, 0, 0, 0, 0, 39, 51, 83, 112, 47, 41, 43, 36, 38, 34, 32, 31, 31, 35, 0, 0, 0, 0, 0, 35, 29, 21, 28, 22, 23, 24, 20, 22, 18, 0, 0, 0, 0, 0, 0, 45, 38, 27, 27, 24, 24, 23, 23, 26, 0, 21, 17, 0, 0, 0, 44, 75, 32, 25, 27, 24, 24, 22, 21, 20, 0, 0, 0, 0, 25, 34, 59, 81, 30, 26, 27, 23, 24, 22, 21, 20, 19, 21, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 5, 2, 2, 6, 11, 23, 10, 3, 5, 6, 0, 0, 0, 0, 0, 0, 988, 72, 152, 133, 97, 115, 9, 10, 1, 0, 1, 1, 0, 0, 0, 6, 10320, 348, 479, 321, 134, 113, 21, 11, 3, 0, 0, 0, 0, 1, 18, 426, 18598, 1580, 1949, 627, 584, 198, 45, 17, 8, 4, 1, 0, 0, 145, 188, 6886, 1122, 0, 1152, 1024, 1024, 2729, 3958, 7000, 1, 4001, 210, 65394, 3861, 4185, 65308, 154, 4337, 4023, 3890, 4128, 65510, 65483, 65519, 3908, 3739, 3978, 65510, 31, 59, 4313, 4064, 56, 17, 4487, 4218, 65502, 65474, 0, 514, 512, 257, 259, 0, 1, 257, 259, 3082, 2305, 0, 11, 1539, 1284, 8, 519, 0, 0, 0, 0, 0, 256, 0, 7661, 0, 17859, 0, 17024, 0, 11005, 1024, 1024, 5, 1024, 0, 1, 0, 0, 0, 8191, 512, 8191, 512, 1024, 1024, 718, 433, 480, 678, 387, 865, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, (entry as ShortArrayIFDEntry).Values);
			}
			// Canon.0x4002 (0x4002/Short/11229) "(Value ommitted)"
			{
				// TODO: Unknown IFD tag: Canon / 0x4002
				var entry = makernote_structure.GetEntry (0, (ushort) 0x4002);
				Assert.IsNotNull (entry, "Entry 0x4002 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 22458, 4098, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 5657, 65535, 65280, 41984, 1297, 65535, 400, 272, 28673, 815, 4046, 320, 192, 40, 472, 3806, 3186, 0, 38297, 37526, 64, 64, 64, 0, 32169, 30872, 40975, 0, 0, 3976, 290, 118, 931, 120, 512, 395, 0, 290, 450, 94, 4416, 470, 152, 8523, 489, 206, 12686, 483, 251, 1000, 12605, 499, 266, 1270, 12548, 475, 285, 1400, 32838, 0, 292, 0, 118, 486, 1954, 0, 312, 1974, 92, 4404, 1999, 153, 8456, 2019, 202, 12491, 2032, 233, 1100, 12417, 2032, 244, 1358, 32798, 0, 250, 1040, 1040, 1808, 0, 17, 19, 1331, 28, 58, 128, 817, 8, 18, 0, 32, 32, 32, 0, 1, 0, 2, 80, 25703, 8500, 512, 512, 512, 1, 1, 78, 151, 28, 0, 0, 0, 0, 0, 0, 0, 3082, 3855, 17, 32, 0, 17151, 1, 2816, 512, 21862, 2672, 63, 9, 29, 6, 26, 133, 63, 13, 32, 5, 15, 67, 63, 10, 41, 5, 21, 68, 63, 21, 36, 16, 28, 102, 63, 13, 32, 10, 27, 118, 40, 512, 202, 132, 0, 1311, 0, 40, 384, 144, 88, 0, 1048, 0, 58, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 560, 400, 3080, 1099, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 256, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 72, 96, 104, 72, 104, 96, 104, 96, 104, 96, 104, 104, 112, 77, 0, 80, 0, 85, 1, 86, 1, 93, 1, 94, 1, 101, 1, 102, 1, 112, 1, 120, 1, 120, 1, 120, 1, 120, 1, 120, 1, 120, 1, 0, 0, 3200, 4800, 7000, 72, 4, 0, 104, 96, 104, 96, 104, 96, 104, 0, 0, 0, 1, 1, 0, 0, 0, 0, 1, 1, 0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 0, 1, 2, 3, 4, 5, 6, 7, 8, 0, 1, 2, 3, 4, 5, 6, 7, 8, 0, 1, 2, 3, 4, 5, 6, 7, 8, 0, 1, 2, 3, 4, 5, 6, 7, 8, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 9, 10, 11, 12, 13, 14, 15, 16, 17, 9, 10, 11, 12, 13, 14, 15, 16, 17, 9, 10, 11, 12, 13, 14, 15, 16, 17, 0, 1, 2, 3, 4, 5, 6, 7, 8, 0, 1, 2, 3, 4, 5, 6, 7, 8, 0, 1, 2, 3, 4, 5, 6, 7, 7, 7, 7, 0, 1, 2, 3, 4, 5, 6, 7, 7, 7, 7, 0, 1, 2, 3, 4, 5, 6, 7, 7, 7, 7, 0, 1, 2, 3, 4, 5, 6, 7, 7, 7, 7, 0, 1, 2, 3, 4, 5, 6, 7, 7, 7, 7, 0, 1, 2, 3, 4, 5, 6, 7, 7, 7, 7, 0, 1, 2, 3, 4, 5, 6, 7, 7, 7, 7, 0, 1, 2, 3, 4, 5, 6, 7, 7, 7, 7, 0, 1, 2, 3, 4, 5, 6, 7, 7, 7, 7, 11, 12, 13, 14, 15, 16, 17, 18, 18, 18, 18, 11, 12, 13, 14, 15, 16, 17, 18, 18, 18, 18, 11, 12, 13, 14, 15, 16, 17, 18, 18, 18, 18, 11, 12, 13, 14, 15, 16, 17, 18, 18, 18, 18, 11, 12, 13, 14, 15, 16, 17, 18, 18, 18, 18, 11, 12, 13, 14, 15, 16, 17, 18, 18, 18, 18, 0, 1, 2, 3, 4, 5, 6, 7, 7, 7, 7, 0, 1, 2, 3, 4, 5, 6, 7, 7, 7, 7, 0, 1, 2, 3, 4, 5, 6, 7, 7, 7, 7, 5657, 65535, 65280, 6681, 65535, 65280, 5657, 65535, 65280, 5657, 65535, 65280, 65535, 400, 272, 28673, 65535, 400, 272, 28673, 65535, 4496, 272, 28673, 815, 65486, 320, 192, 40, 472, 65246, 64626, 815, 65486, 320, 192, 40, 472, 65246, 64626, 815, 65486, 320, 192, 40, 472, 65246, 64626, 815, 65486, 320, 192, 40, 472, 65246, 64626, 815, 65486, 320, 192, 40, 472, 65246, 64626, 815, 65486, 320, 192, 40, 472, 65246, 64626, 815, 65486, 320, 192, 40, 472, 65246, 64626, 815, 65486, 320, 192, 40, 472, 65246, 64626, 815, 65486, 320, 192, 40, 472, 65246, 64626, 815, 65486, 320, 192, 40, 472, 65246, 64626, 815, 65486, 320, 192, 40, 472, 65246, 64626, 815, 65486, 320, 192, 40, 472, 65246, 64626, 815, 65486, 320, 192, 40, 472, 65246, 64626, 815, 65486, 320, 192, 40, 472, 65246, 64626, 815, 65486, 320, 192, 40, 472, 65246, 64626, 815, 65486, 320, 192, 40, 472, 65246, 64626, 815, 65486, 320, 192, 40, 472, 65246, 64626, 815, 65486, 320, 192, 40, 472, 65246, 64626, 770, 65466, 290, 222, 130, 382, 65276, 64606, 770, 65466, 290, 222, 130, 382, 65276, 64606, 770, 65466, 290, 222, 130, 382, 65276, 64606, 770, 65466, 290, 222, 130, 382, 65276, 64606, 770, 65466, 290, 222, 130, 382, 65276, 64606, 770, 65466, 290, 222, 130, 382, 65276, 64606, 770, 65466, 290, 222, 130, 382, 65276, 64606, 770, 65466, 290, 222, 130, 382, 65276, 64606, 770, 65466, 290, 222, 130, 382, 65276, 64606, 770, 65466, 290, 222, 130, 382, 65276, 64606, 770, 65466, 290, 222, 130, 382, 65276, 64606, 770, 65466, 290, 222, 130, 382, 65276, 64606, 815, 65486, 320, 192, 40, 472, 65246, 64626, 815, 65486, 320, 192, 40, 472, 65246, 64626, 815, 65486, 320, 192, 40, 472, 65246, 64626, 815, 65486, 320, 192, 40, 472, 65246, 64626, 815, 65486, 320, 192, 40, 472, 65246, 64626, 815, 65486, 320, 192, 40, 472, 65246, 64626, 38297, 37526, 38297, 37526, 38297, 37526, 38297, 37526, 38297, 37526, 38297, 37526, 35983, 35983, 35983, 35983, 35983, 35983, 35983, 35983, 0, 0, 0, 0, 64, 64, 64, 64, 64, 64, 64, 64, 64, 0, 28, 58, 0, 28, 58, 64, 50, 0, 64, 50, 0, 35, 64, 35, 35, 64, 35, 55, 30, 64, 55, 30, 64, 64, 64, 64, 64, 64, 64, 20, 40, 64, 20, 40, 64, 62, 55, 10, 62, 55, 10, 48, 70, 46, 48, 70, 46, 50, 32, 64, 50, 32, 64, 100, 4, 4, 195, 80, 230, 5, 110, 4, 5, 135, 100, 200, 6, 124, 6, 5, 144, 99, 200, 6, 143, 9, 6, 115, 112, 198, 15, 152, 10, 9, 125, 120, 160, 15, 178, 12, 11, 125, 124, 150, 17, 199, 14, 11, 129, 126, 140, 17, 225, 15, 0, 100, 163, 0, 0, 250, 15, 0, 100, 179, 0, 0, 84, 2, 0, 250, 71, 0, 0, 92, 3, 0, 215, 75, 0, 0, 100, 4, 4, 195, 80, 230, 5, 110, 4, 5, 135, 100, 200, 6, 124, 6, 5, 144, 99, 200, 6, 143, 9, 6, 115, 112, 198, 15, 152, 10, 9, 125, 120, 160, 15, 178, 12, 11, 125, 124, 150, 17, 199, 14, 11, 129, 126, 140, 17, 84, 2, 0, 250, 71, 0, 0, 92, 3, 0, 215, 75, 0, 0, 100, 4, 4, 195, 80, 230, 5, 110, 4, 5, 135, 100, 200, 6, 124, 6, 5, 144, 99, 200, 6, 143, 9, 6, 115, 112, 198, 15, 152, 10, 9, 125, 120, 160, 15, 178, 12, 11, 125, 124, 150, 17, 199, 14, 11, 129, 126, 140, 17, 84, 2, 0, 250, 71, 0, 0, 92, 3, 0, 215, 75, 0, 0, 100, 4, 4, 195, 80, 230, 5, 110, 4, 5, 135, 100, 200, 6, 124, 6, 5, 144, 99, 200, 6, 143, 9, 6, 115, 112, 198, 15, 152, 10, 9, 125, 120, 160, 15, 178, 12, 11, 125, 124, 150, 17, 199, 14, 11, 129, 126, 140, 17, 84, 2, 0, 250, 71, 0, 0, 92, 3, 0, 215, 75, 0, 0, 100, 4, 4, 195, 80, 230, 5, 110, 4, 5, 135, 100, 200, 6, 124, 6, 5, 144, 99, 200, 6, 143, 9, 6, 115, 112, 198, 15, 152, 10, 9, 125, 120, 160, 15, 178, 12, 11, 125, 124, 150, 17, 199, 14, 11, 129, 126, 140, 17, 84, 2, 0, 250, 71, 0, 0, 92, 3, 0, 215, 75, 0, 0, 100, 4, 4, 195, 80, 230, 5, 110, 4, 5, 135, 100, 200, 6, 124, 6, 5, 144, 99, 200, 6, 143, 9, 6, 115, 112, 198, 15, 152, 10, 9, 125, 120, 160, 15, 178, 12, 11, 125, 124, 150, 17, 199, 14, 11, 129, 126, 140, 17, 109, 5, 5, 192, 90, 155, 0, 118, 6, 5, 179, 97, 155, 0, 128, 6, 6, 160, 110, 160, 0, 140, 5, 5, 150, 120, 150, 0, 155, 7, 7, 125, 130, 125, 0, 170, 9, 5, 130, 130, 0, 0, 185, 10, 11, 85, 165, 105, 0, 200, 13, 11, 85, 175, 0, 0, 215, 15, 11, 87, 185, 0, 0, 94, 15, 9, 225, 80, 0, 0, 100, 4, 5, 225, 80, 155, 0, 109, 5, 5, 192, 90, 155, 0, 118, 6, 5, 179, 97, 155, 0, 128, 6, 6, 160, 110, 160, 0, 140, 5, 5, 150, 120, 150, 0, 155, 7, 7, 125, 130, 125, 0, 170, 9, 5, 130, 130, 0, 0, 185, 10, 11, 85, 165, 105, 0, 94, 15, 9, 225, 80, 0, 0, 100, 4, 5, 225, 80, 155, 0, 109, 5, 5, 192, 90, 155, 0, 118, 6, 5, 179, 97, 155, 0, 128, 6, 6, 160, 110, 160, 0, 140, 5, 5, 150, 120, 150, 0, 155, 7, 7, 125, 130, 125, 0, 170, 9, 5, 130, 130, 0, 0, 185, 10, 11, 85, 165, 105, 0, 94, 15, 9, 225, 80, 0, 0, 100, 4, 5, 225, 80, 155, 0, 109, 5, 5, 192, 90, 155, 0, 118, 6, 5, 179, 97, 155, 0, 128, 6, 6, 160, 110, 160, 0, 140, 5, 5, 150, 120, 150, 0, 155, 7, 7, 125, 130, 125, 0, 170, 9, 5, 130, 130, 0, 0, 185, 10, 11, 85, 165, 105, 0, 143, 9, 6, 115, 112, 198, 15, 65512, 65512, 28, 0, 65512, 65512, 28, 0, 65512, 65512, 28, 0, 65518, 65518, 21, 0, 65518, 65518, 21, 0, 65518, 65518, 21, 0, 65524, 65524, 14, 0, 65524, 65524, 14, 0, 65524, 65524, 14, 0, 65530, 65530, 7, 0, 65530, 65530, 7, 0, 65530, 65530, 7, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 6, 6, 65529, 0, 6, 6, 65529, 0, 6, 6, 65529, 0, 12, 12, 65522, 0, 12, 12, 65522, 0, 12, 12, 65522, 0, 18, 18, 65515, 0, 18, 18, 65515, 0, 18, 18, 65515, 0, 24, 24, 65508, 0, 24, 24, 65508, 0, 24, 24, 65508, 0, 65512, 65512, 28, 0, 65512, 65512, 28, 0, 65512, 65512, 28, 0, 65518, 65518, 21, 0, 65518, 65518, 21, 0, 65518, 65518, 21, 0, 65524, 65524, 14, 0, 65524, 65524, 14, 0, 65524, 65524, 14, 0, 65530, 65530, 7, 0, 65530, 65530, 7, 0, 65530, 65530, 7, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 6, 6, 65529, 0, 6, 6, 65529, 0, 6, 6, 65529, 0, 12, 12, 65522, 0, 12, 12, 65522, 0, 12, 12, 65522, 0, 18, 18, 65515, 0, 18, 18, 65515, 0, 18, 18, 65515, 0, 24, 24, 65508, 0, 24, 24, 65508, 0, 24, 24, 65508, 0, 65512, 65512, 28, 0, 65512, 65512, 28, 0, 65512, 65512, 28, 0, 65518, 65518, 21, 0, 65518, 65518, 21, 0, 65518, 65518, 21, 0, 65524, 65524, 14, 0, 65524, 65524, 14, 0, 65524, 65524, 14, 0, 65530, 65530, 7, 0, 65530, 65530, 7, 0, 65530, 65530, 7, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 6, 6, 65529, 0, 6, 6, 65529, 0, 6, 6, 65529, 0, 12, 12, 65522, 0, 12, 12, 65522, 0, 12, 12, 65522, 0, 18, 18, 65515, 0, 18, 18, 65515, 0, 18, 18, 65515, 0, 24, 24, 65508, 0, 24, 24, 65508, 0, 24, 24, 65508, 0, 65512, 65512, 28, 0, 65512, 65512, 28, 0, 65512, 65512, 28, 0, 65518, 65518, 21, 0, 65518, 65518, 21, 0, 65518, 65518, 21, 0, 65524, 65524, 14, 0, 65524, 65524, 14, 0, 65524, 65524, 14, 0, 65530, 65530, 7, 0, 65530, 65530, 7, 0, 65530, 65530, 7, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 6, 6, 65529, 0, 6, 6, 65529, 0, 6, 6, 65529, 0, 12, 12, 65522, 0, 12, 12, 65522, 0, 12, 12, 65522, 0, 18, 18, 65515, 0, 18, 18, 65515, 0, 18, 18, 65515, 0, 24, 24, 65508, 0, 24, 24, 65508, 0, 24, 24, 65508, 0, 65512, 65512, 28, 0, 65512, 65512, 28, 0, 65512, 65512, 28, 0, 65518, 65518, 21, 0, 65518, 65518, 21, 0, 65518, 65518, 21, 0, 65524, 65524, 14, 0, 65524, 65524, 14, 0, 65524, 65524, 14, 0, 65530, 65530, 7, 0, 65530, 65530, 7, 0, 65530, 65530, 7, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 6, 6, 65529, 0, 6, 6, 65529, 0, 6, 6, 65529, 0, 12, 12, 65522, 0, 12, 12, 65522, 0, 12, 12, 65522, 0, 18, 18, 65515, 0, 18, 18, 65515, 0, 18, 18, 65515, 0, 24, 24, 65508, 0, 24, 24, 65508, 0, 24, 24, 65508, 0, 65512, 65512, 28, 0, 65512, 65512, 28, 0, 65512, 65512, 28, 0, 65518, 65518, 21, 0, 65518, 65518, 21, 0, 65518, 65518, 21, 0, 65524, 65524, 14, 0, 65524, 65524, 14, 0, 65524, 65524, 14, 0, 65530, 65530, 7, 0, 65530, 65530, 7, 0, 65530, 65530, 7, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 6, 6, 65529, 0, 6, 6, 65529, 0, 6, 6, 65529, 0, 12, 12, 65522, 0, 12, 12, 65522, 0, 12, 12, 65522, 0, 18, 18, 65515, 0, 18, 18, 65515, 0, 18, 18, 65515, 0, 24, 24, 65508, 0, 24, 24, 65508, 0, 24, 24, 65508, 0, 65512, 65512, 28, 0, 65512, 65512, 28, 0, 65512, 65512, 28, 0, 65518, 65518, 21, 0, 65518, 65518, 21, 0, 65518, 65518, 21, 0, 65524, 65524, 14, 0, 65524, 65524, 14, 0, 65524, 65524, 14, 0, 65530, 65530, 7, 0, 65530, 65530, 7, 0, 65530, 65530, 7, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 6, 6, 65529, 0, 6, 6, 65529, 0, 6, 6, 65529, 0, 12, 12, 65522, 0, 12, 12, 65522, 0, 12, 12, 65522, 0, 18, 18, 65515, 0, 18, 18, 65515, 0, 18, 18, 65515, 0, 24, 24, 65508, 0, 24, 24, 65508, 0, 24, 24, 65508, 0, 65512, 65512, 28, 0, 65512, 65512, 28, 0, 65512, 65512, 28, 0, 65518, 65518, 21, 0, 65518, 65518, 21, 0, 65518, 65518, 21, 0, 65524, 65524, 14, 0, 65524, 65524, 14, 0, 65524, 65524, 14, 0, 65530, 65530, 7, 0, 65530, 65530, 7, 0, 65530, 65530, 7, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 6, 6, 65529, 0, 6, 6, 65529, 0, 6, 6, 65529, 0, 12, 12, 65522, 0, 12, 12, 65522, 0, 12, 12, 65522, 0, 18, 18, 65515, 0, 18, 18, 65515, 0, 18, 18, 65515, 0, 24, 24, 65508, 0, 24, 24, 65508, 0, 24, 24, 65508, 0, 65512, 65512, 28, 0, 65512, 65512, 28, 0, 65512, 65512, 28, 0, 65518, 65518, 21, 0, 65518, 65518, 21, 0, 65518, 65518, 21, 0, 65524, 65524, 14, 0, 65524, 65524, 14, 0, 65524, 65524, 14, 0, 65530, 65530, 7, 0, 65530, 65530, 7, 0, 65530, 65530, 7, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 6, 6, 65529, 0, 6, 6, 65529, 0, 6, 6, 65529, 0, 12, 12, 65522, 0, 12, 12, 65522, 0, 12, 12, 65522, 0, 18, 18, 65515, 0, 18, 18, 65515, 0, 18, 18, 65515, 0, 24, 24, 65508, 0, 24, 24, 65508, 0, 24, 24, 65508, 0, 65512, 65512, 28, 0, 65512, 65512, 28, 0, 65512, 65512, 28, 0, 65518, 65518, 21, 0, 65518, 65518, 21, 0, 65518, 65518, 21, 0, 65524, 65524, 14, 0, 65524, 65524, 14, 0, 65524, 65524, 14, 0, 65530, 65530, 7, 0, 65530, 65530, 7, 0, 65530, 65530, 7, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 6, 6, 65529, 0, 6, 6, 65529, 0, 6, 6, 65529, 0, 12, 12, 65522, 0, 12, 12, 65522, 0, 12, 12, 65522, 0, 18, 18, 65515, 0, 18, 18, 65515, 0, 18, 18, 65515, 0, 24, 24, 65508, 0, 24, 24, 65508, 0, 24, 24, 65508, 0, 0, 0, 0, 0, 3976, 290, 118, 931, 120, 512, 395, 0, 290, 450, 94, 4416, 470, 152, 8523, 489, 206, 12686, 483, 251, 1000, 12605, 499, 266, 1270, 12548, 475, 285, 1400, 32838, 0, 292, 3976, 290, 118, 931, 120, 512, 395, 0, 290, 450, 94, 4416, 470, 152, 8523, 489, 206, 12686, 483, 251, 1000, 12605, 499, 266, 1270, 12548, 475, 285, 1400, 32838, 0, 292, 3976, 290, 118, 931, 120, 512, 395, 0, 290, 450, 94, 4416, 470, 152, 8523, 489, 206, 12686, 483, 251, 1000, 12605, 499, 266, 1270, 12548, 475, 285, 1400, 32838, 0, 292, 3976, 290, 118, 931, 120, 512, 395, 0, 290, 450, 94, 4416, 470, 152, 8523, 489, 206, 12686, 483, 251, 1000, 12605, 499, 266, 1270, 12548, 475, 285, 1400, 32838, 0, 292, 3976, 178, 4224, 927, 48, 658, 4414, 0, 366, 447, 48, 4408, 474, 152, 8538, 483, 206, 12655, 506, 251, 1000, 12636, 485, 266, 1270, 12517, 505, 285, 1400, 32838, 0, 292, 3976, 178, 4224, 927, 48, 658, 4414, 0, 366, 447, 48, 4408, 474, 152, 8538, 483, 206, 12655, 506, 251, 1000, 12636, 485, 266, 1270, 12517, 505, 285, 1400, 32838, 0, 292, 3976, 178, 4224, 927, 48, 658, 4414, 0, 366, 447, 48, 4408, 474, 152, 8538, 483, 206, 12655, 506, 251, 1000, 12636, 485, 266, 1270, 12517, 505, 285, 1400, 32838, 0, 292, 3976, 178, 4224, 927, 48, 658, 4414, 0, 366, 447, 48, 4408, 474, 152, 8538, 483, 206, 12655, 506, 251, 1000, 12636, 485, 266, 1270, 12517, 505, 285, 1400, 32838, 0, 292, 3976, 290, 118, 931, 120, 512, 395, 0, 290, 450, 94, 4416, 470, 152, 8523, 489, 206, 12686, 483, 251, 1000, 12605, 499, 266, 1270, 12548, 475, 285, 1400, 32838, 0, 292, 3976, 290, 118, 931, 120, 512, 395, 0, 290, 450, 94, 4416, 470, 152, 8523, 489, 206, 12686, 483, 251, 1000, 12605, 499, 266, 1270, 12548, 475, 285, 1400, 32838, 0, 292, 3976, 290, 118, 931, 120, 512, 395, 0, 290, 450, 94, 4416, 470, 152, 8523, 489, 206, 12686, 483, 251, 1000, 12605, 499, 266, 1270, 12548, 475, 285, 1400, 32838, 0, 292, 3976, 290, 118, 931, 120, 512, 395, 0, 290, 450, 94, 4416, 470, 152, 8523, 489, 206, 12686, 483, 251, 1000, 12605, 499, 266, 1270, 12548, 475, 285, 1400, 32838, 0, 292, 3976, 279, 112, 935, 120, 489, 401, 0, 279, 449, 90, 4391, 471, 145, 8482, 484, 194, 12556, 455, 230, 1000, 12401, 510, 238, 1270, 12387, 508, 245, 1400, 12378, 508, 248, 3976, 279, 112, 935, 120, 489, 401, 0, 279, 449, 90, 4391, 471, 145, 8482, 484, 194, 12556, 455, 230, 1000, 12401, 510, 238, 1270, 12387, 508, 245, 1400, 12378, 508, 248, 3976, 279, 112, 935, 120, 489, 401, 0, 279, 449, 90, 4391, 471, 145, 8482, 484, 194, 12556, 455, 230, 1000, 12401, 510, 238, 1270, 12387, 508, 245, 1400, 12378, 508, 248, 3976, 279, 112, 935, 120, 489, 401, 0, 279, 449, 90, 4391, 471, 145, 8482, 484, 194, 12556, 455, 230, 1000, 12401, 510, 238, 1270, 12387, 508, 245, 1400, 12378, 508, 248, 3976, 279, 112, 935, 120, 489, 401, 0, 279, 449, 90, 4391, 471, 145, 8482, 484, 194, 12556, 455, 230, 1000, 12401, 510, 238, 1270, 12387, 508, 245, 1400, 12378, 508, 248, 3976, 279, 112, 935, 120, 489, 401, 0, 279, 449, 90, 4391, 471, 145, 8482, 484, 194, 12556, 455, 230, 1000, 12401, 510, 238, 1270, 12387, 508, 245, 1400, 12378, 508, 248, 3976, 279, 112, 935, 120, 489, 401, 0, 279, 449, 90, 4391, 471, 145, 8482, 484, 194, 12556, 455, 230, 1000, 12401, 510, 238, 1270, 12387, 508, 245, 1400, 12378, 508, 248, 3976, 279, 112, 935, 120, 489, 401, 0, 279, 449, 90, 4391, 471, 145, 8482, 484, 194, 12556, 455, 230, 1000, 12401, 510, 238, 1270, 12387, 508, 245, 1400, 12378, 508, 248, 3976, 290, 118, 931, 120, 512, 395, 0, 290, 450, 94, 4416, 470, 152, 8523, 489, 206, 12686, 483, 251, 1000, 12605, 499, 266, 1270, 12548, 475, 285, 1400, 32838, 0, 292, 3976, 290, 118, 931, 120, 512, 395, 0, 290, 450, 94, 4416, 470, 152, 8523, 489, 206, 12686, 483, 251, 1000, 12605, 499, 266, 1270, 12548, 475, 285, 1400, 32838, 0, 292, 3976, 290, 118, 931, 120, 512, 395, 0, 290, 450, 94, 4416, 470, 152, 8523, 489, 206, 12686, 483, 251, 1000, 12605, 499, 266, 1270, 12548, 475, 285, 1400, 32838, 0, 292, 3976, 290, 118, 931, 120, 512, 395, 0, 290, 450, 94, 4416, 470, 152, 8523, 489, 206, 12686, 483, 251, 1000, 12605, 499, 266, 1270, 12548, 475, 285, 1400, 32838, 0, 292, 0, 118, 655, 1823, 0, 238, 1982, 103, 8583, 1996, 147, 12692, 2022, 180, 12544, 0, 208, 1120, 12537, 2035, 226, 1380, 32864, 0, 240, 0, 118, 660, 1808, 0, 4528, 1958, 101, 8673, 1976, 145, 12727, 2017, 184, 12555, 2037, 214, 1100, 12503, 2038, 230, 1380, 32845, 0, 243, 0, 118, 599, 1860, 0, 251, 1984, 98, 8654, 1992, 146, 8449, 2028, 186, 12574, 2024, 220, 1090, 12471, 2044, 235, 1378, 32819, 0, 247, 0, 118, 553, 1892, 0, 263, 1988, 94, 4365, 2011, 146, 8462, 2023, 191, 12548, 2024, 225, 1080, 12450, 2043, 238, 1384, 32807, 0, 249, 0, 118, 486, 1954, 0, 312, 1974, 92, 4404, 1999, 153, 8456, 2019, 202, 12491, 2032, 233, 1100, 12417, 2032, 244, 1358, 32798, 0, 250, 0, 118, 430, 1992, 0, 325, 1980, 87, 4455, 1989, 153, 8480, 2014, 209, 12488, 2011, 242, 1100, 5, 0, 250, 1358, 32780, 0, 253, 0, 118, 4874, 1986, 0, 331, 1992, 83, 4519, 1973, 154, 8496, 2000, 218, 12362, 2042, 247, 1100, 12334, 2042, 251, 1358, 32780, 0, 253, 0, 118, 297, 40, 0, 371, 1986, 77, 4576, 1951, 157, 8453, 2007, 225, 12354, 2039, 250, 1100, 12312, 2045, 253, 1358, 32774, 0, 254, 0, 118, 236, 98, 0, 415, 1970, 75, 4595, 1940, 162, 12735, 1975, 230, 12335, 2041, 251, 1100, 2, 0, 253, 1358, 32774, 0, 254, 1, 41, 631, 1833, 0, 493, 1833, 45, 4508, 1939, 103, 8625, 1982, 138, 12787, 2007, 167, 750, 12569, 2042, 199, 1196, 32882, 0, 227, 1, 41, 645, 1789, 0, 479, 1845, 45, 4512, 1945, 102, 8654, 1978, 138, 8460, 2026, 169, 750, 12581, 2038, 203, 1203, 32867, 0, 231, 1, 41, 535, 1977, 0, 489, 1848, 41, 4540, 1934, 100, 8670, 1983, 138, 8491, 2021, 171, 748, 12598, 2033, 208, 1407, 32838, 0, 245, 1, 41, 399, 196, 0, 525, 1821, 37, 4540, 1964, 99, 4388, 1994, 140, 8479, 2021, 177, 752, 12563, 2036, 212, 1420, 32834, 0, 246, 1, 41, 351, 309, 0, 548, 1804, 36, 4541, 1973, 100, 4405, 1995, 142, 8515, 2013, 182, 773, 12532, 2034, 221, 1468, 32821, 0, 249, 1, 41, 359, 203, 0, 490, 1883, 34, 269, 1989, 97, 4420, 1988, 145, 8509, 2014, 186, 750, 12555, 2027, 223, 1281, 32812, 0, 246, 1, 41, 312, 156, 0, 412, 2000, 29, 347, 1947, 92, 4426, 1988, 149, 8521, 2007, 191, 753, 12501, 2033, 227, 1281, 32812, 0, 246, 1, 41, 410, 1979, 0, 4825, 2003, 31, 334, 1966, 89, 4470, 1977, 147, 8551, 2001, 194, 747, 12505, 2028, 232, 1276, 32797, 0, 249, 1, 50, 353, 13, 0, 9646, 2005, 35, 351, 1963, 88, 4491, 1975, 149, 8580, 1993, 199, 723, 12520, 2021, 237, 1188, 32780, 0, 252, 0, 118, 655, 1823, 0, 238, 1982, 103, 8583, 1996, 147, 12692, 2022, 180, 12544, 0, 208, 1120, 12537, 2035, 226, 1380, 32864, 0, 240, 0, 118, 660, 1808, 0, 4528, 1958, 101, 8673, 1976, 145, 12727, 2017, 184, 12555, 2037, 214, 1100, 12503, 2038, 230, 1380, 32845, 0, 243, 0, 118, 599, 1860, 0, 251, 1984, 98, 8654, 1992, 146, 8449, 2028, 186, 12574, 2024, 220, 1090, 12471, 2044, 235, 1378, 32819, 0, 247, 0, 118, 553, 1892, 0, 263, 1988, 94, 4365, 2011, 146, 8462, 2023, 191, 12548, 2024, 225, 1080, 12450, 2043, 238, 1384, 32807, 0, 249, 0, 118, 486, 1954, 0, 312, 1974, 92, 4404, 1999, 153, 8456, 2019, 202, 12491, 2032, 233, 1100, 12417, 2032, 244, 1358, 32798, 0, 250, 0, 118, 430, 1992, 0, 325, 1980, 87, 4455, 1989, 153, 8480, 2014, 209, 12488, 2011, 242, 1100, 5, 0, 250, 1358, 32780, 0, 253, 0, 118, 4874, 1986, 0, 331, 1992, 83, 4519, 1973, 154, 8496, 2000, 218, 12362, 2042, 247, 1100, 12334, 2042, 251, 1358, 32780, 0, 253, 0, 118, 297, 40, 0, 371, 1986, 77, 4576, 1951, 157, 8453, 2007, 225, 12354, 2039, 250, 1100, 12312, 2045, 253, 1358, 32774, 0, 254, 0, 118, 236, 98, 0, 415, 1970, 75, 4595, 1940, 162, 12735, 1975, 230, 12335, 2041, 251, 1100, 2, 0, 253, 1358, 32774, 0, 254, 1, 41, 631, 1833, 0, 493, 1833, 45, 4508, 1939, 103, 8625, 1982, 138, 12787, 2007, 167, 750, 12569, 2042, 199, 1196, 32882, 0, 227, 1, 41, 645, 1789, 0, 479, 1845, 45, 4512, 1945, 102, 8654, 1978, 138, 8460, 2026, 169, 750, 12581, 2038, 203, 1203, 32867, 0, 231, 1, 41, 535, 1977, 0, 489, 1848, 41, 4540, 1934, 100, 8670, 1983, 138, 8491, 2021, 171, 748, 12598, 2033, 208, 1407, 32838, 0, 245, 1, 41, 399, 196, 0, 525, 1821, 37, 4540, 1964, 99, 4388, 1994, 140, 8479, 2021, 177, 752, 12563, 2036, 212, 1420, 32834, 0, 246, 1, 41, 351, 309, 0, 548, 1804, 36, 4541, 1973, 100, 4405, 1995, 142, 8515, 2013, 182, 773, 12532, 2034, 221, 1468, 32821, 0, 249, 1, 41, 359, 203, 0, 490, 1883, 34, 269, 1989, 97, 4420, 1988, 145, 8509, 2014, 186, 750, 12555, 2027, 223, 1281, 32812, 0, 246, 1, 41, 312, 156, 0, 412, 2000, 29, 347, 1947, 92, 4426, 1988, 149, 8521, 2007, 191, 753, 12501, 2033, 227, 1281, 32812, 0, 246, 1, 41, 410, 1979, 0, 4825, 2003, 31, 334, 1966, 89, 4470, 1977, 147, 8551, 2001, 194, 747, 12505, 2028, 232, 1276, 32797, 0, 249, 1, 50, 353, 13, 0, 9646, 2005, 35, 351, 1963, 88, 4491, 1975, 149, 8580, 1993, 199, 723, 12520, 2021, 237, 1188, 32780, 0, 252, 0, 118, 655, 1823, 0, 238, 1982, 103, 8583, 1996, 147, 12692, 2022, 180, 12544, 2041, 208, 1120, 12509, 2041, 225, 1379, 32842, 0, 238, 0, 118, 660, 1808, 0, 4528, 1958, 101, 8673, 1976, 145, 12727, 2017, 184, 12555, 2030, 214, 1100, 12473, 2047, 229, 1372, 32828, 0, 241, 0, 118, 599, 1860, 0, 251, 1984, 98, 8654, 1992, 146, 8449, 2028, 186, 12574, 2025, 220, 1085, 12480, 2033, 235, 1377, 32807, 0, 246, 0, 118, 553, 1892, 0, 263, 1988, 94, 4365, 2011, 146, 8462, 2023, 191, 12548, 2017, 225, 1075, 12426, 2043, 237, 1338, 32808, 0, 245, 0, 118, 486, 1954, 0, 312, 1974, 92, 4404, 1999, 153, 8456, 2019, 202, 12491, 2032, 233, 1100, 12417, 2032, 244, 1362, 32789, 0, 250, 0, 118, 430, 1992, 0, 325, 1980, 87, 4455, 1989, 153, 8480, 2014, 209, 12488, 2011, 242, 1100, 5, 1, 250, 1338, 32776, 0, 253, 0, 118, 4874, 1986, 0, 331, 1992, 83, 4519, 1973, 154, 8496, 2000, 218, 12362, 2042, 247, 1100, 12334, 2043, 251, 1338, 32776, 0, 253, 0, 118, 297, 40, 0, 371, 1986, 77, 4576, 1951, 157, 8453, 2007, 225, 12354, 2039, 250, 1100, 12312, 2045, 253, 1338, 32772, 0, 254, 0, 118, 236, 98, 0, 415, 1970, 75, 4595, 1940, 162, 12735, 1975, 230, 12335, 2041, 251, 1100, 2, 0, 253, 1338, 32772, 0, 254, 1, 41, 631, 1833, 0, 493, 1833, 45, 4508, 1939, 103, 8625, 1982, 138, 12787, 2007, 167, 750, 12569, 2037, 199, 1196, 32862, 0, 225, 1, 41, 645, 1789, 0, 479, 1845, 45, 4512, 1945, 102, 8654, 1978, 138, 8460, 2026, 169, 750, 12581, 2035, 203, 1201, 32847, 0, 230, 1, 41, 535, 1977, 0, 489, 1848, 41, 4540, 1934, 100, 8670, 1983, 138, 8491, 2021, 171, 752, 12587, 2034, 208, 1408, 32819, 0, 244, 1, 41, 399, 196, 0, 525, 1821, 37, 4540, 1964, 99, 4388, 1994, 140, 8479, 2021, 177, 752, 12563, 2035, 212, 1414, 32815, 0, 245, 1, 41, 351, 309, 0, 548, 1804, 36, 4541, 1973, 100, 4405, 1995, 142, 8515, 2012, 182, 780, 12515, 2035, 221, 1477, 32812, 0, 247, 1, 41, 359, 203, 0, 490, 1883, 34, 269, 1989, 97, 4420, 1988, 145, 8509, 2014, 186, 750, 12555, 2025, 223, 1281, 32804, 0, 245, 1, 41, 312, 156, 0, 412, 2000, 29, 347, 1947, 92, 4426, 1988, 149, 8521, 2007, 191, 750, 12508, 2029, 227, 1281, 32804, 0, 245, 1, 41, 410, 1979, 0, 4825, 2003, 31, 334, 1966, 89, 4470, 1977, 147, 8551, 2001, 194, 765, 12481, 2034, 233, 1311, 32787, 0, 250, 1, 50, 353, 13, 0, 9646, 2005, 35, 351, 1963, 88, 4491, 1975, 149, 8580, 1993, 199, 720, 12530, 2016, 237, 1167, 32780, 0, 251, 0, 118, 655, 1823, 0, 238, 1982, 103, 8583, 1996, 147, 12692, 2022, 180, 12544, 2041, 208, 1120, 12509, 2041, 225, 1379, 32842, 0, 238, 0, 118, 660, 1808, 0, 4528, 1958, 101, 8673, 1976, 145, 12727, 2017, 184, 12555, 2030, 214, 1100, 12473, 2047, 229, 1372, 32828, 0, 241, 0, 118, 599, 1860, 0, 251, 1984, 98, 8654, 1992, 146, 8449, 2028, 186, 12574, 2025, 220, 1085, 12480, 2033, 235, 1377, 32807, 0, 246, 0, 118, 553, 1892, 0, 263, 1988, 94, 4365, 2011, 146, 8462, 2023, 191, 12548, 2017, 225, 1075, 12426, 2043, 237, 1338, 32808, 0, 245, 0, 118, 486, 1954, 0, 312, 1974, 92, 4404, 1999, 153, 8456, 2019, 202, 12491, 2032, 233, 1100, 12417, 2032, 244, 1362, 32789, 0, 250, 0, 118, 430, 1992, 0, 325, 1980, 87, 4455, 1989, 153, 8480, 2014, 209, 12488, 2011, 242, 1100, 5, 1, 250, 1338, 32776, 0, 253, 0, 118, 4874, 1986, 0, 331, 1992, 83, 4519, 1973, 154, 8496, 2000, 218, 12362, 2042, 247, 1100, 12334, 2043, 251, 1338, 32776, 0, 253, 0, 118, 297, 40, 0, 371, 1986, 77, 4576, 1951, 157, 8453, 2007, 225, 12354, 2039, 250, 1100, 12312, 2045, 253, 1338, 32772, 0, 254, 0, 118, 236, 98, 0, 415, 1970, 75, 4595, 1940, 162, 12735, 1975, 230, 12335, 2041, 251, 1100, 2, 0, 253, 1338, 32772, 0, 254, 1, 41, 631, 1833, 0, 493, 1833, 45, 4508, 1939, 103, 8625, 1982, 138, 12787, 2007, 167, 750, 12569, 2037, 199, 1196, 32862, 0, 225, 1, 41, 645, 1789, 0, 479, 1845, 45, 4512, 1945, 102, 8654, 1978, 138, 8460, 2026, 169, 750, 12581, 2035, 203, 1201, 32847, 0, 230, 1, 41, 535, 1977, 0, 489, 1848, 41, 4540, 1934, 100, 8670, 1983, 138, 8491, 2021, 171, 752, 12587, 2034, 208, 1408, 32819, 0, 244, 1, 41, 399, 196, 0, 525, 1821, 37, 4540, 1964, 99, 4388, 1994, 140, 8479, 2021, 177, 752, 12563, 2035, 212, 1414, 32815, 0, 245, 1, 41, 351, 309, 0, 548, 1804, 36, 4541, 1973, 100, 4405, 1995, 142, 8515, 2012, 182, 780, 12515, 2035, 221, 1477, 32812, 0, 247, 1, 41, 359, 203, 0, 490, 1883, 34, 269, 1989, 97, 4420, 1988, 145, 8509, 2014, 186, 750, 12555, 2025, 223, 1281, 32804, 0, 245, 1, 41, 312, 156, 0, 412, 2000, 29, 347, 1947, 92, 4426, 1988, 149, 8521, 2007, 191, 750, 12508, 2029, 227, 1281, 32804, 0, 245, 1, 41, 410, 1979, 0, 4825, 2003, 31, 334, 1966, 89, 4470, 1977, 147, 8551, 2001, 194, 765, 12481, 2034, 233, 1311, 32787, 0, 250, 1, 50, 353, 13, 0, 9646, 2005, 35, 351, 1963, 88, 4491, 1975, 149, 8580, 1993, 199, 720, 12530, 2016, 237, 1167, 32780, 0, 251, 0, 118, 655, 1823, 0, 238, 1982, 103, 8583, 1996, 147, 12692, 2022, 180, 12544, 2041, 208, 1120, 12509, 2041, 225, 1379, 32842, 0, 238, 0, 118, 660, 1808, 0, 4528, 1958, 101, 8673, 1976, 145, 12727, 2017, 184, 12555, 2030, 214, 1100, 12473, 2047, 229, 1372, 32828, 0, 241, 0, 118, 599, 1860, 0, 251, 1984, 98, 8654, 1992, 146, 8449, 2028, 186, 12574, 2025, 220, 1085, 12480, 2033, 235, 1377, 32807, 0, 246, 0, 118, 553, 1892, 0, 263, 1988, 94, 4365, 2011, 146, 8462, 2023, 191, 12548, 2017, 225, 1075, 12426, 2043, 237, 1338, 32808, 0, 245, 0, 118, 486, 1954, 0, 312, 1974, 92, 4404, 1999, 153, 8456, 2019, 202, 12491, 2032, 233, 1100, 12417, 2032, 244, 1362, 32789, 0, 250, 0, 118, 430, 1992, 0, 325, 1980, 87, 4455, 1989, 153, 8480, 2014, 209, 12488, 2011, 242, 1100, 5, 1, 250, 1338, 32776, 0, 253, 0, 118, 4874, 1986, 0, 331, 1992, 83, 4519, 1973, 154, 8496, 2000, 218, 12362, 2042, 247, 1100, 12334, 2043, 251, 1338, 32776, 0, 253, 0, 118, 297, 40, 0, 371, 1986, 77, 4576, 1951, 157, 8453, 2007, 225, 12354, 2039, 250, 1100, 12312, 2045, 253, 1338, 32772, 0, 254, 0, 118, 236, 98, 0, 415, 1970, 75, 4595, 1940, 162, 12735, 1975, 230, 12335, 2041, 251, 1100, 2, 0, 253, 1338, 32772, 0, 254, 1, 41, 631, 1833, 0, 493, 1833, 45, 4508, 1939, 103, 8625, 1982, 138, 12787, 2007, 167, 750, 12569, 2037, 199, 1196, 32862, 0, 225, 1, 41, 645, 1789, 0, 479, 1845, 45, 4512, 1945, 102, 8654, 1978, 138, 8460, 2026, 169, 750, 12581, 2035, 203, 1201, 32847, 0, 230, 1, 41, 535, 1977, 0, 489, 1848, 41, 4540, 1934, 100, 8670, 1983, 138, 8491, 2021, 171, 752, 12587, 2034, 208, 1408, 32819, 0, 244, 1, 41, 399, 196, 0, 525, 1821, 37, 4540, 1964, 99, 4388, 1994, 140, 8479, 2021, 177, 752, 12563, 2035, 212, 1414, 32815, 0, 245, 1, 41, 351, 309, 0, 548, 1804, 36, 4541, 1973, 100, 4405, 1995, 142, 8515, 2012, 182, 780, 12515, 2035, 221, 1477, 32812, 0, 247, 1, 41, 359, 203, 0, 490, 1883, 34, 269, 1989, 97, 4420, 1988, 145, 8509, 2014, 186, 750, 12555, 2025, 223, 1281, 32804, 0, 245, 1, 41, 312, 156, 0, 412, 2000, 29, 347, 1947, 92, 4426, 1988, 149, 8521, 2007, 191, 750, 12508, 2029, 227, 1281, 32804, 0, 245, 1, 41, 410, 1979, 0, 4825, 2003, 31, 334, 1966, 89, 4470, 1977, 147, 8551, 2001, 194, 765, 12481, 2034, 233, 1311, 32787, 0, 250, 1, 50, 353, 13, 0, 9646, 2005, 35, 351, 1963, 88, 4491, 1975, 149, 8580, 1993, 199, 720, 12530, 2016, 237, 1167, 32780, 0, 251, 0, 118, 655, 1823, 0, 238, 1982, 103, 8583, 1996, 147, 12692, 2022, 180, 12544, 2041, 208, 1120, 12509, 2041, 225, 1379, 32842, 0, 238, 0, 118, 660, 1808, 0, 4528, 1958, 101, 8673, 1976, 145, 12727, 2017, 184, 12555, 2030, 214, 1100, 12473, 2047, 229, 1372, 32828, 0, 241, 0, 118, 599, 1860, 0, 251, 1984, 98, 8654, 1992, 146, 8449, 2028, 186, 12574, 2025, 220, 1085, 12480, 2033, 235, 1377, 32807, 0, 246, 0, 118, 553, 1892, 0, 263, 1988, 94, 4365, 2011, 146, 8462, 2023, 191, 12548, 2017, 225, 1075, 12426, 2043, 237, 1338, 32808, 0, 245, 0, 118, 486, 1954, 0, 312, 1974, 92, 4404, 1999, 153, 8456, 2019, 202, 12491, 2032, 233, 1100, 12417, 2032, 244, 1362, 32789, 0, 250, 0, 118, 430, 1992, 0, 325, 1980, 87, 4455, 1989, 153, 8480, 2014, 209, 12488, 2011, 242, 1100, 5, 1, 250, 1338, 32776, 0, 253, 0, 118, 4874, 1986, 0, 331, 1992, 83, 4519, 1973, 154, 8496, 2000, 218, 12362, 2042, 247, 1100, 12334, 2043, 251, 1338, 32776, 0, 253, 0, 118, 297, 40, 0, 371, 1986, 77, 4576, 1951, 157, 8453, 2007, 225, 12354, 2039, 250, 1100, 12312, 2045, 253, 1338, 32772, 0, 254, 0, 118, 236, 98, 0, 415, 1970, 75, 4595, 1940, 162, 12735, 1975, 230, 12335, 2041, 251, 1100, 2, 0, 253, 1338, 32772, 0, 254, 1, 41, 631, 1833, 0, 493, 1833, 45, 4508, 1939, 103, 8625, 1982, 138, 12787, 2007, 167, 750, 12569, 2037, 199, 1196, 32862, 0, 225, 1, 41, 645, 1789, 0, 479, 1845, 45, 4512, 1945, 102, 8654, 1978, 138, 8460, 2026, 169, 750, 12581, 2035, 203, 1201, 32847, 0, 230, 1, 41, 535, 1977, 0, 489, 1848, 41, 4540, 1934, 100, 8670, 1983, 138, 8491, 2021, 171, 752, 12587, 2034, 208, 1408, 32819, 0, 244, 1, 41, 399, 196, 0, 525, 1821, 37, 4540, 1964, 99, 4388, 1994, 140, 8479, 2021, 177, 752, 12563, 2035, 212, 1414, 32815, 0, 245, 1, 41, 351, 309, 0, 548, 1804, 36, 4541, 1973, 100, 4405, 1995, 142, 8515, 2012, 182, 780, 12515, 2035, 221, 1477, 32812, 0, 247, 1, 41, 359, 203, 0, 490, 1883, 34, 269, 1989, 97, 4420, 1988, 145, 8509, 2014, 186, 750, 12555, 2025, 223, 1281, 32804, 0, 245, 1, 41, 312, 156, 0, 412, 2000, 29, 347, 1947, 92, 4426, 1988, 149, 8521, 2007, 191, 750, 12508, 2029, 227, 1281, 32804, 0, 245, 1, 41, 410, 1979, 0, 4825, 2003, 31, 334, 1966, 89, 4470, 1977, 147, 8551, 2001, 194, 765, 12481, 2034, 233, 1311, 32787, 0, 250, 1, 50, 353, 13, 0, 9646, 2005, 35, 351, 1963, 88, 4491, 1975, 149, 8580, 1993, 199, 720, 12530, 2016, 237, 1167, 32780, 0, 251, 0, 118, 655, 1823, 0, 238, 1982, 103, 8583, 1996, 147, 12692, 2022, 180, 12544, 2041, 208, 1120, 12509, 2041, 225, 1379, 32842, 0, 238, 0, 118, 660, 1808, 0, 4528, 1958, 101, 8673, 1976, 145, 12727, 2017, 184, 12555, 2030, 214, 1100, 12473, 2047, 229, 1372, 32828, 0, 241, 0, 118, 599, 1860, 0, 251, 1984, 98, 8654, 1992, 146, 8449, 2028, 186, 12574, 2025, 220, 1085, 12480, 2033, 235, 1377, 32807, 0, 246, 0, 118, 553, 1892, 0, 263, 1988, 94, 4365, 2011, 146, 8462, 2023, 191, 12548, 2017, 225, 1075, 12426, 2043, 237, 1338, 32808, 0, 245, 0, 118, 486, 1954, 0, 312, 1974, 92, 4404, 1999, 153, 8456, 2019, 202, 12491, 2032, 233, 1100, 12417, 2032, 244, 1362, 32789, 0, 250, 0, 118, 430, 1992, 0, 325, 1980, 87, 4455, 1989, 153, 8480, 2014, 209, 12488, 2011, 242, 1100, 5, 1, 250, 1338, 32776, 0, 253, 0, 118, 4874, 1986, 0, 331, 1992, 83, 4519, 1973, 154, 8496, 2000, 218, 12362, 2042, 247, 1100, 12334, 2043, 251, 1338, 32776, 0, 253, 0, 118, 297, 40, 0, 371, 1986, 77, 4576, 1951, 157, 8453, 2007, 225, 12354, 2039, 250, 1100, 12312, 2045, 253, 1338, 32772, 0, 254, 0, 118, 236, 98, 0, 415, 1970, 75, 4595, 1940, 162, 12735, 1975, 230, 12335, 2041, 251, 1100, 2, 0, 253, 1338, 32772, 0, 254, 1, 41, 631, 1833, 0, 493, 1833, 45, 4508, 1939, 103, 8625, 1982, 138, 12787, 2007, 167, 750, 12569, 2037, 199, 1196, 32862, 0, 225, 1, 41, 645, 1789, 0, 479, 1845, 45, 4512, 1945, 102, 8654, 1978, 138, 8460, 2026, 169, 750, 12581, 2035, 203, 1201, 32847, 0, 230, 1, 41, 535, 1977, 0, 489, 1848, 41, 4540, 1934, 100, 8670, 1983, 138, 8491, 2021, 171, 752, 12587, 2034, 208, 1408, 32819, 0, 244, 1, 41, 399, 196, 0, 525, 1821, 37, 4540, 1964, 99, 4388, 1994, 140, 8479, 2021, 177, 752, 12563, 2035, 212, 1414, 32815, 0, 245, 1, 41, 351, 309, 0, 548, 1804, 36, 4541, 1973, 100, 4405, 1995, 142, 8515, 2012, 182, 780, 12515, 2035, 221, 1477, 32812, 0, 247, 1, 41, 359, 203, 0, 490, 1883, 34, 269, 1989, 97, 4420, 1988, 145, 8509, 2014, 186, 750, 12555, 2025, 223, 1281, 32804, 0, 245, 1, 41, 312, 156, 0, 412, 2000, 29, 347, 1947, 92, 4426, 1988, 149, 8521, 2007, 191, 750, 12508, 2029, 227, 1281, 32804, 0, 245, 1, 41, 410, 1979, 0, 4825, 2003, 31, 334, 1966, 89, 4470, 1977, 147, 8551, 2001, 194, 765, 12481, 2034, 233, 1311, 32787, 0, 250, 1, 50, 353, 13, 0, 9646, 2005, 35, 351, 1963, 88, 4491, 1975, 149, 8580, 1993, 199, 720, 12530, 2016, 237, 1167, 32780, 0, 251, 0, 118, 655, 1823, 0, 238, 1982, 103, 8583, 1996, 147, 12692, 2022, 180, 12544, 2041, 208, 1120, 12509, 2041, 225, 1379, 32842, 0, 238, 0, 118, 660, 1808, 0, 4528, 1958, 101, 8673, 1976, 145, 12727, 2017, 184, 12555, 2030, 214, 1100, 12473, 2047, 229, 1372, 32828, 0, 241, 0, 118, 599, 1860, 0, 251, 1984, 98, 8654, 1992, 146, 8449, 2028, 186, 12574, 2025, 220, 1085, 12480, 2033, 235, 1377, 32807, 0, 246, 0, 118, 553, 1892, 0, 263, 1988, 94, 4365, 2011, 146, 8462, 2023, 191, 12548, 2017, 225, 1075, 12426, 2043, 237, 1338, 32808, 0, 245, 0, 118, 486, 1954, 0, 312, 1974, 92, 4404, 1999, 153, 8456, 2019, 202, 12491, 2032, 233, 1100, 12417, 2032, 244, 1362, 32789, 0, 250, 0, 118, 430, 1992, 0, 325, 1980, 87, 4455, 1989, 153, 8480, 2014, 209, 12488, 2011, 242, 1100, 5, 1, 250, 1338, 32776, 0, 253, 0, 118, 4874, 1986, 0, 331, 1992, 83, 4519, 1973, 154, 8496, 2000, 218, 12362, 2042, 247, 1100, 12334, 2043, 251, 1338, 32776, 0, 253, 0, 118, 297, 40, 0, 371, 1986, 77, 4576, 1951, 157, 8453, 2007, 225, 12354, 2039, 250, 1100, 12312, 2045, 253, 1338, 32772, 0, 254, 0, 118, 236, 98, 0, 415, 1970, 75, 4595, 1940, 162, 12735, 1975, 230, 12335, 2041, 251, 1100, 2, 0, 253, 1338, 32772, 0, 254, 1, 41, 631, 1833, 0, 493, 1833, 45, 4508, 1939, 103, 8625, 1982, 138, 12787, 2007, 167, 750, 12569, 2037, 199, 1196, 32862, 0, 225, 1, 41, 645, 1789, 0, 479, 1845, 45, 4512, 1945, 102, 8654, 1978, 138, 8460, 2026, 169, 750, 12581, 2035, 203, 1201, 32847, 0, 230, 1, 41, 535, 1977, 0, 489, 1848, 41, 4540, 1934, 100, 8670, 1983, 138, 8491, 2021, 171, 752, 12587, 2034, 208, 1408, 32819, 0, 244, 1, 41, 399, 196, 0, 525, 1821, 37, 4540, 1964, 99, 4388, 1994, 140, 8479, 2021, 177, 752, 12563, 2035, 212, 1414, 32815, 0, 245, 1, 41, 351, 309, 0, 548, 1804, 36, 4541, 1973, 100, 4405, 1995, 142, 8515, 2012, 182, 780, 12515, 2035, 221, 1477, 32812, 0, 247, 1, 41, 359, 203, 0, 490, 1883, 34, 269, 1989, 97, 4420, 1988, 145, 8509, 2014, 186, 750, 12555, 2025, 223, 1281, 32804, 0, 245, 1, 41, 312, 156, 0, 412, 2000, 29, 347, 1947, 92, 4426, 1988, 149, 8521, 2007, 191, 750, 12508, 2029, 227, 1281, 32804, 0, 245, 1, 41, 410, 1979, 0, 4825, 2003, 31, 334, 1966, 89, 4470, 1977, 147, 8551, 2001, 194, 765, 12481, 2034, 233, 1311, 32787, 0, 250, 1, 50, 353, 13, 0, 9646, 2005, 35, 351, 1963, 88, 4491, 1975, 149, 8580, 1993, 199, 720, 12530, 2016, 237, 1167, 32780, 0, 251, 0, 118, 655, 1823, 0, 238, 1982, 103, 8583, 1996, 147, 12692, 2022, 180, 12544, 2041, 208, 1120, 12509, 2041, 225, 1379, 32842, 0, 238, 0, 118, 660, 1808, 0, 4528, 1958, 101, 8673, 1976, 145, 12727, 2017, 184, 12555, 2030, 214, 1100, 12473, 2047, 229, 1372, 32828, 0, 241, 0, 118, 599, 1860, 0, 251, 1984, 98, 8654, 1992, 146, 8449, 2028, 186, 12574, 2025, 220, 1085, 12480, 2033, 235, 1377, 32807, 0, 246, 0, 118, 553, 1892, 0, 263, 1988, 94, 4365, 2011, 146, 8462, 2023, 191, 12548, 2017, 225, 1075, 12426, 2043, 237, 1338, 32808, 0, 245, 0, 118, 486, 1954, 0, 312, 1974, 92, 4404, 1999, 153, 8456, 2019, 202, 12491, 2032, 233, 1100, 12417, 2032, 244, 1362, 32789, 0, 250, 0, 118, 430, 1992, 0, 325, 1980, 87, 4455, 1989, 153, 8480, 2014, 209, 12488, 2011, 242, 1100, 5, 1, 250, 1338, 32776, 0, 253, 0, 118, 4874, 1986, 0, 331, 1992, 83, 4519, 1973, 154, 8496, 2000, 218, 12362, 2042, 247, 1100, 12334, 2043, 251, 1338, 32776, 0, 253, 0, 118, 297, 40, 0, 371, 1986, 77, 4576, 1951, 157, 8453, 2007, 225, 12354, 2039, 250, 1100, 12312, 2045, 253, 1338, 32772, 0, 254, 0, 118, 236, 98, 0, 415, 1970, 75, 4595, 1940, 162, 12735, 1975, 230, 12335, 2041, 251, 1100, 2, 0, 253, 1338, 32772, 0, 254, 1, 41, 631, 1833, 0, 493, 1833, 45, 4508, 1939, 103, 8625, 1982, 138, 12787, 2007, 167, 750, 12569, 2037, 199, 1196, 32862, 0, 225, 1, 41, 645, 1789, 0, 479, 1845, 45, 4512, 1945, 102, 8654, 1978, 138, 8460, 2026, 169, 750, 12581, 2035, 203, 1201, 32847, 0, 230, 1, 41, 535, 1977, 0, 489, 1848, 41, 4540, 1934, 100, 8670, 1983, 138, 8491, 2021, 171, 752, 12587, 2034, 208, 1408, 32819, 0, 244, 1, 41, 399, 196, 0, 525, 1821, 37, 4540, 1964, 99, 4388, 1994, 140, 8479, 2021, 177, 752, 12563, 2035, 212, 1414, 32815, 0, 245, 1, 41, 351, 309, 0, 548, 1804, 36, 4541, 1973, 100, 4405, 1995, 142, 8515, 2012, 182, 780, 12515, 2035, 221, 1477, 32812, 0, 247, 1, 41, 359, 203, 0, 490, 1883, 34, 269, 1989, 97, 4420, 1988, 145, 8509, 2014, 186, 750, 12555, 2025, 223, 1281, 32804, 0, 245, 1, 41, 312, 156, 0, 412, 2000, 29, 347, 1947, 92, 4426, 1988, 149, 8521, 2007, 191, 750, 12508, 2029, 227, 1281, 32804, 0, 245, 1, 41, 410, 1979, 0, 4825, 2003, 31, 334, 1966, 89, 4470, 1977, 147, 8551, 2001, 194, 765, 12481, 2034, 233, 1311, 32787, 0, 250, 1, 50, 353, 13, 0, 9646, 2005, 35, 351, 1963, 88, 4491, 1975, 149, 8580, 1993, 199, 720, 12530, 2016, 237, 1167, 32780, 0, 251, 0, 118, 655, 1823, 0, 238, 1982, 103, 8583, 1996, 147, 12692, 2022, 180, 12544, 2041, 208, 1120, 12509, 2041, 225, 1379, 32842, 0, 238, 0, 118, 660, 1808, 0, 4528, 1958, 101, 8673, 1976, 145, 12727, 2017, 184, 12555, 2030, 214, 1100, 12473, 2047, 229, 1372, 32828, 0, 241, 0, 118, 599, 1860, 0, 251, 1984, 98, 8654, 1992, 146, 8449, 2028, 186, 12574, 2025, 220, 1085, 12480, 2033, 235, 1377, 32807, 0, 246, 0, 118, 553, 1892, 0, 263, 1988, 94, 4365, 2011, 146, 8462, 2023, 191, 12548, 2017, 225, 1075, 12426, 2043, 237, 1338, 32808, 0, 245, 0, 118, 486, 1954, 0, 312, 1974, 92, 4404, 1999, 153, 8456, 2019, 202, 12491, 2032, 233, 1100, 12417, 2032, 244, 1362, 32789, 0, 250, 0, 118, 430, 1992, 0, 325, 1980, 87, 4455, 1989, 153, 8480, 2014, 209, 12488, 2011, 242, 1100, 5, 1, 250, 1338, 32776, 0, 253, 0, 118, 4874, 1986, 0, 331, 1992, 83, 4519, 1973, 154, 8496, 2000, 218, 12362, 2042, 247, 1100, 12334, 2043, 251, 1338, 32776, 0, 253, 0, 118, 297, 40, 0, 371, 1986, 77, 4576, 1951, 157, 8453, 2007, 225, 12354, 2039, 250, 1100, 12312, 2045, 253, 1338, 32772, 0, 254, 0, 118, 236, 98, 0, 415, 1970, 75, 4595, 1940, 162, 12735, 1975, 230, 12335, 2041, 251, 1100, 2, 0, 253, 1338, 32772, 0, 254, 1, 41, 631, 1833, 0, 493, 1833, 45, 4508, 1939, 103, 8625, 1982, 138, 12787, 2007, 167, 750, 12569, 2037, 199, 1196, 32862, 0, 225, 1, 41, 645, 1789, 0, 479, 1845, 45, 4512, 1945, 102, 8654, 1978, 138, 8460, 2026, 169, 750, 12581, 2035, 203, 1201, 32847, 0, 230, 1, 41, 535, 1977, 0, 489, 1848, 41, 4540, 1934, 100, 8670, 1983, 138, 8491, 2021, 171, 752, 12587, 2034, 208, 1408, 32819, 0, 244, 1, 41, 399, 196, 0, 525, 1821, 37, 4540, 1964, 99, 4388, 1994, 140, 8479, 2021, 177, 752, 12563, 2035, 212, 1414, 32815, 0, 245, 1, 41, 351, 309, 0, 548, 1804, 36, 4541, 1973, 100, 4405, 1995, 142, 8515, 2012, 182, 780, 12515, 2035, 221, 1477, 32812, 0, 247, 1, 41, 359, 203, 0, 490, 1883, 34, 269, 1989, 97, 4420, 1988, 145, 8509, 2014, 186, 750, 12555, 2025, 223, 1281, 32804, 0, 245, 1, 41, 312, 156, 0, 412, 2000, 29, 347, 1947, 92, 4426, 1988, 149, 8521, 2007, 191, 750, 12508, 2029, 227, 1281, 32804, 0, 245, 1, 41, 410, 1979, 0, 4825, 2003, 31, 334, 1966, 89, 4470, 1977, 147, 8551, 2001, 194, 765, 12481, 2034, 233, 1311, 32787, 0, 250, 1, 50, 353, 13, 0, 9646, 2005, 35, 351, 1963, 88, 4491, 1975, 149, 8580, 1993, 199, 720, 12530, 2016, 237, 1167, 32780, 0, 251, 0, 2056, 8, 8, 8, 8, 8, 8, 8, 8, 8, 80, 25703, 8500, 80, 25703, 8500, 84, 25703, 8500, 80, 25703, 8500, 80, 25703, 8500, 84, 25703, 8500, 80, 25703, 8500, 80, 25703, 8500, 84, 25703, 8500, 80, 25703, 8500, 80, 25703, 8500, 84, 25703, 8500, 80, 25703, 8500, 80, 25703, 8500, 84, 25703, 8500, 80, 25703, 8500, 80, 25703, 8500, 84, 25703, 8500, 192, 19335, 511, 192, 19335, 511, 192, 19335, 511, 192, 19335, 511, 192, 19335, 511, 192, 19335, 511, 192, 19335, 511, 192, 19335, 511, 192, 19335, 511, 192, 19335, 511, 192, 19335, 511, 192, 19335, 511, 80, 25703, 8500, 80, 25703, 8500, 84, 25703, 8500, 80, 25703, 8500, 80, 25703, 8500, 84, 25703, 8500, 512, 512, 512, 1, 512, 512, 512, 0, 78, 151, 28, 78, 151, 28, 98, 158, 1, 138, 118, 1, 254, 1, 1, 1, 254, 1, 3082, 3855, 17, 32, 4881, 5654, 17, 24, 11300, 15669, 564, 18, 1540, 1542, 1, 32, 1540, 1542, 1, 32, 1540, 1542, 1, 32, 0, 17151, 0, 17151, 1, 2816, 512, 1, 2816, 32, 21862, 1366, 2672, 2672, 63, 9, 29, 6, 26, 133, 63, 9, 29, 6, 26, 133, 63, 10, 22, 12, 26, 153, 63, 9, 29, 6, 26, 133, 63, 9, 29, 6, 26, 133, 63, 9, 29, 6, 26, 133, 63, 13, 32, 5, 15, 67, 63, 13, 32, 5, 15, 67, 63, 10, 22, 7, 15, 85, 63, 13, 32, 5, 15, 67, 63, 13, 32, 5, 15, 67, 63, 13, 32, 5, 15, 67, 63, 10, 41, 5, 21, 68, 63, 10, 41, 5, 21, 68, 63, 15, 28, 12, 22, 102, 63, 10, 41, 5, 21, 68, 63, 10, 41, 5, 21, 68, 63, 10, 41, 5, 21, 68, 63, 21, 36, 16, 28, 102, 63, 21, 36, 8, 14, 51, 63, 0, 0, 0, 0, 0, 63, 21, 36, 8, 14, 51, 63, 21, 36, 8, 14, 51, 63, 21, 36, 8, 14, 51, 63, 13, 32, 10, 27, 118, 63, 13, 32, 10, 27, 118, 63, 11, 23, 13, 27, 153, 63, 13, 32, 10, 27, 118, 63, 13, 32, 10, 27, 118, 63, 13, 32, 10, 27, 118, 40, 512, 202, 132, 0, 1311, 0, 40, 384, 144, 88, 0, 1048, 0, 40, 512, 202, 62, 1792, 1311, 2, 40, 256, 106, 64, 0, 783, 0, 8, 38, 48, 58, 68, 88, 118, 158, 0, 0, 0, 8, 48, 58, 68, 88, 118, 158, 208, 0, 0, 0, 560, 400, 3080, 1099, 0, 0, 0, 0, 560, 400, 3080, 1099, 0, 0, 0, 0, 560, 400, 3080, 1099, 0, 0, 0, 0, 560, 400, 3080, 1099, 0, 0, 0, 0, 560, 400, 3080, 1099, 0, 0, 0, 0, 560, 400, 3080, 1099, 0, 0, 0, 0, 560, 400, 3080, 1099, 0, 0, 0, 0, 560, 400, 3080, 1099, 0, 0, 0, 0, 560, 400, 3080, 1099, 0, 0, 0, 0, 560, 400, 3080, 1099, 0, 0, 0, 0, 560, 400, 3080, 1099, 0, 0, 0, 0, 560, 400, 3080, 1099, 0, 0, 0, 0, 560, 400, 3080, 1099, 0, 0, 0, 0, 560, 400, 3080, 1099, 0, 0, 0, 0, 560, 400, 3080, 1099, 0, 0, 0, 0, 560, 400, 3080, 1099, 0, 0, 0, 0, 560, 400, 3080, 1099, 0, 0, 0, 0, 560, 400, 3080, 1099, 0, 0, 0, 0, 560, 300, 3847, 843, 0, 0, 0, 0, 560, 300, 3847, 843, 0, 0, 0, 0, 560, 300, 3847, 843, 0, 0, 0, 0, 560, 300, 3847, 843, 0, 0, 0, 0, 560, 300, 3847, 843, 0, 0, 0, 0, 560, 300, 3847, 843, 0, 0, 0, 0, 560, 300, 3847, 843, 0, 0, 0, 0, 560, 300, 3847, 843, 0, 0, 0, 0, 560, 300, 3847, 843, 0, 0, 0, 0, 560, 300, 3847, 843, 0, 0, 0, 0, 560, 300, 3847, 843, 0, 0, 0, 0, 560, 300, 3847, 843, 0, 0, 0, 0, 560, 400, 3080, 1099, 0, 0, 0, 0, 560, 400, 3080, 1099, 0, 0, 0, 0, 560, 400, 3080, 1099, 0, 0, 0, 0, 560, 400, 3080, 1099, 0, 0, 0, 0, 560, 400, 3080, 1099, 0, 0, 0, 0, 560, 400, 3080, 1099, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, 65535, 65535, 0, 0, 0, 2, 65535, 65535, 0, 0, 0, 4, 65535, 65535, 0, 0, 0, 0, 65535, 65535, 0, 0, 0, 0, 65535, 65535, 0, 32767, 32767, 3, 0, 0, 0, 2, 6, 10, 14, 18, 22, 26, 30, 34, 38, 42, 46, 50, 54, 58, 62, 66, 70, 74, 78, 82, 86, 90, 94, 98, 102, 106, 110, 114, 118, 122, 126, 130, 134, 138, 142, 146, 150, 154, 158, 162, 166, 170, 174, 178, 182, 186, 190, 194, 198, 202, 206, 210, 214, 218, 222, 226, 230, 234, 238, 242, 246, 250, 254, 258, 262, 266, 270, 274, 278, 282, 286, 290, 294, 298, 304, 308, 312, 316, 320, 324, 328, 332, 336, 340, 346, 350, 354, 360, 364, 368, 374, 378, 384, 388, 394, 400, 404, 410, 416, 420, 426, 432, 438, 444, 452, 458, 464, 472, 478, 486, 494, 500, 510, 518, 526, 534, 542, 552, 560, 568, 578, 586, 596, 604, 614, 624, 632, 642, 652, 662, 672, 682, 692, 704, 714, 726, 736, 748, 758, 770, 782, 794, 806, 820, 834, 846, 860, 874, 888, 904, 920, 936, 952, 970, 988, 1004, 1022, 1040, 1058, 1076, 1094, 1112, 1130, 1148, 1166, 1184, 1202, 1222, 1240, 1258, 1276, 1296, 1314, 1334, 1352, 1372, 1390, 1410, 1428, 1448, 1468, 1488, 1506, 1526, 1546, 1566, 1586, 1606, 1626, 1646, 1668, 1688, 1708, 1730, 1752, 1772, 1794, 1816, 1840, 1862, 1886, 1908, 1932, 1956, 1982, 2006, 2032, 2058, 2084, 2110, 2138, 2166, 2194, 2222, 2250, 2280, 2310, 2340, 2370, 2400, 2430, 2462, 2492, 2524, 2554, 2586, 2618, 2650, 2682, 2714, 2746, 2780, 2812, 2846, 2880, 2914, 2948, 2982, 3018, 3052, 3088, 3124, 3160, 3196, 3232, 3270, 3306, 3344, 3382, 3420, 3460, 3498, 3538, 3578 }, (entry as ShortArrayIFDEntry).Values);
			}
			// Canon.0x4005 (0x4005/Undefined/49288) "(Value ommitted)"
			{
				// TODO: Unknown IFD tag: Canon / 0x4005
				var entry = makernote_structure.GetEntry (0, (ushort) 0x4005);
				Assert.IsNotNull (entry, "Entry 0x4005 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var parsed_hash = Utils.Md5Encode (parsed_bytes);
				Assert.AreEqual ("dd77ff6e107d6f6279f580217d70e82f", parsed_hash);
				Assert.AreEqual (49288, parsed_bytes.Length);
			}
			// Canon.0x4008 (0x4008/Short/3) "129 129 129"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.BlackLevel);
				Assert.IsNotNull (entry, "Entry 0x4008 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 129, 129, 129 }, (entry as ShortArrayIFDEntry).Values);
			}
			// Canon.0x4009 (0x4009/Short/3) "0 0 0"
			{
				// TODO: Unknown IFD tag: Canon / 0x4009
				var entry = makernote_structure.GetEntry (0, (ushort) 0x4009);
				Assert.IsNotNull (entry, "Entry 0x4009 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 0, 0, 0 }, (entry as ShortArrayIFDEntry).Values);
			}
			// Canon.0x4010 (0x4010/Ascii/32) ""
			{
				// TODO: Unknown IFD tag: Canon / 0x4010
				var entry = makernote_structure.GetEntry (0, (ushort) 0x4010);
				Assert.IsNotNull (entry, "Entry 0x4010 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("", (entry as StringIFDEntry).Value.Trim ());
			}
			// Canon.0x4011 (0x4011/Undefined/252) "0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0"
			{
				// TODO: Unknown IFD tag: Canon / 0x4011
				var entry = makernote_structure.GetEntry (0, (ushort) 0x4011);
				Assert.IsNotNull (entry, "Entry 0x4011 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Photo.0x9286 (UserComment/UserComment/264) ""
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.UserComment);
				Assert.IsNotNull (entry, "Entry 0x9286 missing in IFD 0");
				Assert.IsNotNull (entry as UserCommentIFDEntry, "Entry is not a user comment!");
				Assert.AreEqual ("", (entry as UserCommentIFDEntry).Value.Trim ());
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
			// Photo.0xA002 (PixelXDimension/Short/1) "3888"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.PixelXDimension);
				Assert.IsNotNull (entry, "Entry 0xA002 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (3888, (entry as ShortIFDEntry).Value);
			}
			// Photo.0xA003 (PixelYDimension/Short/1) "2592"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.PixelYDimension);
				Assert.IsNotNull (entry, "Entry 0xA003 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (2592, (entry as ShortIFDEntry).Value);
			}
			// Photo.0xA005 (InteroperabilityTag/SubIFD/1) "77610"
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
			// Photo.0xA20E (FocalPlaneXResolution/Rational/1) "3888000/877"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.FocalPlaneXResolution);
				Assert.IsNotNull (entry, "Entry 0xA20E missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (3888000, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (877, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Photo.0xA20F (FocalPlaneYResolution/Rational/1) "2592000/582"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.FocalPlaneYResolution);
				Assert.IsNotNull (entry, "Entry 0xA20F missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (2592000, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (582, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Photo.0xA210 (FocalPlaneResolutionUnit/Short/1) "2"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.FocalPlaneResolutionUnit);
				Assert.IsNotNull (entry, "Entry 0xA210 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (2, (entry as ShortIFDEntry).Value);
			}
			// Photo.0xA401 (CustomRendered/Short/1) "0"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.CustomRendered);
				Assert.IsNotNull (entry, "Entry 0xA401 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Photo.0xA402 (ExposureMode/Short/1) "1"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ExposureMode);
				Assert.IsNotNull (entry, "Entry 0xA402 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1, (entry as ShortIFDEntry).Value);
			}
			// Photo.0xA403 (WhiteBalance/Short/1) "0"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.WhiteBalance);
				Assert.IsNotNull (entry, "Entry 0xA403 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Photo.0xA406 (SceneCaptureType/Short/1) "0"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.SceneCaptureType);
				Assert.IsNotNull (entry, "Entry 0xA406 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Thumbnail.0x0201 (JPEGInterchangeFormat/ThumbnailDataIFD/1) "78336"
			{
				var entry = structure.GetEntry (1, (ushort) IFDEntryTag.JPEGInterchangeFormat);
				Assert.IsNotNull (entry, "Entry 0x0201 missing in IFD 1");
				Assert.IsNotNull (entry as ThumbnailDataIFDEntry, "Entry is not a thumbnail IFD!");
			}
			// Thumbnail.0x0202 (JPEGInterchangeFormatLength/Long/1) "4373"
			{
				var entry = structure.GetEntry (1, (ushort) IFDEntryTag.JPEGInterchangeFormatLength);
				Assert.IsNotNull (entry, "Entry 0x0202 missing in IFD 1");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (4373, (entry as LongIFDEntry).Value);
			}
			// Image2.0x0100 (ImageWidth/Short/1) "384"
			{
				var entry = structure.GetEntry (2, (ushort) IFDEntryTag.ImageWidth);
				Assert.IsNotNull (entry, "Entry 0x0100 missing in IFD 2");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (384, (entry as ShortIFDEntry).Value);
			}
			// Image2.0x0101 (ImageLength/Short/1) "256"
			{
				var entry = structure.GetEntry (2, (ushort) IFDEntryTag.ImageLength);
				Assert.IsNotNull (entry, "Entry 0x0101 missing in IFD 2");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (256, (entry as ShortIFDEntry).Value);
			}
			// Image2.0x0102 (BitsPerSample/Short/3) "8 8 8"
			{
				var entry = structure.GetEntry (2, (ushort) IFDEntryTag.BitsPerSample);
				Assert.IsNotNull (entry, "Entry 0x0102 missing in IFD 2");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 8, 8, 8 }, (entry as ShortArrayIFDEntry).Values);
			}
			// Image2.0x0103 (Compression/Short/1) "6"
			{
				var entry = structure.GetEntry (2, (ushort) IFDEntryTag.Compression);
				Assert.IsNotNull (entry, "Entry 0x0103 missing in IFD 2");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (6, (entry as ShortIFDEntry).Value);
			}
			// Image2.0x0106 (PhotometricInterpretation/Short/1) "2"
			{
				var entry = structure.GetEntry (2, (ushort) IFDEntryTag.PhotometricInterpretation);
				Assert.IsNotNull (entry, "Entry 0x0106 missing in IFD 2");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (2, (entry as ShortIFDEntry).Value);
			}
			// Image2.0x0111 (StripOffsets/StripOffsets/1) "2456371"
			{
				var entry = structure.GetEntry (2, (ushort) IFDEntryTag.StripOffsets);
				Assert.IsNotNull (entry, "Entry 0x0111 missing in IFD 2");
				Assert.IsNotNull (entry as StripOffsetsIFDEntry, "Entry is not a strip offsets entry!");
				Assert.AreEqual (1, (entry as StripOffsetsIFDEntry).Values.Length);
			}
			// Image2.0x0115 (SamplesPerPixel/Short/1) "3"
			{
				var entry = structure.GetEntry (2, (ushort) IFDEntryTag.SamplesPerPixel);
				Assert.IsNotNull (entry, "Entry 0x0115 missing in IFD 2");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (3, (entry as ShortIFDEntry).Value);
			}
			// Image2.0x0116 (RowsPerStrip/Short/1) "256"
			{
				var entry = structure.GetEntry (2, (ushort) IFDEntryTag.RowsPerStrip);
				Assert.IsNotNull (entry, "Entry 0x0116 missing in IFD 2");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (256, (entry as ShortIFDEntry).Value);
			}
			// Image2.0x0117 (StripByteCounts/Long/1) "294912"
			{
				var entry = structure.GetEntry (2, (ushort) IFDEntryTag.StripByteCounts);
				Assert.IsNotNull (entry, "Entry 0x0117 missing in IFD 2");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (294912, (entry as LongIFDEntry).Value);
			}
			// Image2.0x011C (PlanarConfiguration/Short/1) "1"
			{
				var entry = structure.GetEntry (2, (ushort) IFDEntryTag.PlanarConfiguration);
				Assert.IsNotNull (entry, "Entry 0x011C missing in IFD 2");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1, (entry as ShortIFDEntry).Value);
			}
			// Image2.0xC5D9 (0xc5d9/Long/1) "2"
			{
				// TODO: Unknown IFD tag: Image2 / 0xC5D9
				var entry = structure.GetEntry (2, (ushort) 0xC5D9);
				Assert.IsNotNull (entry, "Entry 0xC5D9 missing in IFD 2");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (2, (entry as LongIFDEntry).Value);
			}
			// Image3.0x0103 (Compression/Short/1) "6"
			{
				var entry = structure.GetEntry (3, (ushort) IFDEntryTag.Compression);
				Assert.IsNotNull (entry, "Entry 0x0103 missing in IFD 3");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (6, (entry as ShortIFDEntry).Value);
			}
			// Image3.0x0111 (StripOffsets/StripOffsets/1) "2751283"
			{
				var entry = structure.GetEntry (3, (ushort) IFDEntryTag.StripOffsets);
				Assert.IsNotNull (entry, "Entry 0x0111 missing in IFD 3");
				Assert.IsNotNull (entry as StripOffsetsIFDEntry, "Entry is not a strip offsets entry!");
				Assert.AreEqual (1, (entry as StripOffsetsIFDEntry).Values.Length);
			}
			// Image3.0x0117 (StripByteCounts/Long/1) "7887144"
			{
				var entry = structure.GetEntry (3, (ushort) IFDEntryTag.StripByteCounts);
				Assert.IsNotNull (entry, "Entry 0x0117 missing in IFD 3");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (7887144, (entry as LongIFDEntry).Value);
			}
			// Image3.0xC5D8 (0xc5d8/Long/1) "1"
			{
				// TODO: Unknown IFD tag: Image3 / 0xC5D8
				var entry = structure.GetEntry (3, (ushort) 0xC5D8);
				Assert.IsNotNull (entry, "Entry 0xC5D8 missing in IFD 3");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (1, (entry as LongIFDEntry).Value);
			}
			// Image3.0xC5E0 (0xc5e0/Long/1) "1"
			{
				// TODO: Unknown IFD tag: Image3 / 0xC5E0
				var entry = structure.GetEntry (3, (ushort) 0xC5E0);
				Assert.IsNotNull (entry, "Entry 0xC5E0 missing in IFD 3");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (1, (entry as LongIFDEntry).Value);
			}
			// Image3.0xC640 (0xc640/Short/3) "1 1974 1974"
			{
				// TODO: Unknown IFD tag: Image3 / 0xC640
				var entry = structure.GetEntry (3, (ushort) 0xC640);
				Assert.IsNotNull (entry, "Entry 0xC640 missing in IFD 3");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 1, 1974, 1974 }, (entry as ShortArrayIFDEntry).Values);
			}

			//  ---------- End of IFD tests ----------

		}
	}
}
