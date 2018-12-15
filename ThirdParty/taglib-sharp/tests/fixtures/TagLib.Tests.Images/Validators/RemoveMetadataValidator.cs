using System;
using NUnit.Framework;

namespace TagLib.Tests.Images.Validators
{
	/// <summary>
	///    This class tests the removal of metadata
	/// </summary>
	public class RemoveMetadataValidator : IMetadataModificationValidator
	{
		TagTypes remove_types;
		TagTypes contained_types;

		public RemoveMetadataValidator (TagTypes contained_types) : this (contained_types, contained_types) {}

		public RemoveMetadataValidator (TagTypes contained_types, TagTypes remove_types)
		{
			this.contained_types = contained_types;
			this.remove_types = remove_types;
		}

		public void ValidatePreModification (Image.File file)
		{
			Assert.AreEqual (contained_types, file.TagTypes);
		}

		public void ModifyMetadata (Image.File file)
		{
			file.RemoveTags (remove_types);
		}

		public void ValidatePostModification (Image.File file) {
			Assert.AreEqual (contained_types & (~remove_types), file.TagTypes);
		}
	}
}
