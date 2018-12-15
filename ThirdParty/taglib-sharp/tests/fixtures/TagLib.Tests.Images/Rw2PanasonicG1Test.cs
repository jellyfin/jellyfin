
// TODO: Further manual verification is needed

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
	public class Rw2PanasonicG1Test
	{
		[Test]
		public void Test ()
		{
			ImageTest.Run ("raw-samples/RW2", "RAW_PANASONIC_G1.RW2",
				false,
				new Rw2PanasonicG1TestInvariantValidator ()
			);
		}
	}

	public class Rw2PanasonicG1TestInvariantValidator : IMetadataInvariantValidator
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
			Assert.AreEqual (Image.ImageOrientation.TopLeft, imagetag.Orientation, "Orientation");
			Assert.AreEqual (null, imagetag.Software, "Software");
			Assert.AreEqual (null, imagetag.Latitude, "Latitude");
			Assert.AreEqual (null, imagetag.Longitude, "Longitude");
			Assert.AreEqual (null, imagetag.Altitude, "Altitude");
			Assert.AreEqual (0.0025, imagetag.ExposureTime, "ExposureTime");
			Assert.AreEqual (6.3, imagetag.FNumber, "FNumber");
			Assert.AreEqual (100, imagetag.ISOSpeedRatings, "ISOSpeedRatings");
			Assert.AreEqual (14, imagetag.FocalLength, "FocalLength");
			Assert.AreEqual (28, imagetag.FocalLengthIn35mmFilm, "FocalLengthIn35mmFilm");
			Assert.AreEqual ("Panasonic", imagetag.Make, "Make");
			Assert.AreEqual ("DMC-G1", imagetag.Model, "Model");
			Assert.AreEqual (null, imagetag.Creator, "Creator");

			var properties = file.Properties;
			Assert.IsNotNull (properties);
			Assert.AreEqual (4008, properties.PhotoWidth, "PhotoWidth");
			Assert.AreEqual (3004, properties.PhotoHeight, "PhotoHeight");

			//  ---------- End of ImageTag tests ----------

			//  ---------- Start of IFD tests ----------

			var tag = file.GetTag (TagTypes.TiffIFD) as IFDTag;
			Assert.IsNotNull (tag, "IFD tag not found");

			var pana_structure = tag.Structure;

			var jpg_file = (file as TagLib.Tiff.Rw2.File).JpgFromRaw;
			Assert.IsNotNull (tag, "JpgFromRaw not found!");
			var jpg_tag = jpg_file.GetTag (TagTypes.TiffIFD) as IFDTag;
			Assert.IsNotNull (tag, "Jpg has no Exif tag!");
			var structure = jpg_tag.Structure;
			// PanasonicRaw.0x0001 (Version/Undefined/4) "48 51 49 48"
			{
				// TODO: Unknown IFD tag: PanasonicRaw / 0x0001
				var entry = pana_structure.GetEntry (0, (ushort) 0x0001);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 48, 51, 49, 48 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// PanasonicRaw.0x0002 (SensorWidth/Short/1) "4060"
			{
				// TODO: Unknown IFD tag: PanasonicRaw / 0x0002
				var entry = pana_structure.GetEntry (0, (ushort) 0x0002);
				Assert.IsNotNull (entry, "Entry 0x0002 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (4060, (entry as ShortIFDEntry).Value);
			}
			// PanasonicRaw.0x0003 (SensorHeight/Short/1) "3016"
			{
				// TODO: Unknown IFD tag: PanasonicRaw / 0x0003
				var entry = pana_structure.GetEntry (0, (ushort) 0x0003);
				Assert.IsNotNull (entry, "Entry 0x0003 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (3016, (entry as ShortIFDEntry).Value);
			}
			// PanasonicRaw.0x0004 (SensorTopBorder/Short/1) "4"
			{
				// TODO: Unknown IFD tag: PanasonicRaw / 0x0004
				var entry = pana_structure.GetEntry (0, (ushort) 0x0004);
				Assert.IsNotNull (entry, "Entry 0x0004 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (4, (entry as ShortIFDEntry).Value);
			}
			// PanasonicRaw.0x0005 (SensorLeftBorder/Short/1) "8"
			{
				// TODO: Unknown IFD tag: PanasonicRaw / 0x0005
				var entry = pana_structure.GetEntry (0, (ushort) 0x0005);
				Assert.IsNotNull (entry, "Entry 0x0005 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (8, (entry as ShortIFDEntry).Value);
			}
			// PanasonicRaw.0x0006 (ImageHeight/Short/1) "3004"
			{
				// TODO: Unknown IFD tag: PanasonicRaw / 0x0006
				var entry = pana_structure.GetEntry (0, (ushort) 0x0006);
				Assert.IsNotNull (entry, "Entry 0x0006 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (3004, (entry as ShortIFDEntry).Value);
			}
			// PanasonicRaw.0x0007 (ImageWidth/Short/1) "4008"
			{
				// TODO: Unknown IFD tag: PanasonicRaw / 0x0007
				var entry = pana_structure.GetEntry (0, (ushort) 0x0007);
				Assert.IsNotNull (entry, "Entry 0x0007 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (4008, (entry as ShortIFDEntry).Value);
			}
			// PanasonicRaw.0x0008 (0x0008/Short/1) "1"
			{
				// TODO: Unknown IFD tag: PanasonicRaw / 0x0008
				var entry = pana_structure.GetEntry (0, (ushort) 0x0008);
				Assert.IsNotNull (entry, "Entry 0x0008 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1, (entry as ShortIFDEntry).Value);
			}
			// PanasonicRaw.0x0009 (0x0009/Short/1) "3"
			{
				// TODO: Unknown IFD tag: PanasonicRaw / 0x0009
				var entry = pana_structure.GetEntry (0, (ushort) 0x0009);
				Assert.IsNotNull (entry, "Entry 0x0009 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (3, (entry as ShortIFDEntry).Value);
			}
			// PanasonicRaw.0x000A (0x000a/Short/1) "12"
			{
				// TODO: Unknown IFD tag: PanasonicRaw / 0x000A
				var entry = pana_structure.GetEntry (0, (ushort) 0x000A);
				Assert.IsNotNull (entry, "Entry 0x000A missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (12, (entry as ShortIFDEntry).Value);
			}
			// PanasonicRaw.0x000B (0x000b/Short/1) "34316"
			{
				// TODO: Unknown IFD tag: PanasonicRaw / 0x000B
				var entry = pana_structure.GetEntry (0, (ushort) 0x000B);
				Assert.IsNotNull (entry, "Entry 0x000B missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (34316, (entry as ShortIFDEntry).Value);
			}
			// PanasonicRaw.0x000D (0x000d/Short/1) "1"
			{
				// TODO: Unknown IFD tag: PanasonicRaw / 0x000D
				var entry = pana_structure.GetEntry (0, (ushort) 0x000D);
				Assert.IsNotNull (entry, "Entry 0x000D missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1, (entry as ShortIFDEntry).Value);
			}
			// PanasonicRaw.0x000E (0x000e/Short/1) "4095"
			{
				// TODO: Unknown IFD tag: PanasonicRaw / 0x000E
				var entry = pana_structure.GetEntry (0, (ushort) 0x000E);
				Assert.IsNotNull (entry, "Entry 0x000E missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (4095, (entry as ShortIFDEntry).Value);
			}
			// PanasonicRaw.0x000F (0x000f/Short/1) "4095"
			{
				// TODO: Unknown IFD tag: PanasonicRaw / 0x000F
				var entry = pana_structure.GetEntry (0, (ushort) 0x000F);
				Assert.IsNotNull (entry, "Entry 0x000F missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (4095, (entry as ShortIFDEntry).Value);
			}
			// PanasonicRaw.0x0010 (0x0010/Short/1) "4095"
			{
				// TODO: Unknown IFD tag: PanasonicRaw / 0x0010
				var entry = pana_structure.GetEntry (0, (ushort) 0x0010);
				Assert.IsNotNull (entry, "Entry 0x0010 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (4095, (entry as ShortIFDEntry).Value);
			}
			// PanasonicRaw.0x0017 (ISOSpeed/Short/1) "100"
			{
				// TODO: Unknown IFD tag: PanasonicRaw / 0x0017
				var entry = pana_structure.GetEntry (0, (ushort) 0x0017);
				Assert.IsNotNull (entry, "Entry 0x0017 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (100, (entry as ShortIFDEntry).Value);
			}
			// PanasonicRaw.0x0018 (0x0018/Short/1) "0"
			{
				// TODO: Unknown IFD tag: PanasonicRaw / 0x0018
				var entry = pana_structure.GetEntry (0, (ushort) 0x0018);
				Assert.IsNotNull (entry, "Entry 0x0018 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// PanasonicRaw.0x0019 (0x0019/Short/1) "0"
			{
				// TODO: Unknown IFD tag: PanasonicRaw / 0x0019
				var entry = pana_structure.GetEntry (0, (ushort) 0x0019);
				Assert.IsNotNull (entry, "Entry 0x0019 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// PanasonicRaw.0x001A (0x001a/Short/1) "0"
			{
				// TODO: Unknown IFD tag: PanasonicRaw / 0x001A
				var entry = pana_structure.GetEntry (0, (ushort) 0x001A);
				Assert.IsNotNull (entry, "Entry 0x001A missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// PanasonicRaw.0x001B (0x001b/Undefined/42) "5 0 100 0 4 0 4 0 4 0 200 0 8 0 8 0 8 0 144 1 16 0 16 0 16 0 32 3 32 0 32 0 32 0 64 6 64 0 64 0 64 0"
			{
				// TODO: Unknown IFD tag: PanasonicRaw / 0x001B
				var entry = pana_structure.GetEntry (0, (ushort) 0x001B);
				Assert.IsNotNull (entry, "Entry 0x001B missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 5, 0, 100, 0, 4, 0, 4, 0, 4, 0, 200, 0, 8, 0, 8, 0, 8, 0, 144, 1, 16, 0, 16, 0, 16, 0, 32, 3, 32, 0, 32, 0, 32, 0, 64, 6, 64, 0, 64, 0, 64, 0 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// PanasonicRaw.0x001C (0x001c/Short/1) "0"
			{
				// TODO: Unknown IFD tag: PanasonicRaw / 0x001C
				var entry = pana_structure.GetEntry (0, (ushort) 0x001C);
				Assert.IsNotNull (entry, "Entry 0x001C missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// PanasonicRaw.0x001D (0x001d/Short/1) "0"
			{
				// TODO: Unknown IFD tag: PanasonicRaw / 0x001D
				var entry = pana_structure.GetEntry (0, (ushort) 0x001D);
				Assert.IsNotNull (entry, "Entry 0x001D missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// PanasonicRaw.0x001E (0x001e/Short/1) "0"
			{
				// TODO: Unknown IFD tag: PanasonicRaw / 0x001E
				var entry = pana_structure.GetEntry (0, (ushort) 0x001E);
				Assert.IsNotNull (entry, "Entry 0x001E missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// PanasonicRaw.0x0024 (WBRedLevel/Short/1) "584"
			{
				// TODO: Unknown IFD tag: PanasonicRaw / 0x0024
				var entry = pana_structure.GetEntry (0, (ushort) 0x0024);
				Assert.IsNotNull (entry, "Entry 0x0024 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (584, (entry as ShortIFDEntry).Value);
			}
			// PanasonicRaw.0x0025 (WBGreenLevel/Short/1) "263"
			{
				// TODO: Unknown IFD tag: PanasonicRaw / 0x0025
				var entry = pana_structure.GetEntry (0, (ushort) 0x0025);
				Assert.IsNotNull (entry, "Entry 0x0025 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (263, (entry as ShortIFDEntry).Value);
			}
			// PanasonicRaw.0x0026 (WBBlueLevel/Short/1) "355"
			{
				// TODO: Unknown IFD tag: PanasonicRaw / 0x0026
				var entry = pana_structure.GetEntry (0, (ushort) 0x0026);
				Assert.IsNotNull (entry, "Entry 0x0026 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (355, (entry as ShortIFDEntry).Value);
			}
			// PanasonicRaw.0x0027 (0x0027/Undefined/58) "7 0 9 0 21 2 0 1 111 1 10 0 55 2 0 1 89 1 11 0 106 2 0 1 63 1 3 0 95 1 0 1 61 2 4 0 151 1 0 1 77 1 20 0 223 1 0 1 119 1 24 0 95 1 0 1 61 2"
			{
				// TODO: Unknown IFD tag: PanasonicRaw / 0x0027
				var entry = pana_structure.GetEntry (0, (ushort) 0x0027);
				Assert.IsNotNull (entry, "Entry 0x0027 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 7, 0, 9, 0, 21, 2, 0, 1, 111, 1, 10, 0, 55, 2, 0, 1, 89, 1, 11, 0, 106, 2, 0, 1, 63, 1, 3, 0, 95, 1, 0, 1, 61, 2, 4, 0, 151, 1, 0, 1, 77, 1, 20, 0, 223, 1, 0, 1, 119, 1, 24, 0, 95, 1, 0, 1, 61, 2 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// PanasonicRaw.0x0029 (0x0029/Undefined/36) "0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0"
			{
				// TODO: Unknown IFD tag: PanasonicRaw / 0x0029
				var entry = pana_structure.GetEntry (0, (ushort) 0x0029);
				Assert.IsNotNull (entry, "Entry 0x0029 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// PanasonicRaw.0x002A (0x002a/Undefined/32) "0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0"
			{
				// TODO: Unknown IFD tag: PanasonicRaw / 0x002A
				var entry = pana_structure.GetEntry (0, (ushort) 0x002A);
				Assert.IsNotNull (entry, "Entry 0x002A missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// PanasonicRaw.0x002B (0x002b/Undefined/16) "0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0"
			{
				// TODO: Unknown IFD tag: PanasonicRaw / 0x002B
				var entry = pana_structure.GetEntry (0, (ushort) 0x002B);
				Assert.IsNotNull (entry, "Entry 0x002B missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// PanasonicRaw.0x002C (0x002c/Undefined/72) "0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0"
			{
				// TODO: Unknown IFD tag: PanasonicRaw / 0x002C
				var entry = pana_structure.GetEntry (0, (ushort) 0x002C);
				Assert.IsNotNull (entry, "Entry 0x002C missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// PanasonicRaw.0x002D (0x002d/Short/1) "4"
			{
				// TODO: Unknown IFD tag: PanasonicRaw / 0x002D
				var entry = pana_structure.GetEntry (0, (ushort) 0x002D);
				Assert.IsNotNull (entry, "Entry 0x002D missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (4, (entry as ShortIFDEntry).Value);
			}
			// PanasonicRaw.0x002E (PreviewImage/Undefined/687616) "(Value ommitted)"
			{
				// TODO: Unknown IFD tag: PanasonicRaw / 0x002E
				var entry = pana_structure.GetEntry (0, (ushort) 0x002E);
				Assert.IsNotNull (entry, "Entry 0x002E missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var parsed_hash = Utils.Md5Encode (parsed_bytes);
				Assert.AreEqual ("7770d7802a09f4b2f9854788720b01b9", parsed_hash);
				Assert.AreEqual (687616, parsed_bytes.Length);
			}
			// PanasonicRaw.0x010F (Make/Ascii/10) "Panasonic"
			{
				var entry = pana_structure.GetEntry (0, (ushort) IFDEntryTag.Make);
				Assert.IsNotNull (entry, "Entry 0x010F missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("Panasonic", (entry as StringIFDEntry).Value);
			}
			// PanasonicRaw.0x0110 (Model/Ascii/7) "DMC-G1"
			{
				var entry = pana_structure.GetEntry (0, (ushort) IFDEntryTag.Model);
				Assert.IsNotNull (entry, "Entry 0x0110 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("DMC-G1", (entry as StringIFDEntry).Value);
			}
			// PanasonicRaw.0x0111 (StripOffsets/StripOffsets/1) "689664"
			{
				var entry = pana_structure.GetEntry (0, (ushort) IFDEntryTag.StripOffsets);
				Assert.IsNotNull (entry, "Entry 0x0111 missing in IFD 0");
				Assert.IsNotNull (entry as StripOffsetsIFDEntry, "Entry is not a strip offsets entry!");
				Assert.AreEqual (1, (entry as StripOffsetsIFDEntry).Values.Length);
			}
			// PanasonicRaw.0x0112 (Orientation/Short/1) "1"
			{
				var entry = pana_structure.GetEntry (0, (ushort) IFDEntryTag.Orientation);
				Assert.IsNotNull (entry, "Entry 0x0112 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1, (entry as ShortIFDEntry).Value);
			}
			// PanasonicRaw.0x0116 (RowsPerStrip/Short/1) "3016"
			{
				var entry = pana_structure.GetEntry (0, (ushort) IFDEntryTag.RowsPerStrip);
				Assert.IsNotNull (entry, "Entry 0x0116 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (3016, (entry as ShortIFDEntry).Value);
			}
			// PanasonicRaw.0x0117 (StripByteCounts/Long/1) "0"
			{
				var entry = pana_structure.GetEntry (0, (ushort) IFDEntryTag.StripByteCounts);
				Assert.IsNotNull (entry, "Entry 0x0117 missing in IFD 0");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (0, (entry as LongIFDEntry).Value);
			}
			// PanasonicRaw.0x0118 (RawDataOffset/Long/1) "689664"
			{
				var entry = pana_structure.GetEntry (0, (ushort) IFDEntryTag.MinSampleValue);
				Assert.IsNotNull (entry, "Entry 0x0118 missing in IFD 0");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (689664, (entry as LongIFDEntry).Value);
			}
			// PanasonicRaw.0x0119 (0x0119/Undefined/32) "153 224 77 65 10 1 0 0 76 1 0 0 185 1 1 0 139 15 46 1 86 2 125 251 196 9 34 3 123 154 233 139"
			{
				var entry = pana_structure.GetEntry (0, (ushort) IFDEntryTag.MaxSampleValue);
				Assert.IsNotNull (entry, "Entry 0x0119 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 153, 224, 77, 65, 10, 1, 0, 0, 76, 1, 0, 0, 185, 1, 1, 0, 139, 15, 46, 1, 86, 2, 125, 251, 196, 9, 34, 3, 123, 154, 233, 139 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// PanasonicRaw.0x011A (0x011a/Short/1) "2"
			{
				var entry = pana_structure.GetEntry (0, (ushort) IFDEntryTag.XResolution);
				Assert.IsNotNull (entry, "Entry 0x011A missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (2, (entry as ShortIFDEntry).Value);
			}
			// PanasonicRaw.0x011B (0x011b/Undefined/64) "76 105 30 27 36 25 136 248 52 8 64 2 0 155 237 31 190 254 16 6 23 253 216 9 204 254 255 255 0 1 168 8 144 6 72 3 48 40 128 17 74 250 0 0 0 12 132 254 144 143 132 254 216 253 6 255 0 72 46 250 250 44 247 88"
			{
				var entry = pana_structure.GetEntry (0, (ushort) IFDEntryTag.YResolution);
				Assert.IsNotNull (entry, "Entry 0x011B missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 76, 105, 30, 27, 36, 25, 136, 248, 52, 8, 64, 2, 0, 155, 237, 31, 190, 254, 16, 6, 23, 253, 216, 9, 204, 254, 255, 255, 0, 1, 168, 8, 144, 6, 72, 3, 48, 40, 128, 17, 74, 250, 0, 0, 0, 12, 132, 254, 144, 143, 132, 254, 216, 253, 6, 255, 0, 72, 46, 250, 250, 44, 247, 88 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// PanasonicRaw.0x8769 (ExifTag/SubIFD/1) "928"
			{
				var entry = pana_structure.GetEntry (0, (ushort) IFDEntryTag.ExifIFD);
				Assert.IsNotNull (entry, "Entry 0x8769 missing in IFD 0");
				Assert.IsNotNull (entry as SubIFDEntry, "Entry is not a sub IFD!");
			}

			var exif = structure.GetEntry (0, (ushort) IFDEntryTag.ExifIFD) as SubIFDEntry;
			Assert.IsNotNull (exif, "Exif tag not found");
			var exif_structure = exif.Structure;

			// Photo.0x829A (ExposureTime/Rational/1) "10/4000"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ExposureTime);
				Assert.IsNotNull (entry, "Entry 0x829A missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (10, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (4000, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Photo.0x829D (FNumber/Rational/1) "63/10"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.FNumber);
				Assert.IsNotNull (entry, "Entry 0x829D missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (63, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (10, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Photo.0x8822 (ExposureProgram/Short/1) "3"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ExposureProgram);
				Assert.IsNotNull (entry, "Entry 0x8822 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (3, (entry as ShortIFDEntry).Value);
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
			// Photo.0x9003 (DateTimeOriginal/Ascii/20) "2008:12:10 15:06:33"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.DateTimeOriginal);
				Assert.IsNotNull (entry, "Entry 0x9003 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("2008:12:10 15:06:33", (entry as StringIFDEntry).Value);
			}
			// Photo.0x9004 (DateTimeDigitized/Ascii/20) "2008:12:10 15:06:33"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.DateTimeDigitized);
				Assert.IsNotNull (entry, "Entry 0x9004 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("2008:12:10 15:06:33", (entry as StringIFDEntry).Value);
			}
			// Photo.0x9204 (ExposureBiasValue/SRational/1) "-33/100"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ExposureBiasValue);
				Assert.IsNotNull (entry, "Entry 0x9204 missing in IFD 0");
				Assert.IsNotNull (entry as SRationalIFDEntry, "Entry is not a srational!");
				Assert.AreEqual (-33, (entry as SRationalIFDEntry).Value.Numerator);
				Assert.AreEqual (100, (entry as SRationalIFDEntry).Value.Denominator);
			}
			// Photo.0x9205 (MaxApertureValue/Rational/1) "925/256"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.MaxApertureValue);
				Assert.IsNotNull (entry, "Entry 0x9205 missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (925, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (256, (entry as RationalIFDEntry).Value.Denominator);
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
			// Photo.0x920A (FocalLength/Rational/1) "140/10"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.FocalLength);
				Assert.IsNotNull (entry, "Entry 0x920A missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (140, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (10, (entry as RationalIFDEntry).Value.Denominator);
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
			// Image.0x010F (Make/Ascii/10) "Panasonic"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.Make);
				Assert.IsNotNull (entry, "Entry 0x010F missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("Panasonic", (entry as StringIFDEntry).Value);
			}
			// Image.0x0110 (Model/Ascii/7) "DMC-G1"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.Model);
				Assert.IsNotNull (entry, "Entry 0x0110 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("DMC-G1", (entry as StringIFDEntry).Value);
			}
			// Image.0x0112 (Orientation/Short/1) "1"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.Orientation);
				Assert.IsNotNull (entry, "Entry 0x0112 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1, (entry as ShortIFDEntry).Value);
			}
			// Image.0x011A (XResolution/Rational/1) "180/1"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.XResolution);
				Assert.IsNotNull (entry, "Entry 0x011A missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (180, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (1, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Image.0x011B (YResolution/Rational/1) "180/1"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.YResolution);
				Assert.IsNotNull (entry, "Entry 0x011B missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (180, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (1, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Image.0x0128 (ResolutionUnit/Short/1) "2"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.ResolutionUnit);
				Assert.IsNotNull (entry, "Entry 0x0128 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (2, (entry as ShortIFDEntry).Value);
			}
			// Image.0x0131 (Software/Ascii/10) "Ver.1.0  "
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.Software);
				Assert.IsNotNull (entry, "Entry 0x0131 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("Ver.1.0  ", (entry as StringIFDEntry).Value);
			}
			// Image.0x0132 (DateTime/Ascii/20) "2008:12:10 15:06:33"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.DateTime);
				Assert.IsNotNull (entry, "Entry 0x0132 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("2008:12:10 15:06:33", (entry as StringIFDEntry).Value);
			}
			// Image.0x8769 (ExifTag/SubIFD/1) "570"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.ExifIFD);
				Assert.IsNotNull (entry, "Entry 0x8769 missing in IFD 0");
				Assert.IsNotNull (entry as SubIFDEntry, "Entry is not a sub IFD!");
			}
			// Photo.0x8827 (ISOSpeedRatings/Short/1) "100"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ISOSpeedRatings);
				Assert.IsNotNull (entry, "Entry 0x8827 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (100, (entry as ShortIFDEntry).Value);
			}
			// Photo.0x9208 (LightSource/Short/1) "10"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.LightSource);
				Assert.IsNotNull (entry, "Entry 0x9208 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (10, (entry as ShortIFDEntry).Value);
			}
			// Photo.0x927C (MakerNote/MakerNote/7292) "(Value ommitted)"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.MakerNote);
				Assert.IsNotNull (entry, "Entry 0x927C missing in IFD 0");
				Assert.IsNotNull (entry as MakernoteIFDEntry, "Entry is not a makernote IFD!");
			}

			var makernote = exif_structure.GetEntry (0, (ushort) ExifEntryTag.MakerNote) as MakernoteIFDEntry;
			Assert.IsNotNull (makernote, "MakerNote tag not found");
			var makernote_structure = makernote.Structure;


			Assert.AreEqual (MakernoteType.Panasonic, makernote.MakernoteType);

			// Panasonic.0x0001 (Quality/Short/1) "7"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PanasonicMakerNoteEntryTag.Quality);
				Assert.IsNotNull (entry, "Entry 0x0001 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (7, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x0002 (FirmwareVersion/Undefined/4) "0 1 0 0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PanasonicMakerNoteEntryTag.FirmwareVersion);
				Assert.IsNotNull (entry, "Entry 0x0002 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 0, 1, 0, 0 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Panasonic.0x0003 (WhiteBalance/Short/1) "3"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PanasonicMakerNoteEntryTag.WhiteBalance);
				Assert.IsNotNull (entry, "Entry 0x0003 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (3, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x0007 (FocusMode/Short/1) "6"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PanasonicMakerNoteEntryTag.FocusMode);
				Assert.IsNotNull (entry, "Entry 0x0007 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (6, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x000F (AFMode/Byte/2) "32 0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PanasonicMakerNoteEntryTag.AFMode);
				Assert.IsNotNull (entry, "Entry 0x000F missing in IFD 0");
				Assert.IsNotNull (entry as ByteVectorIFDEntry, "Entry is not a byte array!");
				var parsed_bytes = (entry as ByteVectorIFDEntry).Data.Data;
				var bytes = new byte [] { 32, 0 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Panasonic.0x001A (ImageStabilization/Short/1) "3"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PanasonicMakerNoteEntryTag.ImageStabilization);
				Assert.IsNotNull (entry, "Entry 0x001A missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (3, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x001C (Macro/Short/1) "2"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PanasonicMakerNoteEntryTag.Macro);
				Assert.IsNotNull (entry, "Entry 0x001C missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (2, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x001F (ShootingMode/Short/1) "7"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PanasonicMakerNoteEntryTag.ShootingMode);
				Assert.IsNotNull (entry, "Entry 0x001F missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (7, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x0020 (Audio/Short/1) "2"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PanasonicMakerNoteEntryTag.Audio);
				Assert.IsNotNull (entry, "Entry 0x0020 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (2, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x0021 (DataDump/Undefined/6120) "(Value ommitted)"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PanasonicMakerNoteEntryTag.DataDump);
				Assert.IsNotNull (entry, "Entry 0x0021 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var parsed_hash = Utils.Md5Encode (parsed_bytes);
				Assert.AreEqual ("3d168b9a433490cf9f4a54b5e46a8827", parsed_hash);
				Assert.AreEqual (6120, parsed_bytes.Length);
			}
			// Panasonic.0x0022 (0x0022/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PanasonicMakerNoteEntryTag.Unknown34);
				Assert.IsNotNull (entry, "Entry 0x0022 missing in IFD 0");
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
			// Panasonic.0x0025 (InternalSerialNumber/Undefined/16) "70 57 53 48 56 49 49 48 52 48 48 52 55 0 0 0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PanasonicMakerNoteEntryTag.InternalSerialNumber);
				Assert.IsNotNull (entry, "Entry 0x0025 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 70, 57, 53, 48, 56, 49, 49, 48, 52, 48, 48, 52, 55, 0, 0, 0 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Panasonic.0x0026 (ExifVersion/Undefined/4) "48 50 55 48"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PanasonicMakerNoteEntryTag.ExifVersion);
				Assert.IsNotNull (entry, "Entry 0x0026 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 48, 50, 55, 48 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Panasonic.0x0027 (0x0027/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PanasonicMakerNoteEntryTag.Unknown39);
				Assert.IsNotNull (entry, "Entry 0x0027 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x0029 (TimeSincePowerOn/Long/1) "6572"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PanasonicMakerNoteEntryTag.TimeSincePowerOn);
				Assert.IsNotNull (entry, "Entry 0x0029 missing in IFD 0");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (6572, (entry as LongIFDEntry).Value);
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
			// Panasonic.0x0033 (BabyAge/Ascii/20) "9999:99:99 00:00:00"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PanasonicMakerNoteEntryTag.BabyAge);
				Assert.IsNotNull (entry, "Entry 0x0033 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("9999:99:99 00:00:00", (entry as StringIFDEntry).Value);
			}
			// Panasonic.0x0035 (ConversionLens/Short/1) "1"
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
			// Panasonic.0x0038 (0x0038/Short/1) "2"
			{
				// TODO: Unknown IFD tag: Panasonic / 0x0038
				var entry = makernote_structure.GetEntry (0, (ushort) 0x0038);
				Assert.IsNotNull (entry, "Entry 0x0038 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (2, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x003A (WorldTimeLocation/Short/1) "1"
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
			// Panasonic.0x003C (ProgramISO/Short/1) "65535"
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
			// Panasonic.0x0043 (0x0043/Short/1) "3"
			{
				// TODO: Unknown IFD tag: Panasonic / 0x0043
				var entry = makernote_structure.GetEntry (0, (ushort) 0x0043);
				Assert.IsNotNull (entry, "Entry 0x0043 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (3, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x0044 (0x0044/Short/1) "3400"
			{
				// TODO: Unknown IFD tag: Panasonic / 0x0044
				var entry = makernote_structure.GetEntry (0, (ushort) 0x0044);
				Assert.IsNotNull (entry, "Entry 0x0044 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (3400, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x0045 (0x0045/Short/1) "0"
			{
				// TODO: Unknown IFD tag: Panasonic / 0x0045
				var entry = makernote_structure.GetEntry (0, (ushort) 0x0045);
				Assert.IsNotNull (entry, "Entry 0x0045 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x0046 (WBAdjustAB/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PanasonicMakerNoteEntryTag.WBAdjustAB);
				Assert.IsNotNull (entry, "Entry 0x0046 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x0047 (WBAdjustGM/Short/1) "0"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PanasonicMakerNoteEntryTag.WBAdjustGM);
				Assert.IsNotNull (entry, "Entry 0x0047 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x0048 (0x0048/Short/1) "0"
			{
				// TODO: Unknown IFD tag: Panasonic / 0x0048
				var entry = makernote_structure.GetEntry (0, (ushort) 0x0048);
				Assert.IsNotNull (entry, "Entry 0x0048 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x0049 (0x0049/Short/1) "2"
			{
				// TODO: Unknown IFD tag: Panasonic / 0x0049
				var entry = makernote_structure.GetEntry (0, (ushort) 0x0049);
				Assert.IsNotNull (entry, "Entry 0x0049 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (2, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x004A (0x004a/Short/1) "0"
			{
				// TODO: Unknown IFD tag: Panasonic / 0x004A
				var entry = makernote_structure.GetEntry (0, (ushort) 0x004A);
				Assert.IsNotNull (entry, "Entry 0x004A missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x004B (0x004b/Long/1) "4000"
			{
				// TODO: Unknown IFD tag: Panasonic / 0x004B
				var entry = makernote_structure.GetEntry (0, (ushort) 0x004B);
				Assert.IsNotNull (entry, "Entry 0x004B missing in IFD 0");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (4000, (entry as LongIFDEntry).Value);
			}
			// Panasonic.0x004C (0x004c/Long/1) "3000"
			{
				// TODO: Unknown IFD tag: Panasonic / 0x004C
				var entry = makernote_structure.GetEntry (0, (ushort) 0x004C);
				Assert.IsNotNull (entry, "Entry 0x004C missing in IFD 0");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (3000, (entry as LongIFDEntry).Value);
			}
			// Panasonic.0x004D (0x004d/Rational/2) "128/256 128/256"
			{
				// TODO: Unknown IFD tag: Panasonic / 0x004D
				var entry = makernote_structure.GetEntry (0, (ushort) 0x004D);
				Assert.IsNotNull (entry, "Entry 0x004D missing in IFD 0");
				Assert.IsNotNull (entry as RationalArrayIFDEntry, "Entry is not a rational array!");
				var parts = (entry as RationalArrayIFDEntry).Values;
				Assert.AreEqual (2, parts.Length);
				Assert.AreEqual (128, parts[0].Numerator);
				Assert.AreEqual (256, parts[0].Denominator);
				Assert.AreEqual (128, parts[1].Numerator);
				Assert.AreEqual (256, parts[1].Denominator);
			}
			// Panasonic.0x004E (0x004e/Undefined/42) "0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0"
			{
				// TODO: Unknown IFD tag: Panasonic / 0x004E
				var entry = makernote_structure.GetEntry (0, (ushort) 0x004E);
				Assert.IsNotNull (entry, "Entry 0x004E missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
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
			// Panasonic.0x0050 (0x0050/Short/1) "0"
			{
				// TODO: Unknown IFD tag: Panasonic / 0x0050
				var entry = makernote_structure.GetEntry (0, (ushort) 0x0050);
				Assert.IsNotNull (entry, "Entry 0x0050 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x0051 (LensType/Ascii/34) "LUMIX G VARIO 14-45/F3.5-5.6"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PanasonicMakerNoteEntryTag.LensType);
				Assert.IsNotNull (entry, "Entry 0x0051 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("LUMIX G VARIO 14-45/F3.5-5.6", (entry as StringIFDEntry).Value);
			}
			// Panasonic.0x0052 (LensSerialNumber/Ascii/14) "08JG1201858"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PanasonicMakerNoteEntryTag.LensSerialNumber);
				Assert.IsNotNull (entry, "Entry 0x0052 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("08JG1201858", (entry as StringIFDEntry).Value);
			}
			// Panasonic.0x0053 (AccessoryType/Ascii/34) "NO-ACCESSORY"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PanasonicMakerNoteEntryTag.AccessoryType);
				Assert.IsNotNull (entry, "Entry 0x0053 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("NO-ACCESSORY", (entry as StringIFDEntry).Value);
			}
			// Panasonic.0x0054 (0x0054/Ascii/14) "0000000"
			{
				// TODO: Unknown IFD tag: Panasonic / 0x0054
				var entry = makernote_structure.GetEntry (0, (ushort) 0x0054);
				Assert.IsNotNull (entry, "Entry 0x0054 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("0000000", (entry as StringIFDEntry).Value);
			}
			// Panasonic.0x0055 (0x0055/Short/1) "1"
			{
				// TODO: Unknown IFD tag: Panasonic / 0x0055
				var entry = makernote_structure.GetEntry (0, (ushort) 0x0055);
				Assert.IsNotNull (entry, "Entry 0x0055 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x005A (0x005a/Short/1) "0"
			{
				// TODO: Unknown IFD tag: Panasonic / 0x005A
				var entry = makernote_structure.GetEntry (0, (ushort) 0x005A);
				Assert.IsNotNull (entry, "Entry 0x005A missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x005B (0x005b/Short/1) "0"
			{
				// TODO: Unknown IFD tag: Panasonic / 0x005B
				var entry = makernote_structure.GetEntry (0, (ushort) 0x005B);
				Assert.IsNotNull (entry, "Entry 0x005B missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x005C (0x005c/Short/1) "0"
			{
				// TODO: Unknown IFD tag: Panasonic / 0x005C
				var entry = makernote_structure.GetEntry (0, (ushort) 0x005C);
				Assert.IsNotNull (entry, "Entry 0x005C missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x005D (0x005d/Short/1) "0"
			{
				// TODO: Unknown IFD tag: Panasonic / 0x005D
				var entry = makernote_structure.GetEntry (0, (ushort) 0x005D);
				Assert.IsNotNull (entry, "Entry 0x005D missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x005E (0x005e/Undefined/4) "0 0 0 1"
			{
				// TODO: Unknown IFD tag: Panasonic / 0x005E
				var entry = makernote_structure.GetEntry (0, (ushort) 0x005E);
				Assert.IsNotNull (entry, "Entry 0x005E missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 0, 0, 0, 1 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Panasonic.0x005F (0x005f/Undefined/4) "0 0 0 0"
			{
				// TODO: Unknown IFD tag: Panasonic / 0x005F
				var entry = makernote_structure.GetEntry (0, (ushort) 0x005F);
				Assert.IsNotNull (entry, "Entry 0x005F missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 0, 0, 0, 0 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Panasonic.0x0060 (0x0060/Undefined/4) "0 1 0 0"
			{
				// TODO: Unknown IFD tag: Panasonic / 0x0060
				var entry = makernote_structure.GetEntry (0, (ushort) 0x0060);
				Assert.IsNotNull (entry, "Entry 0x0060 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 0, 1, 0, 0 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Panasonic.0x8000 (MakerNoteVersion/Undefined/4) "48 49 51 48"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PanasonicMakerNoteEntryTag.MakerNoteVersion);
				Assert.IsNotNull (entry, "Entry 0x8000 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 48, 49, 51, 48 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Panasonic.0x8002 (0x8002/Short/1) "0"
			{
				// TODO: Unknown IFD tag: Panasonic / 0x8002
				var entry = makernote_structure.GetEntry (0, (ushort) 0x8002);
				Assert.IsNotNull (entry, "Entry 0x8002 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Panasonic.0x8003 (0x8003/Short/1) "1"
			{
				// TODO: Unknown IFD tag: Panasonic / 0x8003
				var entry = makernote_structure.GetEntry (0, (ushort) 0x8003);
				Assert.IsNotNull (entry, "Entry 0x8003 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1, (entry as ShortIFDEntry).Value);
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
			// Panasonic.0x8010 (BabyAge/Ascii/20) "9999:99:99 00:00:00"
			{
				var entry = makernote_structure.GetEntry (0, (ushort) PanasonicMakerNoteEntryTag.BabyAge2);
				Assert.IsNotNull (entry, "Entry 0x8010 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("9999:99:99 00:00:00", (entry as StringIFDEntry).Value);
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
			// Photo.0xA005 (InteroperabilityTag/SubIFD/1) "8372"
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
			// Photo.0xA405 (FocalLengthIn35mmFilm/Short/1) "28"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.FocalLengthIn35mmFilm);
				Assert.IsNotNull (entry, "Entry 0xA405 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (28, (entry as ShortIFDEntry).Value);
			}
			// Image.0xC6D2 (0xc6d2/Undefined/64) "0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0"
			{
				// TODO: Unknown IFD tag: Image / 0xC6D2
				var entry = structure.GetEntry (0, (ushort) 0xC6D2);
				Assert.IsNotNull (entry, "Entry 0xC6D2 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Image.0xC6D3 (0xc6d3/Undefined/64) "0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0"
			{
				// TODO: Unknown IFD tag: Image / 0xC6D3
				var entry = structure.GetEntry (0, (ushort) 0xC6D3);
				Assert.IsNotNull (entry, "Entry 0xC6D3 missing in IFD 0");
				Assert.IsNotNull (entry as UndefinedIFDEntry, "Entry is not an undefined IFD entry!");
				var parsed_bytes = (entry as UndefinedIFDEntry).Data.Data;
				var bytes = new byte [] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
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
			// Thumbnail.0x0201 (JPEGInterchangeFormat/ThumbnailDataIFD/1) "8692"
			{
				var entry = structure.GetEntry (1, (ushort) IFDEntryTag.JPEGInterchangeFormat);
				Assert.IsNotNull (entry, "Entry 0x0201 missing in IFD 1");
				Assert.IsNotNull (entry as ThumbnailDataIFDEntry, "Entry is not a thumbnail IFD!");
			}
			// Thumbnail.0x0202 (JPEGInterchangeFormatLength/Long/1) "5768"
			{
				var entry = structure.GetEntry (1, (ushort) IFDEntryTag.JPEGInterchangeFormatLength);
				Assert.IsNotNull (entry, "Entry 0x0202 missing in IFD 1");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (5768, (entry as LongIFDEntry).Value);
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
