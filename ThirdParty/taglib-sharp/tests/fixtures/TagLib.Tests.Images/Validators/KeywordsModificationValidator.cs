using NUnit.Framework;

namespace TagLib.Tests.Images.Validators
{
	/// <summary>
	///    This class tests the modification of the Keywords field,
	///    regardless of which metadata format is used.
	/// </summary>
	public class KeywordsModificationValidator : IMetadataModificationValidator
	{
		string[] orig_keywords;
		readonly string[] test_keywords = new string[] {"keyword 1", "§$&§%", "99 dsf", "ഈ ヰᛥกツ"};

		public KeywordsModificationValidator () : this (new string [] { }) { }

		public KeywordsModificationValidator (string[] orig_keywords)
		{
			this.orig_keywords = orig_keywords;
		}

		/// <summary>
		///    Check if the original keywords are found.
		/// </summary>
		public virtual void ValidatePreModification (Image.File file) {
			Assert.AreEqual (orig_keywords, GetTag (file).Keywords);
		}

		/// <summary>
		///    Changes the keywords.
		/// </summary>
		public virtual void ModifyMetadata (Image.File file) {
			GetTag (file).Keywords = test_keywords;
		}

		/// <summary>
		///    Validates if changes survived a write.
		/// </summary>
		public void ValidatePostModification (Image.File file) {
			Assert.IsNotNull (file.GetTag (TagTypes.XMP, false));
			Assert.AreEqual (test_keywords, GetTag (file).Keywords);
		}

		/// <summary>
		///    Returns the tag that should be tested. Default
		///    behavior is no specific tag.
		/// </summary>
		public virtual Image.ImageTag GetTag (Image.File file) {
			return file.ImageTag;
		}
	}
}
