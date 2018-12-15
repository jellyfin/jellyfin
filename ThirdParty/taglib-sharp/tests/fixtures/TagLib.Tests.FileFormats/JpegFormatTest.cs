using System;
using NUnit.Framework;
using TagLib;
using TagLib.IFD;
using TagLib.IFD.Entries;
using TagLib.IFD.Tags;
using TagLib.Jpeg;
using TagLib.Xmp;

namespace TagLib.Tests.FileFormats
{
	[TestFixture]
	public class JpegFormatTest
	{
		private static string sample_file = TestPath.Samples + "sample.jpg";
		private Image.File file;

		private TagTypes contained_types =
				TagTypes.JpegComment |
				TagTypes.TiffIFD |
				TagTypes.XMP;

		[OneTimeSetUp]
		public void Init()
		{
			file = File.Create(sample_file) as Image.File;
		}

		[Test]
		public void TestJpegRead()
		{
			Assert.IsTrue (file is Jpeg.File);

			Assert.AreEqual (contained_types, file.TagTypes);
			Assert.AreEqual (contained_types, file.TagTypesOnDisk);

			Assert.IsNotNull (file.Properties, "properties");
			Assert.AreEqual (7, file.Properties.PhotoHeight);
			Assert.AreEqual (10, file.Properties.PhotoWidth);
			Assert.AreEqual (90, file.Properties.PhotoQuality);

			JpegCommentTag tag = file.GetTag (TagTypes.JpegComment) as JpegCommentTag;
			Assert.IsFalse (tag == null);
			Assert.AreEqual ("Test Comment", tag.Value);
		}

		[Test]
		public void TestExif()
		{
			var tag = file.GetTag (TagTypes.TiffIFD) as IFDTag;
			Assert.IsFalse (tag == null);

			var exif_ifd = tag.Structure.GetEntry(0, IFDEntryTag.ExifIFD) as SubIFDEntry;
			Assert.IsFalse (exif_ifd == null);
			var exif_tag = exif_ifd.Structure;

			{
				var entry = exif_tag.GetEntry (0, (ushort) ExifEntryTag.ExposureTime) as RationalIFDEntry;
				Assert.IsFalse (entry == null);
				Assert.AreEqual (0.008, (double)entry.Value);
			}
			{
				var entry = exif_tag.GetEntry (0, (ushort) ExifEntryTag.FNumber) as RationalIFDEntry;
				Assert.IsFalse (entry == null);
				Assert.AreEqual (3.2, (double)entry.Value);
			}
			{
				var entry = exif_tag.GetEntry (0, (ushort) ExifEntryTag.ISOSpeedRatings) as ShortIFDEntry;
				Assert.IsFalse (entry == null);
				Assert.AreEqual (100, entry.Value);
			}
		}

		[Test]
		public void TestXmp()
		{
			XmpTag tag = file.GetTag (TagTypes.XMP) as XmpTag;
			Assert.IsFalse (tag == null);

			TestBagNode (tag, XmpTag.DC_NS, "subject", new string[] {"keyword1", "keyword2", "keyword3"});
			TestAltNode (tag, XmpTag.DC_NS, "description", new string[] {"Sample Image"});
		}

		[Test]
		public void TestConstructor1 ()
		{
			var file = new Jpeg.File (sample_file);
			Assert.IsNotNull (file.ImageTag, "ImageTag");
			Assert.AreEqual (contained_types, file.TagTypes);

			Assert.IsNotNull (file.Properties, "properties");
		}

		[Test]
		public void TestConstructor2 ()
		{
			var file = new Jpeg.File (sample_file, ReadStyle.None);
			Assert.IsNotNull (file.ImageTag, "ImageTag");
			Assert.AreEqual (contained_types, file.TagTypes);

			Assert.IsNull (file.Properties, "properties");
		}

		[Test]
		public void TestConstructor3 ()
		{
			var file = new Jpeg.File (new File.LocalFileAbstraction (sample_file), ReadStyle.None);
			Assert.IsNotNull (file.ImageTag, "ImageTag");
			Assert.AreEqual (contained_types, file.TagTypes);

			Assert.IsNull (file.Properties, "properties");
		}

		private void TestBagNode (XmpTag tag, string ns, string name, string[] values)
		{
			var node = tag.FindNode (ns, name);
			Assert.IsFalse (node == null);
			Assert.AreEqual (XmpNodeType.Bag, node.Type);
			Assert.AreEqual (values.Length, node.Children.Count);

			int i = 0;
			foreach (var child_node in node.Children) {
				Assert.AreEqual (values[i], child_node.Value);
				Assert.AreEqual (0, child_node.Children.Count);
				i++;
			}
		}

		private void TestAltNode (XmpTag tag, string ns, string name, string[] values)
		{
			var node = tag.FindNode (ns, name);
			Assert.IsFalse (node == null);
			Assert.AreEqual (XmpNodeType.Alt, node.Type);
			Assert.AreEqual (values.Length, node.Children.Count);

			int i = 0;
			foreach (var child_node in node.Children) {
				Assert.AreEqual (values[i], child_node.Value);
				Assert.AreEqual (0, child_node.Children.Count);
				i++;
			}
		}
	}
}
