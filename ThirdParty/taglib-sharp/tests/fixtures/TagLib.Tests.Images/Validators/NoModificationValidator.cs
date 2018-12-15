namespace TagLib.Tests.Images.Validators
{
	/// <summary>
	///    This class writes a file unmodified and tests if all metadata
	///    is still present. Default behavior for the modification
	///    validator.
	/// </summary>
	public class NoModificationValidator : IMetadataModificationValidator {
		static NoModificationValidator instance = new NoModificationValidator ();
		public static NoModificationValidator Instance {
			get { return instance; }
		}

		/// <summary>
		///    No preconditions that will change (everything is checked
		///    in the invariant validator).
		/// </summary>
		public void ValidatePreModification (Image.File file) { }

		/// <summary>
		///    No modifications.
		/// </summary>
		public void ModifyMetadata (Image.File file) { }

		/// <summary>
		///    No changes.
		/// </summary>
		public void ValidatePostModification (Image.File file) { }
	}
}
