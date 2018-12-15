using System;
using NUnit.Framework;

namespace TagLib.Tests.Images.Validators
{
	/// <summary>
	///    This class tests the modification of the Comment field,
	///    regardless of which metadata format is used.
	/// </summary>
	public class CommentModificationValidator : PropertyModificationValidator<string>
	{
		public CommentModificationValidator () : this (String.Empty) { }

		public CommentModificationValidator (string orig_comment)
			: this (orig_comment, "This is a TagLib# &Test?Comment%$@_ ")
		{}

		public CommentModificationValidator (string orig_comment, string test_comment)
			: base ("Comment", orig_comment, test_comment)
		{}
	}
}
