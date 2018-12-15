using System;
using NUnit.Framework;

namespace TagLib.Tests.FileFormats
{   
	[TestFixture]
	public class MpgFormatTest : IFormatTest
	{
		private static string sample_file = TestPath.Samples + "Turning Lime.mpg";
		private static string sample_picture = TestPath.Samples + "sample_gimp.gif";
		private static string sample_other = TestPath.Samples + "apple_tags.m4a";
		private static string tmp_file = TestPath.Samples + "tmpwrite.mpg";
		private File file;
		

		[OneTimeSetUp]
		public void Init()
		{
			file = File.Create(sample_file);
		}
	

		[Test]
		public void ReadAudioProperties()
		{
			Assert.AreEqual(44100, file.Properties.AudioSampleRate);
			Assert.AreEqual(1391, file.Properties.Duration.TotalMilliseconds);
		}


		[Test]
		public void ReadTags()
		{
			Assert.IsTrue(file.Tag.IsEmpty);
		}


		[Test]
		public void WritePictures()
		{
			if (System.IO.File.Exists(tmp_file))
				System.IO.File.Delete(tmp_file);
			File file = null;
			try
			{
				System.IO.File.Copy(sample_file, tmp_file);
				file = File.Create(tmp_file);
			}
			finally { }
			Assert.NotNull(file);

			var pics = file.Tag.Pictures;
			Assert.AreEqual(0, pics.Length);

			// Insert new picture
			Array.Resize(ref pics, 3);
			pics[0] = new Picture(sample_picture);
			pics[0].Type = PictureType.BackCover;
			pics[0].Description = "TEST description 1";
			pics[1] = new Picture(sample_other);
			pics[1].Description = "TEST description 2";
			pics[2] = new Picture(sample_picture);
			pics[2].Type = PictureType.Other;
			pics[2].Description = "TEST description 3";
			file.Tag.Pictures = pics;

			file.Save();

			// Read back the Matroska-specific tags 
			file = File.Create(tmp_file);
			Assert.NotNull(file);
			pics = file.Tag.Pictures;

			Assert.AreEqual(3, pics.Length);

			// Filename has been changed to keep the PictureType information
			Assert.AreEqual(PictureType.BackCover, pics[0].Type);
			Assert.IsNull(pics[0].Filename);
			Assert.AreEqual("TEST description 1", pics[0].Description);
			Assert.AreEqual("image/gif", pics[0].MimeType);
			Assert.AreEqual(73, pics[0].Data.Count);

			Assert.IsNull(pics[1].Filename);
			Assert.AreEqual("TEST description 2", pics[1].Description);
			Assert.AreEqual("audio/mp4", pics[1].MimeType);
			Assert.AreEqual(PictureType.NotAPicture, pics[1].Type);
			Assert.AreEqual(102400, pics[1].Data.Count);

			Assert.AreEqual(PictureType.Other, pics[2].Type);
			Assert.IsNull(pics[2].Filename);
			Assert.AreEqual("TEST description 3", pics[2].Description);
			Assert.AreEqual("image/gif", pics[2].MimeType);
			Assert.AreEqual(73, pics[2].Data.Count);
		}


		[Test]
		public void WriteStandardTags ()
		{
			StandardTests.WriteStandardTags (sample_file, tmp_file, StandardTests.TestTagLevel.Medium);
		}

		[Test]
		public void WriteStandardPictures()
		{
			StandardTests.WriteStandardPictures(sample_file, tmp_file, ReadStyle.None);
		}

		[Test]
		public void WriteStandardPicturesLazy()
		{
			StandardTests.WriteStandardPictures(sample_file, tmp_file, ReadStyle.PictureLazy);
		}

		[Test]
		public void RemoveStandardTags()
		{
			StandardTests.RemoveStandardTags(sample_file, tmp_file);
		}

		[Test]
		public void TestCorruptionResistance()
		{
			StandardTests.TestCorruptionResistance (TestPath.Samples + "corrupt/a.mpg");
		}
	}
}
