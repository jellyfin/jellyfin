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
	public class JpegPanasonicTest
	{
		[Test]
		public void Test ()
		{
			ImageTest.Run ("sample_panasonic.jpg",
				new JpegPanasonicTestInvariantValidator (),
				NoModificationValidator.Instance,
				new CommentModificationValidator (),
				new TagCommentModificationValidator ("", TagTypes.TiffIFD, true),
				// Interestingly, this file contains an empty XMP packet
				new TagCommentModificationValidator (null, TagTypes.XMP, true),
				new TagKeywordsModificationValidator (TagTypes.XMP, true)
			);
		}
	}

	public class JpegPanasonicTestInvariantValidator : IMetadataInvariantValidator
	{
		public void ValidateMetadataInvariants (Image.File file)
		{
			Assert.IsNotNull (file);

			Assert.AreEqual (64, file.Properties.PhotoWidth);
			Assert.AreEqual (40, file.Properties.PhotoHeight);
			Assert.AreEqual (98, file.Properties.PhotoQuality);

			//  ---------- Start of IFD tests ----------

			var tag = file.GetTag (TagTypes.TiffIFD) as IFDTag;
			Assert.IsNotNull (tag, "IFD tag not found");

			var structure = tag.Structure;

			// Image.0x010F (Make/Ascii/10) "Panasonic"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.Make);
				Assert.IsNotNull (entry, "Entry 0x010F missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("Panasonic", (entry as StringIFDEntry).Value);
			}
			// Image.0x0110 (Model/Ascii/9) "DMC-FX35"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.Model);
				Assert.IsNotNull (entry, "Entry 0x0110 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("DMC-FX35", (entry as StringIFDEntry).Value);
			}
			// Image.0x0112 (Orientation/Short/1) "1"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.Orientation);
				Assert.IsNotNull (entry, "Entry 0x0112 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1, (entry as ShortIFDEntry).Value);
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
			// Image.0x0131 (Software/Ascii/11) "GIMP 2.6.6"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.Software);
				Assert.IsNotNull (entry, "Entry 0x0131 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("GIMP 2.6.6", (entry as StringIFDEntry).Value);
			}
			// Image.0x0132 (DateTime/Ascii/20) "2009:10:27 19:45:56"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.DateTime);
				Assert.IsNotNull (entry, "Entry 0x0132 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("2009:10:27 19:45:56", (entry as StringIFDEntry).Value);
			}
			// Image.0x0213 (YCbCrPositioning/Short/1) "2"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.YCbCrPositioning);
				Assert.IsNotNull (entry, "Entry 0x0213 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (2, (entry as ShortIFDEntry).Value);
			}
			// Image.0x8769 (ExifTag/SubIFD/1) "422"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.ExifIFD);
				Assert.IsNotNull (entry, "Entry 0x8769 missing in IFD 0");
				Assert.IsNotNull (entry as SubIFDEntry, "Entry is not a sub IFD!");
			}

			var exif = structure.GetEntry (0, (ushort) IFDEntryTag.ExifIFD) as SubIFDEntry;
			Assert.IsNotNull (exif, "Exif tag not found");
			var exif_structure = exif.Structure;

			// Photo.0x829A (ExposureTime/Rational/1) "10/800"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ExposureTime);
				Assert.IsNotNull (entry, "Entry 0x829A missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (10, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (800, (entry as RationalIFDEntry).Value.Denominator);
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
			// Photo.0x8827 (ISOSpeedRatings/Short/1) "100"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ISOSpeedRatings);
				Assert.IsNotNull (entry, "Entry 0x8827 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (100, (entry as ShortIFDEntry).Value);
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
			// Photo.0x9003 (DateTimeOriginal/Ascii/20) "2009:06:26 12:58:30"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.DateTimeOriginal);
				Assert.IsNotNull (entry, "Entry 0x9003 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("2009:06:26 12:58:30", (entry as StringIFDEntry).Value);
			}
			// Photo.0x9004 (DateTimeDigitized/Ascii/20) "2009:06:26 14:58:30"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.DateTimeDigitized);
				Assert.IsNotNull (entry, "Entry 0x9004 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("2009:06:26 14:58:30", (entry as StringIFDEntry).Value);
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
			// Photo.0x9102 (CompressedBitsPerPixel/Rational/1) "4/1"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.CompressedBitsPerPixel);
				Assert.IsNotNull (entry, "Entry 0x9102 missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (4, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (1, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Photo.0x9204 (ExposureBiasValue/SRational/1) "0/100"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ExposureBiasValue);
				Assert.IsNotNull (entry, "Entry 0x9204 missing in IFD 0");
				Assert.IsNotNull (entry as SRationalIFDEntry, "Entry is not a srational!");
				Assert.AreEqual (0, (entry as SRationalIFDEntry).Value.Numerator);
				Assert.AreEqual (100, (entry as SRationalIFDEntry).Value.Denominator);
			}
			// Photo.0x9205 (MaxApertureValue/Rational/1) "30/10"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.MaxApertureValue);
				Assert.IsNotNull (entry, "Entry 0x9205 missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (30, (entry as RationalIFDEntry).Value.Numerator);
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
			// Photo.0x9209 (Flash/Short/1) "24"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.Flash);
				Assert.IsNotNull (entry, "Entry 0x9209 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (24, (entry as ShortIFDEntry).Value);
			}
			// Photo.0x920A (FocalLength/Rational/1) "44/10"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.FocalLength);
				Assert.IsNotNull (entry, "Entry 0x920A missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (44, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (10, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Photo.0x927C (MakerNote/MakerNote/8964) ""
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.MakerNote);
				Assert.IsNotNull (entry, "Entry 0x927C missing in IFD 0");
				Assert.IsNotNull (entry as MakernoteIFDEntry, "Entry is not a makernote IFD!");
			}

			var makernote = exif_structure.GetEntry (0, (ushort) ExifEntryTag.MakerNote) as MakernoteIFDEntry;
			Assert.IsNotNull (makernote, "MakerNote tag not found");
			var makernote_structure = makernote.Structure;


			Assert.AreEqual (MakernoteType.Panasonic, makernote.MakernoteType);

			// Panasonic.0x0001 (Quality/Short/1) "2"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PanasonicMakerNoteEntryTag.Quality);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (2, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x0002 (FirmwareVersion/Undefined/4) "0 1 0 6 "
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PanasonicMakerNoteEntryTag.FirmwareVersion);
				Assert.IsNotNull (entry, "Entry 0x0002 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var bytes = new byte [] { 0, 1, 0, 6 };
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Panasonic.0x0003 (WhiteBalance/Short/1) "1"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PanasonicMakerNoteEntryTag.WhiteBalance);
				Assert.IsNotNull (entry, "Entry 0x0003 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x0007 (FocusMode/Short/1) "1"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PanasonicMakerNoteEntryTag.FocusMode);
				Assert.IsNotNull (entry, "Entry 0x0007 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x000F (SpotMode/Byte/2) "16 0 "
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PanasonicMakerNoteEntryTag.AFMode);
				Assert.IsNotNull (entry, "Entry 0x000F missing in IFD 0");
				Assert.IsNotNull (entry as ByteVectorIFDEntry, "Entry is not a byte array!");
				var bytes = new byte [] { 16, 0 };
				var parsed_bytes = (entry as ByteVectorIFDEntry).Data.Data;
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Panasonic.0x001A (ImageStabilizer/Short/1) "4"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PanasonicMakerNoteEntryTag.ImageStabilization);
				Assert.IsNotNull (entry, "Entry 0x001A missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (4, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x001C (Macro/Short/1) "2"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PanasonicMakerNoteEntryTag.Macro);
				Assert.IsNotNull (entry, "Entry 0x001C missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (2, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x001F (ShootingMode/Short/1) "1"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PanasonicMakerNoteEntryTag.ShootingMode);
				Assert.IsNotNull (entry, "Entry 0x001F missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x0020 (Audio/Short/1) "2"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PanasonicMakerNoteEntryTag.Audio);
				Assert.IsNotNull (entry, "Entry 0x0020 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (2, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x0021 (DataDump/Undefined/8200) "0 0 250 175 0 0 168 175 172 3 170 175 55 10 200 175 110 1 216 175 47 1 182 175 128 0 184 175 0 0 206 175 141 0 210 175 20 0 208 175 40 0 172 175 8 1 176 175 81 0 174 175 122 0 212 175 58 0 244 175 30 0 242 175 45 0 226 175 0 0 228 175 1 0 224 175 55 10 230 175 1 0 232 175 1 0 234 175 0 0 236 175 0 0 246 175 0 0 218 175 0 0 214 175 0 0 240 175 0 0 4 6 8 6 10 6 55 10 200 6 4 4 154 175 0 0 156 175 0 0 158 175 0 0 240 255 83 84 114 0 164 6 0 0 166 6 0 0 168 6 0 0 170 6 0 0 172 6 0 0 250 7 0 0 174 6 0 0 176 6 0 0 182 6 0 0 184 6 0 0 186 6 0 0 244 7 0 0 246 7 0 0 178 6 0 0 180 6 0 0 176 4 0 0 178 4 0 0 96 169 0 0 98 169 0 0 100 169 0 0 104 169 0 0 102 169 0 0 106 169 0 0 120 169 0 0 122 169 0 0 124 169 0 0 126 169 0 0 240 255 65 69 38 1 32 5 152 2 34 5 80 2 238 7 152 2 36 5 133 5 16 5 147 4 222 6 193 4 38 5 196 0 192 6 0 0 40 5 182 0 24 5 44 1 58 5 7 0 197 25 0 0 80 5 9 2 82 5 124 3 202 6 63 0 0 5 3 0 54 5 0 0 66 5 0 0 8 5 0 0 2 5 0 0 10 5 0 0 4 5 0 0 64 5 0 0 4 7 0 0 12 7 0 0 6 7 0 0 8 7 0 0 10 7 0 0 242 6 0 0 246 6 255 255 244 6 0 0 248 6 255 255 14 5 168 4 42 5 6 0 44 5 44 1 50 5 44 1 250 6 44 1 252 6 0 0 0 7 0 1 59 5 4 0 62 5 0 0 198 25 137 1 200 25 31 3 48 5 128 1 28 5 103 0 232 6 120 47 26 5 214 20 239 6 1 0 240 6 0 0 254 6 0 0 46 5 0 0 1 5 0 0 96 5 0 0 52 5 0 0 14 103 151 2 56 5 0 0 14 7 1 0 16 7 0 0 18 7 102 0 20 7 1 0 22 7 1 0 23 7 0 0 24 7 0 0 72 5 0 0 74 5 0 0 68 5 0 0 70 5 0 0 20 5 83 1 22 5 39 0 60 5 34 1 88 5 18 2 90 5 64 0 240 255 87 66 234 0 0 4 118 6 2 4 103 7 92 4 30 4 4 4 238 0 6 4 6 1 96 4 193 4 26 4 69 0 94 4 74 0 95 4 10 0 18 4 238 0 20 4 6 1 22 4 104 1 24 4 74 1 204 4 159 4 206 4 145 9 208 4 32 7 210 4 236 6 8 4 187 0 10 4 231 0 180 4 226 1 182 4 0 0 64 4 238 0 48 4 95 0 56 4 119 0 50 4 127 0 58 4 138 0 52 4 230 255 60 4 9 0 54 4 17 0 62 4 40 0 76 4 231 0 78 4 6 1 192 4 231 0 194 4 6 1 234 4 0 0 128 5 74 0 130 5 115 0 131 5 131 0 140 5 0 0 142 5 133 0 143 5 141 0 82 4 207 0 84 4 242 0 86 4 207 0 88 4 242 0 212 4 226 0 214 4 8 1 216 4 207 0 218 4 242 0 240 4 125 4 242 4 15 9 244 4 167 0 246 4 191 0 248 4 0 0 250 4 0 0 252 4 0 0 254 4 0 0 240 255 89 67 230 0 78 170 5 0 80 170 5 0 82 170 5 0 84 170 5 0 68 170 136 136 70 170 221 221 72 170 136 136 74 170 0 0 76 170 0 0 56 170 48 0 58 170 48 0 60 170 48 0 62 170 48 0 46 170 136 136 48 170 204 136 50 170 119 102 52 170 34 17 54 170 0 0 130 4 0 0 128 4 9 0 132 4 0 0 132 170 96 0 96 170 138 138 98 170 138 138 100 170 88 113 102 170 88 113 104 170 93 93 106 170 2 0 108 170 0 0 110 170 0 0 134 170 0 0 136 170 0 0 138 170 8 0 140 170 8 0 142 170 0 0 144 170 0 0 146 170 31 0 148 170 31 0 150 170 0 0 152 170 0 0 160 170 240 0 162 170 16 0 164 170 232 0 166 170 0 0 168 170 10 0 88 170 1 0 90 170 24 0 92 170 24 0 94 170 32 0 154 170 0 0 156 170 0 0 192 170 255 255 194 170 255 255 196 170 255 255 198 170 255 255 200 170 255 255 240 255 67 77 14 0 252 5 0 48 4 172 0 0 240 255 68 83 46 0 0 175 0 0 2 175 0 0 10 175 0 0 4 175 0 0 6 175 0 0 8 175 0 0 12 175 0 0 14 175 0 0 16 175 0 0 18 175 0 0 240 255 73 83 202 0 136 174 0 0 180 174 209 0 182 174 237 0 184 174 154 1 186 174 154 1 128 174 61 4 130 174 22 4 132 174 231 3 134 174 231 3 0 174 56 4 2 174 54 4 4 174 52 4 6 174 52 4 8 174 48 4 10 174 48 4 12 174 50 4 14 174 44 4 16 174 40 4 18 174 50 4 20 174 50 4 22 174 46 4 24 174 48 4 26 174 50 4 28 174 48 4 30 174 40 4 32 174 24 4 34 174 30 4 36 174 24 4 38 174 30 4 40 174 26 4 42 174 32 4 44 174 28 4 46 174 22 4 48 174 6 4 50 174 24 4 52 174 14 4 54 174 18 4 56 174 14 4 58 174 2 4 60 174 10 4 62 174 0 4 64 174 29 2 66 174 0 0 68 174 0 0 70 174 0 0 96 174 254 1 98 174 0 0 100 174 0 0 102 174 0 0 240 255 70 68 166 0 96 172 0 0 98 172 0 0 128 172 0 0 130 172 0 0 132 172 0 0 134 172 0 0 136 172 0 0 138 172 0 0 140 172 0 0 142 172 0 0 144 172 0 0 146 172 0 0 148 172 0 0 150 172 0 0 152 172 0 0 154 172 0 0 156 172 0 0 158 172 0 0 64 172 0 0 66 172 0 0 68 172 0 0 70 172 0 0 72 172 0 0 74 172 0 0 76 172 0 0 78 172 0 0 80 172 0 0 82 172 0 0 84 172 0 0 86 172 0 0 88 172 0 0 90 172 0 0 92 172 0 0 94 172 0 0 102 5 0 0 110 5 0 0 112 5 0 0 114 5 0 0 108 5 0 0 100 5 0 0 240 255 73 65 58 0 160 169 255 255 162 169 255 255 164 169 255 255 166 169 255 255 168 169 255 255 170 169 255 255 172 169 255 255 174 169 255 255 128 169 0 0 130 169 0 0 132 169 0 0 136 169 0 0 134 169 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 65 69 66 77 248 1 65 2 103 2 173 2 253 2 179 2 159 2 130 2 56 2 12 2 198 1 151 1 82 1 26 1 246 0 174 0 78 2 155 2 169 2 221 2 94 4 4 3 134 2 80 2 22 2 239 1 194 1 144 1 92 1 39 1 250 0 197 0 147 2 180 2 96 4 149 4 162 4 199 5 43 4 101 3 57 2 16 4 30 3 161 2 51 2 197 1 238 0 194 0 186 2 13 3 143 4 2 7 169 6 105 6 153 6 195 3 47 2 80 4 253 2 48 1 4 1 2 1 175 0 169 0 229 2 51 3 226 4 171 7 37 7 202 6 117 6 6 3 212 1 105 2 118 1 39 1 185 1 105 1 132 0 125 0 1 3 54 3 189 6 139 7 236 6 188 5 180 3 78 1 15 1 16 1 148 1 80 2 216 1 91 1 99 0 87 0 12 3 90 3 151 7 126 7 106 7 142 3 170 0 244 0 102 0 14 1 181 3 136 3 90 2 207 0 67 0 69 0 6 3 106 3 202 7 142 7 85 7 5 5 157 0 182 0 134 0 26 1 183 2 62 3 8 1 245 0 76 0 57 0 249 2 81 3 66 3 242 2 160 2 29 2 6 1 243 0 139 0 89 1 43 2 16 1 150 0 143 0 52 0 53 0 193 2 41 3 148 4 4 4 178 3 154 2 7 3 104 3 156 0 179 0 6 2 55 2 60 2 83 2 111 0 52 0 175 2 15 3 143 5 135 5 37 3 243 3 178 4 182 2 191 0 164 0 132 1 109 1 156 1 58 1 90 0 50 0 137 2 221 2 58 5 225 4 77 4 208 4 237 4 246 1 183 0 122 0 166 1 173 1 133 1 59 1 79 0 49 0 91 2 156 2 188 4 245 3 33 5 39 5 36 5 196 2 200 0 176 0 161 1 116 2 47 2 23 2 102 0 91 0 33 2 108 2 8 4 218 2 196 4 122 4 172 4 160 2 138 1 186 0 196 1 253 2 202 2 112 2 168 0 148 0 252 1 0 2 220 2 125 2 200 3 118 3 191 3 74 2 127 2 126 0 11 2 134 2 64 2 231 1 207 0 170 0 172 1 222 1 249 1 237 1 16 2 8 2 207 1 179 1 156 1 88 1 65 1 69 1 32 1 0 1 214 0 135 0 80 82 83 84 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 70 67 67 86 8 0 1 0 55 10 55 10 242 9 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 10 12 70 7 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 1 0 136 7 30 1 112 2 163 1 25 1 218 1 46 1 214 1 239 0 0 0 177 0 64 2 254 5 0 2 81 1 140 2 124 1 151 2 0 2 0 0 73 1 187 1 0 0 0 0 39 0 10 0 0 0 0 0 8 8 62 1 238 2 41 2 77 1 102 2 104 1 107 2 249 0 0 0 194 0 144 2 245 6 75 2 122 1 228 2 170 1 206 2 78 2 0 0 117 1 236 1 0 0 0 0 36 0 7 0 0 0 0 0 136 8 172 1 92 4 199 2 154 1 234 2 156 1 231 2 33 1 0 0 240 0 246 2 12 8 134 2 162 1 33 3 209 1 252 2 131 2 0 0 171 1 36 2 0 0 0 0 37 0 10 0 0 0 0 0 8 9 103 2 149 6 64 3 244 1 83 3 212 1 51 3 82 1 0 0 70 1 94 3 9 9 166 2 189 1 69 3 222 1 32 3 160 2 0 0 222 1 79 2 0 0 0 0 38 0 9 0 0 0 0 0 136 9 22 3 89 8 56 3 3 2 92 3 228 1 60 3 119 1 0 0 170 1 178 3 212 9 174 2 198 1 70 3 233 1 39 3 173 2 0 0 8 2 110 2 0 0 0 0 38 0 9 0 0 0 0 0 8 10 172 3 41 9 220 2 232 1 233 2 184 1 232 2 115 1 0 0 8 2 232 3 51 10 138 2 184 1 22 3 211 1 12 3 168 2 0 0 41 2 136 2 0 0 0 0 38 0 9 0 0 0 0 0 136 10 139 3 2 8 90 2 147 1 102 2 122 1 96 2 71 1 0 0 249 1 219 3 226 9 72 2 153 1 205 2 185 1 214 2 139 2 0 0 36 2 136 2 0 0 0 0 40 0 10 0 0 0 0 0 8 11 214 2 5 6 205 1 87 1 222 1 73 1 209 1 35 1 0 0 148 1 156 3 34 9 0 2 105 1 113 2 140 1 132 2 101 2 0 0 8 2 104 2 0 0 0 0 40 0 11 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 87 66 67 90 3 12 3 12 12 12 12 12 12 12 12 12 12 12 12 12 3 12 12 12 9 1 12 12 12 12 12 12 12 12 12 12 12 12 3 1 9 1 1 10 9 3 1 9 9 9 12 12 12 12 3 1 1 1 1 9 9 1 9 14 14 0 10 12 12 12 3 1 1 1 9 9 14 9 9 10 3 12 12 3 12 12 1 1 1 9 9 14 0 10 4 9 10 12 10 3 12 12 1 1 1 9 0 0 0 10 10 9 9 3 10 4 12 12 1 1 1 9 0 0 0 4 4 9 4 4 10 4 12 12 9 14 14 14 14 0 0 4 11 4 10 10 10 4 12 12 10 10 10 9 9 10 0 0 4 11 4 12 10 4 12 12 1 1 11 3 9 10 0 0 4 4 4 4 12 4 12 12 1 1 3 3 10 10 0 0 4 4 10 3 3 4 12 12 1 1 1 1 1 9 0 10 10 4 11 12 12 4 12 12 1 1 1 1 1 9 10 14 10 1 3 3 12 3 12 12 10 10 1 1 1 9 10 14 10 1 1 1 12 12 12 12 12 12 12 3 3 10 12 10 10 3 3 12 12 12 66 77 72 76 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 2 0 1 0 0 0 0 0 0 0 0 0 0 0 0 0 0 4 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 70 49 53 48 56 48 50 49 57 48 54 48 49 0 48 44 57 57 57 57 58 57 57 58 57 57 32 48 48 58 48 48 58 48 48 0 127 0 0 0 0 1 0 0 127 0 0 0 0 1 0 0 0 0 0 0 0 0 0 0 0 0 "
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PanasonicMakerNoteEntryTag.DataDump);
				Assert.IsNotNull (entry, "Entry 0x0021 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var bytes = new byte [] { 0, 0, 250, 175, 0, 0, 168, 175, 172, 3, 170, 175, 55, 10, 200, 175, 110, 1, 216, 175, 47, 1, 182, 175, 128, 0, 184, 175, 0, 0, 206, 175, 141, 0, 210, 175, 20, 0, 208, 175, 40, 0, 172, 175, 8, 1, 176, 175, 81, 0, 174, 175, 122, 0, 212, 175, 58, 0, 244, 175, 30, 0, 242, 175, 45, 0, 226, 175, 0, 0, 228, 175, 1, 0, 224, 175, 55, 10, 230, 175, 1, 0, 232, 175, 1, 0, 234, 175, 0, 0, 236, 175, 0, 0, 246, 175, 0, 0, 218, 175, 0, 0, 214, 175, 0, 0, 240, 175, 0, 0, 4, 6, 8, 6, 10, 6, 55, 10, 200, 6, 4, 4, 154, 175, 0, 0, 156, 175, 0, 0, 158, 175, 0, 0, 240, 255, 83, 84, 114, 0, 164, 6, 0, 0, 166, 6, 0, 0, 168, 6, 0, 0, 170, 6, 0, 0, 172, 6, 0, 0, 250, 7, 0, 0, 174, 6, 0, 0, 176, 6, 0, 0, 182, 6, 0, 0, 184, 6, 0, 0, 186, 6, 0, 0, 244, 7, 0, 0, 246, 7, 0, 0, 178, 6, 0, 0, 180, 6, 0, 0, 176, 4, 0, 0, 178, 4, 0, 0, 96, 169, 0, 0, 98, 169, 0, 0, 100, 169, 0, 0, 104, 169, 0, 0, 102, 169, 0, 0, 106, 169, 0, 0, 120, 169, 0, 0, 122, 169, 0, 0, 124, 169, 0, 0, 126, 169, 0, 0, 240, 255, 65, 69, 38, 1, 32, 5, 152, 2, 34, 5, 80, 2, 238, 7, 152, 2, 36, 5, 133, 5, 16, 5, 147, 4, 222, 6, 193, 4, 38, 5, 196, 0, 192, 6, 0, 0, 40, 5, 182, 0, 24, 5, 44, 1, 58, 5, 7, 0, 197, 25, 0, 0, 80, 5, 9, 2, 82, 5, 124, 3, 202, 6, 63, 0, 0, 5, 3, 0, 54, 5, 0, 0, 66, 5, 0, 0, 8, 5, 0, 0, 2, 5, 0, 0, 10, 5, 0, 0, 4, 5, 0, 0, 64, 5, 0, 0, 4, 7, 0, 0, 12, 7, 0, 0, 6, 7, 0, 0, 8, 7, 0, 0, 10, 7, 0, 0, 242, 6, 0, 0, 246, 6, 255, 255, 244, 6, 0, 0, 248, 6, 255, 255, 14, 5, 168, 4, 42, 5, 6, 0, 44, 5, 44, 1, 50, 5, 44, 1, 250, 6, 44, 1, 252, 6, 0, 0, 0, 7, 0, 1, 59, 5, 4, 0, 62, 5, 0, 0, 198, 25, 137, 1, 200, 25, 31, 3, 48, 5, 128, 1, 28, 5, 103, 0, 232, 6, 120, 47, 26, 5, 214, 20, 239, 6, 1, 0, 240, 6, 0, 0, 254, 6, 0, 0, 46, 5, 0, 0, 1, 5, 0, 0, 96, 5, 0, 0, 52, 5, 0, 0, 14, 103, 151, 2, 56, 5, 0, 0, 14, 7, 1, 0, 16, 7, 0, 0, 18, 7, 102, 0, 20, 7, 1, 0, 22, 7, 1, 0, 23, 7, 0, 0, 24, 7, 0, 0, 72, 5, 0, 0, 74, 5, 0, 0, 68, 5, 0, 0, 70, 5, 0, 0, 20, 5, 83, 1, 22, 5, 39, 0, 60, 5, 34, 1, 88, 5, 18, 2, 90, 5, 64, 0, 240, 255, 87, 66, 234, 0, 0, 4, 118, 6, 2, 4, 103, 7, 92, 4, 30, 4, 4, 4, 238, 0, 6, 4, 6, 1, 96, 4, 193, 4, 26, 4, 69, 0, 94, 4, 74, 0, 95, 4, 10, 0, 18, 4, 238, 0, 20, 4, 6, 1, 22, 4, 104, 1, 24, 4, 74, 1, 204, 4, 159, 4, 206, 4, 145, 9, 208, 4, 32, 7, 210, 4, 236, 6, 8, 4, 187, 0, 10, 4, 231, 0, 180, 4, 226, 1, 182, 4, 0, 0, 64, 4, 238, 0, 48, 4, 95, 0, 56, 4, 119, 0, 50, 4, 127, 0, 58, 4, 138, 0, 52, 4, 230, 255, 60, 4, 9, 0, 54, 4, 17, 0, 62, 4, 40, 0, 76, 4, 231, 0, 78, 4, 6, 1, 192, 4, 231, 0, 194, 4, 6, 1, 234, 4, 0, 0, 128, 5, 74, 0, 130, 5, 115, 0, 131, 5, 131, 0, 140, 5, 0, 0, 142, 5, 133, 0, 143, 5, 141, 0, 82, 4, 207, 0, 84, 4, 242, 0, 86, 4, 207, 0, 88, 4, 242, 0, 212, 4, 226, 0, 214, 4, 8, 1, 216, 4, 207, 0, 218, 4, 242, 0, 240, 4, 125, 4, 242, 4, 15, 9, 244, 4, 167, 0, 246, 4, 191, 0, 248, 4, 0, 0, 250, 4, 0, 0, 252, 4, 0, 0, 254, 4, 0, 0, 240, 255, 89, 67, 230, 0, 78, 170, 5, 0, 80, 170, 5, 0, 82, 170, 5, 0, 84, 170, 5, 0, 68, 170, 136, 136, 70, 170, 221, 221, 72, 170, 136, 136, 74, 170, 0, 0, 76, 170, 0, 0, 56, 170, 48, 0, 58, 170, 48, 0, 60, 170, 48, 0, 62, 170, 48, 0, 46, 170, 136, 136, 48, 170, 204, 136, 50, 170, 119, 102, 52, 170, 34, 17, 54, 170, 0, 0, 130, 4, 0, 0, 128, 4, 9, 0, 132, 4, 0, 0, 132, 170, 96, 0, 96, 170, 138, 138, 98, 170, 138, 138, 100, 170, 88, 113, 102, 170, 88, 113, 104, 170, 93, 93, 106, 170, 2, 0, 108, 170, 0, 0, 110, 170, 0, 0, 134, 170, 0, 0, 136, 170, 0, 0, 138, 170, 8, 0, 140, 170, 8, 0, 142, 170, 0, 0, 144, 170, 0, 0, 146, 170, 31, 0, 148, 170, 31, 0, 150, 170, 0, 0, 152, 170, 0, 0, 160, 170, 240, 0, 162, 170, 16, 0, 164, 170, 232, 0, 166, 170, 0, 0, 168, 170, 10, 0, 88, 170, 1, 0, 90, 170, 24, 0, 92, 170, 24, 0, 94, 170, 32, 0, 154, 170, 0, 0, 156, 170, 0, 0, 192, 170, 255, 255, 194, 170, 255, 255, 196, 170, 255, 255, 198, 170, 255, 255, 200, 170, 255, 255, 240, 255, 67, 77, 14, 0, 252, 5, 0, 48, 4, 172, 0, 0, 240, 255, 68, 83, 46, 0, 0, 175, 0, 0, 2, 175, 0, 0, 10, 175, 0, 0, 4, 175, 0, 0, 6, 175, 0, 0, 8, 175, 0, 0, 12, 175, 0, 0, 14, 175, 0, 0, 16, 175, 0, 0, 18, 175, 0, 0, 240, 255, 73, 83, 202, 0, 136, 174, 0, 0, 180, 174, 209, 0, 182, 174, 237, 0, 184, 174, 154, 1, 186, 174, 154, 1, 128, 174, 61, 4, 130, 174, 22, 4, 132, 174, 231, 3, 134, 174, 231, 3, 0, 174, 56, 4, 2, 174, 54, 4, 4, 174, 52, 4, 6, 174, 52, 4, 8, 174, 48, 4, 10, 174, 48, 4, 12, 174, 50, 4, 14, 174, 44, 4, 16, 174, 40, 4, 18, 174, 50, 4, 20, 174, 50, 4, 22, 174, 46, 4, 24, 174, 48, 4, 26, 174, 50, 4, 28, 174, 48, 4, 30, 174, 40, 4, 32, 174, 24, 4, 34, 174, 30, 4, 36, 174, 24, 4, 38, 174, 30, 4, 40, 174, 26, 4, 42, 174, 32, 4, 44, 174, 28, 4, 46, 174, 22, 4, 48, 174, 6, 4, 50, 174, 24, 4, 52, 174, 14, 4, 54, 174, 18, 4, 56, 174, 14, 4, 58, 174, 2, 4, 60, 174, 10, 4, 62, 174, 0, 4, 64, 174, 29, 2, 66, 174, 0, 0, 68, 174, 0, 0, 70, 174, 0, 0, 96, 174, 254, 1, 98, 174, 0, 0, 100, 174, 0, 0, 102, 174, 0, 0, 240, 255, 70, 68, 166, 0, 96, 172, 0, 0, 98, 172, 0, 0, 128, 172, 0, 0, 130, 172, 0, 0, 132, 172, 0, 0, 134, 172, 0, 0, 136, 172, 0, 0, 138, 172, 0, 0, 140, 172, 0, 0, 142, 172, 0, 0, 144, 172, 0, 0, 146, 172, 0, 0, 148, 172, 0, 0, 150, 172, 0, 0, 152, 172, 0, 0, 154, 172, 0, 0, 156, 172, 0, 0, 158, 172, 0, 0, 64, 172, 0, 0, 66, 172, 0, 0, 68, 172, 0, 0, 70, 172, 0, 0, 72, 172, 0, 0, 74, 172, 0, 0, 76, 172, 0, 0, 78, 172, 0, 0, 80, 172, 0, 0, 82, 172, 0, 0, 84, 172, 0, 0, 86, 172, 0, 0, 88, 172, 0, 0, 90, 172, 0, 0, 92, 172, 0, 0, 94, 172, 0, 0, 102, 5, 0, 0, 110, 5, 0, 0, 112, 5, 0, 0, 114, 5, 0, 0, 108, 5, 0, 0, 100, 5, 0, 0, 240, 255, 73, 65, 58, 0, 160, 169, 255, 255, 162, 169, 255, 255, 164, 169, 255, 255, 166, 169, 255, 255, 168, 169, 255, 255, 170, 169, 255, 255, 172, 169, 255, 255, 174, 169, 255, 255, 128, 169, 0, 0, 130, 169, 0, 0, 132, 169, 0, 0, 136, 169, 0, 0, 134, 169, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 65, 69, 66, 77, 248, 1, 65, 2, 103, 2, 173, 2, 253, 2, 179, 2, 159, 2, 130, 2, 56, 2, 12, 2, 198, 1, 151, 1, 82, 1, 26, 1, 246, 0, 174, 0, 78, 2, 155, 2, 169, 2, 221, 2, 94, 4, 4, 3, 134, 2, 80, 2, 22, 2, 239, 1, 194, 1, 144, 1, 92, 1, 39, 1, 250, 0, 197, 0, 147, 2, 180, 2, 96, 4, 149, 4, 162, 4, 199, 5, 43, 4, 101, 3, 57, 2, 16, 4, 30, 3, 161, 2, 51, 2, 197, 1, 238, 0, 194, 0, 186, 2, 13, 3, 143, 4, 2, 7, 169, 6, 105, 6, 153, 6, 195, 3, 47, 2, 80, 4, 253, 2, 48, 1, 4, 1, 2, 1, 175, 0, 169, 0, 229, 2, 51, 3, 226, 4, 171, 7, 37, 7, 202, 6, 117, 6, 6, 3, 212, 1, 105, 2, 118, 1, 39, 1, 185, 1, 105, 1, 132, 0, 125, 0, 1, 3, 54, 3, 189, 6, 139, 7, 236, 6, 188, 5, 180, 3, 78, 1, 15, 1, 16, 1, 148, 1, 80, 2, 216, 1, 91, 1, 99, 0, 87, 0, 12, 3, 90, 3, 151, 7, 126, 7, 106, 7, 142, 3, 170, 0, 244, 0, 102, 0, 14, 1, 181, 3, 136, 3, 90, 2, 207, 0, 67, 0, 69, 0, 6, 3, 106, 3, 202, 7, 142, 7, 85, 7, 5, 5, 157, 0, 182, 0, 134, 0, 26, 1, 183, 2, 62, 3, 8, 1, 245, 0, 76, 0, 57, 0, 249, 2, 81, 3, 66, 3, 242, 2, 160, 2, 29, 2, 6, 1, 243, 0, 139, 0, 89, 1, 43, 2, 16, 1, 150, 0, 143, 0, 52, 0, 53, 0, 193, 2, 41, 3, 148, 4, 4, 4, 178, 3, 154, 2, 7, 3, 104, 3, 156, 0, 179, 0, 6, 2, 55, 2, 60, 2, 83, 2, 111, 0, 52, 0, 175, 2, 15, 3, 143, 5, 135, 5, 37, 3, 243, 3, 178, 4, 182, 2, 191, 0, 164, 0, 132, 1, 109, 1, 156, 1, 58, 1, 90, 0, 50, 0, 137, 2, 221, 2, 58, 5, 225, 4, 77, 4, 208, 4, 237, 4, 246, 1, 183, 0, 122, 0, 166, 1, 173, 1, 133, 1, 59, 1, 79, 0, 49, 0, 91, 2, 156, 2, 188, 4, 245, 3, 33, 5, 39, 5, 36, 5, 196, 2, 200, 0, 176, 0, 161, 1, 116, 2, 47, 2, 23, 2, 102, 0, 91, 0, 33, 2, 108, 2, 8, 4, 218, 2, 196, 4, 122, 4, 172, 4, 160, 2, 138, 1, 186, 0, 196, 1, 253, 2, 202, 2, 112, 2, 168, 0, 148, 0, 252, 1, 0, 2, 220, 2, 125, 2, 200, 3, 118, 3, 191, 3, 74, 2, 127, 2, 126, 0, 11, 2, 134, 2, 64, 2, 231, 1, 207, 0, 170, 0, 172, 1, 222, 1, 249, 1, 237, 1, 16, 2, 8, 2, 207, 1, 179, 1, 156, 1, 88, 1, 65, 1, 69, 1, 32, 1, 0, 1, 214, 0, 135, 0, 80, 82, 83, 84, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 70, 67, 67, 86, 8, 0, 1, 0, 55, 10, 55, 10, 242, 9, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 10, 12, 70, 7, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 136, 7, 30, 1, 112, 2, 163, 1, 25, 1, 218, 1, 46, 1, 214, 1, 239, 0, 0, 0, 177, 0, 64, 2, 254, 5, 0, 2, 81, 1, 140, 2, 124, 1, 151, 2, 0, 2, 0, 0, 73, 1, 187, 1, 0, 0, 0, 0, 39, 0, 10, 0, 0, 0, 0, 0, 8, 8, 62, 1, 238, 2, 41, 2, 77, 1, 102, 2, 104, 1, 107, 2, 249, 0, 0, 0, 194, 0, 144, 2, 245, 6, 75, 2, 122, 1, 228, 2, 170, 1, 206, 2, 78, 2, 0, 0, 117, 1, 236, 1, 0, 0, 0, 0, 36, 0, 7, 0, 0, 0, 0, 0, 136, 8, 172, 1, 92, 4, 199, 2, 154, 1, 234, 2, 156, 1, 231, 2, 33, 1, 0, 0, 240, 0, 246, 2, 12, 8, 134, 2, 162, 1, 33, 3, 209, 1, 252, 2, 131, 2, 0, 0, 171, 1, 36, 2, 0, 0, 0, 0, 37, 0, 10, 0, 0, 0, 0, 0, 8, 9, 103, 2, 149, 6, 64, 3, 244, 1, 83, 3, 212, 1, 51, 3, 82, 1, 0, 0, 70, 1, 94, 3, 9, 9, 166, 2, 189, 1, 69, 3, 222, 1, 32, 3, 160, 2, 0, 0, 222, 1, 79, 2, 0, 0, 0, 0, 38, 0, 9, 0, 0, 0, 0, 0, 136, 9, 22, 3, 89, 8, 56, 3, 3, 2, 92, 3, 228, 1, 60, 3, 119, 1, 0, 0, 170, 1, 178, 3, 212, 9, 174, 2, 198, 1, 70, 3, 233, 1, 39, 3, 173, 2, 0, 0, 8, 2, 110, 2, 0, 0, 0, 0, 38, 0, 9, 0, 0, 0, 0, 0, 8, 10, 172, 3, 41, 9, 220, 2, 232, 1, 233, 2, 184, 1, 232, 2, 115, 1, 0, 0, 8, 2, 232, 3, 51, 10, 138, 2, 184, 1, 22, 3, 211, 1, 12, 3, 168, 2, 0, 0, 41, 2, 136, 2, 0, 0, 0, 0, 38, 0, 9, 0, 0, 0, 0, 0, 136, 10, 139, 3, 2, 8, 90, 2, 147, 1, 102, 2, 122, 1, 96, 2, 71, 1, 0, 0, 249, 1, 219, 3, 226, 9, 72, 2, 153, 1, 205, 2, 185, 1, 214, 2, 139, 2, 0, 0, 36, 2, 136, 2, 0, 0, 0, 0, 40, 0, 10, 0, 0, 0, 0, 0, 8, 11, 214, 2, 5, 6, 205, 1, 87, 1, 222, 1, 73, 1, 209, 1, 35, 1, 0, 0, 148, 1, 156, 3, 34, 9, 0, 2, 105, 1, 113, 2, 140, 1, 132, 2, 101, 2, 0, 0, 8, 2, 104, 2, 0, 0, 0, 0, 40, 0, 11, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 87, 66, 67, 90, 3, 12, 3, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 3, 12, 12, 12, 9, 1, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 3, 1, 9, 1, 1, 10, 9, 3, 1, 9, 9, 9, 12, 12, 12, 12, 3, 1, 1, 1, 1, 9, 9, 1, 9, 14, 14, 0, 10, 12, 12, 12, 3, 1, 1, 1, 9, 9, 14, 9, 9, 10, 3, 12, 12, 3, 12, 12, 1, 1, 1, 9, 9, 14, 0, 10, 4, 9, 10, 12, 10, 3, 12, 12, 1, 1, 1, 9, 0, 0, 0, 10, 10, 9, 9, 3, 10, 4, 12, 12, 1, 1, 1, 9, 0, 0, 0, 4, 4, 9, 4, 4, 10, 4, 12, 12, 9, 14, 14, 14, 14, 0, 0, 4, 11, 4, 10, 10, 10, 4, 12, 12, 10, 10, 10, 9, 9, 10, 0, 0, 4, 11, 4, 12, 10, 4, 12, 12, 1, 1, 11, 3, 9, 10, 0, 0, 4, 4, 4, 4, 12, 4, 12, 12, 1, 1, 3, 3, 10, 10, 0, 0, 4, 4, 10, 3, 3, 4, 12, 12, 1, 1, 1, 1, 1, 9, 0, 10, 10, 4, 11, 12, 12, 4, 12, 12, 1, 1, 1, 1, 1, 9, 10, 14, 10, 1, 3, 3, 12, 3, 12, 12, 10, 10, 1, 1, 1, 9, 10, 14, 10, 1, 1, 1, 12, 12, 12, 12, 12, 12, 12, 3, 3, 10, 12, 10, 10, 3, 3, 12, 12, 12, 66, 77, 72, 76, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 70, 49, 53, 48, 56, 48, 50, 49, 57, 48, 54, 48, 49, 0, 48, 44, 57, 57, 57, 57, 58, 57, 57, 58, 57, 57, 32, 48, 48, 58, 48, 48, 58, 48, 48, 0, 127, 0, 0, 0, 0, 1, 0, 0, 127, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Panasonic.0x0022 (0x0022/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PanasonicMakerNoteEntryTag.Unknown34);
				Assert.IsNotNull (entry, "Entry 0x0022 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x0023 (WhiteBalanceBias/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PanasonicMakerNoteEntryTag.WhiteBalanceBias);
				Assert.IsNotNull (entry, "Entry 0x0023 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x0024 (FlashBias/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PanasonicMakerNoteEntryTag.FlashBias);
				Assert.IsNotNull (entry, "Entry 0x0024 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x0025 (SerialNumber/Undefined/16) "0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 "
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PanasonicMakerNoteEntryTag.InternalSerialNumber);
				Assert.IsNotNull (entry, "Entry 0x0025 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var bytes = new byte [] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Panasonic.0x0026 (0x0026/Undefined/4) "48 50 54 48 "
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PanasonicMakerNoteEntryTag.ExifVersion);
				Assert.IsNotNull (entry, "Entry 0x0026 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var bytes = new byte [] { 48, 50, 54, 48 };
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Panasonic.0x0027 (0x0027/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PanasonicMakerNoteEntryTag.Unknown39);
				Assert.IsNotNull (entry, "Entry 0x0027 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x0028 (ColorEffect/Short/1) "1"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PanasonicMakerNoteEntryTag.ColorEffect);
				Assert.IsNotNull (entry, "Entry 0x0028 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x0029 (0x0029/Long/1) "2286"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PanasonicMakerNoteEntryTag.TimeSincePowerOn);
				Assert.IsNotNull (entry, "Entry 0x0029 missing in IFD 0");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (2286, (entry as LongIFDEntry).Value);
			}
			// Panasonic.0x002A (BurstMode/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PanasonicMakerNoteEntryTag.BurstMode);
				Assert.IsNotNull (entry, "Entry 0x002A missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x002B (SequenceNumber/Long/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PanasonicMakerNoteEntryTag.SequenceNumber);
				Assert.IsNotNull (entry, "Entry 0x002B missing in IFD 0");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (0, (entry as LongIFDEntry).Value);
			}
			// Panasonic.0x002C (Contrast/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PanasonicMakerNoteEntryTag.Contrast);
				Assert.IsNotNull (entry, "Entry 0x002C missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x002D (NoiseReduction/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PanasonicMakerNoteEntryTag.NoiseReduction);
				Assert.IsNotNull (entry, "Entry 0x002D missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x002E (SelfTimer/Short/1) "1"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PanasonicMakerNoteEntryTag.SelfTimer);
				Assert.IsNotNull (entry, "Entry 0x002E missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x002F (0x002f/Short/1) "1"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PanasonicMakerNoteEntryTag.Unknown47);
				Assert.IsNotNull (entry, "Entry 0x002F missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x0030 (Rotation/Short/1) "1"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PanasonicMakerNoteEntryTag.Rotation);
				Assert.IsNotNull (entry, "Entry 0x0030 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x0031 (0x0031/Short/1) "2"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PanasonicMakerNoteEntryTag.Unknown49);
				Assert.IsNotNull (entry, "Entry 0x0031 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (2, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x0032 (ColorMode/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PanasonicMakerNoteEntryTag.ColorMode);
				Assert.IsNotNull (entry, "Entry 0x0032 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x0033 (0x0033/Ascii/20) ""
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PanasonicMakerNoteEntryTag.BabyAge);
				Assert.IsNotNull (entry, "Entry 0x0033 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("", (entry as StringIFDEntry).Value.Trim ());
			}
			// Panasonic.0x0034 (0x0034/Short/1) "1"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PanasonicMakerNoteEntryTag.OpticalZoomMode);
				Assert.IsNotNull (entry, "Entry 0x0034 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x0035 (0x0035/Short/1) "1"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PanasonicMakerNoteEntryTag.ConversionLens);
				Assert.IsNotNull (entry, "Entry 0x0035 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x0036 (TravelDay/Short/1) "65535"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PanasonicMakerNoteEntryTag.TravelDay);
				Assert.IsNotNull (entry, "Entry 0x0036 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (65535, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x0037 (0x0037/Short/1) "257"
			{
				// TODO: Unknown IFD tag: Panasonic / 0x0037
				var entry = makernote_structure.GetEntry (0, (ushort) 0x0037);
				Assert.IsNotNull (entry, "Entry 0x0037 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (257, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x0038 (0x0038/Short/1) "1"
			{
				// TODO: Unknown IFD tag: Panasonic / 0x0038
				var entry = makernote_structure.GetEntry (0, (ushort) 0x0038);
				Assert.IsNotNull (entry, "Entry 0x0038 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x003A (0x003a/Short/1) "1"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PanasonicMakerNoteEntryTag.WorldTimeLocation);
				Assert.IsNotNull (entry, "Entry 0x003A missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x003B (0x003b/Short/1) "1"
			{
				// TODO: Unknown IFD tag: Panasonic / 0x003B
				var entry = makernote_structure.GetEntry (0, (ushort) 0x003B);
				Assert.IsNotNull (entry, "Entry 0x003B missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x003C (0x003c/Short/1) "65535"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PanasonicMakerNoteEntryTag.ProgramISO);
				Assert.IsNotNull (entry, "Entry 0x003C missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (65535, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x003D (0x003d/Short/1) "1"
			{
				// TODO: Unknown IFD tag: Panasonic / 0x003D
				var entry = makernote_structure.GetEntry (0, (ushort) 0x003D);
				Assert.IsNotNull (entry, "Entry 0x003D missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x003E (0x003e/Short/1) "1"
			{
				// TODO: Unknown IFD tag: Panasonic / 0x003E
				var entry = makernote_structure.GetEntry (0, (ushort) 0x003E);
				Assert.IsNotNull (entry, "Entry 0x003E missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x003F (0x003f/Short/1) "0"
			{
				// TODO: Unknown IFD tag: Panasonic / 0x003F
				var entry = makernote_structure.GetEntry (0, (ushort) 0x003F);
				Assert.IsNotNull (entry, "Entry 0x003F missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x004D (0x004d/Rational/2) "976828730/807418169 808466992/3158074"
			{
				// TODO: Unknown IFD tag: Panasonic / 0x004D
				var entry = makernote_structure.GetEntry (0, (ushort) 0x004D);
				Assert.IsNotNull (entry, "Entry 0x004D missing in IFD 0");
				Assert.IsNotNull (entry as RationalArrayIFDEntry, "Entry is not a rational array!");
				var parts = (entry as RationalArrayIFDEntry).Values;
				Assert.AreEqual (2, parts.Length);
				Assert.AreEqual (976828730, parts[0].Numerator);
				Assert.AreEqual (807418169, parts[0].Denominator);
				Assert.AreEqual (808466992, parts[1].Numerator);
				Assert.AreEqual (3158074, parts[1].Denominator);
			}
			// Panasonic.0x004E (0x004e/Undefined/42) "65 83 67 73 73 0 0 0 0 0 0 0 10 0 0 0 2 0 1 0 2 0 4 0 0 0 82 57 56 0 2 0 7 0 4 0 0 0 48 49 48 48 "
			{
				// TODO: Unknown IFD tag: Panasonic / 0x004E
				var entry = makernote_structure.GetEntry (0, (ushort) 0x004E);
				Assert.IsNotNull (entry, "Entry 0x004E missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var bytes = new byte [] { 65, 83, 67, 73, 73, 0, 0, 0, 0, 0, 0, 0, 10, 0, 0, 0, 2, 0, 1, 0, 2, 0, 4, 0, 0, 0, 82, 57, 56, 0, 2, 0, 7, 0, 4, 0, 0, 0, 48, 49, 48, 48 };
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Panasonic.0x004F (0x004f/Short/1) "0"
			{
				// TODO: Unknown IFD tag: Panasonic / 0x004F
				var entry = makernote_structure.GetEntry (0, (ushort) 0x004F);
				Assert.IsNotNull (entry, "Entry 0x004F missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x8000 (0x8000/Undefined/4) "48 49 50 49 "
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PanasonicMakerNoteEntryTag.MakerNoteVersion);
				Assert.IsNotNull (entry, "Entry 0x8000 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var bytes = new byte [] { 48, 49, 50, 49 };
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Panasonic.0x8001 (0x8001/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PanasonicMakerNoteEntryTag.SceneMode);
				Assert.IsNotNull (entry, "Entry 0x8001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x8002 (0x8002/Short/1) "2"
			{
				// TODO: Unknown IFD tag: Panasonic / 0x8002
				var entry = makernote_structure.GetEntry (0, (ushort) 0x8002);
				Assert.IsNotNull (entry, "Entry 0x8002 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (2, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x8003 (0x8003/Short/1) "1"
			{
				// TODO: Unknown IFD tag: Panasonic / 0x8003
				var entry = makernote_structure.GetEntry (0, (ushort) 0x8003);
				Assert.IsNotNull (entry, "Entry 0x8003 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x8004 (0x8004/Short/1) "1654"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PanasonicMakerNoteEntryTag.WBRedLevel);
				Assert.IsNotNull (entry, "Entry 0x8004 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1654, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x8005 (0x8005/Short/1) "1054"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PanasonicMakerNoteEntryTag.WBGreenLevel);
				Assert.IsNotNull (entry, "Entry 0x8005 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1054, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x8006 (0x8006/Short/1) "1895"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PanasonicMakerNoteEntryTag.WBBlueLevel);
				Assert.IsNotNull (entry, "Entry 0x8006 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1895, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x8007 (0x8007/Short/1) "1"
			{
				// TODO: Unknown IFD tag: Panasonic / 0x8007
				var entry = makernote_structure.GetEntry (0, (ushort) 0x8007);
				Assert.IsNotNull (entry, "Entry 0x8007 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x8008 (0x8008/Short/1) "1"
			{
				// TODO: Unknown IFD tag: Panasonic / 0x8008
				var entry = makernote_structure.GetEntry (0, (ushort) 0x8008);
				Assert.IsNotNull (entry, "Entry 0x8008 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x8009 (0x8009/Short/1) "1"
			{
				// TODO: Unknown IFD tag: Panasonic / 0x8009
				var entry = makernote_structure.GetEntry (0, (ushort) 0x8009);
				Assert.IsNotNull (entry, "Entry 0x8009 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x8010 (0x8010/Ascii/20) ""
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PanasonicMakerNoteEntryTag.BabyAge2);
				Assert.IsNotNull (entry, "Entry 0x8010 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("", (entry as StringIFDEntry).Value.Trim ());
			}
			// Photo.0x9286 (UserComment/UserComment/8) "charset="Ascii" "
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
			// Photo.0xA002 (PixelXDimension/Long/1) "64"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.PixelXDimension);
				Assert.IsNotNull (entry, "Entry 0xA002 missing in IFD 0");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (64, (entry as LongIFDEntry).Value);
			}
			// Photo.0xA003 (PixelYDimension/Long/1) "40"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.PixelYDimension);
				Assert.IsNotNull (entry, "Entry 0xA003 missing in IFD 0");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (40, (entry as LongIFDEntry).Value);
			}
			// Photo.0xA005 (InteroperabilityTag/SubIFD/1) "9916"
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
			// Photo.0xA217 (SensingMethod/Short/1) "2"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.SensingMethod);
				Assert.IsNotNull (entry, "Entry 0xA217 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (2, (entry as ShortIFDEntry).Value);
			}
			// Photo.0xA300 (FileSource/Undefined/1) "3 "
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.FileSource);
				Assert.IsNotNull (entry, "Entry 0xA300 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var bytes = new byte [] { 3 };
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Photo.0xA301 (SceneType/Undefined/1) "1 "
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.SceneType);
				Assert.IsNotNull (entry, "Entry 0xA301 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var bytes = new byte [] { 1 };
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
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
			// Photo.0xA404 (DigitalZoomRatio/Rational/1) "0/10"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.DigitalZoomRatio);
				Assert.IsNotNull (entry, "Entry 0xA404 missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (0, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (10, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Photo.0xA405 (FocalLengthIn35mmFilm/Short/1) "25"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.FocalLengthIn35mmFilm);
				Assert.IsNotNull (entry, "Entry 0xA405 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (25, (entry as ShortIFDEntry).Value);
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
			// Image.0xC4A5 (PrintImageMatching/Undefined/208) "80 114 105 110 116 73 77 0 48 50 53 48 0 0 14 0 1 0 22 0 22 0 2 0 0 0 0 0 3 0 100 0 0 0 7 0 0 0 0 0 8 0 0 0 0 0 9 0 0 0 0 0 10 0 0 0 0 0 11 0 172 0 0 0 12 0 0 0 0 0 13 0 0 0 0 0 14 0 196 0 0 0 0 1 5 0 0 0 1 1 1 0 0 0 16 1 128 0 0 0 9 17 0 0 16 39 0 0 11 15 0 0 16 39 0 0 151 5 0 0 16 39 0 0 176 8 0 0 16 39 0 0 1 28 0 0 16 39 0 0 94 2 0 0 16 39 0 0 139 0 0 0 16 39 0 0 203 3 0 0 16 39 0 0 229 27 0 0 16 39 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 "
			{
				// TODO: Unknown IFD tag: Image / 0xC4A5
				var entry = structure.GetEntry (0, (ushort) 0xC4A5);
				Assert.IsNotNull (entry, "Entry 0xC4A5 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var bytes = new byte [] { 80, 114, 105, 110, 116, 73, 77, 0, 48, 50, 53, 48, 0, 0, 14, 0, 1, 0, 22, 0, 22, 0, 2, 0, 0, 0, 0, 0, 3, 0, 100, 0, 0, 0, 7, 0, 0, 0, 0, 0, 8, 0, 0, 0, 0, 0, 9, 0, 0, 0, 0, 0, 10, 0, 0, 0, 0, 0, 11, 0, 172, 0, 0, 0, 12, 0, 0, 0, 0, 0, 13, 0, 0, 0, 0, 0, 14, 0, 196, 0, 0, 0, 0, 1, 5, 0, 0, 0, 1, 1, 1, 0, 0, 0, 16, 1, 128, 0, 0, 0, 9, 17, 0, 0, 16, 39, 0, 0, 11, 15, 0, 0, 16, 39, 0, 0, 151, 5, 0, 0, 16, 39, 0, 0, 176, 8, 0, 0, 16, 39, 0, 0, 1, 28, 0, 0, 16, 39, 0, 0, 94, 2, 0, 0, 16, 39, 0, 0, 139, 0, 0, 0, 16, 39, 0, 0, 203, 3, 0, 0, 16, 39, 0, 0, 229, 27, 0, 0, 16, 39, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Thumbnail.0x0103 (Compression/Short/1) "6"
			{
				var entry = structure.GetEntry (1, (ushort) IFDEntryTag.Compression);
				Assert.IsNotNull (entry, "Entry 0x0103 missing in IFD 1");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (6, (entry as ShortIFDEntry).Value);
			}
			// Thumbnail.0x0112 (Orientation/Short/1) "1"
			{
				var entry = structure.GetEntry (1, (ushort) IFDEntryTag.Orientation);
				Assert.IsNotNull (entry, "Entry 0x0112 missing in IFD 1");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1, (entry as ShortIFDEntry).Value);
			}
			// Thumbnail.0x011A (XResolution/Rational/1) "180/1"
			{
				var entry = structure.GetEntry (1, (ushort) IFDEntryTag.XResolution);
				Assert.IsNotNull (entry, "Entry 0x011A missing in IFD 1");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (180, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (1, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Thumbnail.0x011B (YResolution/Rational/1) "180/1"
			{
				var entry = structure.GetEntry (1, (ushort) IFDEntryTag.YResolution);
				Assert.IsNotNull (entry, "Entry 0x011B missing in IFD 1");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (180, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (1, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Thumbnail.0x0128 (ResolutionUnit/Short/1) "2"
			{
				var entry = structure.GetEntry (1, (ushort) IFDEntryTag.ResolutionUnit);
				Assert.IsNotNull (entry, "Entry 0x0128 missing in IFD 1");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (2, (entry as ShortIFDEntry).Value);
			}
			// Thumbnail.0x0201 (JPEGInterchangeFormat/ThumbnailDataIFD/1) "10064"
			{
				var entry = structure.GetEntry (1, (ushort) IFDEntryTag.JPEGInterchangeFormat);
				Assert.IsNotNull (entry, "Entry 0x0201 missing in IFD 1");
				Assert.IsNotNull (entry as ThumbnailDataIFDEntry, "Entry is not a thumbnail IFD!");
			}
			// Thumbnail.0x0202 (JPEGInterchangeFormatLength/Long/1) "668"
			{
				var entry = structure.GetEntry (1, (ushort) IFDEntryTag.JPEGInterchangeFormatLength);
				Assert.IsNotNull (entry, "Entry 0x0202 missing in IFD 1");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (668, (entry as LongIFDEntry).Value);
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
