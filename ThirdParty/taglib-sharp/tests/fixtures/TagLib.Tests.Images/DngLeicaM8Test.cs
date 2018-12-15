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
	public class DngLeicaM8Test
	{
		[Test]
		public void Test ()
		{
			ImageTest.Run ("raw-samples/DNG", "RAW_LEICA_M8.DNG",
				false,
				new DngLeicaM8InvariantValidator ()
			);
		}
	}

	public class DngLeicaM8InvariantValidator : IMetadataInvariantValidator
	{
		public void ValidateMetadataInvariants (Image.File file)
		{
			Assert.IsNotNull (file);

			var properties = file.Properties;
			Assert.IsNotNull (properties);
			Assert.AreEqual (3920, properties.PhotoWidth, "PhotoWidth");
			Assert.AreEqual (2638, properties.PhotoHeight, "PhotoHeight");

			//  ---------- Start of IFD tests ----------

			var tag = file.GetTag (TagTypes.TiffIFD) as IFDTag;
			Assert.IsNotNull (tag, "IFD tag not found");

			var structure = tag.Structure;

			// Image.0x00FE (NewSubfileType/Long/1) "1"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.NewSubfileType);
				Assert.IsNotNull (entry, "Entry 0x00FE missing in IFD 0");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (1, (entry as LongIFDEntry).Value);
			}
			// Image.0x0100 (ImageWidth/Long/1) "320"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.ImageWidth);
				Assert.IsNotNull (entry, "Entry 0x0100 missing in IFD 0");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (320, (entry as LongIFDEntry).Value);
			}
			// Image.0x0101 (ImageLength/Long/1) "240"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.ImageLength);
				Assert.IsNotNull (entry, "Entry 0x0101 missing in IFD 0");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (240, (entry as LongIFDEntry).Value);
			}
			// Image.0x0102 (BitsPerSample/Short/3) "8 8 8"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.BitsPerSample);
				Assert.IsNotNull (entry, "Entry 0x0102 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 8, 8, 8 }, (entry as ShortArrayIFDEntry).Values);
			}
			// Image.0x0103 (Compression/Short/1) "1"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.Compression);
				Assert.IsNotNull (entry, "Entry 0x0103 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1, (entry as ShortIFDEntry).Value);
			}
			// Image.0x0106 (PhotometricInterpretation/Short/1) "2"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.PhotometricInterpretation);
				Assert.IsNotNull (entry, "Entry 0x0106 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (2, (entry as ShortIFDEntry).Value);
			}
			// Image.0x010F (Make/Ascii/16) "Leica Camera AG"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.Make);
				Assert.IsNotNull (entry, "Entry 0x010F missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("Leica Camera AG", (entry as StringIFDEntry).Value);
			}
			// Image.0x0110 (Model/Ascii/18) "M8 Digital Camera"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.Model);
				Assert.IsNotNull (entry, "Entry 0x0110 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("M8 Digital Camera", (entry as StringIFDEntry).Value);
			}
			// Image.0x0111 (StripOffsets/StripOffsets/4) "3936 69216 134496 199776"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.StripOffsets);
				Assert.IsNotNull (entry, "Entry 0x0111 missing in IFD 0");
				Assert.IsNotNull (entry as StripOffsetsIFDEntry, "Entry is not a strip offsets entry!");
				Assert.AreEqual (4, (entry as StripOffsetsIFDEntry).Values.Length);
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
			// Image.0x0116 (RowsPerStrip/Long/1) "68"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.RowsPerStrip);
				Assert.IsNotNull (entry, "Entry 0x0116 missing in IFD 0");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (68, (entry as LongIFDEntry).Value);
			}
			// Image.0x0117 (StripByteCounts/Long/4) "65280 65280 65280 34560"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.StripByteCounts);
				Assert.IsNotNull (entry, "Entry 0x0117 missing in IFD 0");
				Assert.IsNotNull (entry as LongArrayIFDEntry, "Entry is not a long array!");
				Assert.AreEqual (new long [] { 65280, 65280, 65280, 34560 }, (entry as LongArrayIFDEntry).Values);
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
			// Image.0x0131 (Software/Ascii/6) "1.107"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.Software);
				Assert.IsNotNull (entry, "Entry 0x0131 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("1.107", (entry as StringIFDEntry).Value);
			}
			// Image.0x013B (Artist/Ascii/1) ""
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.Artist);
				Assert.IsNotNull (entry, "Entry 0x013B missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("", (entry as StringIFDEntry).Value.Trim ());
			}

			var SubImage1_structure = (structure.GetEntry (0, (ushort) IFDEntryTag.SubIFDs) as SubIFDArrayEntry).Entries [0];
			Assert.IsNotNull (SubImage1_structure, "SubImage1 structure not found");

			// SubImage1.0x00FE (NewSubfileType/Long/1) "0"
			{
				var entry = SubImage1_structure.GetEntry (0, (ushort) IFDEntryTag.NewSubfileType);
				Assert.IsNotNull (entry, "Entry 0x00FE missing in IFD 0");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (0, (entry as LongIFDEntry).Value);
			}
			// SubImage1.0x0100 (ImageWidth/Long/1) "3920"
			{
				var entry = SubImage1_structure.GetEntry (0, (ushort) IFDEntryTag.ImageWidth);
				Assert.IsNotNull (entry, "Entry 0x0100 missing in IFD 0");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (3920, (entry as LongIFDEntry).Value);
			}
			// SubImage1.0x0101 (ImageLength/Long/1) "2638"
			{
				var entry = SubImage1_structure.GetEntry (0, (ushort) IFDEntryTag.ImageLength);
				Assert.IsNotNull (entry, "Entry 0x0101 missing in IFD 0");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (2638, (entry as LongIFDEntry).Value);
			}
			// SubImage1.0x0102 (BitsPerSample/Short/1) "8"
			{
				var entry = SubImage1_structure.GetEntry (0, (ushort) IFDEntryTag.BitsPerSample);
				Assert.IsNotNull (entry, "Entry 0x0102 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (8, (entry as ShortIFDEntry).Value);
			}
			// SubImage1.0x0103 (Compression/Short/1) "1"
			{
				var entry = SubImage1_structure.GetEntry (0, (ushort) IFDEntryTag.Compression);
				Assert.IsNotNull (entry, "Entry 0x0103 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1, (entry as ShortIFDEntry).Value);
			}
			// SubImage1.0x0106 (PhotometricInterpretation/Short/1) "32803"
			{
				var entry = SubImage1_structure.GetEntry (0, (ushort) IFDEntryTag.PhotometricInterpretation);
				Assert.IsNotNull (entry, "Entry 0x0106 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (32803, (entry as ShortIFDEntry).Value);
			}
			// SubImage1.0x0111 (StripOffsets/StripOffsets/165) "234336 297056 359776 422496 485216 547936 610656 673376 736096 798816 861536 924256 986976 1049696 1112416 1175136 1237856 1300576 1363296 1426016 1488736 1551456 1614176 1676896 1739616 1802336 1865056 1927776 1990496 2053216 2115936 2178656 2241376 2304096 2366816 2429536 2492256 2554976 2617696 2680416 2743136 2805856 2868576 2931296 2994016 3056736 3119456 3182176 3244896 3307616 3370336 3433056 3495776 3558496 3621216 3683936 3746656 3809376 3872096 3934816 3997536 4060256 4122976 4185696 4248416 4311136 4373856 4436576 4499296 4562016 4624736 4687456 4750176 4812896 4875616 4938336 5001056 5063776 5126496 5189216 5251936 5314656 5377376 5440096 5502816 5565536 5628256 5690976 5753696 5816416 5879136 5941856 6004576 6067296 6130016 6192736 6255456 6318176 6380896 6443616 6506336 6569056 6631776 6694496 6757216 6819936 6882656 6945376 7008096 7070816 7133536 7196256 7258976 7321696 7384416 7447136 7509856 7572576 7635296 7698016 7760736 7823456 7886176 7948896 8011616 8074336 8137056 8199776 8262496 8325216 8387936 8450656 8513376 8576096 8638816 8701536 8764256 8826976 8889696 8952416 9015136 9077856 9140576 9203296 9266016 9328736 9391456 9454176 9516896 9579616 9642336 9705056 9767776 9830496 9893216 9955936 10018656 10081376 10144096 10206816 10269536 10332256 10394976 10457696 10520416"
			{
				var entry = SubImage1_structure.GetEntry (0, (ushort) IFDEntryTag.StripOffsets);
				Assert.IsNotNull (entry, "Entry 0x0111 missing in IFD 0");
				Assert.IsNotNull (entry as StripOffsetsIFDEntry, "Entry is not a strip offsets entry!");
				Assert.AreEqual (165, (entry as StripOffsetsIFDEntry).Values.Length);
			}
			// SubImage1.0x0115 (SamplesPerPixel/Short/1) "1"
			{
				var entry = SubImage1_structure.GetEntry (0, (ushort) IFDEntryTag.SamplesPerPixel);
				Assert.IsNotNull (entry, "Entry 0x0115 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1, (entry as ShortIFDEntry).Value);
			}
			// SubImage1.0x0116 (RowsPerStrip/Long/1) "16"
			{
				var entry = SubImage1_structure.GetEntry (0, (ushort) IFDEntryTag.RowsPerStrip);
				Assert.IsNotNull (entry, "Entry 0x0116 missing in IFD 0");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (16, (entry as LongIFDEntry).Value);
			}
			// SubImage1.0x0117 (StripByteCounts/Long/165) "62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 62720 54880"
			{
				var entry = SubImage1_structure.GetEntry (0, (ushort) IFDEntryTag.StripByteCounts);
				Assert.IsNotNull (entry, "Entry 0x0117 missing in IFD 0");
				Assert.IsNotNull (entry as LongArrayIFDEntry, "Entry is not a long array!");
				Assert.AreEqual (new long [] { 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 62720, 54880 }, (entry as LongArrayIFDEntry).Values);
			}
			// SubImage1.0x011A (XResolution/Rational/1) "300/1"
			{
				var entry = SubImage1_structure.GetEntry (0, (ushort) IFDEntryTag.XResolution);
				Assert.IsNotNull (entry, "Entry 0x011A missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (300, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (1, (entry as RationalIFDEntry).Value.Denominator);
			}
			// SubImage1.0x011B (YResolution/Rational/1) "300/1"
			{
				var entry = SubImage1_structure.GetEntry (0, (ushort) IFDEntryTag.YResolution);
				Assert.IsNotNull (entry, "Entry 0x011B missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (300, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (1, (entry as RationalIFDEntry).Value.Denominator);
			}
			// SubImage1.0x011C (PlanarConfiguration/Short/1) "1"
			{
				var entry = SubImage1_structure.GetEntry (0, (ushort) IFDEntryTag.PlanarConfiguration);
				Assert.IsNotNull (entry, "Entry 0x011C missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1, (entry as ShortIFDEntry).Value);
			}
			// SubImage1.0x0128 (ResolutionUnit/Short/1) "2"
			{
				var entry = SubImage1_structure.GetEntry (0, (ushort) IFDEntryTag.ResolutionUnit);
				Assert.IsNotNull (entry, "Entry 0x0128 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (2, (entry as ShortIFDEntry).Value);
			}
			// SubImage1.0x828D (CFARepeatPatternDim/Short/2) "2 2"
			{
				// TODO: Unknown IFD tag: SubImage1 / 0x828D
				var entry = SubImage1_structure.GetEntry (0, (ushort) 0x828D);
				Assert.IsNotNull (entry, "Entry 0x828D missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 2, 2 }, (entry as ShortArrayIFDEntry).Values);
			}
			// SubImage1.0x828E (CFAPattern/Byte/4) "0 1 1 2"
			{
				// TODO: Unknown IFD tag: SubImage1 / 0x828E
				var entry = SubImage1_structure.GetEntry (0, (ushort) 0x828E);
				Assert.IsNotNull (entry, "Entry 0x828E missing in IFD 0");
				Assert.IsNotNull (entry as ByteVectorIFDEntry, "Entry is not a byte array!");
				var parsed_bytes = (entry as ByteVectorIFDEntry).Data.Data;
				var bytes = new byte [] { 0, 1, 1, 2 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// SubImage1.0xC618 (LinearizationTable/Short/256) "0 0 1 2 4 6 9 12 16 20 25 30 36 42 49 56 64 72 81 90 100 110 121 132 144 156 169 182 196 210 225 240 256 272 289 306 324 342 361 380 400 420 441 462 484 506 529 552 576 600 625 650 676 702 729 756 784 812 841 870 900 930 961 992 1024 1056 1089 1122 1156 1190 1225 1260 1296 1332 1369 1406 1444 1482 1521 1560 1600 1640 1681 1722 1764 1806 1849 1892 1936 1980 2025 2070 2116 2162 2209 2256 2304 2352 2401 2450 2500 2550 2601 2652 2704 2756 2809 2862 2916 2970 3025 3080 3136 3192 3249 3306 3364 3422 3481 3540 3600 3660 3721 3782 3844 3906 3969 4032 4096 4160 4225 4290 4356 4422 4489 4556 4624 4692 4761 4830 4900 4970 5041 5112 5184 5256 5329 5402 5476 5550 5625 5700 5776 5852 5929 6006 6084 6162 6241 6320 6400 6480 6561 6642 6724 6806 6889 6972 7056 7140 7225 7310 7396 7482 7569 7656 7744 7832 7921 8010 8100 8190 8281 8372 8464 8556 8649 8742 8836 8930 9025 9120 9216 9312 9409 9506 9604 9702 9801 9900 10000 10100 10201 10302 10404 10506 10609 10712 10816 10920 11025 11130 11236 11342 11449 11556 11664 11772 11881 11990 12100 12210 12321 12432 12544 12656 12769 12882 12996 13110 13225 13340 13456 13572 13689 13806 13924 14042 14161 14280 14400 14520 14641 14762 14884 15006 15129 15252 15376 15500 15625 15750 15876 16002 16129 16256"
			{
				var entry = SubImage1_structure.GetEntry (0, (ushort) IFDEntryTag.LinearizationTable);
				Assert.IsNotNull (entry, "Entry 0xC618 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 0, 0, 1, 2, 4, 6, 9, 12, 16, 20, 25, 30, 36, 42, 49, 56, 64, 72, 81, 90, 100, 110, 121, 132, 144, 156, 169, 182, 196, 210, 225, 240, 256, 272, 289, 306, 324, 342, 361, 380, 400, 420, 441, 462, 484, 506, 529, 552, 576, 600, 625, 650, 676, 702, 729, 756, 784, 812, 841, 870, 900, 930, 961, 992, 1024, 1056, 1089, 1122, 1156, 1190, 1225, 1260, 1296, 1332, 1369, 1406, 1444, 1482, 1521, 1560, 1600, 1640, 1681, 1722, 1764, 1806, 1849, 1892, 1936, 1980, 2025, 2070, 2116, 2162, 2209, 2256, 2304, 2352, 2401, 2450, 2500, 2550, 2601, 2652, 2704, 2756, 2809, 2862, 2916, 2970, 3025, 3080, 3136, 3192, 3249, 3306, 3364, 3422, 3481, 3540, 3600, 3660, 3721, 3782, 3844, 3906, 3969, 4032, 4096, 4160, 4225, 4290, 4356, 4422, 4489, 4556, 4624, 4692, 4761, 4830, 4900, 4970, 5041, 5112, 5184, 5256, 5329, 5402, 5476, 5550, 5625, 5700, 5776, 5852, 5929, 6006, 6084, 6162, 6241, 6320, 6400, 6480, 6561, 6642, 6724, 6806, 6889, 6972, 7056, 7140, 7225, 7310, 7396, 7482, 7569, 7656, 7744, 7832, 7921, 8010, 8100, 8190, 8281, 8372, 8464, 8556, 8649, 8742, 8836, 8930, 9025, 9120, 9216, 9312, 9409, 9506, 9604, 9702, 9801, 9900, 10000, 10100, 10201, 10302, 10404, 10506, 10609, 10712, 10816, 10920, 11025, 11130, 11236, 11342, 11449, 11556, 11664, 11772, 11881, 11990, 12100, 12210, 12321, 12432, 12544, 12656, 12769, 12882, 12996, 13110, 13225, 13340, 13456, 13572, 13689, 13806, 13924, 14042, 14161, 14280, 14400, 14520, 14641, 14762, 14884, 15006, 15129, 15252, 15376, 15500, 15625, 15750, 15876, 16002, 16129, 16256 }, (entry as ShortArrayIFDEntry).Values);
			}
			// SubImage1.0xC61D (WhiteLevel/Long/1) "16383"
			{
				var entry = SubImage1_structure.GetEntry (0, (ushort) IFDEntryTag.WhiteLevel);
				Assert.IsNotNull (entry, "Entry 0xC61D missing in IFD 0");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (16383, (entry as LongIFDEntry).Value);
			}
			// SubImage1.0xC61F (DefaultCropOrigin/Short/2) "2 2"
			{
				var entry = SubImage1_structure.GetEntry (0, (ushort) IFDEntryTag.DefaultCropOrigin);
				Assert.IsNotNull (entry, "Entry 0xC61F missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 2, 2 }, (entry as ShortArrayIFDEntry).Values);
			}
			// SubImage1.0xC620 (DefaultCropSize/Short/2) "3916 2634"
			{
				var entry = SubImage1_structure.GetEntry (0, (ushort) IFDEntryTag.DefaultCropSize);
				Assert.IsNotNull (entry, "Entry 0xC620 missing in IFD 0");
				Assert.IsNotNull (entry as ShortArrayIFDEntry, "Entry is not a short array!");
				Assert.AreEqual (new ushort [] { 3916, 2634 }, (entry as ShortArrayIFDEntry).Values);
			}
			// SubImage1.0xC62D (BayerGreenSplit/Long/1) "500"
			{
				var entry = SubImage1_structure.GetEntry (0, (ushort) IFDEntryTag.BayerGreenSplit);
				Assert.IsNotNull (entry, "Entry 0xC62D missing in IFD 0");
				Assert.IsNotNull (entry as LongIFDEntry, "Entry is not a long!");
				Assert.AreEqual (500, (entry as LongIFDEntry).Value);
			}
			// SubImage1.0xC632 (AntiAliasStrength/Rational/1) "0/1"
			{
				var entry = SubImage1_structure.GetEntry (0, (ushort) IFDEntryTag.AntiAliasStrength);
				Assert.IsNotNull (entry, "Entry 0xC632 missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (0, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (1, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Image.0x8298 (Copyright/Ascii/1) ""
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.Copyright);
				Assert.IsNotNull (entry, "Entry 0x8298 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("", (entry as StringIFDEntry).Value.Trim ());
			}
			// Image.0x8769 (ExifTag/SubIFD/1) "764"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.ExifIFD);
				Assert.IsNotNull (entry, "Entry 0x8769 missing in IFD 0");
				Assert.IsNotNull (entry as SubIFDEntry, "Entry is not a sub IFD!");
			}

			var exif = structure.GetEntry (0, (ushort) IFDEntryTag.ExifIFD) as SubIFDEntry;
			Assert.IsNotNull (exif, "Exif tag not found");
			var exif_structure = exif.Structure;

			// Photo.0x829A (ExposureTime/Rational/1) "12000000/1000000"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ExposureTime);
				Assert.IsNotNull (entry, "Entry 0x829A missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (12000000, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (1000000, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Photo.0x8822 (ExposureProgram/Short/1) "1"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ExposureProgram);
				Assert.IsNotNull (entry, "Entry 0x8822 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (1, (entry as ShortIFDEntry).Value);
			}
			// Photo.0x8827 (ISOSpeedRatings/Short/1) "160"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ISOSpeedRatings);
				Assert.IsNotNull (entry, "Entry 0x8827 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (160, (entry as ShortIFDEntry).Value);
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
			// Photo.0x9004 (DateTimeDigitized/Ascii/20) "2007:08:02 22:13:49"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.DateTimeDigitized);
				Assert.IsNotNull (entry, "Entry 0x9004 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("2007:08:02 22:13:49", (entry as StringIFDEntry).Value);
			}
			// Photo.0x9201 (ShutterSpeedValue/SRational/1) "-229376/65536"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ShutterSpeedValue);
				Assert.IsNotNull (entry, "Entry 0x9201 missing in IFD 0");
				Assert.IsNotNull (entry as SRationalIFDEntry, "Entry is not a srational!");
				Assert.AreEqual (-229376, (entry as SRationalIFDEntry).Value.Numerator);
				Assert.AreEqual (65536, (entry as SRationalIFDEntry).Value.Denominator);
			}
			// Photo.0x9204 (ExposureBiasValue/SRational/1) "0/65536"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ExposureBiasValue);
				Assert.IsNotNull (entry, "Entry 0x9204 missing in IFD 0");
				Assert.IsNotNull (entry as SRationalIFDEntry, "Entry is not a srational!");
				Assert.AreEqual (0, (entry as SRationalIFDEntry).Value.Numerator);
				Assert.AreEqual (65536, (entry as SRationalIFDEntry).Value.Denominator);
			}
			// Photo.0x9205 (MaxApertureValue/Rational/1) "131072/65536"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.MaxApertureValue);
				Assert.IsNotNull (entry, "Entry 0x9205 missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (131072, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (65536, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Photo.0x9207 (MeteringMode/Short/1) "2"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.MeteringMode);
				Assert.IsNotNull (entry, "Entry 0x9207 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (2, (entry as ShortIFDEntry).Value);
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
			// Photo.0x920A (FocalLength/Rational/1) "50000/1000"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.FocalLength);
				Assert.IsNotNull (entry, "Entry 0x920A missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (50000, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (1000, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Photo.0x927C (MakerNote/MakerNote/220) ""
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.MakerNote);
				Assert.IsNotNull (entry, "Entry 0x927C missing in IFD 0");
				Assert.IsNotNull (entry as MakernoteIFDEntry, "Entry is not a makernote IFD!");
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
			// Photo.0xA403 (WhiteBalance/Short/1) "0"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.WhiteBalance);
				Assert.IsNotNull (entry, "Entry 0xA403 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Photo.0xA404 (DigitalZoomRatio/Rational/1) "0/1"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.DigitalZoomRatio);
				Assert.IsNotNull (entry, "Entry 0xA404 missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (0, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (1, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Photo.0xA405 (FocalLengthIn35mmFilm/Short/1) "67"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.FocalLengthIn35mmFilm);
				Assert.IsNotNull (entry, "Entry 0xA405 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (67, (entry as ShortIFDEntry).Value);
			}
			// Photo.0xA406 (SceneCaptureType/Short/1) "0"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.SceneCaptureType);
				Assert.IsNotNull (entry, "Entry 0xA406 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Photo.0xA420 (ImageUniqueID/Ascii/33) "00000000000000000000000000000147"
			{
				var entry = exif_structure.GetEntry (0, (ushort) ExifEntryTag.ImageUniqueID);
				Assert.IsNotNull (entry, "Entry 0xA420 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("00000000000000000000000000000147", (entry as StringIFDEntry).Value);
			}
			// Image.0x882B (SelfTimerMode/Short/1) "0"
			{
				// TODO: Unknown IFD tag: Image / 0x882B
				var entry = structure.GetEntry (0, (ushort) 0x882B);
				Assert.IsNotNull (entry, "Entry 0x882B missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (0, (entry as ShortIFDEntry).Value);
			}
			// Image.0x9003 (DateTimeOriginal/Ascii/20) "2007:08:02 22:13:49"
			{
				var entry = structure.GetEntry (0, (ushort) ExifEntryTag.DateTimeOriginal);
				Assert.IsNotNull (entry, "Entry 0x9003 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("2007:08:02 22:13:49", (entry as StringIFDEntry).Value);
			}
			// Image.0x920E (FocalPlaneXResolution/Rational/1) "3729/1"
			{
				// TODO: Unknown IFD tag: Image / 0x920E
				var entry = structure.GetEntry (0, (ushort) 0x920E);
				Assert.IsNotNull (entry, "Entry 0x920E missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (3729, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (1, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Image.0x920F (FocalPlaneYResolution/Rational/1) "3764/1"
			{
				// TODO: Unknown IFD tag: Image / 0x920F
				var entry = structure.GetEntry (0, (ushort) 0x920F);
				Assert.IsNotNull (entry, "Entry 0x920F missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (3764, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (1, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Image.0x9210 (FocalPlaneResolutionUnit/Short/1) "2"
			{
				// TODO: Unknown IFD tag: Image / 0x9210
				var entry = structure.GetEntry (0, (ushort) 0x9210);
				Assert.IsNotNull (entry, "Entry 0x9210 missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (2, (entry as ShortIFDEntry).Value);
			}
			// Image.0x9216 (TIFFEPStandardID/Byte/4) "0 0 0 1"
			{
				// TODO: Unknown IFD tag: Image / 0x9216
				var entry = structure.GetEntry (0, (ushort) 0x9216);
				Assert.IsNotNull (entry, "Entry 0x9216 missing in IFD 0");
				Assert.IsNotNull (entry as ByteVectorIFDEntry, "Entry is not a byte array!");
				var parsed_bytes = (entry as ByteVectorIFDEntry).Data.Data;
				var bytes = new byte [] { 0, 0, 0, 1 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Image.0xC612 (DNGVersion/Byte/4) "1 0 0 0"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.DNGVersion);
				Assert.IsNotNull (entry, "Entry 0xC612 missing in IFD 0");
				Assert.IsNotNull (entry as ByteVectorIFDEntry, "Entry is not a byte array!");
				var parsed_bytes = (entry as ByteVectorIFDEntry).Data.Data;
				var bytes = new byte [] { 1, 0, 0, 0 };
				Assert.AreEqual (bytes, parsed_bytes);
			}
			// Image.0xC614 (UniqueCameraModel/Ascii/18) "M8 Digital Camera"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.UniqueCameraModel);
				Assert.IsNotNull (entry, "Entry 0xC614 missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("M8 Digital Camera", (entry as StringIFDEntry).Value);
			}
			// Image.0xC621 (ColorMatrix1/SRational/9) "10469/10000 -5314/10000 1280/10000 -4326/10000 12176/10000 2419/10000 -886/10000 2473/10000 7160/10000"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.ColorMatrix1);
				Assert.IsNotNull (entry, "Entry 0xC621 missing in IFD 0");
				Assert.IsNotNull (entry as SRationalArrayIFDEntry, "Entry is not a srational array!");
				var parts = (entry as SRationalArrayIFDEntry).Values;
				Assert.AreEqual (9, parts.Length);
				Assert.AreEqual (10469, parts[0].Numerator);
				Assert.AreEqual (10000, parts[0].Denominator);
				Assert.AreEqual (-5314, parts[1].Numerator);
				Assert.AreEqual (10000, parts[1].Denominator);
				Assert.AreEqual (1280, parts[2].Numerator);
				Assert.AreEqual (10000, parts[2].Denominator);
				Assert.AreEqual (-4326, parts[3].Numerator);
				Assert.AreEqual (10000, parts[3].Denominator);
				Assert.AreEqual (12176, parts[4].Numerator);
				Assert.AreEqual (10000, parts[4].Denominator);
				Assert.AreEqual (2419, parts[5].Numerator);
				Assert.AreEqual (10000, parts[5].Denominator);
				Assert.AreEqual (-886, parts[6].Numerator);
				Assert.AreEqual (10000, parts[6].Denominator);
				Assert.AreEqual (2473, parts[7].Numerator);
				Assert.AreEqual (10000, parts[7].Denominator);
				Assert.AreEqual (7160, parts[8].Numerator);
				Assert.AreEqual (10000, parts[8].Denominator);
			}
			// Image.0xC622 (ColorMatrix2/SRational/9) "7675/10000 -2195/10000 -305/10000 -5860/10000 14118/10000 1857/10000 -2425/10000 4007/10000 6578/10000"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.ColorMatrix2);
				Assert.IsNotNull (entry, "Entry 0xC622 missing in IFD 0");
				Assert.IsNotNull (entry as SRationalArrayIFDEntry, "Entry is not a srational array!");
				var parts = (entry as SRationalArrayIFDEntry).Values;
				Assert.AreEqual (9, parts.Length);
				Assert.AreEqual (7675, parts[0].Numerator);
				Assert.AreEqual (10000, parts[0].Denominator);
				Assert.AreEqual (-2195, parts[1].Numerator);
				Assert.AreEqual (10000, parts[1].Denominator);
				Assert.AreEqual (-305, parts[2].Numerator);
				Assert.AreEqual (10000, parts[2].Denominator);
				Assert.AreEqual (-5860, parts[3].Numerator);
				Assert.AreEqual (10000, parts[3].Denominator);
				Assert.AreEqual (14118, parts[4].Numerator);
				Assert.AreEqual (10000, parts[4].Denominator);
				Assert.AreEqual (1857, parts[5].Numerator);
				Assert.AreEqual (10000, parts[5].Denominator);
				Assert.AreEqual (-2425, parts[6].Numerator);
				Assert.AreEqual (10000, parts[6].Denominator);
				Assert.AreEqual (4007, parts[7].Numerator);
				Assert.AreEqual (10000, parts[7].Denominator);
				Assert.AreEqual (6578, parts[8].Numerator);
				Assert.AreEqual (10000, parts[8].Denominator);
			}
			// Image.0xC623 (CameraCalibration1/SRational/9) "1/1 0/1 0/1 0/1 1/1 0/1 0/1 0/1 1/1"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.CameraCalibration1);
				Assert.IsNotNull (entry, "Entry 0xC623 missing in IFD 0");
				Assert.IsNotNull (entry as SRationalArrayIFDEntry, "Entry is not a srational array!");
				var parts = (entry as SRationalArrayIFDEntry).Values;
				Assert.AreEqual (9, parts.Length);
				Assert.AreEqual (1, parts[0].Numerator);
				Assert.AreEqual (1, parts[0].Denominator);
				Assert.AreEqual (0, parts[1].Numerator);
				Assert.AreEqual (1, parts[1].Denominator);
				Assert.AreEqual (0, parts[2].Numerator);
				Assert.AreEqual (1, parts[2].Denominator);
				Assert.AreEqual (0, parts[3].Numerator);
				Assert.AreEqual (1, parts[3].Denominator);
				Assert.AreEqual (1, parts[4].Numerator);
				Assert.AreEqual (1, parts[4].Denominator);
				Assert.AreEqual (0, parts[5].Numerator);
				Assert.AreEqual (1, parts[5].Denominator);
				Assert.AreEqual (0, parts[6].Numerator);
				Assert.AreEqual (1, parts[6].Denominator);
				Assert.AreEqual (0, parts[7].Numerator);
				Assert.AreEqual (1, parts[7].Denominator);
				Assert.AreEqual (1, parts[8].Numerator);
				Assert.AreEqual (1, parts[8].Denominator);
			}
			// Image.0xC624 (CameraCalibration2/SRational/9) "1/1 0/1 0/1 0/1 1/1 0/1 0/1 0/1 1/1"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.CameraCalibration2);
				Assert.IsNotNull (entry, "Entry 0xC624 missing in IFD 0");
				Assert.IsNotNull (entry as SRationalArrayIFDEntry, "Entry is not a srational array!");
				var parts = (entry as SRationalArrayIFDEntry).Values;
				Assert.AreEqual (9, parts.Length);
				Assert.AreEqual (1, parts[0].Numerator);
				Assert.AreEqual (1, parts[0].Denominator);
				Assert.AreEqual (0, parts[1].Numerator);
				Assert.AreEqual (1, parts[1].Denominator);
				Assert.AreEqual (0, parts[2].Numerator);
				Assert.AreEqual (1, parts[2].Denominator);
				Assert.AreEqual (0, parts[3].Numerator);
				Assert.AreEqual (1, parts[3].Denominator);
				Assert.AreEqual (1, parts[4].Numerator);
				Assert.AreEqual (1, parts[4].Denominator);
				Assert.AreEqual (0, parts[5].Numerator);
				Assert.AreEqual (1, parts[5].Denominator);
				Assert.AreEqual (0, parts[6].Numerator);
				Assert.AreEqual (1, parts[6].Denominator);
				Assert.AreEqual (0, parts[7].Numerator);
				Assert.AreEqual (1, parts[7].Denominator);
				Assert.AreEqual (1, parts[8].Numerator);
				Assert.AreEqual (1, parts[8].Denominator);
			}
			// Image.0xC628 (AsShotNeutral/Rational/3) "16384/34488 16384/16384 16384/20567"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.AsShotNeutral);
				Assert.IsNotNull (entry, "Entry 0xC628 missing in IFD 0");
				Assert.IsNotNull (entry as RationalArrayIFDEntry, "Entry is not a rational array!");
				var parts = (entry as RationalArrayIFDEntry).Values;
				Assert.AreEqual (3, parts.Length);
				Assert.AreEqual (16384, parts[0].Numerator);
				Assert.AreEqual (34488, parts[0].Denominator);
				Assert.AreEqual (16384, parts[1].Numerator);
				Assert.AreEqual (16384, parts[1].Denominator);
				Assert.AreEqual (16384, parts[2].Numerator);
				Assert.AreEqual (20567, parts[2].Denominator);
			}
			// Image.0xC62B (BaselineNoise/Rational/1) "1/1"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.BaselineNoise);
				Assert.IsNotNull (entry, "Entry 0xC62B missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (1, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (1, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Image.0xC62C (BaselineSharpness/Rational/1) "1/1"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.BaselineSharpness);
				Assert.IsNotNull (entry, "Entry 0xC62C missing in IFD 0");
				Assert.IsNotNull (entry as RationalIFDEntry, "Entry is not a rational!");
				Assert.AreEqual (1, (entry as RationalIFDEntry).Value.Numerator);
				Assert.AreEqual (1, (entry as RationalIFDEntry).Value.Denominator);
			}
			// Image.0xC62F (CameraSerialNumber/Ascii/8) "3106091"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.CameraSerialNumber);
				Assert.IsNotNull (entry, "Entry 0xC62F missing in IFD 0");
				Assert.IsNotNull (entry as StringIFDEntry, "Entry is not a string!");
				Assert.AreEqual ("3106091", (entry as StringIFDEntry).Value);
			}
			// Image.0xC65A (CalibrationIlluminant1/Short/1) "17"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.CalibrationIlluminant1);
				Assert.IsNotNull (entry, "Entry 0xC65A missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (17, (entry as ShortIFDEntry).Value);
			}
			// Image.0xC65B (CalibrationIlluminant2/Short/1) "21"
			{
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.CalibrationIlluminant2);
				Assert.IsNotNull (entry, "Entry 0xC65B missing in IFD 0");
				Assert.IsNotNull (entry as ShortIFDEntry, "Entry is not a short!");
				Assert.AreEqual (21, (entry as ShortIFDEntry).Value);
			}

			//  ---------- End of IFD tests ----------

		}
	}
}
