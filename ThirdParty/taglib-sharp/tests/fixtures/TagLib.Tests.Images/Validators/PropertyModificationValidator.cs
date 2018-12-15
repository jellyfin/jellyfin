using System;
using System.Reflection;
using NUnit.Framework;

namespace TagLib.Tests.Images.Validators
{


	public class PropertyModificationValidator<T> : IMetadataModificationValidator
	{
		T test_value;
		T orig_value;

		PropertyInfo property_info;

		public PropertyModificationValidator (string property_name, T orig_value, T test_value)
		{
			this.test_value = test_value;
			this.orig_value = orig_value;

			property_info = typeof (Image.ImageTag).GetProperty (property_name);

			if (property_info == null)
				throw new Exception (String.Format ("There is no property named {0} in ImageTag", property_name));
		}

		public virtual void ValidatePreModification (Image.File file) {
			Assert.AreEqual (orig_value, GetValue (GetTag (file)));
		}

		public virtual void ModifyMetadata (Image.File file) {
			SetValue (GetTag (file), test_value);
		}

		public void ValidatePostModification (Image.File file) {
			Assert.AreEqual (test_value, GetValue (GetTag (file)));
		}

		public virtual Image.ImageTag GetTag (Image.File file) {
			return file.ImageTag;
		}

		public void SetValue (Image.ImageTag tag, T value)
		{
			Assert.IsNotNull (tag);

			property_info.SetValue (tag, value, null);
		}

		public T GetValue (Image.ImageTag tag)
		{
			Assert.IsNotNull (tag);

			return (T) property_info.GetValue (tag, null);
		}
	}
}
