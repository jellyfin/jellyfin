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
	public class TiffGimp2Test
	{
		[Test]
		public void Test ()
		{
			ImageTest.Run ("sample_gimp.tiff",
				new TiffGimp2TestInvariantValidator (),
				NoModificationValidator.Instance
			);
		}
	}

	public class TiffGimp2TestInvariantValidator : IMetadataInvariantValidator
	{
		public void ValidateMetadataInvariants (Image.File file)
		{
			Assert.IsNotNull (file);
			//  ---------- Start of IFD tests ----------

			var tag = file.GetTag (TagTypes.TiffIFD) as IFDTag;
			Assert.IsNotNull (tag, "IFD tag not found");

			var structure = tag.Structure;

			// Image.0x00FE (NewSubfileType/Long/1) "0"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.NewSubfileType);
				Assert.IsNotNull (entry, "Entry 0x00FE missing in IFD 0");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (0, (entry as LongIFDEntry).Value);
			}
			// Image.0x0100 (ImageWidth/Short/1) "10"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.ImageWidth);
				Assert.IsNotNull (entry, "Entry 0x0100 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (10, (entry as ShortIFDEntry).Value);
			}
			// Image.0x0101 (ImageLength/Short/1) "10"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.ImageLength);
				Assert.IsNotNull (entry, "Entry 0x0101 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (10, (entry as ShortIFDEntry).Value);
			}
			// Image.0x0102 (BitsPerSample/Short/3) "8 8 8"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.BitsPerSample);
				Assert.IsNotNull (entry, "Entry 0x0102 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 8, 8, 8 }, (entry as ShortArrayIFDEntry).Values);
			}
			// Image.0x0103 (Compression/Short/1) "5"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.Compression);
				Assert.IsNotNull (entry, "Entry 0x0103 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (5, (entry as ShortIFDEntry).Value);
			}
			// Image.0x0106 (PhotometricInterpretation/Short/1) "2"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.PhotometricInterpretation);
				Assert.IsNotNull (entry, "Entry 0x0106 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (2, (entry as ShortIFDEntry).Value);
			}
			// Image.0x010D (DocumentName/Ascii/30) "/home/ruben/Desktop/test.tiff"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.DocumentName);
				Assert.IsNotNull (entry, "Entry 0x010D missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("/home/ruben/Desktop/test.tiff", (entry as StringIFDEntry).Value);
			}
			// Image.0x010E (ImageDescription/Ascii/18) "Created with GIMP"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.ImageDescription);
				Assert.IsNotNull (entry, "Entry 0x010E missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("Created with GIMP", (entry as StringIFDEntry).Value);
			}
			// Image.0x010F (Make/Ascii/6) "Canon"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.Make);
				Assert.IsNotNull (entry, "Entry 0x010F missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("Canon", (entry as StringIFDEntry).Value);
			}
			// Image.0x0110 (Model/Ascii/26) "Canon DIGITAL IXUS 850 IS"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.Model);
				Assert.IsNotNull (entry, "Entry 0x0110 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("Canon DIGITAL IXUS 850 IS", (entry as StringIFDEntry).Value);
			}
			// Image.0x0111 (StripOffsets/StripOffsets/1) "6980"
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
			// Image.0x0115 (SamplesPerPixel/Short/1) "3"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.SamplesPerPixel);
				Assert.IsNotNull (entry, "Entry 0x0115 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (3, (entry as ShortIFDEntry).Value);
			}
			// Image.0x0116 (RowsPerStrip/Short/1) "64"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.RowsPerStrip);
				Assert.IsNotNull (entry, "Entry 0x0116 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (64, (entry as ShortIFDEntry).Value);
			}
			// Image.0x0117 (StripByteCounts/Long/1) "49"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.StripByteCounts);
				Assert.IsNotNull (entry, "Entry 0x0117 missing in IFD 0");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (49, (entry as LongIFDEntry).Value);
			}
			// Image.0x011A (XResolution/Rational/1) "1207959552/16777216"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.XResolution);
				Assert.IsNotNull (entry, "Entry 0x011A missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (1207959552, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (16777216, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Image.0x011B (YResolution/Rational/1) "1207959552/16777216"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.YResolution);
				Assert.IsNotNull (entry, "Entry 0x011B missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (1207959552, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (16777216, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Image.0x011C (PlanarConfiguration/Short/1) "1"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.PlanarConfiguration);
				Assert.IsNotNull (entry, "Entry 0x011C missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1, (entry as ShortIFDEntry).Value);
			}
			// Image.0x0128 (ResolutionUnit/Short/1) "2"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.ResolutionUnit);
				Assert.IsNotNull (entry, "Entry 0x0128 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (2, (entry as ShortIFDEntry).Value);
			}
			// Image.0x0131 (Software/Ascii/20) "CHDK ver. 0.9.8-782"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.Software);
				Assert.IsNotNull (entry, "Entry 0x0131 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("CHDK ver. 0.9.8-782", (entry as StringIFDEntry).Value);
			}
			// Image.0x0132 (DateTime/Ascii/20) "2009:07:05 19:33:52"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.DateTime);
				Assert.IsNotNull (entry, "Entry 0x0132 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("2009:07:05 19:33:52", (entry as StringIFDEntry).Value);
			}
			// Image.0x013D (0x013d/Short/1) "2"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.Predictor);
				Assert.IsNotNull (entry, "Entry 0x013D missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (2, (entry as ShortIFDEntry).Value);
			}
			// Image.0x02BC (XMLPacket/XMLPacket/6285) "60 63 120 112 97 99 107 101 116 32 98 101 103 105 110 61 34 239 187 191 34 32 105 100 61 34 87 53 77 48 77 112 67 101 104 105 72 122 114 101 83 122 78 84 99 122 107 99 57 100 34 63 62 10 60 120 58 120 109 112 109 101 116 97 32 120 109 108 110 115 58 120 61 34 97 100 111 98 101 58 110 115 58 109 101 116 97 47 34 32 120 58 120 109 112 116 107 61 34 88 77 80 32 67 111 114 101 32 52 46 49 46 49 45 69 120 105 118 50 34 62 10 32 60 114 100 102 58 82 68 70 32 120 109 108 110 115 58 114 100 102 61 34 104 116 116 112 58 47 47 119 119 119 46 119 51 46 111 114 103 47 49 57 57 57 47 48 50 47 50 50 45 114 100 102 45 115 121 110 116 97 120 45 110 115 35 34 62 10 32 32 60 114 100 102 58 68 101 115 99 114 105 112 116 105 111 110 32 114 100 102 58 97 98 111 117 116 61 34 34 10 32 32 32 32 120 109 108 110 115 58 101 120 105 102 61 34 104 116 116 112 58 47 47 110 115 46 97 100 111 98 101 46 99 111 109 47 101 120 105 102 47 49 46 48 47 34 10 32 32 32 32 120 109 108 110 115 58 120 97 112 61 34 104 116 116 112 58 47 47 110 115 46 97 100 111 98 101 46 99 111 109 47 120 97 112 47 49 46 48 47 34 10 32 32 32 32 120 109 108 110 115 58 99 114 115 61 34 104 116 116 112 58 47 47 110 115 46 97 100 111 98 101 46 99 111 109 47 99 97 109 101 114 97 45 114 97 119 45 115 101 116 116 105 110 103 115 47 49 46 48 47 34 10 32 32 32 32 120 109 108 110 115 58 116 105 102 102 61 34 104 116 116 112 58 47 47 110 115 46 97 100 111 98 101 46 99 111 109 47 116 105 102 102 47 49 46 48 47 34 10 32 32 32 32 120 109 108 110 115 58 100 99 61 34 104 116 116 112 58 47 47 112 117 114 108 46 111 114 103 47 100 99 47 101 108 101 109 101 110 116 115 47 49 46 49 47 34 10 32 32 32 101 120 105 102 58 69 120 105 102 86 101 114 115 105 111 110 61 34 48 50 50 49 34 10 32 32 32 101 120 105 102 58 68 97 116 101 84 105 109 101 79 114 105 103 105 110 97 108 61 34 50 48 48 57 45 48 55 45 48 53 84 49 57 58 51 51 58 53 50 34 10 32 32 32 101 120 105 102 58 69 120 112 111 115 117 114 101 84 105 109 101 61 34 49 47 51 48 34 10 32 32 32 101 120 105 102 58 70 78 117 109 98 101 114 61 34 50 56 47 49 48 34 10 32 32 32 101 120 105 102 58 69 120 112 111 115 117 114 101 80 114 111 103 114 97 109 61 34 48 34 10 32 32 32 101 120 105 102 58 83 104 117 116 116 101 114 83 112 101 101 100 86 97 108 117 101 61 34 52 57 48 54 56 57 49 47 49 48 48 48 48 48 48 34 10 32 32 32 101 120 105 102 58 65 112 101 114 116 117 114 101 86 97 108 117 101 61 34 50 57 55 48 56 53 52 47 49 48 48 48 48 48 48 34 10 32 32 32 101 120 105 102 58 69 120 112 111 115 117 114 101 66 105 97 115 86 97 108 117 101 61 34 45 57 54 47 57 54 34 10 32 32 32 101 120 105 102 58 77 97 120 65 112 101 114 116 117 114 101 86 97 108 117 101 61 34 50 57 51 47 57 54 34 10 32 32 32 101 120 105 102 58 77 101 116 101 114 105 110 103 77 111 100 101 61 34 53 34 10 32 32 32 101 120 105 102 58 70 111 99 97 108 76 101 110 103 116 104 61 34 52 54 48 48 47 49 48 48 48 34 10 32 32 32 101 120 105 102 58 70 111 99 97 108 76 101 110 103 116 104 73 110 51 53 109 109 70 105 108 109 61 34 50 55 34 10 32 32 32 120 97 112 58 77 111 100 105 102 121 68 97 116 101 61 34 50 48 48 57 45 48 55 45 48 53 84 49 57 58 51 51 58 53 50 43 48 51 58 48 48 34 10 32 32 32 120 97 112 58 67 114 101 97 116 111 114 84 111 111 108 61 34 67 72 68 75 32 118 101 114 46 32 48 46 57 46 56 45 55 56 50 34 10 32 32 32 120 97 112 58 82 97 116 105 110 103 61 34 48 34 10 32 32 32 99 114 115 58 86 101 114 115 105 111 110 61 34 51 46 55 34 10 32 32 32 99 114 115 58 87 104 105 116 101 66 97 108 97 110 99 101 61 34 67 117 115 116 111 109 34 10 32 32 32 99 114 115 58 84 101 109 112 101 114 97 116 117 114 101 61 34 54 53 53 48 34 10 32 32 32 99 114 115 58 84 105 110 116 61 34 43 49 53 48 34 10 32 32 32 99 114 115 58 69 120 112 111 115 117 114 101 61 34 48 46 48 48 34 10 32 32 32 99 114 115 58 83 104 97 100 111 119 115 61 34 53 34 10 32 32 32 99 114 115 58 66 114 105 103 104 116 110 101 115 115 61 34 43 53 48 34 10 32 32 32 99 114 115 58 67 111 110 116 114 97 115 116 61 34 43 50 53 34 10 32 32 32 99 114 115 58 83 97 116 117 114 97 116 105 111 110 61 34 48 34 10 32 32 32 99 114 115 58 83 104 97 114 112 110 101 115 115 61 34 50 53 34 10 32 32 32 99 114 115 58 76 117 109 105 110 97 110 99 101 83 109 111 111 116 104 105 110 103 61 34 51 49 34 10 32 32 32 99 114 115 58 67 111 108 111 114 78 111 105 115 101 82 101 100 117 99 116 105 111 110 61 34 49 57 34 10 32 32 32 99 114 115 58 67 104 114 111 109 97 116 105 99 65 98 101 114 114 97 116 105 111 110 82 61 34 48 34 10 32 32 32 99 114 115 58 67 104 114 111 109 97 116 105 99 65 98 101 114 114 97 116 105 111 110 66 61 34 48 34 10 32 32 32 99 114 115 58 86 105 103 110 101 116 116 101 65 109 111 117 110 116 61 34 48 34 10 32 32 32 99 114 115 58 83 104 97 100 111 119 84 105 110 116 61 34 43 49 48 48 34 10 32 32 32 99 114 115 58 82 101 100 72 117 101 61 34 43 51 51 34 10 32 32 32 99 114 115 58 82 101 100 83 97 116 117 114 97 116 105 111 110 61 34 45 51 51 34 10 32 32 32 99 114 115 58 71 114 101 101 110 72 117 101 61 34 45 49 48 48 34 10 32 32 32 99 114 115 58 71 114 101 101 110 83 97 116 117 114 97 116 105 111 110 61 34 45 49 48 48 34 10 32 32 32 99 114 115 58 66 108 117 101 72 117 101 61 34 43 49 48 48 34 10 32 32 32 99 114 115 58 66 108 117 101 83 97 116 117 114 97 116 105 111 110 61 34 43 51 51 34 10 32 32 32 99 114 115 58 70 105 108 108 76 105 103 104 116 61 34 48 34 10 32 32 32 99 114 115 58 86 105 98 114 97 110 99 101 61 34 48 34 10 32 32 32 99 114 115 58 72 105 103 104 108 105 103 104 116 82 101 99 111 118 101 114 121 61 34 48 34 10 32 32 32 99 114 115 58 72 117 101 65 100 106 117 115 116 109 101 110 116 82 101 100 61 34 48 34 10 32 32 32 99 114 115 58 72 117 101 65 100 106 117 115 116 109 101 110 116 79 114 97 110 103 101 61 34 48 34 10 32 32 32 99 114 115 58 72 117 101 65 100 106 117 115 116 109 101 110 116 89 101 108 108 111 119 61 34 48 34 10 32 32 32 99 114 115 58 72 117 101 65 100 106 117 115 116 109 101 110 116 71 114 101 101 110 61 34 48 34 10 32 32 32 99 114 115 58 72 117 101 65 100 106 117 115 116 109 101 110 116 65 113 117 97 61 34 48 34 10 32 32 32 99 114 115 58 72 117 101 65 100 106 117 115 116 109 101 110 116 66 108 117 101 61 34 48 34 10 32 32 32 99 114 115 58 72 117 101 65 100 106 117 115 116 109 101 110 116 80 117 114 112 108 101 61 34 48 34 10 32 32 32 99 114 115 58 72 117 101 65 100 106 117 115 116 109 101 110 116 77 97 103 101 110 116 97 61 34 48 34 10 32 32 32 99 114 115 58 83 97 116 117 114 97 116 105 111 110 65 100 106 117 115 116 109 101 110 116 82 101 100 61 34 48 34 10 32 32 32 99 114 115 58 83 97 116 117 114 97 116 105 111 110 65 100 106 117 115 116 109 101 110 116 79 114 97 110 103 101 61 34 48 34 10 32 32 32 99 114 115 58 83 97 116 117 114 97 116 105 111 110 65 100 106 117 115 116 109 101 110 116 89 101 108 108 111 119 61 34 48 34 10 32 32 32 99 114 115 58 83 97 116 117 114 97 116 105 111 110 65 100 106 117 115 116 109 101 110 116 71 114 101 101 110 61 34 48 34 10 32 32 32 99 114 115 58 83 97 116 117 114 97 116 105 111 110 65 100 106 117 115 116 109 101 110 116 65 113 117 97 61 34 48 34 10 32 32 32 99 114 115 58 83 97 116 117 114 97 116 105 111 110 65 100 106 117 115 116 109 101 110 116 66 108 117 101 61 34 48 34 10 32 32 32 99 114 115 58 83 97 116 117 114 97 116 105 111 110 65 100 106 117 115 116 109 101 110 116 80 117 114 112 108 101 61 34 48 34 10 32 32 32 99 114 115 58 83 97 116 117 114 97 116 105 111 110 65 100 106 117 115 116 109 101 110 116 77 97 103 101 110 116 97 61 34 48 34 10 32 32 32 99 114 115 58 76 117 109 105 110 97 110 99 101 65 100 106 117 115 116 109 101 110 116 82 101 100 61 34 48 34 10 32 32 32 99 114 115 58 76 117 109 105 110 97 110 99 101 65 100 106 117 115 116 109 101 110 116 79 114 97 110 103 101 61 34 48 34 10 32 32 32 99 114 115 58 76 117 109 105 110 97 110 99 101 65 100 106 117 115 116 109 101 110 116 89 101 108 108 111 119 61 34 48 34 10 32 32 32 99 114 115 58 76 117 109 105 110 97 110 99 101 65 100 106 117 115 116 109 101 110 116 71 114 101 101 110 61 34 48 34 10 32 32 32 99 114 115 58 76 117 109 105 110 97 110 99 101 65 100 106 117 115 116 109 101 110 116 65 113 117 97 61 34 48 34 10 32 32 32 99 114 115 58 76 117 109 105 110 97 110 99 101 65 100 106 117 115 116 109 101 110 116 66 108 117 101 61 34 48 34 10 32 32 32 99 114 115 58 76 117 109 105 110 97 110 99 101 65 100 106 117 115 116 109 101 110 116 80 117 114 112 108 101 61 34 48 34 10 32 32 32 99 114 115 58 76 117 109 105 110 97 110 99 101 65 100 106 117 115 116 109 101 110 116 77 97 103 101 110 116 97 61 34 48 34 10 32 32 32 99 114 115 58 83 112 108 105 116 84 111 110 105 110 103 83 104 97 100 111 119 72 117 101 61 34 48 34 10 32 32 32 99 114 115 58 83 112 108 105 116 84 111 110 105 110 103 83 104 97 100 111 119 83 97 116 117 114 97 116 105 111 110 61 34 48 34 10 32 32 32 99 114 115 58 83 112 108 105 116 84 111 110 105 110 103 72 105 103 104 108 105 103 104 116 72 117 101 61 34 48 34 10 32 32 32 99 114 115 58 83 112 108 105 116 84 111 110 105 110 103 72 105 103 104 108 105 103 104 116 83 97 116 117 114 97 116 105 111 110 61 34 48 34 10 32 32 32 99 114 115 58 83 112 108 105 116 84 111 110 105 110 103 66 97 108 97 110 99 101 61 34 48 34 10 32 32 32 99 114 115 58 80 97 114 97 109 101 116 114 105 99 83 104 97 100 111 119 115 61 34 48 34 10 32 32 32 99 114 115 58 80 97 114 97 109 101 116 114 105 99 68 97 114 107 115 61 34 48 34 10 32 32 32 99 114 115 58 80 97 114 97 109 101 116 114 105 99 76 105 103 104 116 115 61 34 48 34 10 32 32 32 99 114 115 58 80 97 114 97 109 101 116 114 105 99 72 105 103 104 108 105 103 104 116 115 61 34 48 34 10 32 32 32 99 114 115 58 80 97 114 97 109 101 116 114 105 99 83 104 97 100 111 119 83 112 108 105 116 61 34 50 53 34 10 32 32 32 99 114 115 58 80 97 114 97 109 101 116 114 105 99 77 105 100 116 111 110 101 83 112 108 105 116 61 34 53 48 34 10 32 32 32 99 114 115 58 80 97 114 97 109 101 116 114 105 99 72 105 103 104 108 105 103 104 116 83 112 108 105 116 61 34 55 53 34 10 32 32 32 99 114 115 58 67 111 110 118 101 114 116 84 111 71 114 97 121 115 99 97 108 101 61 34 70 97 108 115 101 34 10 32 32 32 99 114 115 58 84 111 110 101 67 117 114 118 101 78 97 109 101 61 34 67 117 115 116 111 109 34 10 32 32 32 99 114 115 58 67 97 109 101 114 97 80 114 111 102 105 108 101 61 34 69 109 98 101 100 100 101 100 34 10 32 32 32 99 114 115 58 72 97 115 83 101 116 116 105 110 103 115 61 34 84 114 117 101 34 10 32 32 32 99 114 115 58 72 97 115 67 114 111 112 61 34 70 97 108 115 101 34 10 32 32 32 99 114 115 58 65 108 114 101 97 100 121 65 112 112 108 105 101 100 61 34 70 97 108 115 101 34 10 32 32 32 116 105 102 102 58 73 109 97 103 101 87 105 100 116 104 61 34 50 53 54 34 10 32 32 32 116 105 102 102 58 73 109 97 103 101 76 101 110 103 116 104 61 34 49 57 50 34 10 32 32 32 116 105 102 102 58 67 111 109 112 114 101 115 115 105 111 110 61 34 49 34 10 32 32 32 116 105 102 102 58 80 104 111 116 111 109 101 116 114 105 99 73 110 116 101 114 112 114 101 116 97 116 105 111 110 61 34 50 34 10 32 32 32 116 105 102 102 58 79 114 105 101 110 116 97 116 105 111 110 61 34 49 34 10 32 32 32 116 105 102 102 58 83 97 109 112 108 101 115 80 101 114 80 105 120 101 61 34 51 34 10 32 32 32 116 105 102 102 58 80 108 97 110 97 114 67 111 110 102 105 103 117 114 97 116 105 111 110 61 34 49 34 10 32 32 32 116 105 102 102 58 68 97 116 101 84 105 109 101 61 34 50 48 48 57 45 48 55 45 48 53 84 49 57 58 51 51 58 53 50 34 10 32 32 32 116 105 102 102 58 77 97 107 101 61 34 67 97 110 111 110 34 10 32 32 32 116 105 102 102 58 77 111 100 101 108 61 34 67 97 110 111 110 32 68 73 71 73 84 65 76 32 73 88 85 83 32 56 53 48 32 73 83 34 10 32 32 32 116 105 102 102 58 83 111 102 116 119 97 114 101 61 34 67 72 68 75 32 118 101 114 46 32 48 46 57 46 56 45 55 56 50 34 62 10 32 32 32 60 101 120 105 102 58 70 108 97 115 104 10 32 32 32 32 101 120 105 102 58 70 105 114 101 100 61 34 70 97 108 115 101 34 10 32 32 32 32 101 120 105 102 58 82 101 116 117 114 110 61 34 48 34 10 32 32 32 32 101 120 105 102 58 77 111 100 101 61 34 50 34 10 32 32 32 32 101 120 105 102 58 70 117 110 99 116 105 111 110 61 34 70 97 108 115 101 34 10 32 32 32 32 101 120 105 102 58 82 101 100 69 121 101 77 111 100 101 61 34 70 97 108 115 101 34 47 62 10 32 32 32 60 101 120 105 102 58 73 83 79 83 112 101 101 100 82 97 116 105 110 103 115 62 10 32 32 32 32 60 114 100 102 58 83 101 113 62 10 32 32 32 32 32 60 114 100 102 58 108 105 62 52 48 48 60 47 114 100 102 58 108 105 62 10 32 32 32 32 60 47 114 100 102 58 83 101 113 62 10 32 32 32 60 47 101 120 105 102 58 73 83 79 83 112 101 101 100 82 97 116 105 110 103 115 62 10 32 32 32 60 99 114 115 58 84 111 110 101 67 117 114 118 101 62 10 32 32 32 32 60 114 100 102 58 83 101 113 62 10 32 32 32 32 32 60 114 100 102 58 108 105 62 48 44 32 48 60 47 114 100 102 58 108 105 62 10 32 32 32 32 32 60 114 100 102 58 108 105 62 51 53 44 32 49 53 52 60 47 114 100 102 58 108 105 62 10 32 32 32 32 32 60 114 100 102 58 108 105 62 54 52 44 32 53 54 60 47 114 100 102 58 108 105 62 10 32 32 32 32 32 60 114 100 102 58 108 105 62 57 55 44 32 49 56 52 60 47 114 100 102 58 108 105 62 10 32 32 32 32 32 60 114 100 102 58 108 105 62 49 51 52 44 32 49 56 50 60 47 114 100 102 58 108 105 62 10 32 32 32 32 32 60 114 100 102 58 108 105 62 49 53 49 44 32 49 56 57 60 47 114 100 102 58 108 105 62 10 32 32 32 32 32 60 114 100 102 58 108 105 62 49 56 49 44 32 49 55 55 60 47 114 100 102 58 108 105 62 10 32 32 32 32 32 60 114 100 102 58 108 105 62 49 57 50 44 32 49 57 54 60 47 114 100 102 58 108 105 62 10 32 32 32 32 32 60 114 100 102 58 108 105 62 50 48 50 44 32 50 52 48 60 47 114 100 102 58 108 105 62 10 32 32 32 32 32 60 114 100 102 58 108 105 62 50 53 53 44 32 50 53 53 60 47 114 100 102 58 108 105 62 10 32 32 32 32 60 47 114 100 102 58 83 101 113 62 10 32 32 32 60 47 99 114 115 58 84 111 110 101 67 117 114 118 101 62 10 32 32 32 60 116 105 102 102 58 66 105 116 115 80 101 114 83 97 109 112 108 101 62 10 32 32 32 32 60 114 100 102 58 83 101 113 62 10 32 32 32 32 32 60 114 100 102 58 108 105 62 56 32 56 32 56 60 47 114 100 102 58 108 105 62 10 32 32 32 32 60 47 114 100 102 58 83 101 113 62 10 32 32 32 60 47 116 105 102 102 58 66 105 116 115 80 101 114 83 97 109 112 108 101 62 10 32 32 32 60 100 99 58 100 101 115 99 114 105 112 116 105 111 110 62 10 32 32 32 32 60 114 100 102 58 65 108 116 62 10 32 32 32 32 32 60 114 100 102 58 108 105 32 120 109 108 58 108 97 110 103 61 34 120 45 100 101 102 97 117 108 116 34 62 84 101 115 116 60 47 114 100 102 58 108 105 62 10 32 32 32 32 60 47 114 100 102 58 65 108 116 62 10 32 32 32 60 47 100 99 58 100 101 115 99 114 105 112 116 105 111 110 62 10 32 32 60 47 114 100 102 58 68 101 115 99 114 105 112 116 105 111 110 62 10 32 60 47 114 100 102 58 82 68 70 62 10 60 47 120 58 120 109 112 109 101 116 97 62 10 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 10 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 10 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 10 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 10 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 10 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 10 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 10 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 10 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 10 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 10 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 10 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 10 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 10 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 10 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 10 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 10 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 10 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 10 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 10 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 10 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 32 10 60 63 120 112 97 99 107 101 116 32 101 110 100 61 34 119 34 63 62 "
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.XMP);
				Assert.IsNotNull (entry, "Entry 0x02BC missing in IFD 0");
				Assert.IsNotNull (entry as ByteVectorIFDEntry, "Entry is not a byte array!");
			}
			// Image.0x8769 (ExifTag/SubIFD/1) "6730"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.ExifIFD);
				Assert.IsNotNull (entry, "Entry 0x8769 missing in IFD 0");
				Assert.IsNotNull (entry as SubIFDEntry, "Entry is not a sub IFD!");
			}

			var exif = structure.GetEntry (0, (ushort) IFDEntryTag.ExifIFD) as SubIFDEntry;
			Assert.IsNotNull (exif, "Exif tag not found");
			var exif_structure = exif.Structure;

			// Photo.0x829A (ExposureTime/Rational/1) "1/30"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ExposureTime);
				Assert.IsNotNull (entry, "Entry 0x829A missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (1, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (30, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Photo.0x829D (FNumber/Rational/1) "28/10"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.FNumber);
				Assert.IsNotNull (entry, "Entry 0x829D missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (28, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (10, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Photo.0x8822 (ExposureProgram/Short/1) "0"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ExposureProgram);
				Assert.IsNotNull (entry, "Entry 0x8822 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
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
			// Photo.0x9003 (DateTimeOriginal/Ascii/20) "2009:07:05 19:33:52"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.DateTimeOriginal);
				Assert.IsNotNull (entry, "Entry 0x9003 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("2009:07:05 19:33:52", (entry as StringIFDEntry).Value);
			}
			// Photo.0x9201 (ShutterSpeedValue/SRational/1) "4906891/1000000"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ShutterSpeedValue);
				Assert.IsNotNull (entry, "Entry 0x9201 missing in IFD 0");
				Assert.IsNotNull (entry as SRationalIFDEntry, "Entry is not a srational!");
				Assert.AreEqual (4906891, (entry as SRationalIFDEntry).Value.Numerator);
				Assert.AreEqual (1000000, (entry as SRationalIFDEntry).Value.Denominator);
			}
			// Photo.0x9202 (ApertureValue/Rational/1) "2970854/1000000"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ApertureValue);
				Assert.IsNotNull (entry, "Entry 0x9202 missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (2970854, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (1000000, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Photo.0x9204 (ExposureBiasValue/SRational/1) "-96/96"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ExposureBiasValue);
				Assert.IsNotNull (entry, "Entry 0x9204 missing in IFD 0");
				Assert.IsNotNull (entry as SRationalIFDEntry, "Entry is not a srational!");
				Assert.AreEqual (-96, (entry as SRationalIFDEntry).Value.Numerator);
				Assert.AreEqual (96, (entry as SRationalIFDEntry).Value.Denominator);
			}
			// Photo.0x9205 (MaxApertureValue/Rational/1) "293/96"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.MaxApertureValue);
				Assert.IsNotNull (entry, "Entry 0x9205 missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (293, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (96, (entry as RationalIFDEntry).Value.Denominator);
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
			// Photo.0x920A (FocalLength/Rational/1) "4600/1000"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.FocalLength);
				Assert.IsNotNull (entry, "Entry 0x920A missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (4600, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (1000, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Photo.0xA405 (FocalLengthIn35mmFilm/Short/1) "27"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.FocalLengthIn35mmFilm);
				Assert.IsNotNull (entry, "Entry 0xA405 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (27, (entry as ShortIFDEntry).Value);
			}

			//  ---------- End of IFD tests ----------


			//  ---------- Start of XMP tests ----------

			XmpTag xmp = file.GetTag (TagTypes.XMP) as XmpTag;
			// Xmp.exif.ExifVersion (XmpText/4) "0221"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.EXIF_NS, "ExifVersion");
				Assert.IsNotNull (node);
				Assert.AreEqual ("0221", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.exif.DateTimeOriginal (XmpText/19) "2009-07-05T19:33:52"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.EXIF_NS, "DateTimeOriginal");
				Assert.IsNotNull (node);
				Assert.AreEqual ("2009-07-05T19:33:52", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.exif.ExposureTime (XmpText/4) "1/30"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.EXIF_NS, "ExposureTime");
				Assert.IsNotNull (node);
				Assert.AreEqual ("1/30", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.exif.FNumber (XmpText/5) "28/10"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.EXIF_NS, "FNumber");
				Assert.IsNotNull (node);
				Assert.AreEqual ("28/10", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.exif.ExposureProgram (XmpText/1) "0"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.EXIF_NS, "ExposureProgram");
				Assert.IsNotNull (node);
				Assert.AreEqual ("0", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.exif.ShutterSpeedValue (XmpText/15) "4906891/1000000"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.EXIF_NS, "ShutterSpeedValue");
				Assert.IsNotNull (node);
				Assert.AreEqual ("4906891/1000000", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.exif.ApertureValue (XmpText/15) "2970854/1000000"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.EXIF_NS, "ApertureValue");
				Assert.IsNotNull (node);
				Assert.AreEqual ("2970854/1000000", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.exif.ExposureBiasValue (XmpText/6) "-96/96"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.EXIF_NS, "ExposureBiasValue");
				Assert.IsNotNull (node);
				Assert.AreEqual ("-96/96", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.exif.MaxApertureValue (XmpText/6) "293/96"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.EXIF_NS, "MaxApertureValue");
				Assert.IsNotNull (node);
				Assert.AreEqual ("293/96", node.Value);
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
			// Xmp.exif.FocalLength (XmpText/9) "4600/1000"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.EXIF_NS, "FocalLength");
				Assert.IsNotNull (node);
				Assert.AreEqual ("4600/1000", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.exif.FocalLengthIn35mmFilm (XmpText/2) "27"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.EXIF_NS, "FocalLengthIn35mmFilm");
				Assert.IsNotNull (node);
				Assert.AreEqual ("27", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.exif.Flash (XmpText/0) "type="Struct""
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.EXIF_NS, "Flash");
				Assert.IsNotNull (node);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
			}
			// Xmp.exif.Flash/exif:Fired (XmpText/5) "False"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.EXIF_NS, "Flash");
				Assert.IsNotNull (node);
				node = node.GetChild (XmpTag.EXIF_NS, "Fired");
				Assert.IsNotNull (node);
				Assert.AreEqual ("False", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.exif.Flash/exif:Return (XmpText/1) "0"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.EXIF_NS, "Flash");
				Assert.IsNotNull (node);
				node = node.GetChild (XmpTag.EXIF_NS, "Return");
				Assert.IsNotNull (node);
				Assert.AreEqual ("0", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.exif.Flash/exif:Mode (XmpText/1) "2"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.EXIF_NS, "Flash");
				Assert.IsNotNull (node);
				node = node.GetChild (XmpTag.EXIF_NS, "Mode");
				Assert.IsNotNull (node);
				Assert.AreEqual ("2", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.exif.Flash/exif:Function (XmpText/5) "False"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.EXIF_NS, "Flash");
				Assert.IsNotNull (node);
				node = node.GetChild (XmpTag.EXIF_NS, "Function");
				Assert.IsNotNull (node);
				Assert.AreEqual ("False", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.exif.Flash/exif:RedEyeMode (XmpText/5) "False"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.EXIF_NS, "Flash");
				Assert.IsNotNull (node);
				node = node.GetChild (XmpTag.EXIF_NS, "RedEyeMode");
				Assert.IsNotNull (node);
				Assert.AreEqual ("False", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.exif.ISOSpeedRatings (XmpSeq/1) "400"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.EXIF_NS, "ISOSpeedRatings");
				Assert.IsNotNull (node);
				Assert.AreEqual (XmpNodeType.Seq, node.Type);
				Assert.AreEqual ("", node.Value);
				Assert.AreEqual (1, node.Children.Count);
				Assert.AreEqual ("400", node.Children [0].Value);
			}
			// Xmp.xmp.ModifyDate (XmpText/25) "2009-07-05T19:33:52+03:00"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.XAP_NS, "ModifyDate");
				Assert.IsNotNull (node);
				Assert.AreEqual ("2009-07-05T19:33:52+03:00", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.xmp.CreatorTool (XmpText/19) "CHDK ver. 0.9.8-782"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.XAP_NS, "CreatorTool");
				Assert.IsNotNull (node);
				Assert.AreEqual ("CHDK ver. 0.9.8-782", node.Value);
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
			// Xmp.crs.Version (XmpText/3) "3.7"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "Version");
				Assert.IsNotNull (node);
				Assert.AreEqual ("3.7", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.WhiteBalance (XmpText/6) "Custom"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "WhiteBalance");
				Assert.IsNotNull (node);
				Assert.AreEqual ("Custom", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.Temperature (XmpText/4) "6550"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "Temperature");
				Assert.IsNotNull (node);
				Assert.AreEqual ("6550", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.Tint (XmpText/4) "+150"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "Tint");
				Assert.IsNotNull (node);
				Assert.AreEqual ("+150", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.Exposure (XmpText/4) "0.00"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "Exposure");
				Assert.IsNotNull (node);
				Assert.AreEqual ("0.00", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.Shadows (XmpText/1) "5"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "Shadows");
				Assert.IsNotNull (node);
				Assert.AreEqual ("5", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.Brightness (XmpText/3) "+50"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "Brightness");
				Assert.IsNotNull (node);
				Assert.AreEqual ("+50", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.Contrast (XmpText/3) "+25"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "Contrast");
				Assert.IsNotNull (node);
				Assert.AreEqual ("+25", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.Saturation (XmpText/1) "0"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "Saturation");
				Assert.IsNotNull (node);
				Assert.AreEqual ("0", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.Sharpness (XmpText/2) "25"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "Sharpness");
				Assert.IsNotNull (node);
				Assert.AreEqual ("25", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.LuminanceSmoothing (XmpText/2) "31"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "LuminanceSmoothing");
				Assert.IsNotNull (node);
				Assert.AreEqual ("31", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.ColorNoiseReduction (XmpText/2) "19"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "ColorNoiseReduction");
				Assert.IsNotNull (node);
				Assert.AreEqual ("19", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.ChromaticAberrationR (XmpText/1) "0"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "ChromaticAberrationR");
				Assert.IsNotNull (node);
				Assert.AreEqual ("0", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.ChromaticAberrationB (XmpText/1) "0"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "ChromaticAberrationB");
				Assert.IsNotNull (node);
				Assert.AreEqual ("0", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.VignetteAmount (XmpText/1) "0"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "VignetteAmount");
				Assert.IsNotNull (node);
				Assert.AreEqual ("0", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.ShadowTint (XmpText/4) "+100"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "ShadowTint");
				Assert.IsNotNull (node);
				Assert.AreEqual ("+100", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.RedHue (XmpText/3) "+33"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "RedHue");
				Assert.IsNotNull (node);
				Assert.AreEqual ("+33", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.RedSaturation (XmpText/3) "-33"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "RedSaturation");
				Assert.IsNotNull (node);
				Assert.AreEqual ("-33", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.GreenHue (XmpText/4) "-100"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "GreenHue");
				Assert.IsNotNull (node);
				Assert.AreEqual ("-100", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.GreenSaturation (XmpText/4) "-100"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "GreenSaturation");
				Assert.IsNotNull (node);
				Assert.AreEqual ("-100", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.BlueHue (XmpText/4) "+100"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "BlueHue");
				Assert.IsNotNull (node);
				Assert.AreEqual ("+100", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.BlueSaturation (XmpText/3) "+33"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "BlueSaturation");
				Assert.IsNotNull (node);
				Assert.AreEqual ("+33", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.FillLight (XmpText/1) "0"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "FillLight");
				Assert.IsNotNull (node);
				Assert.AreEqual ("0", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.Vibrance (XmpText/1) "0"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "Vibrance");
				Assert.IsNotNull (node);
				Assert.AreEqual ("0", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.HighlightRecovery (XmpText/1) "0"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "HighlightRecovery");
				Assert.IsNotNull (node);
				Assert.AreEqual ("0", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.HueAdjustmentRed (XmpText/1) "0"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "HueAdjustmentRed");
				Assert.IsNotNull (node);
				Assert.AreEqual ("0", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.HueAdjustmentOrange (XmpText/1) "0"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "HueAdjustmentOrange");
				Assert.IsNotNull (node);
				Assert.AreEqual ("0", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.HueAdjustmentYellow (XmpText/1) "0"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "HueAdjustmentYellow");
				Assert.IsNotNull (node);
				Assert.AreEqual ("0", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.HueAdjustmentGreen (XmpText/1) "0"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "HueAdjustmentGreen");
				Assert.IsNotNull (node);
				Assert.AreEqual ("0", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.HueAdjustmentAqua (XmpText/1) "0"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "HueAdjustmentAqua");
				Assert.IsNotNull (node);
				Assert.AreEqual ("0", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.HueAdjustmentBlue (XmpText/1) "0"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "HueAdjustmentBlue");
				Assert.IsNotNull (node);
				Assert.AreEqual ("0", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.HueAdjustmentPurple (XmpText/1) "0"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "HueAdjustmentPurple");
				Assert.IsNotNull (node);
				Assert.AreEqual ("0", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.HueAdjustmentMagenta (XmpText/1) "0"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "HueAdjustmentMagenta");
				Assert.IsNotNull (node);
				Assert.AreEqual ("0", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.SaturationAdjustmentRed (XmpText/1) "0"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "SaturationAdjustmentRed");
				Assert.IsNotNull (node);
				Assert.AreEqual ("0", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.SaturationAdjustmentOrange (XmpText/1) "0"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "SaturationAdjustmentOrange");
				Assert.IsNotNull (node);
				Assert.AreEqual ("0", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.SaturationAdjustmentYellow (XmpText/1) "0"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "SaturationAdjustmentYellow");
				Assert.IsNotNull (node);
				Assert.AreEqual ("0", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.SaturationAdjustmentGreen (XmpText/1) "0"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "SaturationAdjustmentGreen");
				Assert.IsNotNull (node);
				Assert.AreEqual ("0", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.SaturationAdjustmentAqua (XmpText/1) "0"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "SaturationAdjustmentAqua");
				Assert.IsNotNull (node);
				Assert.AreEqual ("0", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.SaturationAdjustmentBlue (XmpText/1) "0"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "SaturationAdjustmentBlue");
				Assert.IsNotNull (node);
				Assert.AreEqual ("0", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.SaturationAdjustmentPurple (XmpText/1) "0"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "SaturationAdjustmentPurple");
				Assert.IsNotNull (node);
				Assert.AreEqual ("0", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.SaturationAdjustmentMagenta (XmpText/1) "0"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "SaturationAdjustmentMagenta");
				Assert.IsNotNull (node);
				Assert.AreEqual ("0", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.LuminanceAdjustmentRed (XmpText/1) "0"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "LuminanceAdjustmentRed");
				Assert.IsNotNull (node);
				Assert.AreEqual ("0", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.LuminanceAdjustmentOrange (XmpText/1) "0"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "LuminanceAdjustmentOrange");
				Assert.IsNotNull (node);
				Assert.AreEqual ("0", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.LuminanceAdjustmentYellow (XmpText/1) "0"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "LuminanceAdjustmentYellow");
				Assert.IsNotNull (node);
				Assert.AreEqual ("0", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.LuminanceAdjustmentGreen (XmpText/1) "0"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "LuminanceAdjustmentGreen");
				Assert.IsNotNull (node);
				Assert.AreEqual ("0", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.LuminanceAdjustmentAqua (XmpText/1) "0"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "LuminanceAdjustmentAqua");
				Assert.IsNotNull (node);
				Assert.AreEqual ("0", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.LuminanceAdjustmentBlue (XmpText/1) "0"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "LuminanceAdjustmentBlue");
				Assert.IsNotNull (node);
				Assert.AreEqual ("0", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.LuminanceAdjustmentPurple (XmpText/1) "0"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "LuminanceAdjustmentPurple");
				Assert.IsNotNull (node);
				Assert.AreEqual ("0", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.LuminanceAdjustmentMagenta (XmpText/1) "0"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "LuminanceAdjustmentMagenta");
				Assert.IsNotNull (node);
				Assert.AreEqual ("0", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.SplitToningShadowHue (XmpText/1) "0"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "SplitToningShadowHue");
				Assert.IsNotNull (node);
				Assert.AreEqual ("0", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.SplitToningShadowSaturation (XmpText/1) "0"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "SplitToningShadowSaturation");
				Assert.IsNotNull (node);
				Assert.AreEqual ("0", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.SplitToningHighlightHue (XmpText/1) "0"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "SplitToningHighlightHue");
				Assert.IsNotNull (node);
				Assert.AreEqual ("0", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.SplitToningHighlightSaturation (XmpText/1) "0"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "SplitToningHighlightSaturation");
				Assert.IsNotNull (node);
				Assert.AreEqual ("0", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.SplitToningBalance (XmpText/1) "0"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "SplitToningBalance");
				Assert.IsNotNull (node);
				Assert.AreEqual ("0", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.ParametricShadows (XmpText/1) "0"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "ParametricShadows");
				Assert.IsNotNull (node);
				Assert.AreEqual ("0", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.ParametricDarks (XmpText/1) "0"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "ParametricDarks");
				Assert.IsNotNull (node);
				Assert.AreEqual ("0", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.ParametricLights (XmpText/1) "0"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "ParametricLights");
				Assert.IsNotNull (node);
				Assert.AreEqual ("0", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.ParametricHighlights (XmpText/1) "0"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "ParametricHighlights");
				Assert.IsNotNull (node);
				Assert.AreEqual ("0", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.ParametricShadowSplit (XmpText/2) "25"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "ParametricShadowSplit");
				Assert.IsNotNull (node);
				Assert.AreEqual ("25", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.ParametricMidtoneSplit (XmpText/2) "50"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "ParametricMidtoneSplit");
				Assert.IsNotNull (node);
				Assert.AreEqual ("50", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.ParametricHighlightSplit (XmpText/2) "75"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "ParametricHighlightSplit");
				Assert.IsNotNull (node);
				Assert.AreEqual ("75", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.ConvertToGrayscale (XmpText/5) "False"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "ConvertToGrayscale");
				Assert.IsNotNull (node);
				Assert.AreEqual ("False", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.ToneCurveName (XmpText/6) "Custom"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "ToneCurveName");
				Assert.IsNotNull (node);
				Assert.AreEqual ("Custom", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.CameraProfile (XmpText/8) "Embedded"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "CameraProfile");
				Assert.IsNotNull (node);
				Assert.AreEqual ("Embedded", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.HasSettings (XmpText/4) "True"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "HasSettings");
				Assert.IsNotNull (node);
				Assert.AreEqual ("True", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.HasCrop (XmpText/5) "False"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "HasCrop");
				Assert.IsNotNull (node);
				Assert.AreEqual ("False", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.AlreadyApplied (XmpText/5) "False"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "AlreadyApplied");
				Assert.IsNotNull (node);
				Assert.AreEqual ("False", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.crs.ToneCurve (XmpSeq/10) "0, 0, 35, 154, 64, 56, 97, 184, 134, 182, 151, 189, 181, 177, 192, 196, 202, 240, 255, 255"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.CRS_NS, "ToneCurve");
				Assert.IsNotNull (node);
				Assert.AreEqual (XmpNodeType.Seq, node.Type);
				Assert.AreEqual ("", node.Value);
				Assert.AreEqual (10, node.Children.Count);
				Assert.AreEqual ("0, 0", node.Children [0].Value);
				Assert.AreEqual ("35, 154", node.Children [1].Value);
				Assert.AreEqual ("64, 56", node.Children [2].Value);
				Assert.AreEqual ("97, 184", node.Children [3].Value);
				Assert.AreEqual ("134, 182", node.Children [4].Value);
				Assert.AreEqual ("151, 189", node.Children [5].Value);
				Assert.AreEqual ("181, 177", node.Children [6].Value);
				Assert.AreEqual ("192, 196", node.Children [7].Value);
				Assert.AreEqual ("202, 240", node.Children [8].Value);
				Assert.AreEqual ("255, 255", node.Children [9].Value);
			}
			// Xmp.tiff.ImageWidth (XmpText/3) "256"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.TIFF_NS, "ImageWidth");
				Assert.IsNotNull (node);
				Assert.AreEqual ("256", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.tiff.ImageLength (XmpText/3) "192"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.TIFF_NS, "ImageLength");
				Assert.IsNotNull (node);
				Assert.AreEqual ("192", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.tiff.Compression (XmpText/1) "1"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.TIFF_NS, "Compression");
				Assert.IsNotNull (node);
				Assert.AreEqual ("1", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.tiff.PhotometricInterpretation (XmpText/1) "2"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.TIFF_NS, "PhotometricInterpretation");
				Assert.IsNotNull (node);
				Assert.AreEqual ("2", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.tiff.SamplesPerPixe (XmpText/1) "3"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.TIFF_NS, "SamplesPerPixe");
				Assert.IsNotNull (node);
				Assert.AreEqual ("3", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.tiff.PlanarConfiguration (XmpText/1) "1"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.TIFF_NS, "PlanarConfiguration");
				Assert.IsNotNull (node);
				Assert.AreEqual ("1", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.tiff.DateTime (XmpText/19) "2009-07-05T19:33:52"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.TIFF_NS, "DateTime");
				Assert.IsNotNull (node);
				Assert.AreEqual ("2009-07-05T19:33:52", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.tiff.Make (XmpText/5) "Canon"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.TIFF_NS, "Make");
				Assert.IsNotNull (node);
				Assert.AreEqual ("Canon", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.tiff.Model (XmpText/25) "Canon DIGITAL IXUS 850 IS"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.TIFF_NS, "Model");
				Assert.IsNotNull (node);
				Assert.AreEqual ("Canon DIGITAL IXUS 850 IS", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.tiff.Software (XmpText/19) "CHDK ver. 0.9.8-782"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.TIFF_NS, "Software");
				Assert.IsNotNull (node);
				Assert.AreEqual ("CHDK ver. 0.9.8-782", node.Value);
				Assert.AreEqual (XmpNodeType.Simple, node.Type);
				Assert.AreEqual (0, node.Children.Count);
			}
			// Xmp.tiff.BitsPerSample (XmpSeq/1) "8 8 8"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.TIFF_NS, "BitsPerSample");
				Assert.IsNotNull (node);
				Assert.AreEqual (XmpNodeType.Seq, node.Type);
				Assert.AreEqual ("", node.Value);
				Assert.AreEqual (1, node.Children.Count);
				Assert.AreEqual ("8 8 8", node.Children [0].Value);
			}
			// Xmp.dc.description (LangAlt/1) "lang="x-default" Test"
			{
				var node = xmp.NodeTree;
				node = node.GetChild (XmpTag.DC_NS, "description");
				Assert.IsNotNull (node);
				Assert.AreEqual ("x-default", node.Children [0].GetQualifier (XmpTag.XML_NS, "lang").Value);
				Assert.AreEqual ("Test", node.Children [0].Value);
			}

			//  ---------- End of XMP tests ----------

		}
	}
}
