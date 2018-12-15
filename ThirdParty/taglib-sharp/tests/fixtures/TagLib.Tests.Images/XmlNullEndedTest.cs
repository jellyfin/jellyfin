using System;
using NUnit.Framework;
using TagLib;
using TagLib.Image;
using TagLib.Xmp;

namespace TagLib.Tests.Images
{
	/// <summary>
	///    This file contains XMP data ended with null (0x00) value.
	/// </summary>
	[TestFixture]
	public class XmpNullEndedTest
	{
		private static string sample_file = TestPath.Samples + "sample_xmpnullended.jpg";

		[Test]
		public void ParseXmp ()
		{
			var file = File.Create (sample_file, "taglib/jpeg", ReadStyle.Average) as Image.File;
			Assert.IsNotNull (file, "file");

			var tag = file.ImageTag;
			Assert.IsNotNull (tag, "ImageTag");
			Assert.AreEqual ("SONY ", tag.Make);
			Assert.AreEqual ("DSLR-A330", tag.Model);
		}
	}
}
