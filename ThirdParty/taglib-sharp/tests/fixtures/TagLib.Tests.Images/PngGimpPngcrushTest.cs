using System;
using NUnit.Framework;
using TagLib.Png;
using TagLib.Xmp;
using TagLib.Tests.Images.Validators;

namespace TagLib.Tests.Images
{
	[TestFixture]
	public class PngGimpPngcrushTest
	{
		[Test]
		public void Test ()
		{
			// This file is originally created with GIMP and was modified with png crush:
			ImageTest.Run ("sample_gimp_pngcrush.png",
				true,
				new PngGimpPngcrushTestInvariantValidator (),
				NoModificationValidator.Instance,

				new PropertyModificationValidator<string> ("Comment", "Modified with pngcrush", "$% ¬ Test Comment äö "),
				new PropertyModificationValidator<string> ("Creator", "Isaac Newton", "Albert Einstein"),
				new PropertyModificationValidator<string> ("Title", "Sunrise", "Eclipse"),
				new TagPropertyModificationValidator<string> ("Comment", "Modified with pngcrush", "$% ¬ Test Comment äö ", TagTypes.Png, true),
				new TagPropertyModificationValidator<string> ("Creator", "Isaac Newton", "Albert Einstein", TagTypes.Png, true),
				new TagPropertyModificationValidator<string> ("Title", "Sunrise", "Eclipse", TagTypes.Png, true),
				new TagPropertyModificationValidator<string> ("Comment", null, "$% ¬ Test Comment äö ", TagTypes.XMP, false),
				new TagPropertyModificationValidator<string> ("Creator", null, "Albert Einstein", TagTypes.XMP, false),
				new TagPropertyModificationValidator<string> ("Title", null, "Eclipse", TagTypes.XMP, false),
				new TagKeywordsModificationValidator (TagTypes.XMP, false),
				new RemoveMetadataValidator (TagTypes.Png)
			);
		}
	}

	public class PngGimpPngcrushTestInvariantValidator : IMetadataInvariantValidator
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
