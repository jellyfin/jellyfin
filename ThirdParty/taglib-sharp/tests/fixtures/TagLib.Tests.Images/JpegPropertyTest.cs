using System;
using NUnit.Framework;
using TagLib.Gif;
using TagLib.Xmp;
using TagLib.Tests.Images.Validators;



namespace TagLib.Tests.Images
{

	[TestFixture]
	public class JpegPropertyTest
	{
		[Test]
		public void Test ()
		{
			// This file is originally created with GIMP and the metadata was modified
			// by exiftool.
			// The test is to test some properties of ImageTag
			ImageTest.Run ("sample_gimp_exiftool.jpg",
				true,
				new JpegPropertyTestInvariantValidator (),
				NoModificationValidator.Instance,
				new PropertyModificationValidator<string> ("Creator", "Isaac Newton", "Albert Einstein"),
				new PropertyModificationValidator<string> ("Copyright", "Free to Copy", "Place something here"),
				new PropertyModificationValidator<string> ("Comment", "This is an image Comment", "And here comes another image comment"),
				new PropertyModificationValidator<string> ("Title", "Sunrise", "Eclipse"),
				new PropertyModificationValidator<string> ("Software", "Exiftool", "Unit tests"),
				new PropertyModificationValidator<string[]> ("Keywords", new string [] {"keyword 1", "keyword 2"}, new string [] {"keyword a", "keyword b", "keyword 2"}),
				new PropertyModificationValidator<uint?> ("Rating", 5, 2),

				new TagPropertyModificationValidator<string> ("Creator", null, "Albert Einstein", TagTypes.TiffIFD, true),
				new TagPropertyModificationValidator<string> ("Copyright", "Free to Copy", "Place something here", TagTypes.TiffIFD, true),
				new TagPropertyModificationValidator<string> ("Comment", null, "And here comes another image comment", TagTypes.TiffIFD, true),
				new TagPropertyModificationValidator<string> ("Software", "Exiftool", "Unit tests", TagTypes.TiffIFD, true),

				new TagPropertyModificationValidator<string> ("Creator", "Isaac Newton", "Albert Einstein", TagTypes.XMP, true),
				new TagPropertyModificationValidator<string> ("Copyright", null, "Place something here", TagTypes.XMP, true),
				new TagPropertyModificationValidator<string> ("Comment", "This is an image Comment", "And here comes another image comment", TagTypes.XMP, true),
				new TagPropertyModificationValidator<string> ("Title", "Sunrise", "Eclipse", TagTypes.XMP, true),
				new TagPropertyModificationValidator<string> ("Software", null, "Unit tests", TagTypes.XMP, true),
				new TagPropertyModificationValidator<string[]> ("Keywords", new string [] {"keyword 1", "keyword 2"}, new string [] {"keyword a", "keyword b", "keyword 2"}, TagTypes.XMP, true),
				new TagPropertyModificationValidator<uint?> ("Rating", 5, 2, TagTypes.XMP, true),

				new TagPropertyModificationValidator<string> ("Comment", "This is an image Comment", "And here comes another image comment", TagTypes.JpegComment, true)
			);
		}
	}

	public class JpegPropertyTestInvariantValidator : IMetadataInvariantValidator
	{
		public void ValidateMetadataInvariants (Image.File file)
		{
			Assert.IsNotNull (file);
			Assert.IsNotNull (file.Properties);

			Assert.AreEqual (42, file.Properties.PhotoWidth);
			Assert.AreEqual (50, file.Properties.PhotoHeight);
		}
	}
}
