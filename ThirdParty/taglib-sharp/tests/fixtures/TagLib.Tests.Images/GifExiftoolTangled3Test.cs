using System;
using NUnit.Framework;
using TagLib.Gif;
using TagLib.Xmp;
using TagLib.Tests.Images.Validators;

namespace TagLib.Tests.Images
{
	[TestFixture]
	public class GifExiftoolTangled3Test
	{
		[Test]
		public void Test ()
		{
			// This file is originally created with GIMP and the metadata was modified
			// by exiftool. Furthermore, the file is modified in the following way:
			// (1) the blocks which contain the metadata are moved to the end of the file.
			//     This is allowed and should be handled correctly by taglib.
			// (2) XMP Block is removed.
			ImageTest.Run ("sample_exiftool_tangled3.gif",
				true,
				new GifExiftoolTangled3TestInvariantValidator (),
				NoModificationValidator.Instance,
				new TagKeywordsModificationValidator (new string [] {}, TagTypes.XMP, false),
				new CommentModificationValidator ("Created with GIMP"),
				new TagCommentModificationValidator ("Created with GIMP", TagTypes.GifComment, true),
				new RemoveMetadataValidator (TagTypes.GifComment)
			);
		}
	}

	public class GifExiftoolTangled3TestInvariantValidator : IMetadataInvariantValidator
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
