using System;
using NUnit.Framework;
using TagLib.Gif;
using TagLib.Xmp;
using TagLib.Tests.Images.Validators;

namespace TagLib.Tests.Images
{
	[TestFixture]
	public class GifExiftoolTangled1Test
	{
		[Test]
		public void Test ()
		{
			// This file is originally created with GIMP and the metadata was modified
			// by exiftool. Furthermore, the file is modified in the following way:
			// (1) the version 89a is substituted by 87a, this leads to an invalid 87a
			//     file, but we change the version if we write metadata.
			// (2) the blocks which contain the metadata are moved to the end of the file.
			//     This is allowed and should be handled correctly by taglib.
			ImageTest.Run ("sample_exiftool_tangled1.gif",
				true,
				new GifExiftoolTangled1TestInvariantValidator (),
				NoModificationValidator.Instance,
				new TagKeywordsModificationValidator (new string [] {"Keyword 1", "Keyword 2"}, TagTypes.XMP, true),
				new CommentModificationValidator ("Created with GIMP"),
				new TagCommentModificationValidator ("Created with GIMP", TagTypes.GifComment, true),
				new RemoveMetadataValidator (TagTypes.GifComment | TagTypes.XMP),
				new RemoveMetadataValidator (TagTypes.GifComment | TagTypes.XMP, TagTypes.GifComment),
				new RemoveMetadataValidator (TagTypes.GifComment | TagTypes.XMP, TagTypes.XMP)
			);
		}
	}

	public class GifExiftoolTangled1TestInvariantValidator : IMetadataInvariantValidator
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
