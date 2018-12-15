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
	public class JpegNoMetadataTest
	{
		[Test]
		public void Test ()
		{
			ImageTest.Run ("sample_no_metadata.jpg",
				new JpegNoMetadataTestInvariantValidator (),
				NoModificationValidator.Instance,
				new NoModificationValidator (),
				new TagCommentModificationValidator (TagTypes.TiffIFD, false),
				new TagCommentModificationValidator (TagTypes.XMP, false),
				new TagKeywordsModificationValidator (TagTypes.XMP, false)
			);
		}
	}

	public class JpegNoMetadataTestInvariantValidator : IMetadataInvariantValidator
	{
		public void ValidateMetadataInvariants (Image.File file)
		{
			Assert.IsNotNull (file);
		}
	}
}
