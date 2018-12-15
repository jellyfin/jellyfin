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
	public class JpegNikon2Bibble5Test
	{
		[Test]
		public void Test ()
		{
			ImageTest.Run ("sample_nikon2_bibble5.jpg",
				new JpegNikon2Bibble5TestInvariantValidator (),
				NoModificationValidator.Instance,
				new CommentModificationValidator (String.Empty),
				new TagCommentModificationValidator (null, TagTypes.TiffIFD, true),
				new TagCommentModificationValidator (null, TagTypes.XMP, true),
				new TagKeywordsModificationValidator (new string[] {}, TagTypes.XMP, true)
			);
		}
	}

	public class JpegNikon2Bibble5TestInvariantValidator : IMetadataInvariantValidator
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
			// Image.0x011A (XResolution/Rational/1) "150/1"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.XResolution);
				Assert.IsNotNull (entry, "Entry 0x011A missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (150, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (1, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Image.0x011B (YResolution/Rational/1) "150/1"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.YResolution);
				Assert.IsNotNull (entry, "Entry 0x011B missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (150, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (1, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Image.0x0128 (ResolutionUnit/Short/1) "2"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.ResolutionUnit);
				Assert.IsNotNull (entry, "Entry 0x0128 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (2, (entry as ShortIFDEntry).Value);
			}
			// Image.0x0131 (Software/Ascii/17) "Bibble 5 Pro 5.0"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.Software);
				Assert.IsNotNull (entry, "Entry 0x0131 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("Bibble 5 Pro 5.0", (entry as StringIFDEntry).Value);
			}
			// Image.0x8769 (ExifTag/SubIFD/1) "8"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.ExifIFD);
				Assert.IsNotNull (entry, "Entry 0x8769 missing in IFD 0");
				Assert.IsNotNull (entry as SubIFDEntry, "Entry is not a sub IFD!");
			}

			var exif = structure.GetEntry (0, (ushort) IFDEntryTag.ExifIFD) as SubIFDEntry;
			Assert.IsNotNull (exif, "Exif tag not found");
			var exif_structure = exif.Structure;

			// Photo.0x010F (0x010f/Ascii/18) "NIKON CORPORATION"
			{
				var entry = exif_structure.GetEntry (0, (ushort) IFDEntryTag.Make);
				Assert.IsNotNull (entry, "Entry 0x010F missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("NIKON CORPORATION", (entry as StringIFDEntry).Value);
			}
			// Photo.0x0110 (0x0110/Ascii/10) "NIKON D90"
			{
				var entry = exif_structure.GetEntry (0, (ushort) IFDEntryTag.Model);
				Assert.IsNotNull (entry, "Entry 0x0110 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("NIKON D90", (entry as StringIFDEntry).Value);
			}
			// Photo.0x0132 (0x0132/Ascii/20) "2010:02:03 10:51:31"
			{
				var entry = exif_structure.GetEntry (0, (ushort) IFDEntryTag.DateTime);
				Assert.IsNotNull (entry, "Entry 0x0132 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("2010:02:03 10:51:31", (entry as StringIFDEntry).Value);
			}
			// Photo.0x829A (ExposureTime/Rational/1) "10/600"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ExposureTime);
				Assert.IsNotNull (entry, "Entry 0x829A missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (10, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (600, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Photo.0x829D (FNumber/Rational/1) "5557453/1048576"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.FNumber);
				Assert.IsNotNull (entry, "Entry 0x829D missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (5557453, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (1048576, (entry as RationalIFDEntry).Value.Denominator);
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
			// Photo.0x9003 (DateTimeOriginal/Ascii/20) "2010:01:02 13:45:49"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.DateTimeOriginal);
				Assert.IsNotNull (entry, "Entry 0x9003 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("2010:01:02 13:45:49", (entry as StringIFDEntry).Value);
			}
			// Photo.0x9004 (DateTimeDigitized/Ascii/20) "2010:01:02 13:45:49"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.DateTimeDigitized);
				Assert.IsNotNull (entry, "Entry 0x9004 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("2010:01:02 13:45:49", (entry as StringIFDEntry).Value);
			}
			// Photo.0x9201 (ShutterSpeedValue/Rational/1) "193557/32768"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ShutterSpeedValue);
				Assert.IsNotNull (entry, "Entry 0x9201 missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (193557, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (32768, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Photo.0x9202 (ApertureValue/Rational/1) "1261433/262144"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ApertureValue);
				Assert.IsNotNull (entry, "Entry 0x9202 missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (1261433, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (262144, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Photo.0x9204 (ExposureBiasValue/SRational/1) "0/6"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ExposureBiasValue);
				Assert.IsNotNull (entry, "Entry 0x9204 missing in IFD 0");
				Assert.IsNotNull (entry as SRationalIFDEntry, "Entry is not a srational!");
				Assert.AreEqual (0, (entry as SRationalIFDEntry).Value.Numerator);
				Assert.AreEqual (6, (entry as SRationalIFDEntry).Value.Denominator);
			}
			// Photo.0x9205 (MaxApertureValue/Rational/1) "49/10"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.MaxApertureValue);
				Assert.IsNotNull (entry, "Entry 0x9205 missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (49, (entry as RationalIFDEntry).Value.Numerator);
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
			// Photo.0x920A (FocalLength/Rational/1) "120/1"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.FocalLength);
				Assert.IsNotNull (entry, "Entry 0x920A missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (120, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (1, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Photo.0x9290 (SubSecTime/Ascii/4) "617"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.SubsecTime);
				Assert.IsNotNull (entry, "Entry 0x9290 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("617", (entry as StringIFDEntry).Value);
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
			// Photo.0xA217 (SensingMethod/Byte/1) "2 "
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.SensingMethod);
				Assert.IsNotNull (entry, "Entry 0xA217 missing in IFD 0");
				Assert.IsNotNull (entry as ByteIFDEntry, "Entry is not a byte!");
				Assert.AreEqual (2 , (entry as ByteIFDEntry).Value);
			}
			// Photo.0xA301 (SceneType/Byte/1) "1 "
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.SceneType);
				Assert.IsNotNull (entry, "Entry 0xA301 missing in IFD 0");
				Assert.IsNotNull (entry as ByteIFDEntry, "Entry is not a byte!");
				Assert.AreEqual (1 , (entry as ByteIFDEntry).Value);
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
			// Photo.0xA405 (FocalLengthIn35mmFilm/Short/1) "180"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.FocalLengthIn35mmFilm);
				Assert.IsNotNull (entry, "Entry 0xA405 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (180, (entry as ShortIFDEntry).Value);
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
			// Photo.0xA40C (SubjectDistanceRange/Short/1) "0"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.SubjectDistanceRange);
				Assert.IsNotNull (entry, "Entry 0xA40C missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}

			//  ---------- End of IFD tests ----------


			//  ---------- Start of XMP tests ----------

			XmpTag xmp = file.GetTag (TagTypes.XMP) as XmpTag;
			// Xmp.tiff.Model (XmpText/9) "NIKON D90"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.TIFF_NS, "Model");
				Assert.IsNotNull (node);
				Assert.AreEqual ("NIKON D90", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.tiff.Make (XmpText/17) "NIKON CORPORATION"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.TIFF_NS, "Make");
				Assert.IsNotNull (node);
				Assert.AreEqual ("NIKON CORPORATION", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.tiff.ImageWidth (XmpText/4) "4290"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.TIFF_NS, "ImageWidth");
				Assert.IsNotNull (node);
				Assert.AreEqual ("4290", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.tiff.ImageLength (XmpText/4) "2860"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.TIFF_NS, "ImageLength");
				Assert.IsNotNull (node);
				Assert.AreEqual ("2860", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.tiff.FNumber (XmpText/5) "53/10"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.TIFF_NS, "FNumber");
				Assert.IsNotNull (node);
				Assert.AreEqual ("53/10", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.tiff.XResolution (XmpText/5) "150/1"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.TIFF_NS, "XResolution");
				Assert.IsNotNull (node);
				Assert.AreEqual ("150/1", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.tiff.YResolution (XmpText/5) "150/1"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.TIFF_NS, "YResolution");
				Assert.IsNotNull (node);
				Assert.AreEqual ("150/1", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.tiff.ResolutionUnit (XmpText/1) "2"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.TIFF_NS, "ResolutionUnit");
				Assert.IsNotNull (node);
				Assert.AreEqual ("2", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.exif.ExposureProgram (XmpText/1) "3"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.EXIF_NS, "ExposureProgram");
				Assert.IsNotNull (node);
				Assert.AreEqual ("3", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.exif.MeteringMode (XmpText/1) "5"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.EXIF_NS, "MeteringMode");
				Assert.IsNotNull (node);
				Assert.AreEqual ("5", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.exif.LightSource (XmpText/1) "0"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.EXIF_NS, "LightSource");
				Assert.IsNotNull (node);
				Assert.AreEqual ("0", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.exif.ExposureMode (XmpText/1) "0"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.EXIF_NS, "ExposureMode");
				Assert.IsNotNull (node);
				Assert.AreEqual ("0", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.exif.WhiteBalance (XmpText/1) "0"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.EXIF_NS, "WhiteBalance");
				Assert.IsNotNull (node);
				Assert.AreEqual ("0", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.exif.FocalLengthIn35mmFilm (XmpText/3) "180"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.EXIF_NS, "FocalLengthIn35mmFilm");
				Assert.IsNotNull (node);
				Assert.AreEqual ("180", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.exif.SceneCaptureType (XmpText/1) "0"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.EXIF_NS, "SceneCaptureType");
				Assert.IsNotNull (node);
				Assert.AreEqual ("0", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.exif.Contrast (XmpText/1) "0"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.EXIF_NS, "Contrast");
				Assert.IsNotNull (node);
				Assert.AreEqual ("0", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.exif.Saturation (XmpText/1) "0"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.EXIF_NS, "Saturation");
				Assert.IsNotNull (node);
				Assert.AreEqual ("0", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.exif.Sharpness (XmpText/1) "0"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.EXIF_NS, "Sharpness");
				Assert.IsNotNull (node);
				Assert.AreEqual ("0", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.exif.SubjectDistanceRange (XmpText/1) "0"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.EXIF_NS, "SubjectDistanceRange");
				Assert.IsNotNull (node);
				Assert.AreEqual ("0", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.exif.ISOSpeedRating (XmpText/3) "200"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.EXIF_NS, "ISOSpeedRating");
				Assert.IsNotNull (node);
				Assert.AreEqual ("200", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.exif.Flash (XmpText/1) "0"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.EXIF_NS, "Flash");
				Assert.IsNotNull (node);
				Assert.AreEqual ("0", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.exif.MaxApertureValue (XmpText/5) "49/10"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.EXIF_NS, "MaxApertureValue");
				Assert.IsNotNull (node);
				Assert.AreEqual ("49/10", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.exif.ExposureTime (XmpText/6) "10/600"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.EXIF_NS, "ExposureTime");
				Assert.IsNotNull (node);
				Assert.AreEqual ("10/600", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.exif.ExposureBiasValue (XmpText/3) "0/6"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.EXIF_NS, "ExposureBiasValue");
				Assert.IsNotNull (node);
				Assert.AreEqual ("0/6", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.exif.FocalLength (XmpText/7) "1200/10"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.EXIF_NS, "FocalLength");
				Assert.IsNotNull (node);
				Assert.AreEqual ("1200/10", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.photoshop.DateCreated (XmpText/24) "2010-01-02T13:45:49.000Z"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.PHOTOSHOP_NS, "DateCreated");
				Assert.IsNotNull (node);
				Assert.AreEqual ("2010-01-02T13:45:49.000Z", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.xmp.Rating (XmpText/1) "0"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.XAP_NS, "Rating");
				Assert.IsNotNull (node);
				Assert.AreEqual ("0", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}

			//  ---------- End of XMP tests ----------

		}
	}
}
