using System;
using NUnit.Framework;
using TagLib.Gif;
using TagLib.Xmp;
using TagLib.Tests.Images.Validators;

namespace TagLib.Tests.Images
{
	[TestFixture]
	public class GifExiftoolLongCommentTest
	{
		static readonly string long_comment_orig = "This is a very long comment, because long comments must be stored in mutiple sub-blocks. This comment is used to check that long comments are parsed correctly and that they are written back correctly. So what else to say? taglib rocks - 1234567890abcdefghijklmnopqrstuvwxyz1234567890abcdefghijklmnopqrstuvwxyz1234567890abcdefghijklmnopqrstuvwxyz1234567890abcdefghijklmnopqrstuvwxyz1234567890abcdefghijklmnopqrstuvwxyz1234567890abcdefghijklmnopqrstuvwxyz1234567890abcdefghijklmnopqrstuvwxyz1234567890abcdefghijklmnopqrstuvwxyz1234567890abcdefghijklmnopqrstuvwxyz1234567890abcdefghijklmnopqrstuvwxyz1234567890abcdefghijklmnopqrstuvwxyz1234567890abcdefghijklmnopqrstuvwxyz1234567890abcdefghijklmnopqrstuvwxyz1234567890abcdefghijklmnopqrstuvwxyz 1234567890abcdefghijklmnopqrstuvwxyz1234567890abcdefghijklmnopqrstuvwxyz 1234567890abcdefghijklmnopqrstuvwxyz1234567890abcdefghijklmnopqrstuvwxyz 1234567890abcdefghijklmnopqrstuvwxyz1234567890abcdefghijklmnopqrstuvwxyz1234567890abcdefghijklmnopqrstuvwxyz";
		static readonly string long_comment_test = "ABCD " + long_comment_orig + " CDEF " + long_comment_orig;

		[Test]
		public void Test ()
		{
			// This file is originally created with GIMP and the metadata was modified
			// by exiftool. A very long comment is added by exiftool because such comments
			// are stored in multiple sub-blocks. This should be handled by taglib.
			ImageTest.Run ("sample_exiftool_long_comment.gif",
				true,
				new GifExiftoolLongCommentTestInvariantValidator (),
				NoModificationValidator.Instance,
				new TagKeywordsModificationValidator (new string [] {}, TagTypes.XMP, true),
				new CommentModificationValidator (long_comment_orig),
				new CommentModificationValidator (long_comment_orig, long_comment_test),
				new TagCommentModificationValidator (long_comment_orig, TagTypes.GifComment, true),
				new TagCommentModificationValidator (long_comment_orig, long_comment_test, TagTypes.GifComment, true),
				new RemoveMetadataValidator (TagTypes.GifComment | TagTypes.XMP),
				new RemoveMetadataValidator (TagTypes.GifComment | TagTypes.XMP, TagTypes.GifComment),
				new RemoveMetadataValidator (TagTypes.GifComment | TagTypes.XMP, TagTypes.XMP)
			);
		}
	}

	public class GifExiftoolLongCommentTestInvariantValidator : IMetadataInvariantValidator
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
