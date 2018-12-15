using System;
using System.Reflection;
using NUnit.Framework;
using Gdk;

namespace TagLib.Tests.Images.Validators
{
	public class ImageTest
	{

		static ImageTest() {
			// Initialize GDK 
			var args = Environment.GetCommandLineArgs();
			Global.InitCheck(ref args);
		}

		string pre_hash;
		string post_hash;

		public static bool CompareLargeImages {
			get {
				return Environment.GetEnvironmentVariable ("COMPARE_LARGE_FILES") == "1";
			}
		}

		public bool CompareImageData {
			get; set;
		}

		public static void Run (string filename, IMetadataInvariantValidator invariant, params IMetadataModificationValidator[] modifications)
		{
			Run (filename, true, invariant, modifications);
		}

		public static void Run (string directory, string filename, IMetadataInvariantValidator invariant, params IMetadataModificationValidator[] modifications)
		{
			Run (directory, filename, true, invariant, modifications);
		}

		public static void Run (string filename, bool compare_image_data, IMetadataInvariantValidator invariant, params IMetadataModificationValidator[] modifications)
		{
			Run (TestPath.Samples, filename, compare_image_data, invariant, modifications);
		}

		public static void Run (string directory, string filename, bool compare_image_data, IMetadataInvariantValidator invariant, params IMetadataModificationValidator[] modifications)
		{
			if (modifications.Length == 0) {
				ImageTest test = new ImageTest () {
						ImageFileName = filename,
						ImageDirectory = directory,
						TempDirectory = directory,
						CompareImageData = compare_image_data,
						InvariantValidator = invariant,
						ModificationValidator = null
					};
					test.TestImage ();
			} else {
				foreach (var modification in modifications) {
					ImageTest test = new ImageTest () {
						ImageFileName = filename,
						ImageDirectory = directory,
						TempDirectory = directory,
						CompareImageData = compare_image_data,
						InvariantValidator = invariant,
						ModificationValidator = modification
					};
					test.TestImage ();
				}
			}
		}

		void TestImage ()
		{
			var file = ParseUnmodifiedFile ();

			if (file.Writeable && ModificationValidator == null)
				throw new Exception ("Wrong usage of test. A writeable file must be tested at least with a NoModificationValidator");

			if (ModificationValidator == null)
				return;

			ModifyFile ();
			ParseModifiedFile ();
		}

		/// <summary>
		///    Parse the unmodified file.
		/// </summary>
		TagLib.Image.File ParseUnmodifiedFile ()
		{
			var file = ReadFile (ImageFile);
			InvariantValidator.ValidateMetadataInvariants (file);
			if (CompareImageData)
				pre_hash = ReadImageData (file);

			return file;
		}

		/// <summary>
		///    Modify and save the file.
		/// </summary>
		void ModifyFile ()
		{
			CreateTmpFile ();
			var tmp = ReadFile (TempImageFile);
			InvariantValidator.ValidateMetadataInvariants (tmp);
			ModificationValidator.ValidatePreModification (tmp);
			ModificationValidator.ModifyMetadata (tmp);
			ModificationValidator.ValidatePostModification (tmp);
			Assert.IsTrue (tmp.Writeable, "File should be writeable");
			Assert.IsFalse (tmp.PossiblyCorrupt, "Corrupt files should never be written");
			tmp.Save ();
		}

		/// <summary>
		///    Re-parse the modified file.
		/// </summary>
		void ParseModifiedFile ()
		{
			var tmp = ReadFile (TempImageFile);
			InvariantValidator.ValidateMetadataInvariants (tmp);
			ModificationValidator.ValidatePostModification (tmp);
			if (CompareImageData) {
				post_hash = ReadImageData (tmp);
				ValidateImageData ();
			}
		}

		Image.File ReadFile (string path)
		{
			return File.Create (path) as Image.File;
		}

		/// <summary>
		///    Only files we can actually render are supported, for most types
		///    this just means adding the type here. RAW files need extra
		///    attention.
		/// </summary>
		bool IsSupportedImageFile (Image.File file)
		{
			return (file is Jpeg.File) || (file is Tiff.File) || (file is Gif.File) || (file is Png.File);
		}

		string ReadImageData (Image.File file)
		{
			if (!IsSupportedImageFile (file))
				Assert.Fail("Unsupported type for data reading: "+file);

			file.Mode = File.AccessMode.Read;
			ByteVector v = file.ReadBlock ((int) file.Length);
			byte [] result = null;
			using (Pixbuf buf = new Pixbuf(v.Data))
				result = buf.SaveToBuffer("png");
			file.Mode = File.AccessMode.Closed;
			return Utils.Md5Encode (result);
		}

		void ValidateImageData ()
		{
			string label = String.Format ("Image data mismatch for {0}/{1}", ImageFileName, ModificationValidator);
			Assert.AreEqual (pre_hash, post_hash, label);
		}

		void CreateTmpFile ()
		{
			if (System.IO.File.Exists (TempImageFile))
				System.IO.File.Delete (TempImageFile);
			System.IO.File.Copy (ImageFile, TempImageFile);
		}

		/// <summary>
		///    The filename of the file to test. Name only, no paths.
		/// </summary>
		string ImageFileName {
			get; set;
		}

		string TempImageFileName {
			get { return String.Format ("tmpwrite_{0}", ImageFileName); }
		}

		string ImageDirectory {
			get; set;
		}

		string TempDirectory {
			get; set;
		}

		string ImageFile {
			get { return String.Format ("{0}/{1}", ImageDirectory, ImageFileName); }
		}

		string TempImageFile {
			get { return String.Format ("{0}/{1}", TempDirectory, TempImageFileName); }
		}

		/// <summary>
		///    The invariant validator tests for properties that are
		///    never changed during the course of testing and should
		///    stay the same, even during writing.
		/// </summary>
		public IMetadataInvariantValidator InvariantValidator {
			get; set;
		}

		/// <summary>
		///    The modification validator modifies a file and tests the
		///    assumptions about this modification. Default behavior is
		///    to write a file unmodified and see if it's still the same.
		///    Setting this to null disables write testing.
		/// </summary>
		public IMetadataModificationValidator ModificationValidator {
			get; set;
		}
	}
}
