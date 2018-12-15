using System;
using NUnit.Framework;
using TagLib;
using TagLib.Image;

namespace TagLib.Tests.Images
{
	[TestFixture]
	public class CopyFromTest
	{
		[Test]
		public void TestJPGtoTIFF () {
			var file1 = TagLib.File.Create (TestPath.Samples + "sample_canon_zoombrowser.jpg") as TagLib.Image.File;
			Assert.IsNotNull (file1);
			var file2 = TagLib.File.Create (TestPath.Samples + "sample_nikon1_bibble5_16bit.tiff") as TagLib.Image.File;
			Assert.IsNotNull (file2);

			// Verify initial values
			Assert.AreEqual (TagTypes.TiffIFD, file1.TagTypes);
			Assert.AreEqual (TagTypes.TiffIFD | TagTypes.XMP, file2.TagTypes);
			Assert.AreEqual ("%test comment%", file1.ImageTag.Comment);
			Assert.AreEqual (String.Empty, file2.ImageTag.Comment);
			Assert.AreEqual (new string [] {}, file1.ImageTag.Keywords);
			Assert.AreEqual (new string [] {}, file2.ImageTag.Keywords);
			Assert.AreEqual (null, file1.ImageTag.Rating);
			Assert.AreEqual (0, file2.ImageTag.Rating);
			Assert.AreEqual (new DateTime (2009, 8, 9, 19, 12, 44), (DateTime) file1.ImageTag.DateTime);
			Assert.AreEqual (new DateTime (2007, 2, 15, 17, 7, 48), (DateTime) file2.ImageTag.DateTime);
			Assert.AreEqual (ImageOrientation.TopLeft, file1.ImageTag.Orientation);
			Assert.AreEqual (ImageOrientation.TopLeft, file2.ImageTag.Orientation);
			Assert.AreEqual ("Digital Photo Professional", file1.ImageTag.Software);
			Assert.AreEqual (null, file2.ImageTag.Software);
			Assert.AreEqual (null, file1.ImageTag.Latitude);
			Assert.AreEqual (null, file2.ImageTag.Latitude);
			Assert.AreEqual (null, file1.ImageTag.Longitude);
			Assert.AreEqual (null, file2.ImageTag.Longitude);
			Assert.AreEqual (null, file1.ImageTag.Altitude);
			Assert.AreEqual (null, file2.ImageTag.Altitude);
			Assert.IsTrue (Math.Abs ((double) file1.ImageTag.ExposureTime - 0.005) < 0.0001);
			Assert.IsTrue (Math.Abs ((double) file2.ImageTag.ExposureTime - 0.0013) < 0.0001);
			Assert.IsTrue (Math.Abs ((double) file1.ImageTag.FNumber - 6.3) < 0.0001);
			Assert.IsTrue (Math.Abs ((double) file2.ImageTag.FNumber - 13) < 0.0001);
			Assert.AreEqual (400, file1.ImageTag.ISOSpeedRatings);
			Assert.AreEqual (1600, file2.ImageTag.ISOSpeedRatings);
			Assert.AreEqual (180, file1.ImageTag.FocalLength);
			Assert.AreEqual (50, file2.ImageTag.FocalLength);
			Assert.AreEqual (null, file1.ImageTag.FocalLengthIn35mmFilm);
			Assert.AreEqual (75, file2.ImageTag.FocalLengthIn35mmFilm);
			Assert.AreEqual ("Canon", file1.ImageTag.Make);
			Assert.AreEqual ("NIKON CORPORATION", file2.ImageTag.Make);
			Assert.AreEqual ("Canon EOS 400D DIGITAL", file1.ImageTag.Model);
			Assert.AreEqual ("NIKON D70s", file2.ImageTag.Model);
			Assert.AreEqual (null, file1.ImageTag.Creator);
			Assert.AreEqual (null, file2.ImageTag.Creator);

			// Copy Metadata
			file2.CopyFrom (file1);

			// Verify copied values
			Assert.AreEqual (TagTypes.TiffIFD, file1.TagTypes);
			Assert.AreEqual (TagTypes.TiffIFD | TagTypes.XMP, file2.TagTypes);
			Assert.AreEqual ("%test comment%", file1.ImageTag.Comment);
			Assert.AreEqual ("%test comment%", file2.ImageTag.Comment);
			Assert.AreEqual (new string [] {}, file1.ImageTag.Keywords);
			Assert.AreEqual (new string [] {}, file2.ImageTag.Keywords);
			Assert.AreEqual (null, file1.ImageTag.Rating);
			Assert.AreEqual (null, file2.ImageTag.Rating);
			Assert.AreEqual (new DateTime (2009, 8, 9, 19, 12, 44), (DateTime) file1.ImageTag.DateTime);
			Assert.AreEqual (new DateTime (2009, 8, 9, 19, 12, 44), (DateTime) file2.ImageTag.DateTime);
			Assert.AreEqual (ImageOrientation.TopLeft, file1.ImageTag.Orientation);
			Assert.AreEqual (ImageOrientation.TopLeft, file2.ImageTag.Orientation);
			Assert.AreEqual ("Digital Photo Professional", file1.ImageTag.Software);
			Assert.AreEqual ("Digital Photo Professional", file2.ImageTag.Software);
			Assert.AreEqual (null, file1.ImageTag.Latitude);
			Assert.AreEqual (null, file2.ImageTag.Latitude);
			Assert.AreEqual (null, file1.ImageTag.Longitude);
			Assert.AreEqual (null, file2.ImageTag.Longitude);
			Assert.AreEqual (null, file1.ImageTag.Altitude);
			Assert.AreEqual (null, file2.ImageTag.Altitude);
			Assert.IsTrue (Math.Abs ((double) file1.ImageTag.ExposureTime - 0.005) < 0.0001);
			Assert.IsTrue (Math.Abs ((double) file2.ImageTag.ExposureTime - 0.005) < 0.0001);
			Assert.IsTrue (Math.Abs ((double) file1.ImageTag.FNumber - 6.3) < 0.0001);
			Assert.IsTrue (Math.Abs ((double) file2.ImageTag.FNumber - 6.3) < 0.0001);
			Assert.AreEqual (400, file1.ImageTag.ISOSpeedRatings);
			Assert.AreEqual (400, file2.ImageTag.ISOSpeedRatings);
			Assert.AreEqual (180, file1.ImageTag.FocalLength);
			Assert.AreEqual (180, file2.ImageTag.FocalLength);
			Assert.AreEqual (null, file1.ImageTag.FocalLengthIn35mmFilm);
			Assert.AreEqual (null, file2.ImageTag.FocalLengthIn35mmFilm);
			Assert.AreEqual ("Canon", file1.ImageTag.Make);
			Assert.AreEqual ("Canon", file2.ImageTag.Make);
			Assert.AreEqual ("Canon EOS 400D DIGITAL", file1.ImageTag.Model);
			Assert.AreEqual ("Canon EOS 400D DIGITAL", file2.ImageTag.Model);
			Assert.AreEqual (null, file1.ImageTag.Creator);
			Assert.AreEqual (null, file2.ImageTag.Creator);
		}
	}
}
