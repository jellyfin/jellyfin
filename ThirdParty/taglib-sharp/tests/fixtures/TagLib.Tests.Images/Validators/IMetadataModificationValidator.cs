namespace TagLib.Tests.Images.Validators
{
	public interface IMetadataModificationValidator {
		/// <summary>
		///    Validate metadata assumptions that should hold
		///    before modification.
		/// </summary>
		void ValidatePreModification (Image.File file);

		/// <summary>
		///    Modify the metadata of a file.
		/// </summary>
		void ModifyMetadata (Image.File file);

		/// <summary>
		///    Validate metadata assumptions that should hold
		///    after modification.
		/// </summary>
		void ValidatePostModification (Image.File file);
	}
}
