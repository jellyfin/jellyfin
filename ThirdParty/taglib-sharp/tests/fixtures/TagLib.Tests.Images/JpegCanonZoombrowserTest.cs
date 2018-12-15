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
	public class JpegCanonZoombrowserTest
	{
		[Test]
		public void Test ()
		{
			ImageTest.Run ("sample_canon_zoombrowser.jpg",
				new JpegCanonZoombrowserTestInvariantValidator (),
				new NoModificationValidator (),
				new CommentModificationValidator ("%test comment%"),
				new TagCommentModificationValidator ("%test comment%", TagTypes.TiffIFD, true),
				new TagCommentModificationValidator ("%test comment%", TagTypes.XMP, false),
				new TagKeywordsModificationValidator (TagTypes.XMP, false)
			);
		}
	}

	public class JpegCanonZoombrowserTestInvariantValidator : IMetadataInvariantValidator
	{
		public void ValidateMetadataInvariants (Image.File file)
		{
			Assert.IsNotNull (file);
			//  ---------- Start of IFD tests ----------

			var tag = file.GetTag (TagTypes.TiffIFD) as IFDTag;
			Assert.IsNotNull (tag, "IFD tag not found");

			var structure = tag.Structure;

			// Image.0x010F (Make/Ascii/6) "Canon"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.Make);
				Assert.IsNotNull (entry, "Entry 0x010F missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("Canon", (entry as StringIFDEntry).Value);
			}
			// Image.0x0110 (Model/Ascii/32) "Canon EOS 400D DIGITAL"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.Model);
				Assert.IsNotNull (entry, "Entry 0x0110 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("Canon EOS 400D DIGITAL", (entry as StringIFDEntry).Value);
			}
			// Image.0x0112 (Orientation/Short/1) "1"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.Orientation);
				Assert.IsNotNull (entry, "Entry 0x0112 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1, (entry as ShortIFDEntry).Value);
			}
			// Image.0x011A (XResolution/Rational/1) "350/1"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.XResolution);
				Assert.IsNotNull (entry, "Entry 0x011A missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (350, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (1, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Image.0x011B (YResolution/Rational/1) "350/1"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.YResolution);
				Assert.IsNotNull (entry, "Entry 0x011B missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (350, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (1, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Image.0x0128 (ResolutionUnit/Short/1) "2"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.ResolutionUnit);
				Assert.IsNotNull (entry, "Entry 0x0128 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (2, (entry as ShortIFDEntry).Value);
			}
			// Image.0x0131 (Software/Ascii/28) "Digital Photo Professional"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.Software);
				Assert.IsNotNull (entry, "Entry 0x0131 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("Digital Photo Professional", (entry as StringIFDEntry).Value);
			}
			// Image.0x0132 (DateTime/Ascii/20) "2009:08:09 19:12:44"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.DateTime);
				Assert.IsNotNull (entry, "Entry 0x0132 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("2009:08:09 19:12:44", (entry as StringIFDEntry).Value);
			}
			// Image.0x0213 (YCbCrPositioning/Short/1) "1"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.YCbCrPositioning);
				Assert.IsNotNull (entry, "Entry 0x0213 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1, (entry as ShortIFDEntry).Value);
			}
			// Image.0x8769 (ExifTag/SubIFD/1) "236"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.ExifIFD);
				Assert.IsNotNull (entry, "Entry 0x8769 missing in IFD 0");
				Assert.IsNotNull (entry as SubIFDEntry, "Entry is not a sub IFD!");
			}

			var exif = structure.GetEntry (0, (ushort) IFDEntryTag.ExifIFD) as SubIFDEntry;
			Assert.IsNotNull (exif, "Exif tag not found");
			var exif_structure = exif.Structure;

			// Photo.0x829A (ExposureTime/Rational/1) "1/200"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ExposureTime);
				Assert.IsNotNull (entry, "Entry 0x829A missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (1, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (200, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Photo.0x829D (FNumber/Rational/1) "63/10"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.FNumber);
				Assert.IsNotNull (entry, "Entry 0x829D missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (63, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (10, (entry as RationalIFDEntry).Value.Denominator);
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
				var bytes = new byte [] { 48, 50, 50, 49 };
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Photo.0x9003 (DateTimeOriginal/Ascii/20) "2009:08:09 19:12:44"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.DateTimeOriginal);
				Assert.IsNotNull (entry, "Entry 0x9003 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("2009:08:09 19:12:44", (entry as StringIFDEntry).Value);
			}
			// Photo.0x9004 (DateTimeDigitized/Ascii/20) "2009:08:09 19:12:44"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.DateTimeDigitized);
				Assert.IsNotNull (entry, "Entry 0x9004 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("2009:08:09 19:12:44", (entry as StringIFDEntry).Value);
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
			// Photo.0x9201 (ShutterSpeedValue/SRational/1) "500948/65536"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ShutterSpeedValue);
				Assert.IsNotNull (entry, "Entry 0x9201 missing in IFD 0");
				Assert.IsNotNull (entry as SRationalIFDEntry, "Entry is not a srational!");
				Assert.AreEqual (500948, (entry as SRationalIFDEntry).Value.Numerator);
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
			// Photo.0x9209 (Flash/Short/1) "16"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.Flash);
				Assert.IsNotNull (entry, "Entry 0x9209 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (16, (entry as ShortIFDEntry).Value);
			}
			// Photo.0x920A (FocalLength/Rational/1) "180/1"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.FocalLength);
				Assert.IsNotNull (entry, "Entry 0x920A missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (180, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (1, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Photo.0x927C (MakerNote/MakerNote/1072) ""
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.MakerNote);
				Assert.IsNotNull (entry, "Entry 0x927C missing in IFD 0");
				Assert.IsNotNull (entry as MakernoteIFDEntry, "Entry is not a makernote IFD!");
			}

			var makernote = exif_structure.GetEntry (0, (ushort) ExifEntryTag.MakerNote) as MakernoteIFDEntry;
			Assert.IsNotNull (makernote, "MakerNote tag not found");
			var makernote_structure = makernote.Structure;


			Assert.AreEqual (MakernoteType.Canon, makernote.MakernoteType);

			// CanonCs.0x0000 (0x0000/Short/1) "94"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (1 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (94, (entry as ShortArrayIFDEntry).Values [0]);
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
			// CanonCs.0x0003 (Quality/Short/1) "3"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (4 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (3, (entry as ShortArrayIFDEntry).Values [3]);
			}
			// CanonCs.0x0004 (FlashMode/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (5 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [4]);
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
			// CanonCs.0x0007 (FocusMode/Short/1) "3"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (8 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (3, (entry as ShortArrayIFDEntry).Values [7]);
			}
			// CanonCs.0x0008 (0x0008/Short/1) "65535"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (9 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (65535, (entry as ShortArrayIFDEntry).Values [8]);
			}
			// CanonCs.0x0009 (0x0009/Short/1) "65535"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (10 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (65535, (entry as ShortArrayIFDEntry).Values [9]);
			}
			// CanonCs.0x000A (ImageSize/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (11 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [10]);
			}
			// CanonCs.0x000B (EasyMode/Short/1) "1"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (12 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (1, (entry as ShortArrayIFDEntry).Values [11]);
			}
			// CanonCs.0x000C (DigitalZoom/Short/1) "65535"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (13 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (65535, (entry as ShortArrayIFDEntry).Values [12]);
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
			// CanonCs.0x0014 (ExposureProgram/Short/1) "3"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (21 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (3, (entry as ShortArrayIFDEntry).Values [20]);
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
			// CanonCs.0x0017 (Lens/Short/3) "300 70 1"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (26 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (300, (entry as ShortArrayIFDEntry).Values [23]);
				Assert.AreEqual (70, (entry as ShortArrayIFDEntry).Values [24]);
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
			// CanonCs.0x001B (MinAperture/Short/1) "336"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (28 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (336, (entry as ShortArrayIFDEntry).Values [27]);
			}
			// CanonCs.0x001C (FlashActivity/Short/1) "65535"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (29 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (65535, (entry as ShortArrayIFDEntry).Values [28]);
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
			// CanonCs.0x0020 (FocusContinuous/Short/1) "8"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (33 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (8, (entry as ShortArrayIFDEntry).Values [32]);
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
			// CanonCs.0x0029 (0x0029/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (42 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [41]);
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
			// CanonCs.0x002E (0x002e/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CameraSettings);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (47 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [46]);
			}
			// Canon.0x0002 (0x0002/Short/4) "2 180 907 605"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.FocalLength);
				Assert.IsNotNull (entry, "Entry 0x0002 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 2, 180, 907, 605 }, (entry as ShortArrayIFDEntry).Values);
			}
			// CanonSi.0x0000 (0x0000/Short/1) "66"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ShotInfo);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (1 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (66, (entry as ShortArrayIFDEntry).Values [0]);
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
			// CanonSi.0x0003 (0x0003/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ShotInfo);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (4 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [3]);
			}
			// CanonSi.0x0004 (TargetAperture/Short/1) "170"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ShotInfo);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (5 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (170, (entry as ShortArrayIFDEntry).Values [4]);
			}
			// CanonSi.0x0005 (TargetShutterSpeed/Short/1) "245"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ShotInfo);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (6 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (245, (entry as ShortArrayIFDEntry).Values [5]);
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
			// CanonSi.0x0012 (0x0012/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ShotInfo);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (19 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [18]);
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
			// CanonSi.0x0015 (ApertureValue/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ShotInfo);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (22 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [21]);
			}
			// CanonSi.0x0016 (ShutterSpeedValue/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ShotInfo);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (23 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [22]);
			}
			// CanonSi.0x0017 (0x0017/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ShotInfo);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (24 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [23]);
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
			// CanonSi.0x001A (0x001a/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ShotInfo);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (27 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (0, (entry as ShortArrayIFDEntry).Values [26]);
			}
			// CanonSi.0x001B (0x001b/Short/1) "65535"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ShotInfo);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (28 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (65535, (entry as ShortArrayIFDEntry).Values [27]);
			}
			// CanonSi.0x001C (0x001c/Short/1) "65535"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ShotInfo);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (29 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (65535, (entry as ShortArrayIFDEntry).Values [28]);
			}
			// CanonSi.0x001D (0x001d/Short/1) "65535"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ShotInfo);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.IsTrue (30 <= (entry as ShortArrayIFDEntry).Values.Length);
				Assert.AreEqual (65535, (entry as ShortArrayIFDEntry).Values [29]);
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
			// Canon.0x0008 (ImageNumber/Long/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ImageNumber);
				Assert.IsNotNull (entry, "Entry 0x0008 missing in IFD 0");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (0, (entry as LongIFDEntry).Value);
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
			// Canon.0x0015 (0x0015/Long/1) "2684354560"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.SerialNumberFormat);
				Assert.IsNotNull (entry, "Entry 0x0015 missing in IFD 0");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (2684354560, (entry as LongIFDEntry).Value);
			}
			// Canon.0x0016 (0x0016/Long/1) "0"
			{
				// TODO: Unknown IFD tag: Canon / 0x0016
				var entry = makernote_structure.GetEntry (0, (ushort) 0x0016);
				Assert.IsNotNull (entry, "Entry 0x0016 missing in IFD 0");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (0, (entry as LongIFDEntry).Value);
			}
			// Canon.0x0019 (0x0019/Short/1) "1"
			{
				// TODO: Unknown IFD tag: Canon / 0x0019
				var entry = makernote_structure.GetEntry (0, (ushort) 0x0019);
				Assert.IsNotNull (entry, "Entry 0x0019 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1, (entry as ShortIFDEntry).Value);
			}
			// Canon.0x0093 (0x0093/Short/17) "34 44371 96 0 0 0 65535 65535 0 0 0 0 0 0 0 0 101"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.CanonFileInfo);
				Assert.IsNotNull (entry, "Entry 0x0093 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 34, 44371, 96, 0, 0, 0, 65535, 65535, 0, 0, 0, 0, 0, 0, 0, 0, 101 }, (entry as ShortArrayIFDEntry).Values);
			}
			// Canon.0x0095 (0x0095/Ascii/64) "EF70-300mm f/4-5.6 IS USM"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.LensModel);
				Assert.IsNotNull (entry, "Entry 0x0095 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("EF70-300mm f/4-5.6 IS USM", (entry as StringIFDEntry).Value);
			}
			// Canon.0x0096 (0x0096/Ascii/16) "H0733598"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.SerialInfo);
				Assert.IsNotNull (entry, "Entry 0x0096 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("H0733598", (entry as StringIFDEntry).Value);
			}
			// Canon.0x00A0 (0x00a0/Short/14) "28 0 3 0 0 0 0 0 32768 5200 129 0 0 0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ProcessingInfo);
				Assert.IsNotNull (entry, "Entry 0x00A0 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 28, 0, 3, 0, 0, 0, 0, 0, 32768, 5200, 129, 0, 0, 0 }, (entry as ShortArrayIFDEntry).Values);
			}
			// Canon.0x00AA (0x00aa/Short/5) "10 468 1024 1024 228"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.MeasuredColor);
				Assert.IsNotNull (entry, "Entry 0x00AA missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 10, 468, 1024, 1024, 228 }, (entry as ShortArrayIFDEntry).Values);
			}
			// Canon.0x00B4 (0x00b4/Short/1) "1"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.ColorSpace);
				Assert.IsNotNull (entry, "Entry 0x00B4 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1, (entry as ShortIFDEntry).Value);
			}
			// Canon.0x00E0 (0x00e0/Short/17) "34 3948 2622 1 1 52 23 3939 2614 0 0 0 0 0 0 0 0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) CanonMakerNoteEntryTag.SensorInfo);
				Assert.IsNotNull (entry, "Entry 0x00E0 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 34, 3948, 2622, 1, 1, 52, 23, 3939, 2614, 0, 0, 0, 0, 0, 0, 0, 0 }, (entry as ShortArrayIFDEntry).Values);
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
			// Canon.0x4011 (0x4011/Undefined/252) "0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 "
			{
				// TODO: Unknown IFD tag: Canon / 0x4011
				var entry = makernote_structure.GetEntry (0, (ushort) 0x4011);
				Assert.IsNotNull (entry, "Entry 0x4011 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var bytes = new byte [] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Photo.0x9286 (UserComment/UserComment/264) "%test comment%"
			//  --> Test removed because of CommentModificationValidator, value is checked there.
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
			// Photo.0xA002 (PixelXDimension/Short/1) "19"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.PixelXDimension);
				Assert.IsNotNull (entry, "Entry 0xA002 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (19, (entry as ShortIFDEntry).Value);
			}
			// Photo.0xA003 (PixelYDimension/Short/1) "41"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.PixelYDimension);
				Assert.IsNotNull (entry, "Entry 0xA003 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (41, (entry as ShortIFDEntry).Value);
			}
			// Photo.0xA005 (InteroperabilityTag/SubIFD/1) "1978"
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
				var bytes = new byte [] { 48, 49, 48, 48 };
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Iop.0x1001 (RelatedImageWidth/Short/1) "19"
			{
				var entry = iop_structure.GetEntry (0, (ushort) IOPEntryTag.RelatedImageWidth);
				Assert.IsNotNull (entry, "Entry 0x1001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (19, (entry as ShortIFDEntry).Value);
			}
			// Iop.0x1002 (RelatedImageLength/Short/1) "41"
			{
				var entry = iop_structure.GetEntry (0, (ushort) IOPEntryTag.RelatedImageLength);
				Assert.IsNotNull (entry, "Entry 0x1002 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (41, (entry as ShortIFDEntry).Value);
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
			// Photo.0xA401 (CustomRendered/Short/1) "1"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.CustomRendered);
				Assert.IsNotNull (entry, "Entry 0xA401 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1, (entry as ShortIFDEntry).Value);
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
			// Photo.0xA406 (SceneCaptureType/Short/1) "0"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.SceneCaptureType);
				Assert.IsNotNull (entry, "Entry 0xA406 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Thumbnail.0x0103 (Compression/Short/1) "6"
			{
				var entry = structure.GetEntry (1, (ushort) IFDEntryTag.Compression);
				Assert.IsNotNull (entry, "Entry 0x0103 missing in IFD 1");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (6, (entry as ShortIFDEntry).Value);
			}
			// Thumbnail.0x011A (XResolution/Rational/1) "350/1"
			{
				var entry = structure.GetEntry (1, (ushort) IFDEntryTag.XResolution);
				Assert.IsNotNull (entry, "Entry 0x011A missing in IFD 1");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (350, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (1, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Thumbnail.0x011B (YResolution/Rational/1) "350/1"
			{
				var entry = structure.GetEntry (1, (ushort) IFDEntryTag.YResolution);
				Assert.IsNotNull (entry, "Entry 0x011B missing in IFD 1");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (350, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (1, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Thumbnail.0x0128 (ResolutionUnit/Short/1) "2"
			{
				var entry = structure.GetEntry (1, (ushort) IFDEntryTag.ResolutionUnit);
				Assert.IsNotNull (entry, "Entry 0x0128 missing in IFD 1");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (2, (entry as ShortIFDEntry).Value);
			}
			// Thumbnail.0x0201 (JPEGInterchangeFormat/ThumbnailDataIFD/1) "2142"
			{
				var entry = structure.GetEntry (1, (ushort) IFDEntryTag.JPEGInterchangeFormat);
				Assert.IsNotNull (entry, "Entry 0x0201 missing in IFD 1");
				Assert.IsNotNull (entry as ThumbnailDataIFDEntry, "Entry is not a thumbnail IFD!");
			}
			// Thumbnail.0x0202 (JPEGInterchangeFormatLength/Long/1) "2305"
			{
				var entry = structure.GetEntry (1, (ushort) IFDEntryTag.JPEGInterchangeFormatLength);
				Assert.IsNotNull (entry, "Entry 0x0202 missing in IFD 1");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (2305, (entry as LongIFDEntry).Value);
			}

			//  ---------- End of IFD tests ----------

		}
	}
}
