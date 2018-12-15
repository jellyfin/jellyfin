using System;
using NUnit.Framework;
using TagLib.Image;

namespace TagLib.Tests.Images
{
	/// <summary>
	///    This test validates the correct mapping of different metadata formats onto ImageTag.
	/// </summary>
	[TestFixture]
	public class ImageTagTests
	{
		[Test]
		public void TestXMPImageTag ()
		{
			var file = TagLib.File.Create (Debugger.Samples + "sample_canon_bibble5.jpg") as TagLib.Image.File;
			Assert.IsNotNull (file);

			var tag = file.GetTag (TagTypes.XMP) as TagLib.Image.ImageTag;
			Assert.IsNotNull (tag);

			Assert.AreEqual (null, tag.Comment, "Comment");
			Assert.AreEqual (new string [] {}, tag.Keywords, "Keywords");
			Assert.AreEqual (0, tag.Rating, "Rating");
			Assert.AreEqual (null, tag.DateTime, "DateTime");
			Assert.AreEqual (ImageOrientation.None, tag.Orientation, "Orientation");
			Assert.AreEqual (null, tag.Software, "Software");
			Assert.AreEqual (null, tag.Latitude, "Latitude");
			Assert.AreEqual (null, tag.Longitude, "Longitude");
			Assert.AreEqual (null, tag.Altitude, "Altitude");
			Assert.AreEqual (0.005, tag.ExposureTime, "ExposureTime");
			Assert.AreEqual (5, tag.FNumber, "FNumber");
			Assert.AreEqual (100, tag.ISOSpeedRatings, "ISOSpeedRatings");
			Assert.AreEqual (21, tag.FocalLength, "FocalLength");
			Assert.AreEqual (null, tag.FocalLengthIn35mmFilm, "FocalLengthIn35mmFilm");
			Assert.AreEqual ("Canon", tag.Make, "Make");
			Assert.AreEqual ("Canon EOS 400D DIGITAL", tag.Model, "Model");
			Assert.AreEqual (null, tag.Creator, "Creator");
		}

		[Test]
		public void TestXMPImageTag2 ()
		{
			var file = TagLib.File.Create (Debugger.Samples + "sample_gimp_exiftool.jpg") as TagLib.Image.File;
			Assert.IsNotNull (file);

			var tag = file.GetTag (TagTypes.XMP) as TagLib.Image.ImageTag;
			Assert.IsNotNull (tag);

			Assert.AreEqual ("This is an image Comment", tag.Comment, "Comment");
			Assert.AreEqual (new string [] { "keyword 1", "keyword 2" }, tag.Keywords, "Keywords");
			Assert.AreEqual (5, tag.Rating, "Rating");
			Assert.AreEqual (null, tag.DateTime, "DateTime");
			Assert.AreEqual (ImageOrientation.None, tag.Orientation, "Orientation");
			Assert.AreEqual (null, tag.Software, "Software");
			Assert.AreEqual (null, tag.Latitude, "Latitude");
			Assert.AreEqual (null, tag.Longitude, "Longitude");
			Assert.AreEqual (null, tag.Altitude, "Altitude");
			Assert.AreEqual (null, tag.ExposureTime, "ExposureTime");
			Assert.AreEqual (null, tag.FNumber, "FNumber");
			Assert.AreEqual (null, tag.ISOSpeedRatings, "ISOSpeedRatings");
			Assert.AreEqual (null, tag.FocalLength, "FocalLength");
			Assert.AreEqual (null, tag.FocalLengthIn35mmFilm, "FocalLengthIn35mmFilm");
			Assert.AreEqual (null, tag.Make, "Make");
			Assert.AreEqual (null, tag.Model, "Model");
			Assert.AreEqual ("Isaac Newton", tag.Creator, "Creator");
		}
	}
}
