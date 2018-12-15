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
	public class BGO633972Test
	{
		[Test]
		public void Test ()
		{
			ImageTest.Run ("raw-samples/CR2", "sample_canon_350d_broken.cr2",
				false,
				new BGO633972TestInvariantValidator ()
			);
		}
	}

	public class BGO633972TestInvariantValidator : IMetadataInvariantValidator
	{
		public void ValidateMetadataInvariants (Image.File file)
		{
			Assert.IsNotNull (file);
			Assert.IsTrue (file.PossiblyCorrupt);

			//  ---------- Start of IFD tests ----------

			var tag = file.GetTag (TagTypes.TiffIFD) as IFDTag;
			Assert.IsNotNull (tag, "IFD tag not found");

			var structure = tag.Structure;

			// Image.0x0100 (ImageWidth/Short/1) "3456"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.ImageWidth);
				Assert.IsNotNull (entry, "Entry 0x0100 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (3456, (entry as ShortIFDEntry).Value);
			}
			// Image.0x0101 (ImageLength/Short/1) "2304"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.ImageLength);
				Assert.IsNotNull (entry, "Entry 0x0101 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (2304, (entry as ShortIFDEntry).Value);
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
			// Image.0x0110 (Model/Ascii/27) "Canon EOS DIGITAL REBEL XT"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.Model);
				Assert.IsNotNull (entry, "Entry 0x0110 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("Canon EOS DIGITAL REBEL XT", (entry as StringIFDEntry).Value);
			}
			// Image.0x0111 (StripOffsets/StripOffsets/1) "9728"
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
			// Image.0x0117 (StripByteCounts/Long/1) "2887781"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.StripByteCounts);
				Assert.IsNotNull (entry, "Entry 0x0117 missing in IFD 0");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (2887781, (entry as LongIFDEntry).Value);
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
			// Image.0x0132 (DateTime/Ascii/20) "2010:10:13 02:16:29"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.DateTime);
				Assert.IsNotNull (entry, "Entry 0x0132 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("2010:10:13 02:16:29", (entry as StringIFDEntry).Value);
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

			// Photo.0x829A (ExposureTime/Rational/1) "1/15"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ExposureTime);
				Assert.IsNotNull (entry, "Entry 0x829A missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (1, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (15, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Photo.0x829D (FNumber/Rational/1) "50/10"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.FNumber);
				Assert.IsNotNull (entry, "Entry 0x829D missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (50, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (10, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Photo.0x8822 (ExposureProgram/Short/1) "2"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ExposureProgram);
				Assert.IsNotNull (entry, "Entry 0x8822 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (2, (entry as ShortIFDEntry).Value);
			}
			// Photo.0x8827 (ISOSpeedRatings/Short/1) "400"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ISOSpeedRatings);
				Assert.IsNotNull (entry, "Entry 0x8827 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (400, (entry as ShortIFDEntry).Value);
			}
			// Photo.0x9000 (ExifVersion/Undefined/4) "48 50 50 49 "
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ExifVersion);
				Assert.IsNotNull (entry, "Entry 0x9000 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 48, 50, 50, 49 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Photo.0x9003 (DateTimeOriginal/Ascii/20) "2010:10:13 02:16:29"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.DateTimeOriginal);
				Assert.IsNotNull (entry, "Entry 0x9003 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("2010:10:13 02:16:29", (entry as StringIFDEntry).Value);
			}
			// Photo.0x9004 (DateTimeDigitized/Ascii/20) "2010:10:13 02:16:29"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.DateTimeDigitized);
				Assert.IsNotNull (entry, "Entry 0x9004 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("2010:10:13 02:16:29", (entry as StringIFDEntry).Value);
			}
			// Photo.0x9101 (ComponentsConfiguration/Undefined/4) "0 0 0 0 "
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ComponentsConfiguration);
				Assert.IsNotNull (entry, "Entry 0x9101 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 0, 0, 0, 0 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Photo.0x9201 (ShutterSpeedValue/SRational/1) "256042/65536"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ShutterSpeedValue);
				Assert.IsNotNull (entry, "Entry 0x9201 missing in IFD 0");
				Assert.IsNotNull (entry as SRationalIFDEntry, "Entry is not a srational!");
				Assert.AreEqual (256042, (entry as SRationalIFDEntry).Value.Numerator);
				Assert.AreEqual (65536, (entry as SRationalIFDEntry).Value.Denominator);
			}
			// Photo.0x9202 (ApertureValue/Rational/1) "304340/65536"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ApertureValue);
				Assert.IsNotNull (entry, "Entry 0x9202 missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (304340, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (65536, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Photo.0x9204 (ExposureBiasValue/SRational/1) "0/2"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ExposureBiasValue);
				Assert.IsNotNull (entry, "Entry 0x9204 missing in IFD 0");
				Assert.IsNotNull (entry as SRationalIFDEntry, "Entry is not a srational!");
				Assert.AreEqual (0, (entry as SRationalIFDEntry).Value.Numerator);
				Assert.AreEqual (2, (entry as SRationalIFDEntry).Value.Denominator);
			}
			// Photo.0x9207 (MeteringMode/Short/1) "5"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.MeteringMode);
				Assert.IsNotNull (entry, "Entry 0x9207 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (5, (entry as ShortIFDEntry).Value);
			}
			// Photo.0x9209 (Flash/Short/1) "16"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.Flash);
				Assert.IsNotNull (entry, "Entry 0x9209 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (16, (entry as ShortIFDEntry).Value);
			}
			// Photo.0x920A (FocalLength/Rational/1) "38/1"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.FocalLength);
				Assert.IsNotNull (entry, "Entry 0x920A missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (38, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (1, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Photo.0x927C (MakerNote/MakerNote/8340) "(Value ommitted)"
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
			// CanonCs.0x0004 (FlashMode/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (5 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [4]);
			}
			// CanonCs.0x0005 (DriveMode/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (6 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [5]);
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
			// CanonCs.0x000D (Contrast/Short/1) "1"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (14 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (1, (entry as ShortArrayIFDEntry).Values [13]);
			}
			// CanonCs.0x000E (Saturation/Short/1) "1"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (15 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (1, (entry as ShortArrayIFDEntry).Values [14]);
			}
			// CanonCs.0x000F (Sharpness/Short/1) "1"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (16 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (1, (entry as ShortArrayIFDEntry).Values [15]);
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
			// CanonCs.0x0014 (ExposureProgram/Short/1) "1"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (21 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (1, (entry as ShortArrayIFDEntry).Values [20]);
			}
			// CanonCs.0x0015 (0x0015/Short/1) "65535"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (22 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (65535, (entry as ShortArrayIFDEntry).Values [21]);
			}
			// CanonCs.0x0016 (LensType/Short/1) "65535"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (23 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (65535, (entry as ShortArrayIFDEntry).Values [22]);
			}
			// CanonCs.0x0017 (Lens/Short/3) "85 17 1"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (26 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (85, (entry as ShortArrayIFDEntry).Values [23]);
				Assert.AreEqual (17, (entry as ShortArrayIFDEntry).Values [24]);
				Assert.AreEqual (1, (entry as ShortArrayIFDEntry).Values [25]);
			}
			// CanonCs.0x001A (MaxAperture/Short/1) "139"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (27 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (139, (entry as ShortArrayIFDEntry).Values [26]);
			}
			// CanonCs.0x001B (MinAperture/Short/1) "304"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (28 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (304, (entry as ShortArrayIFDEntry).Values [27]);
			}
			// CanonCs.0x001C (FlashActivity/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (29 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [28]);
			}
			// CanonCs.0x001D (FlashDetails/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (30 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [29]);
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
			// Canon.0x0002 (FocalLength/Short/4) "2 38 907 605"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.FocalLength);
				Assert.IsNotNull (entry, "Entry 0x0002 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 2, 38, 907, 605 }, (entry as ShortArrayIFDEntry).Values);
			}
			// Canon.0x0003 (0x0003/Short/4) "100 0 0 0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.Unknown3);
				Assert.IsNotNull (entry, "Entry 0x0003 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 100, 0, 0, 0 }, (entry as ShortArrayIFDEntry).Values);
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
			// CanonSi.0x0002 (ISOSpeed/Short/1) "224"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ShotInfo);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (3 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (224, (entry as ShortArrayIFDEntry).Values [2]);
			}
			// CanonSi.0x0003 (0x0003/Short/1) "60"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ShotInfo);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (4 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (60, (entry as ShortArrayIFDEntry).Values [3]);
			}
			// CanonSi.0x0004 (TargetAperture/Short/1) "149"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ShotInfo);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (5 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (149, (entry as ShortArrayIFDEntry).Values [4]);
			}
			// CanonSi.0x0005 (TargetShutterSpeed/Short/1) "125"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ShotInfo);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (6 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (125, (entry as ShortArrayIFDEntry).Values [5]);
			}
			// CanonSi.0x0006 (0x0006/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ShotInfo);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (7 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [6]);
			}
			// CanonSi.0x0007 (WhiteBalance/Short/1) "3"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ShotInfo);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (8 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (3, (entry as ShortArrayIFDEntry).Values [7]);
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
			// CanonSi.0x000C (0x000c/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ShotInfo);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (13 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [12]);
			}
			// CanonSi.0x000D (0x000d/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ShotInfo);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (14 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [13]);
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
			// CanonSi.0x0013 (SubjectDistance/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ShotInfo);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (20 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [19]);
			}
			// CanonSi.0x0014 (0x0014/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ShotInfo);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (21 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [20]);
			}
			// CanonSi.0x0015 (ApertureValue/Short/1) "152"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ShotInfo);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (22 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (152, (entry as ShortArrayIFDEntry).Values [21]);
			}
			// CanonSi.0x0016 (ShutterSpeedValue/Short/1) "292"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ShotInfo);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (23 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (292, (entry as ShortArrayIFDEntry).Values [22]);
			}
			// CanonSi.0x0017 (0x0017/Short/1) "103"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ShotInfo);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (24 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (103, (entry as ShortArrayIFDEntry).Values [23]);
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
			// Canon.0x0006 (ImageType/Ascii/32) "Canon EOS DIGITAL REBEL XT"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ImageType);
				Assert.IsNotNull (entry, "Entry 0x0006 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("Canon EOS DIGITAL REBEL XT", (entry as StringIFDEntry).Value);
			}
			// Canon.0x0007 (FirmwareVersion/Ascii/32) "Firmware 1.0.3"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.FirmwareVersion);
				Assert.IsNotNull (entry, "Entry 0x0007 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("Firmware 1.0.3", (entry as StringIFDEntry).Value);
			}
			// Canon.0x0009 (OwnerName/Ascii/32) "unknown"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.OwnerName);
				Assert.IsNotNull (entry, "Entry 0x0009 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("unknown", (entry as StringIFDEntry).Value);
			}
			// Canon.0x000C (SerialNumber/Long/1) "2220701995"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.SerialNumber);
				Assert.IsNotNull (entry, "Entry 0x000C missing in IFD 0");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (2220701995, (entry as LongIFDEntry).Value);
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
			// CanonCf.0x0000 (0x0000/Short/1) "20"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CustomFunctions);
				Assert.IsNotNull (entry, "Entry 0x000F missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (1 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (20, (entry as ShortArrayIFDEntry).Values [0]);
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
			// Canon.0x0010 (ModelID/Long/1) "2147484041"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ModelID);
				Assert.IsNotNull (entry, "Entry 0x0010 missing in IFD 0");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (2147484041, (entry as LongIFDEntry).Value);
			}
			// CanonPi.0x0000 (0x0000/Short/1) "7"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.PictureInfo);
				Assert.IsNotNull (entry, "Entry 0x0012 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (1 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (7, (entry as ShortArrayIFDEntry).Values [0]);
			}
			// CanonPi.0x0001 (0x0001/Short/1) "7"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.PictureInfo);
				Assert.IsNotNull (entry, "Entry 0x0012 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (2 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (7, (entry as ShortArrayIFDEntry).Values [1]);
			}
			// CanonPi.0x0002 (ImageWidth/Short/1) "3456"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.PictureInfo);
				Assert.IsNotNull (entry, "Entry 0x0012 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (3 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (3456, (entry as ShortArrayIFDEntry).Values [2]);
			}
			// CanonPi.0x0003 (ImageHeight/Short/1) "2304"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.PictureInfo);
				Assert.IsNotNull (entry, "Entry 0x0012 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (4 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (2304, (entry as ShortArrayIFDEntry).Values [3]);
			}
			// CanonPi.0x0004 (ImageWidthAsShot/Short/1) "3456"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.PictureInfo);
				Assert.IsNotNull (entry, "Entry 0x0012 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (5 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (3456, (entry as ShortArrayIFDEntry).Values [4]);
			}
			// CanonPi.0x0005 (ImageHeightAsShot/Short/1) "2304"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.PictureInfo);
				Assert.IsNotNull (entry, "Entry 0x0012 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (6 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (2304, (entry as ShortArrayIFDEntry).Values [5]);
			}
			// CanonPi.0x0006 (0x0006/Short/1) "189"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.PictureInfo);
				Assert.IsNotNull (entry, "Entry 0x0012 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (7 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (189, (entry as ShortArrayIFDEntry).Values [6]);
			}
			// CanonPi.0x0007 (0x0007/Short/1) "188"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.PictureInfo);
				Assert.IsNotNull (entry, "Entry 0x0012 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (8 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (188, (entry as ShortArrayIFDEntry).Values [7]);
			}
			// CanonPi.0x0008 (0x0008/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.PictureInfo);
				Assert.IsNotNull (entry, "Entry 0x0012 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (9 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [8]);
			}
			// CanonPi.0x0009 (0x0009/Short/1) "64299"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.PictureInfo);
				Assert.IsNotNull (entry, "Entry 0x0012 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (10 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (64299, (entry as ShortArrayIFDEntry).Values [9]);
			}
			// CanonPi.0x000A (0x000a/Short/1) "64794"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.PictureInfo);
				Assert.IsNotNull (entry, "Entry 0x0012 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (11 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (64794, (entry as ShortArrayIFDEntry).Values [10]);
			}
			// CanonPi.0x000B (0x000b/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.PictureInfo);
				Assert.IsNotNull (entry, "Entry 0x0012 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (12 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [11]);
			}
			// CanonPi.0x000C (0x000c/Short/1) "742"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.PictureInfo);
				Assert.IsNotNull (entry, "Entry 0x0012 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (13 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (742, (entry as ShortArrayIFDEntry).Values [12]);
			}
			// CanonPi.0x000D (0x000d/Short/1) "1237"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.PictureInfo);
				Assert.IsNotNull (entry, "Entry 0x0012 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (14 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (1237, (entry as ShortArrayIFDEntry).Values [13]);
			}
			// CanonPi.0x000E (0x000e/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.PictureInfo);
				Assert.IsNotNull (entry, "Entry 0x0012 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (15 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [14]);
			}
			// CanonPi.0x000F (0x000f/Short/1) "64919"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.PictureInfo);
				Assert.IsNotNull (entry, "Entry 0x0012 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (16 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (64919, (entry as ShortArrayIFDEntry).Values [15]);
			}
			// CanonPi.0x0010 (0x0010/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.PictureInfo);
				Assert.IsNotNull (entry, "Entry 0x0012 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (17 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [16]);
			}
			// CanonPi.0x0011 (0x0011/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.PictureInfo);
				Assert.IsNotNull (entry, "Entry 0x0012 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (18 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [17]);
			}
			// CanonPi.0x0012 (0x0012/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.PictureInfo);
				Assert.IsNotNull (entry, "Entry 0x0012 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (19 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [18]);
			}
			// CanonPi.0x0013 (0x0013/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.PictureInfo);
				Assert.IsNotNull (entry, "Entry 0x0012 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (20 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [19]);
			}
			// CanonPi.0x0014 (0x0014/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.PictureInfo);
				Assert.IsNotNull (entry, "Entry 0x0012 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (21 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [20]);
			}
			// CanonPi.0x0015 (0x0015/Short/1) "617"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.PictureInfo);
				Assert.IsNotNull (entry, "Entry 0x0012 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (22 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (617, (entry as ShortArrayIFDEntry).Values [21]);
			}
			// CanonPi.0x0016 (AFPointsUsed/Short/1) "8"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.PictureInfo);
				Assert.IsNotNull (entry, "Entry 0x0012 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (23 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (8, (entry as ShortArrayIFDEntry).Values [22]);
			}
			// CanonPi.0x0017 (0x0017/Short/1) "65535"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.PictureInfo);
				Assert.IsNotNull (entry, "Entry 0x0012 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (24 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (65535, (entry as ShortArrayIFDEntry).Values [23]);
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
			// Canon.0x0093 (0x0093/Short/16) "32 13315 89 0 0 0 3 0 0 0 0 0 0 0 0 0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CanonFileInfo);
				Assert.IsNotNull (entry, "Entry 0x0093 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 32, 13315, 89, 0, 0, 0, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, (entry as ShortArrayIFDEntry).Values);
			}
			// Canon.0x00A0 (0x00a0/Short/14) "28 0 0 0 0 0 0 0 32771 5200 0 0 0 0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ProcessingInfo);
				Assert.IsNotNull (entry, "Entry 0x00A0 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 28, 0, 0, 0, 0, 0, 0, 0, 32771, 5200, 0, 0, 0, 0 }, (entry as ShortArrayIFDEntry).Values);
			}
			// Canon.0x00AA (0x00aa/Short/5) "10 913 1024 1024 232"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.MeasuredColor);
				Assert.IsNotNull (entry, "Entry 0x00AA missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 10, 913, 1024, 1024, 232 }, (entry as ShortArrayIFDEntry).Values);
			}
			// Canon.0x00D0 (0x00d0/Long/1) "0"
			{
				// TODO: Unknown IFD tag: Canon / 0x00D0
				var entry = makernote_structure.GetEntry (0, (ushort) 0x00D0);
				Assert.IsNotNull (entry, "Entry 0x00D0 missing in IFD 0");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (0, (entry as LongIFDEntry).Value);
			}
			// Canon.0x00E0 (0x00e0/Short/17) "34 3516 2328 1 1 52 19 3507 2322 0 0 0 0 0 0 0 0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.SensorInfo);
				Assert.IsNotNull (entry, "Entry 0x00E0 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 34, 3516, 2328, 1, 1, 52, 19, 3507, 2322, 0, 0, 0, 0, 0, 0, 0, 0 }, (entry as ShortArrayIFDEntry).Values);
			}
			// Canon.0x4001 (0x4001/Short/582) "(Value ommitted)"
			{
				// TODO: Unknown IFD tag: Canon / 0x4001
				var entry = makernote_structure.GetEntry (0, (ushort) 0x4001);
				Assert.IsNotNull (entry, "Entry 0x4001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 1164, 791, 1024, 1024, 349, 495, 1024, 1024, 508, 329, 1024, 1024, 723, 241, 296, 293, 103, 185, 350, 347, 176, 133, 374, 371, 269, 1611, 1131, 1140, 2679, 3199, 1684, 1019, 1027, 2224, 2718, 2142, 1019, 1027, 1534, 5200, 2518, 1019, 1027, 1274, 7000, 2333, 1019, 1027, 1387, 6000, 1445, 1019, 1027, 2403, 3199, 1812, 1019, 1027, 2046, 3901, 2419, 1019, 1027, 1338, 6442, 1459, 1019, 1027, 2824, 3014, 2142, 1019, 1027, 1534, 5200, 358, 988, 65156, 11109, 372, 939, 65191, 9523, 392, 875, 65238, 8000, 416, 822, 65283, 7000, 449, 755, 65344, 6000, 466, 722, 65374, 5600, 489, 683, 65413, 5200, 520, 637, 65462, 4749, 558, 588, 65517, 4312, 588, 555, 21, 4023, 634, 509, 80, 3696, 714, 442, 173, 3250, 757, 416, 216, 3051, 806, 386, 265, 2871, 982, 317, 406, 2413, 500, 2069, 2087, 2069, 2087, 2069, 2087, 2069, 2087, 2069, 2087, 2069, 2087, 2069, 2087, 2069, 2087, 2069, 2087, 2069, 2087, 2069, 2087, 2069, 2087, 2069, 2087, 2069, 2087, 2069, 2087, 256, 256, 256, 255, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 188, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 37, 82, 92, 117, 93, 72, 0, 0, 0, 0, 0, 0, 0, 0, 0, 115, 69, 106, 99, 108, 78, 73, 0, 0, 293, 42, 0, 86, 175, 61, 50, 58, 60, 68, 62, 81, 95, 95, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 172, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 40, 90, 101, 128, 96, 77, 0, 0, 0, 0, 0, 0, 0, 0, 0, 140, 79, 125, 114, 126, 87, 78, 0, 0, 304, 46, 0, 99, 199, 70, 61, 72, 75, 86, 78, 107, 117, 110, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 212, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 52, 113, 124, 152, 111, 76, 0, 0, 0, 0, 0, 0, 0, 0, 0, 177, 102, 151, 136, 145, 93, 81, 0, 0, 536, 82, 0, 154, 311, 106, 87, 95, 95, 103, 91, 116, 118, 111, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 67, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 18, 37, 41, 52, 35, 28, 0, 0, 0, 0, 0, 0, 0, 0, 0, 66, 34, 52, 45, 50, 31, 25, 0, 0, 215, 23, 0, 46, 81, 29, 26, 29, 29, 33, 28, 40, 40, 34, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 15, 10, 7, 133, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 6, 6, 8, 15, 523, 22, 0, 0, 1, 4, 0, 2, 1, 8, 6, 26, 42, 64, 45, 157, 27710, 2884, 678, 427, 444, 669, 354, 855, 0, 286, 305, 304, 72, 1023, 1024, 1024, 0, 0, 0, 0, 0, 0, 0, 0, 1211, 1024, 1024, 2727, 4011, 6737 }, (entry as ShortArrayIFDEntry).Values);
			}
			// Canon.0x4002 (0x4002/Short/2676) "(Value ommitted)"
			{
				// TODO: Unknown IFD tag: Canon / 0x4002
				var entry = makernote_structure.GetEntry (0, (ushort) 0x4002);
				Assert.IsNotNull (entry, "Entry 0x4002 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 5352, 0, 0, 0, 5657, 65535, 65280, 41984, 1297, 65535, 400, 0, 28673, 794, 4058, 316, 196, 48, 464, 3821, 3207, 0, 38565, 38550, 64, 64, 64, 0, 29590, 28815, 50703, 0, 0, 3996, 481, 13, 929, 120, 502, 406, 0, 300, 444, 94, 4411, 473, 153, 8533, 489, 207, 12713, 475, 254, 1270, 32854, 0, 286, 1270, 32854, 0, 286, 1270, 32854, 0, 286, 0, 118, 578, 1869, 0, 4589, 1951, 95, 4383, 2004, 146, 12794, 2001, 192, 12535, 2032, 224, 1100, 12462, 2038, 238, 1520, 32809, 0, 252, 1040, 1040, 1040, 0, 17, 19, 1331, 28, 58, 128, 817, 8, 18, 0, 32, 32, 32, 0, 1, 0, 2, 192, 19335, 511, 512, 512, 512, 1, 1, 78, 151, 28, 0, 768, 0, 0, 768, 0, 0, 2054, 2827, 17, 32, 0, 17151, 1, 2816, 512, 21862, 2672, 63, 9, 29, 6, 26, 133, 63, 13, 32, 5, 15, 67, 63, 10, 41, 5, 21, 68, 63, 21, 36, 16, 28, 102, 63, 13, 32, 10, 27, 118, 40, 512, 202, 132, 0, 1311, 0, 40, 384, 144, 88, 0, 1048, 0, 48, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 560, 400, 3080, 1099, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 256, 0, 0, 0, 0, 0, 0, 0, 256, 256, 32, 4353, 256, 256, 0, 0, 0, 0, 0, 0, 400, 400, 400, 4496, 4496, 38565, 38550, 38550, 29590, 38565, 38550, 38550, 29590, 38565, 38550, 38550, 29590, 38565, 38550, 38550, 29590, 38565, 38550, 38550, 29590, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 64, 64, 64, 64, 64, 64, 0, 28, 58, 20, 40, 64, 64, 50, 0, 62, 55, 10, 35, 64, 35, 48, 70, 46, 55, 30, 64, 52, 32, 64, 3996, 481, 13, 929, 120, 502, 406, 0, 300, 444, 94, 4411, 473, 153, 8533, 489, 207, 12713, 475, 254, 1270, 32854, 0, 286, 1270, 32854, 0, 286, 1270, 32854, 0, 286, 3996, 481, 13, 929, 120, 502, 406, 0, 300, 444, 94, 4411, 473, 153, 8533, 489, 207, 12713, 475, 254, 1270, 32854, 0, 286, 1270, 32854, 0, 286, 1270, 32854, 0, 286, 3996, 481, 13, 929, 120, 502, 406, 0, 300, 444, 94, 4411, 473, 153, 8533, 489, 207, 12713, 475, 254, 1270, 32854, 0, 286, 1270, 32854, 0, 286, 1270, 32854, 0, 286, 3996, 481, 13, 929, 120, 502, 406, 0, 300, 444, 94, 4411, 473, 153, 8533, 489, 207, 12713, 475, 254, 1270, 32854, 0, 286, 1270, 32854, 0, 286, 1270, 32854, 0, 286, 3996, 481, 13, 929, 120, 502, 406, 0, 300, 444, 94, 4411, 473, 153, 8533, 489, 207, 12713, 475, 254, 1270, 32854, 0, 286, 1270, 32854, 0, 286, 1270, 32854, 0, 286, 3996, 481, 13, 929, 120, 502, 406, 0, 300, 444, 94, 4411, 473, 153, 8533, 489, 207, 12713, 475, 254, 1270, 32854, 0, 286, 1270, 32854, 0, 286, 1270, 32854, 0, 286, 3996, 481, 13, 929, 120, 502, 406, 0, 300, 444, 94, 4411, 473, 153, 8533, 489, 207, 12713, 475, 254, 1270, 32854, 0, 286, 1270, 32854, 0, 286, 1270, 32854, 0, 286, 3996, 481, 13, 929, 120, 502, 406, 0, 300, 444, 94, 4411, 473, 153, 8533, 489, 207, 12713, 475, 254, 1270, 32854, 0, 286, 1270, 32854, 0, 286, 1270, 32854, 0, 286, 3996, 481, 13, 929, 120, 502, 406, 0, 300, 444, 94, 4411, 473, 153, 8533, 489, 207, 12713, 475, 254, 1270, 32854, 0, 286, 1270, 32854, 0, 286, 1270, 32854, 0, 286, 3996, 481, 13, 929, 120, 502, 406, 0, 300, 444, 94, 4411, 473, 153, 8533, 489, 207, 12713, 475, 254, 1270, 32854, 0, 286, 1270, 32854, 0, 286, 1270, 32854, 0, 286, 0, 118, 644, 1826, 0, 4561, 1941, 101, 8669, 1973, 147, 12700, 2027, 185, 12582, 2036, 215, 990, 12550, 2034, 226, 1500, 32816, 0, 251, 0, 118, 598, 1867, 0, 262, 1978, 99, 8643, 1992, 148, 12787, 2008, 187, 12566, 2029, 220, 990, 12515, 2034, 230, 1500, 32816, 0, 251, 0, 118, 578, 1869, 0, 4589, 1951, 95, 4383, 2004, 146, 12794, 2001, 192, 12535, 2032, 224, 1100, 12462, 2038, 238, 1520, 32809, 0, 252, 0, 118, 492, 1954, 0, 316, 1970, 93, 4397, 2006, 154, 8488, 2012, 204, 12473, 2022, 237, 990, 12406, 2040, 243, 1235, 32796, 0, 249, 0, 118, 349, 17, 0, 380, 1953, 84, 4452, 1993, 157, 8504, 2008, 214, 12465, 1989, 248, 990, 12313, 2045, 252, 1217, 32777, 0, 253, 0, 118, 643, 1827, 0, 4564, 1938, 101, 8662, 1982, 147, 12748, 2014, 186, 12557, 2016, 217, 990, 12474, 2043, 226, 1500, 32826, 0, 246, 0, 118, 598, 1867, 0, 262, 1978, 99, 8643, 1992, 148, 12787, 2008, 187, 12566, 2009, 220, 990, 12465, 2041, 229, 1500, 32819, 0, 247, 0, 118, 578, 1870, 0, 4590, 1950, 95, 4382, 2005, 146, 12798, 2000, 192, 12531, 2027, 224, 1100, 12436, 2040, 237, 1520, 32809, 0, 249, 0, 118, 512, 1936, 0, 304, 1978, 94, 4406, 2001, 154, 8471, 2018, 204, 12507, 1996, 237, 990, 12373, 2046, 243, 1250, 32793, 0, 248, 0, 118, 349, 17, 0, 380, 1953, 84, 4452, 1993, 157, 8504, 2008, 214, 12465, 1989, 248, 990, 12313, 2045, 252, 1217, 32777, 0, 253, 0, 118, 643, 1827, 0, 4564, 1938, 101, 8662, 1982, 147, 12748, 2014, 186, 12557, 2016, 217, 990, 12474, 2043, 226, 1500, 32833, 0, 246, 0, 118, 598, 1867, 0, 262, 1978, 99, 8643, 1992, 148, 12787, 2008, 187, 12566, 2009, 220, 990, 12465, 2041, 229, 1500, 32819, 0, 247, 0, 118, 578, 1870, 0, 4590, 1950, 95, 4382, 2005, 146, 12798, 2000, 192, 12531, 2027, 224, 1100, 12436, 2040, 237, 1520, 32814, 0, 249, 0, 118, 512, 1936, 0, 304, 1978, 94, 4406, 2001, 154, 8471, 2018, 204, 12507, 1996, 237, 990, 12373, 2046, 243, 1250, 32795, 0, 248, 0, 118, 349, 17, 0, 380, 1953, 84, 4452, 1993, 157, 8504, 2008, 214, 12465, 1989, 248, 990, 12313, 2045, 252, 1217, 32777, 0, 253, 0, 118, 643, 1827, 0, 4564, 1938, 101, 8662, 1982, 147, 12748, 2018, 186, 12581, 2017, 218, 990, 12501, 2038, 228, 1500, 32827, 0, 249, 0, 118, 598, 1867, 0, 262, 1978, 99, 8643, 1992, 148, 12787, 2013, 187, 12590, 2010, 221, 990, 12492, 2038, 231, 1500, 32807, 0, 251, 0, 118, 578, 1870, 0, 4590, 1950, 95, 4382, 2005, 146, 12798, 2000, 192, 12531, 2034, 224, 1100, 12466, 2034, 238, 1515, 32810, 0, 251, 0, 118, 512, 1936, 0, 304, 1978, 94, 4406, 2001, 154, 8471, 2018, 204, 12507, 1996, 237, 990, 12373, 2046, 243, 1250, 32799, 0, 248, 0, 118, 349, 17, 0, 380, 1953, 84, 4452, 1993, 157, 8504, 2008, 214, 12465, 1989, 248, 990, 12313, 2045, 252, 1217, 32777, 0, 253, 0, 118, 643, 1827, 0, 4564, 1938, 101, 8662, 1982, 147, 12748, 2018, 186, 12581, 2017, 218, 990, 12501, 2038, 228, 1500, 32827, 0, 249, 0, 118, 598, 1867, 0, 262, 1978, 99, 8643, 1992, 148, 12787, 2013, 187, 12590, 2010, 221, 990, 12492, 2038, 231, 1500, 32807, 0, 251, 0, 118, 578, 1870, 0, 4590, 1950, 95, 4382, 2005, 146, 12798, 2000, 192, 12531, 2034, 224, 1100, 12466, 2034, 238, 1515, 32810, 0, 251, 0, 118, 512, 1936, 0, 304, 1978, 94, 4406, 2001, 154, 8471, 2018, 204, 12507, 1996, 237, 990, 12373, 2046, 243, 1250, 32799, 0, 248, 0, 118, 349, 17, 0, 380, 1953, 84, 4452, 1993, 157, 8504, 2008, 214, 12465, 1989, 248, 990, 12313, 2045, 252, 1217, 32777, 0, 253, 0, 2056, 8, 8, 8, 80, 25703, 8500, 80, 25703, 8500, 80, 25703, 8500, 84, 25703, 8500, 84, 25703, 8500, 78, 151, 28, 98, 158, 1, 138, 118, 1, 254, 1, 1, 1, 254, 1, 2054, 2827, 17, 32, 2054, 2827, 17, 32, 2054, 2827, 17, 32, 4104, 6678, 292, 32, 6666, 10019, 312, 32, 63, 21, 36, 16, 28, 102, 63, 13, 32, 10, 27, 118, 63, 21, 36, 16, 28, 102, 63, 13, 32, 10, 27, 118, 63, 21, 36, 8, 14, 51, 63, 13, 32, 10, 27, 118, 63, 0, 0, 0, 0, 0, 63, 13, 32, 10, 27, 118, 63, 0, 0, 0, 0, 0, 63, 13, 32, 10, 27, 118, 38, 43, 48, 58, 67, 128, 128, 128, 128, 128, 128, 128, 128, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 3000, 794, 64742, 38, 65498, 316, 196, 48, 464, 65261, 275, 889, 64647, 5000, 794, 64742, 38, 65498, 316, 196, 48, 464, 65261, 275, 889, 64647, 7000, 794, 64742, 38, 65498, 316, 196, 48, 464, 65261, 275, 889, 64647, 3000, 794, 64742, 38, 65498, 316, 196, 48, 464, 65261, 275, 889, 64647, 5000, 794, 64742, 38, 65498, 316, 196, 48, 464, 65261, 275, 889, 64647, 7000, 794, 64742, 38, 65498, 316, 196, 48, 464, 65261, 275, 889, 64647, 3000, 794, 64742, 38, 65498, 316, 196, 48, 464, 65261, 275, 889, 64647, 5000, 794, 64742, 38, 65498, 316, 196, 48, 464, 65261, 275, 889, 64647, 7000, 794, 64742, 38, 65498, 316, 196, 48, 464, 65261, 275, 889, 64647, 3000, 794, 64742, 38, 65498, 316, 196, 48, 464, 65261, 275, 889, 64647, 5000, 794, 64742, 38, 65498, 316, 196, 48, 464, 65261, 275, 889, 64647, 7000, 794, 64742, 38, 65498, 316, 196, 48, 464, 65261, 275, 889, 64647, 3000, 794, 64742, 38, 65498, 316, 196, 48, 464, 65261, 275, 889, 64647, 5000, 794, 64742, 38, 65498, 316, 196, 48, 464, 65261, 275, 889, 64647, 7000, 794, 64742, 38, 65498, 316, 196, 48, 464, 65261, 275, 889, 64647, 3000, 702, 64834, 15, 65521, 343, 169, 13, 499, 65298, 238, 921, 64615, 5000, 702, 64834, 15, 65521, 343, 169, 13, 499, 65298, 238, 921, 64615, 7000, 702, 64834, 15, 65521, 343, 169, 13, 499, 65298, 238, 921, 64615, 3000, 696, 64840, 26, 65510, 340, 172, 37, 475, 65298, 238, 921, 64615, 5000, 696, 64840, 26, 65510, 340, 172, 37, 475, 65298, 238, 921, 64615, 7000, 696, 64840, 26, 65510, 340, 172, 37, 475, 65298, 238, 921, 64615, 3000, 697, 64839, 14, 65522, 344, 168, 34, 478, 65296, 240, 913, 64623, 5000, 697, 64839, 14, 65522, 344, 168, 34, 478, 65296, 240, 913, 64623, 7000, 697, 64839, 14, 65522, 344, 168, 34, 478, 65296, 240, 913, 64623, 3000, 714, 64822, 11, 65525, 388, 124, 22, 490, 65286, 250, 900, 64636, 5000, 714, 64822, 11, 65525, 388, 124, 22, 490, 65286, 250, 900, 64636, 7000, 714, 64822, 11, 65525, 388, 124, 22, 490, 65286, 250, 900, 64636, 3000, 680, 64856, 15, 65521, 403, 109, 1, 511, 65302, 234, 880, 64656, 5000, 680, 64856, 15, 65521, 403, 109, 1, 511, 65302, 234, 880, 64656, 7000, 680, 64856, 15, 65521, 403, 109, 1, 511, 65302, 234, 880, 64656, 3000, 65524, 65524, 14, 0, 5000, 65524, 65524, 14, 0, 7000, 65524, 65524, 14, 0, 3000, 65530, 65530, 7, 0, 5000, 65530, 65530, 7, 0, 7000, 65530, 65530, 7, 0, 3000, 0, 0, 0, 0, 5000, 0, 0, 0, 0, 7000, 0, 0, 0, 0, 3000, 6, 6, 65529, 0, 5000, 6, 6, 65529, 0, 7000, 6, 6, 65529, 0, 3000, 12, 12, 65522, 0, 5000, 12, 12, 65522, 0, 7000, 12, 12, 65522, 0, 3000, 65522, 65522, 15, 0, 5000, 65522, 65522, 15, 0, 7000, 65522, 65522, 15, 0, 3000, 65530, 65530, 7, 0, 5000, 65530, 65530, 7, 0, 7000, 65530, 65530, 7, 0, 3000, 0, 0, 0, 0, 5000, 0, 0, 0, 0, 7000, 0, 0, 0, 0, 3000, 6, 6, 65529, 0, 5000, 6, 6, 65529, 0, 7000, 6, 6, 65529, 0, 3000, 13, 12, 65522, 0, 5000, 13, 12, 65522, 0, 7000, 13, 12, 65522, 0, 110, 100, 135, 4, 5, 200, 6, 110, 100, 135, 4, 5, 200, 6, 110, 100, 135, 4, 5, 200, 6, 110, 100, 135, 4, 5, 200, 6, 110, 100, 135, 4, 5, 200, 6, 125, 110, 122, 4, 6, 190, 9, 125, 110, 122, 4, 6, 190, 9, 125, 110, 122, 4, 6, 190, 9, 125, 110, 122, 4, 6, 190, 9, 125, 110, 122, 4, 6, 190, 9, 143, 112, 115, 9, 6, 198, 15, 143, 112, 115, 9, 6, 198, 15, 143, 112, 115, 9, 6, 198, 15, 143, 112, 115, 9, 6, 198, 15, 143, 112, 115, 9, 6, 198, 15, 162, 122, 119, 10, 9, 158, 15, 162, 122, 119, 10, 9, 158, 15, 162, 122, 119, 10, 9, 158, 15, 162, 122, 119, 10, 9, 158, 15, 162, 122, 119, 10, 9, 158, 15, 178, 124, 125, 12, 11, 150, 17, 178, 124, 125, 12, 11, 150, 17, 178, 124, 125, 12, 11, 150, 17, 178, 124, 125, 12, 11, 150, 17, 178, 124, 125, 12, 11, 150, 17, 100, 90, 181, 5, 2, 180, 5, 100, 90, 181, 5, 2, 180, 5, 100, 90, 181, 5, 2, 180, 5, 100, 90, 181, 5, 2, 180, 5, 100, 90, 181, 5, 2, 180, 5, 110, 100, 135, 5, 5, 200, 6, 110, 100, 135, 5, 5, 200, 6, 110, 100, 135, 5, 5, 200, 6, 110, 100, 135, 5, 5, 200, 6, 110, 100, 135, 5, 5, 200, 6, 125, 110, 123, 4, 6, 190, 9, 125, 110, 123, 4, 6, 190, 9, 125, 110, 123, 4, 6, 190, 9, 125, 110, 123, 4, 6, 190, 9, 125, 110, 123, 4, 6, 190, 9, 143, 112, 115, 9, 6, 198, 15, 143, 112, 115, 9, 6, 198, 15, 143, 112, 115, 9, 6, 198, 15, 143, 112, 115, 9, 6, 198, 15, 143, 112, 115, 9, 6, 198, 15, 162, 122, 119, 10, 9, 158, 15, 162, 122, 119, 10, 9, 158, 15, 162, 122, 119, 10, 9, 158, 15, 162, 122, 119, 10, 9, 158, 15, 162, 122, 119, 10, 9, 158, 15, 2, 65535, 144, 142, 146, 0, 2, 6, 10, 14, 18, 22, 26, 30, 34, 38, 42, 46, 50, 54, 58, 62, 66, 70, 74, 78, 82, 86, 90, 94, 98, 102, 106, 110, 114, 118, 122, 126, 130, 134, 138, 142, 146, 150, 154, 158, 162, 166, 170, 174, 178, 182, 186, 190, 194, 198, 202, 206, 210, 214, 218, 222, 226, 230, 234, 238, 242, 246, 250, 254, 258, 262, 266, 270, 274, 278, 282, 286, 290, 294, 298, 304, 308, 312, 316, 320, 324, 328, 332, 336, 340, 346, 350, 354, 360, 364, 368, 374, 378, 384, 388, 394, 400, 404, 410, 416, 420, 426, 432, 438, 444, 452, 458, 464, 472, 478, 486, 494, 500, 510, 518, 526, 534, 542, 552, 560, 568, 578, 586, 596, 604, 614, 624, 632, 642, 652, 662, 672, 682, 692, 704, 714, 726, 736, 748, 758, 770, 782, 794, 806, 820, 834, 846, 860, 874, 888, 904, 920, 936, 952, 970, 988, 1004, 1022, 1040, 1058, 1076, 1094, 1112, 1130, 1148, 1166, 1184, 1202, 1222, 1240, 1258, 1276, 1296, 1314, 1334, 1352, 1372, 1390, 1410, 1428, 1448, 1468, 1488, 1506, 1526, 1546, 1566, 1586, 1606, 1626, 1646, 1668, 1688, 1708, 1730, 1752, 1772, 1794, 1816, 1840, 1862, 1886, 1908, 1932, 1956, 1982, 2006, 2032, 2058, 2084, 2110, 2138, 2166, 2194, 2222, 2250, 2280, 2310, 2340, 2370, 2400, 2430, 2462, 2492, 2524, 2554, 2586, 2618, 2650, 2682, 2714, 2746, 2780, 2812, 2846, 2880, 2914, 2948, 2982, 3018, 3052, 3088, 3124, 3160, 3196, 3232, 3270, 3306, 3344, 3382, 3420, 3460, 3498, 3538, 3578 }, (entry as ShortArrayIFDEntry).Values);
			}
			// Canon.0x4003 (0x4003/Short/22) "0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0"
			{
				// TODO: Unknown IFD tag: Canon / 0x4003
				var entry = makernote_structure.GetEntry (0, (ushort) 0x4003);
				Assert.IsNotNull (entry, "Entry 0x4003 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, (entry as ShortArrayIFDEntry).Values);
			}
			// Photo.0x9286 (UserComment/UserComment/264) ""
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.UserComment);
				Assert.IsNotNull (entry, "Entry 0x9286 missing in IFD 0");
				Assert.IsNotNull (entry as UserCommentIFDEntry, "Entry is not a user comment!");
				Assert.AreEqual ("", (entry as UserCommentIFDEntry).Value.Trim ());
			}
			// Photo.0xA000 (FlashpixVersion/Undefined/4) "48 49 48 48 "
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
			// Photo.0xA002 (PixelXDimension/Short/1) "3456"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.PixelXDimension);
				Assert.IsNotNull (entry, "Entry 0xA002 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (3456, (entry as ShortIFDEntry).Value);
			}
			// Photo.0xA003 (PixelYDimension/Short/1) "2304"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.PixelYDimension);
				Assert.IsNotNull (entry, "Entry 0xA003 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (2304, (entry as ShortIFDEntry).Value);
			}
			// Photo.0xA005 (InteroperabilityTag/SubIFD/1) "9304"
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
			// Iop.0x0002 (InteroperabilityVersion/Undefined/4) "48 49 48 48 "
			{
				var entry = iop_structure.GetEntry (0, (ushort) IOPEntryTag.InteroperabilityVersion);
				Assert.IsNotNull (entry, "Entry 0x0002 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 48, 49, 48, 48 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Photo.0xA20E (FocalPlaneXResolution/Rational/1) "3456000/874"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.FocalPlaneXResolution);
				Assert.IsNotNull (entry, "Entry 0xA20E missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (3456000, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (874, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Photo.0xA20F (FocalPlaneYResolution/Rational/1) "2304000/582"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.FocalPlaneYResolution);
				Assert.IsNotNull (entry, "Entry 0xA20F missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (2304000, (entry as RationalIFDEntry).Value.Numerator);
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
			// Photo.0xA402 (ExposureMode/Short/1) "0"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ExposureMode);
				Assert.IsNotNull (entry, "Entry 0xA402 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Photo.0xA403 (WhiteBalance/Short/1) "1"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.WhiteBalance);
				Assert.IsNotNull (entry, "Entry 0xA403 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1, (entry as ShortIFDEntry).Value);
			}
			// Photo.0xA406 (SceneCaptureType/Short/1) "0"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.SceneCaptureType);
				Assert.IsNotNull (entry, "Entry 0xA406 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Thumbnail.0x0201 (JPEGInterchangeFormat/ThumbnailDataIFD/1) "2898021"
			{
				var entry = structure.GetEntry (1, (ushort) IFDEntryTag.JPEGInterchangeFormat);
				Assert.IsNotNull (entry, "Entry 0x0201 missing in IFD 1");
				Assert.IsNotNull (entry as ThumbnailDataIFDEntry, "Entry is not a thumbnail IFD!");
			}
			// Thumbnail.0x0202 (JPEGInterchangeFormatLength/Long/1) "6632"
			{
				var entry = structure.GetEntry (1, (ushort) IFDEntryTag.JPEGInterchangeFormatLength);
				Assert.IsNotNull (entry, "Entry 0x0202 missing in IFD 1");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (6632, (entry as LongIFDEntry).Value);
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
			// Image2.0x0111 (StripOffsets/StripOffsets/1) "2905165"
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

			//  ---------- End of IFD tests ----------

		}
	}
}
