using System;
using NUnit.Framework;

namespace TagLib.Tests.Images.Validators
{
	/// <summary>
	///    This class tests the modification of the Comment field,
	///    in a specific tag.
	/// </summary>
	public class TagCommentModificationValidator : TagPropertyModificationValidator<string>
	{
		public TagCommentModificationValidator (TagTypes type, bool tag_present)
			: this (null, type, tag_present)
		{}

		public TagCommentModificationValidator (string orig_comment, TagTypes type, bool tag_present)
			: this (orig_comment, "This is a TagLib# &Test?Comment%$@_ ", type, tag_present)
		{}

		public TagCommentModificationValidator (string orig_comment, string test_comment, TagTypes type, bool tag_present)
			: base ("Comment", orig_comment, test_comment, type, tag_present)
		{}
	}
}
