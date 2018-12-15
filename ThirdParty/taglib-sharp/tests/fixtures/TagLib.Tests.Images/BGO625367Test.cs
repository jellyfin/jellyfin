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
	public class BGO625367Test
	{
		[Test]
		public void Test ()
		{
			ImageTest.Run ("sample_bgo625367.jpg",
				false,
				new BGO625367TestInvariantValidator ()
			);
		}
	}

	public class BGO625367TestInvariantValidator : IMetadataInvariantValidator
	{
		public void ValidateMetadataInvariants (Image.File file)
		{
			Assert.IsNotNull (file);
			//  ---------- Start of IFD tests ----------

			var tag = file.GetTag (TagTypes.TiffIFD) as IFDTag;
			Assert.IsNotNull (tag, "IFD tag not found");

			var structure = tag.Structure;

			// Image.0x010E (ImageDescription/Ascii/32) "                               "
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.ImageDescription);
				Assert.IsNotNull (entry, "Entry 0x010E missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("                               ", (entry as StringIFDEntry).Value);
			}
			// Image.0x010F (Make/Ascii/5) "SONY"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.Make);
				Assert.IsNotNull (entry, "Entry 0x010F missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("SONY", (entry as StringIFDEntry).Value);
			}
			// Image.0x0110 (Model/Ascii/7) "DSC-P8"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.Model);
				Assert.IsNotNull (entry, "Entry 0x0110 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("DSC-P8", (entry as StringIFDEntry).Value);
			}
			// Image.0x0112 (Orientation/Short/1) "6"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.Orientation);
				Assert.IsNotNull (entry, "Entry 0x0112 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (6, (entry as ShortIFDEntry).Value);
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
			// Image.0x0131 (Software/Ascii/22) "f-spot version 0.1.11"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.Software);
				Assert.IsNotNull (entry, "Entry 0x0131 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("f-spot version 0.1.11", (entry as StringIFDEntry).Value);
			}
			// Image.0x0132 (DateTime/Ascii/20) "2006:06:12 13:30:01"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.DateTime);
				Assert.IsNotNull (entry, "Entry 0x0132 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("2006:06:12 13:30:01", (entry as StringIFDEntry).Value);
			}
			// Image.0x0213 (YCbCrPositioning/Short/1) "2"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.YCbCrPositioning);
				Assert.IsNotNull (entry, "Entry 0x0213 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (2, (entry as ShortIFDEntry).Value);
			}
			// Image.0x8769 (ExifTag/SubIFD/1) "250"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.ExifIFD);
				Assert.IsNotNull (entry, "Entry 0x8769 missing in IFD 0");
				Assert.IsNotNull (entry as SubIFDEntry, "Entry is not a sub IFD!");
			}

			var exif = structure.GetEntry (0, (ushort) IFDEntryTag.ExifIFD) as SubIFDEntry;
			Assert.IsNotNull (exif, "Exif tag not found");
			var exif_structure = exif.Structure;

			// Photo.0x829A (ExposureTime/Rational/1) "10/80"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ExposureTime);
				Assert.IsNotNull (entry, "Entry 0x829A missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (10, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (80, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Photo.0x829D (FNumber/Rational/1) "28/10"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.FNumber);
				Assert.IsNotNull (entry, "Entry 0x829D missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (28, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (10, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Photo.0x8822 (ExposureProgram/Short/1) "2"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ExposureProgram);
				Assert.IsNotNull (entry, "Entry 0x8822 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (2, (entry as ShortIFDEntry).Value);
			}
			// Photo.0x8827 (ISOSpeedRatings/Short/1) "320"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ISOSpeedRatings);
				Assert.IsNotNull (entry, "Entry 0x8827 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (320, (entry as ShortIFDEntry).Value);
			}
			// Photo.0x9000 (ExifVersion/Undefined/4) "48 50 50 48"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ExifVersion);
				Assert.IsNotNull (entry, "Entry 0x9000 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 48, 50, 50, 48 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Photo.0x9003 (DateTimeOriginal/Ascii/20) "2006:06:12 13:28:25"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.DateTimeOriginal);
				Assert.IsNotNull (entry, "Entry 0x9003 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("2006:06:12 13:28:25", (entry as StringIFDEntry).Value);
			}
			// Photo.0x9004 (DateTimeDigitized/Ascii/20) "2006:06:12 13:28:25"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.DateTimeDigitized);
				Assert.IsNotNull (entry, "Entry 0x9004 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("2006:06:12 13:28:25", (entry as StringIFDEntry).Value);
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
			// Photo.0x9204 (ExposureBiasValue/SRational/1) "0/10"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ExposureBiasValue);
				Assert.IsNotNull (entry, "Entry 0x9204 missing in IFD 0");
				Assert.IsNotNull (entry as SRationalIFDEntry, "Entry is not a srational!");
				Assert.AreEqual (0, (entry as SRationalIFDEntry).Value.Numerator);
				Assert.AreEqual (10, (entry as SRationalIFDEntry).Value.Denominator);
			}
			// Photo.0x9205 (MaxApertureValue/Rational/1) "48/16"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.MaxApertureValue);
				Assert.IsNotNull (entry, "Entry 0x9205 missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (48, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (16, (entry as RationalIFDEntry).Value.Denominator);
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
			// Photo.0x920A (FocalLength/Rational/1) "60/10"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.FocalLength);
				Assert.IsNotNull (entry, "Entry 0x920A missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (60, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (10, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Photo.0x927C (MakerNote/MakerNote/1504) "(Value ommitted)"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.MakerNote);
				Assert.IsNotNull (entry, "Entry 0x927C missing in IFD 0");
				Assert.IsNotNull (entry as MakernoteIFDEntry, "Entry is not a makernote IFD!");
			}

			var makernote = exif_structure.GetEntry (0, (ushort) ExifEntryTag.MakerNote) as MakernoteIFDEntry;
			Assert.IsNotNull (makernote, "MakerNote tag not found");
			var makernote_structure = makernote.Structure;

			// Sony.0x9001 (0x9001/Undefined/148) "0 55 0 0 0 8 0 136 147 53 0 194 66 169 0 125 23 0 0 0 0 203 0 0 0 0 0 0 0 203 0 1 91 213 255 0 0 216 0 0 105 4 149 0 255 106 224 125 64 220 14 136 255 74 255 240 0 0 0 74 136 0 136 125 5 74 136 0 136 125 5 74 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 1 0 0 0 0 140"
			{
				// TODO: Unknown IFD tag: Sony / 0x9001
				var entry = makernote_structure.GetEntry (0, (ushort) 0x9001);
				Assert.IsNotNull (entry, "Entry 0x9001 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 0, 55, 0, 0, 0, 8, 0, 136, 147, 53, 0, 194, 66, 169, 0, 125, 23, 0, 0, 0, 0, 203, 0, 0, 0, 0, 0, 0, 0, 203, 0, 1, 91, 213, 255, 0, 0, 216, 0, 0, 105, 4, 149, 0, 255, 106, 224, 125, 64, 220, 14, 136, 255, 74, 255, 240, 0, 0, 0, 74, 136, 0, 136, 125, 5, 74, 136, 0, 136, 125, 5, 74, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 140 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Sony.0x9002 (0x9002/Undefined/200) "2 239 0 140 2 239 0 0 0 125 0 0 0 0 0 0 0 0 0 0 0 0 205 0 0 0 0 0 112 0 112 42 14 172 205 0 0 0 0 0 0 0 0 0 112 143 138 95 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 27 11 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 182 8 92 0 125 0 182 0 0 0 4 0 63 63 0 112 170 112 170 112 42 64 0 0 0 220 220 115 70 0 79"
			{
				// TODO: Unknown IFD tag: Sony / 0x9002
				var entry = makernote_structure.GetEntry (0, (ushort) 0x9002);
				Assert.IsNotNull (entry, "Entry 0x9002 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 2, 239, 0, 140, 2, 239, 0, 0, 0, 125, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 205, 0, 0, 0, 0, 0, 112, 0, 112, 42, 14, 172, 205, 0, 0, 0, 0, 0, 0, 0, 0, 0, 112, 143, 138, 95, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 27, 11, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 182, 8, 92, 0, 125, 0, 182, 0, 0, 0, 4, 0, 63, 63, 0, 112, 170, 112, 170, 112, 42, 64, 0, 0, 0, 220, 220, 115, 70, 0, 79 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Sony.0x9003 (0x9003/Undefined/200) "0 239 0 110 0 91 0 50 0 43 0 219 0 115 57 131 26 208 135 188 26 208 26 177 234 34 125 13 163 57 219 57 219 57 219 7 148 194 0 0 0 0 1 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 255 0 1 217 0 91 0 147 216 8 48 136 0 0 48 136 1 238 1 139 1 1 0 255 231 253 231 168 123 219 60 123 0 0 75 234 187 7 207 27 146 131 2 11 92 82 121 97 7 238 7 133 16 171 1 42 58 253 185 174 154 252 5 92 140 45 40 130 108 189 189 189 189 189 189 189 189 189 189 189 189 189 189 189 189 189 189 189 189 189 189 189 189 189 189 189 189 189 189 189 189 189 189 189 189 189 189 189 189 189 189 189 189 189 189 189 189 189 189 189 189 189 189 189 32 8 27 0 1 220 220 188 95 16 0 0 8 231 8 182"
			{
				// TODO: Unknown IFD tag: Sony / 0x9003
				var entry = makernote_structure.GetEntry (0, (ushort) 0x9003);
				Assert.IsNotNull (entry, "Entry 0x9003 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 0, 239, 0, 110, 0, 91, 0, 50, 0, 43, 0, 219, 0, 115, 57, 131, 26, 208, 135, 188, 26, 208, 26, 177, 234, 34, 125, 13, 163, 57, 219, 57, 219, 57, 219, 7, 148, 194, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 255, 0, 1, 217, 0, 91, 0, 147, 216, 8, 48, 136, 0, 0, 48, 136, 1, 238, 1, 139, 1, 1, 0, 255, 231, 253, 231, 168, 123, 219, 60, 123, 0, 0, 75, 234, 187, 7, 207, 27, 146, 131, 2, 11, 92, 82, 121, 97, 7, 238, 7, 133, 16, 171, 1, 42, 58, 253, 185, 174, 154, 252, 5, 92, 140, 45, 40, 130, 108, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 189, 32, 8, 27, 0, 1, 220, 220, 188, 95, 16, 0, 0, 8, 231, 8, 182 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Sony.0x9004 (0x9004/Undefined/26) "8 234 8 138 0 227 0 150 0 227 0 227 0 0 0 0 0 1 0 0 1 1 0 0 0 0"
			{
				// TODO: Unknown IFD tag: Sony / 0x9004
				var entry = makernote_structure.GetEntry (0, (ushort) 0x9004);
				Assert.IsNotNull (entry, "Entry 0x9004 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 8, 234, 8, 138, 0, 227, 0, 150, 0, 227, 0, 227, 0, 0, 0, 0, 0, 1, 0, 0, 1, 1, 0, 0, 0, 0 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Sony.0x9005 (0x9005/Undefined/120) "0 14 0 0 0 0 0 58 0 0 0 0 0 0 0 216 0 0 0 0 0 27 0 0 0 1 0 1 0 1 0 1 0 0 0 1 0 1 0 8 0 0 0 0 14 4 234 234 136 108 149 129 86 125 8 0 0 0 0 0 0 187 182 0 0 0 0 255 255 0 255 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 64 0 64 0 64 0 0 0 0 0 0"
			{
				// TODO: Unknown IFD tag: Sony / 0x9005
				var entry = makernote_structure.GetEntry (0, (ushort) 0x9005);
				Assert.IsNotNull (entry, "Entry 0x9005 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 0, 14, 0, 0, 0, 0, 0, 58, 0, 0, 0, 0, 0, 0, 0, 216, 0, 0, 0, 0, 0, 27, 0, 0, 0, 1, 0, 1, 0, 1, 0, 1, 0, 0, 0, 1, 0, 1, 0, 8, 0, 0, 0, 0, 14, 4, 234, 234, 136, 108, 149, 129, 86, 125, 8, 0, 0, 0, 0, 0, 0, 187, 182, 0, 0, 0, 0, 255, 255, 0, 255, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 64, 0, 64, 0, 64, 0, 0, 0, 0, 0, 0 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Sony.0x9006 (0x9006/Undefined/252) "236 109 0 0 190 132 0 0 190 70 0 0 190 216 0 0 182 150 0 0 138 148 0 0 32 216 0 0 48 131 0 0 105 103 0 0 105 139 0 0 105 102 0 0 112 88 0 0 112 84 0 0 32 20 0 0 136 243 0 0 105 85 0 0 215 44 0 0 129 198 0 0 136 157 0 0 136 144 0 0 129 124 0 0 182 35 0 0 138 119 0 0 136 3 0 0 136 120 0 0 182 175 0 0 105 81 0 0 48 76 0 0 138 216 0 0 5 106 0 0 182 63 0 0 105 210 0 0 138 128 0 0 182 45 0 0 48 234 0 0 5 236 0 0 205 117 0 0 182 9 0 0 136 149 0 0 112 189 0 0 112 234 0 0 182 54 0 0 205 172 0 0 4 117 0 0 234 174 0 0 138 125 0 0 205 15 0 0 234 99 0 0 234 181 194 127 246 246 246 127 194 194 127 246 246 246 127 194 127 246 238 56 238 246 127 246 238 208 131 208 238 246 246 56 131 74 131 56 246 246 238 208 131 208 238 246 127 246 238 56 238 246 127 0 0 0 0 0 234 93 234 189"
			{
				// TODO: Unknown IFD tag: Sony / 0x9006
				var entry = makernote_structure.GetEntry (0, (ushort) 0x9006);
				Assert.IsNotNull (entry, "Entry 0x9006 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 236, 109, 0, 0, 190, 132, 0, 0, 190, 70, 0, 0, 190, 216, 0, 0, 182, 150, 0, 0, 138, 148, 0, 0, 32, 216, 0, 0, 48, 131, 0, 0, 105, 103, 0, 0, 105, 139, 0, 0, 105, 102, 0, 0, 112, 88, 0, 0, 112, 84, 0, 0, 32, 20, 0, 0, 136, 243, 0, 0, 105, 85, 0, 0, 215, 44, 0, 0, 129, 198, 0, 0, 136, 157, 0, 0, 136, 144, 0, 0, 129, 124, 0, 0, 182, 35, 0, 0, 138, 119, 0, 0, 136, 3, 0, 0, 136, 120, 0, 0, 182, 175, 0, 0, 105, 81, 0, 0, 48, 76, 0, 0, 138, 216, 0, 0, 5, 106, 0, 0, 182, 63, 0, 0, 105, 210, 0, 0, 138, 128, 0, 0, 182, 45, 0, 0, 48, 234, 0, 0, 5, 236, 0, 0, 205, 117, 0, 0, 182, 9, 0, 0, 136, 149, 0, 0, 112, 189, 0, 0, 112, 234, 0, 0, 182, 54, 0, 0, 205, 172, 0, 0, 4, 117, 0, 0, 234, 174, 0, 0, 138, 125, 0, 0, 205, 15, 0, 0, 234, 99, 0, 0, 234, 181, 194, 127, 246, 246, 246, 127, 194, 194, 127, 246, 246, 246, 127, 194, 127, 246, 238, 56, 238, 246, 127, 246, 238, 208, 131, 208, 238, 246, 246, 56, 131, 74, 131, 56, 246, 246, 238, 208, 131, 208, 238, 246, 127, 246, 238, 56, 238, 246, 127, 0, 0, 0, 0, 0, 234, 93, 234, 189 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Sony.0x9007 (0x9007/Undefined/200) "205 208 5 136 5 253 112 229 105 211 136 29 48 131 190 134 0 163 112 161 182 88 105 28 136 132 48 116 215 21 187 90 12 90 236 140 108 109 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 64 183 64 187 27 253 64 234 64 105 64 90 64 53 125 12 125 29 216 13 0 163 64 53 64 144 64 254 125 138 125 196 125 58 216 36 94 36 14 249 4 114 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 14 199 231 0 14 13 14 67 231 26 231 84 231 74 4 26 4 58 86 194 0 163 138 95 138 233 138 107 138 201 112 234 112 172 112 65 112 204 112 133 112 143 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0"
			{
				// TODO: Unknown IFD tag: Sony / 0x9007
				var entry = makernote_structure.GetEntry (0, (ushort) 0x9007);
				Assert.IsNotNull (entry, "Entry 0x9007 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 205, 208, 5, 136, 5, 253, 112, 229, 105, 211, 136, 29, 48, 131, 190, 134, 0, 163, 112, 161, 182, 88, 105, 28, 136, 132, 48, 116, 215, 21, 187, 90, 12, 90, 236, 140, 108, 109, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 64, 183, 64, 187, 27, 253, 64, 234, 64, 105, 64, 90, 64, 53, 125, 12, 125, 29, 216, 13, 0, 163, 64, 53, 64, 144, 64, 254, 125, 138, 125, 196, 125, 58, 216, 36, 94, 36, 14, 249, 4, 114, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 14, 199, 231, 0, 14, 13, 14, 67, 231, 26, 231, 84, 231, 74, 4, 26, 4, 58, 86, 194, 0, 163, 138, 95, 138, 233, 138, 107, 138, 201, 112, 234, 112, 172, 112, 65, 112, 204, 112, 133, 112, 143, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Sony.0x9008 (0x9008/Undefined/200) "0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 2 0 1 0 2 0"
			{
				// TODO: Unknown IFD tag: Sony / 0x9008
				var entry = makernote_structure.GetEntry (0, (ushort) 0x9008);
				Assert.IsNotNull (entry, "Entry 0x9008 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 0, 1, 0, 2, 0 };
				Assert.AreEqual (bytes, parsed_bytes);
			}

			//  ---------- End of IFD tests ----------

		}
	}
}
