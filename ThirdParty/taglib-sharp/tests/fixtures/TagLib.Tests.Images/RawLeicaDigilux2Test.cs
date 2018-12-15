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
	public class RawLeicaDigilux2Test
	{
		[Test]
		public void Test ()
		{
			ImageTest.Run ("raw-samples/RAW", "RAW_LEICA_DIGILUX2_SRGB.RAW",
				false,
				new RawLeicaDigilux2TestInvariantValidator ()
			);
		}
	}

	public class RawLeicaDigilux2TestInvariantValidator : IMetadataInvariantValidator
	{
		public void ValidateMetadataInvariants (Image.File file)
		{
			Assert.IsNotNull (file);
			//
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
			Assert.AreEqual (0.004, imagetag.ExposureTime, "ExposureTime");
			Assert.AreEqual (11, imagetag.FNumber, "FNumber");
			Assert.AreEqual (100, imagetag.ISOSpeedRatings, "ISOSpeedRatings");
			Assert.AreEqual (7, imagetag.FocalLength, "FocalLength");
			Assert.AreEqual (null, imagetag.FocalLengthIn35mmFilm, "FocalLengthIn35mmFilm");
			Assert.AreEqual ("LEICA", imagetag.Make, "Make");
			Assert.AreEqual ("DIGILUX 2", imagetag.Model, "Model");
			Assert.AreEqual (null, imagetag.Creator, "Creator");

			var properties = file.Properties;
			Assert.IsNotNull (properties);
			Assert.AreEqual (2564, properties.PhotoWidth, "PhotoWidth");
			Assert.AreEqual (1924, properties.PhotoHeight, "PhotoHeight");

			//  ---------- End of ImageTag tests ----------

			//  ---------- Start of IFD tests ----------
			//		--> Omitted, because the test generator doesn't handle them yet.
			//		--> If the above works, I'm happy.
			//  ---------- End of IFD tests ----------

		}
	}
}
