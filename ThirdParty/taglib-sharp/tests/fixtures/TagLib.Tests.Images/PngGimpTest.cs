using System;
using NUnit.Framework;
using TagLib.Png;
using TagLib.Xmp;
using TagLib.Tests.Images.Validators;

namespace TagLib.Tests.Images
{
	[TestFixture]
	public class PngGimpTest
	{
		[Test]
		public void Test ()
		{
			// This file is originally created with GIMP.
			ImageTest.Run ("sample_gimp.png",
				true,
				new PngGimpTestInvariantValidator (),
				NoModificationValidator.Instance,
				new CommentModificationValidator ("Created with GIMP"),
				new TagCommentModificationValidator ("Created with GIMP", TagTypes.Png, true),
				new TagKeywordsModificationValidator (TagTypes.XMP, false),
				new RemoveMetadataValidator (TagTypes.Png)
			);
		}
	}

	public class PngGimpTestInvariantValidator : IMetadataInvariantValidator
	{
		public void ValidateMetadataInvariants (Image.File file)
		{
			Assert.IsNotNull (file);
			Assert.IsNotNull (file.Properties);

			Assert.AreEqual (37, file.Properties.PhotoWidth);
			Assert.AreEqual (71, file.Properties.PhotoHeight);
		}
	}
}
