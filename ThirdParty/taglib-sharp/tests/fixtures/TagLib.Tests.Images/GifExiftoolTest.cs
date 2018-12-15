using System;
using NUnit.Framework;
using TagLib.Gif;
using TagLib.Xmp;
using TagLib.Tests.Images.Validators;

namespace TagLib.Tests.Images
{
	[TestFixture]
	public class GifExiftoolTest
	{
		[Test]
		public void Test ()
		{
			// This file is originally created with GIMP and the metadata was modified
			// by exiftool.
			ImageTest.Run ("sample_exiftool.gif",
				true,
				new GifExiftoolTestInvariantValidator (),
				NoModificationValidator.Instance,
				new TagKeywordsModificationValidator (new string [] {"Keyword 1", "Keyword 2"}, TagTypes.XMP, true),
				new CommentModificationValidator ("Created with GIMP"),
				new TagCommentModificationValidator ("Created with GIMP", TagTypes.GifComment, true),
				new RemoveMetadataValidator (TagTypes.GifComment | TagTypes.XMP),
				new RemoveMetadataValidator (TagTypes.GifComment | TagTypes.XMP, TagTypes.GifComment)
			);
		}
	}

	public class GifExiftoolTestInvariantValidator : IMetadataInvariantValidator
	{
		public void ValidateMetadataInvariants (Image.File file)
		{
			Assert.IsNotNull (file);
			Assert.IsNotNull (file.Properties);

			Assert.AreEqual (12, file.Properties.PhotoWidth);
			Assert.AreEqual (37, file.Properties.PhotoHeight);
		}
	}
}
