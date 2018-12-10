using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageMagickSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using ImageMagickSharp.Extensions;
using System.Diagnostics;

namespace ImageMagickSharp.Tests
{
	[TestClass()]
	public class ImageWandTests : BaseTest
	{
        [TestMethod()]
        public void MediaBrowserPosterCollectionImageTest()
        {
            string imageOut = "TestCollectionImages.png";
            using (var wandImages = new MagickWand(TestImageFolder1, TestImageFolder2, TestImageFolder3, TestImageFolder4))
            {
                var wandReturn = MediaBrowserWandExtension.MediaBrowserPosterCollectionImage(wandImages);
                wandReturn.SaveImage(Path.Combine(SaveDirectory, imageOut));
            }
        }

        [TestMethod()]
        public void MediaBrowserPosterCollectionImageWithText()
        {
            string imageOut = "TestCollectionImages.png";
            using (var wandImages = new MagickWand(TestImageFolder1, TestImageFolder2, TestImageFolder3, TestImageFolder4))
            {
                var wandReturn = MediaBrowserWandExtension.MediaBrowserPosterCollectionImageWithText(wandImages, "Collectionsø", MontserratLightFont);
                wandReturn.SaveImage(Path.Combine(SaveDirectory, imageOut));
            }
        }

        [TestMethod()]
        public void MediaBrowserSquareCollectionImageTest()
        {
            string imageOut = "TestCollectionImages.png";
            using (var wandImages = new MagickWand(TestImageFolder1, TestImageFolder2, TestImageFolder3, TestImageFolder4))
            {
                var wandReturn = MediaBrowserWandExtension.MediaBrowserSquareCollectionImage(wandImages);
                wandReturn.SaveImage(Path.Combine(SaveDirectory, imageOut));
            }
        }

        [TestMethod()]
        public void MediaBrowserSquareCollectionImageWithTextTest()
        {
            string imageOut = "TestCollectionImages.png";
            using (var wandImages = new MagickWand(TestImageFolder1, TestImageFolder2, TestImageFolder3, TestImageFolder4))
            {
                var wandReturn = MediaBrowserWandExtension.MediaBrowserSquareCollectionImageWithText(wandImages, "Collections", MontserratLightFont);
                wandReturn.SaveImage(Path.Combine(SaveDirectory, imageOut));
            }
        }

        [TestMethod()]
		public void MediaBrowserCollectionImageTest()
		{
			string imageOut = "TestCollectionImages.png";
			using (var wandImages = new MagickWand(TestImageFolder1, TestImageFolder2, TestImageFolder3, TestImageFolder4, TestImageFolder1, TestImageFolder2, TestImageFolder3, TestImageFolder4))
			{
				var wandReturn = MediaBrowserWandExtension.MediaBrowserCollectionImage(wandImages);
				wandReturn.SaveImage(Path.Combine(SaveDirectory, imageOut));
			}
		}

        [TestMethod()]
        public void MediaBrowserCollectionImageWithTextTest()
        {
            string imageOut = "TestCollectionImages.png";
            using (var wandImages = new MagickWand(TestImageFolder1, TestImageFolder2, TestImageFolder3, TestImageFolder4, TestImageFolder1, TestImageFolder2, TestImageFolder3, TestImageFolder4))
            {
                var wandReturn = MediaBrowserWandExtension.MediaBrowserCollectionImageWithText(wandImages, "Collections", MontserratLightFont);
                wandReturn.SaveImage(Path.Combine(SaveDirectory, imageOut));
            }
        }

        [TestMethod()]
		public void ResizeImageTestDir()
		{
			var path = @"D:\Video\TV\Carnivàle\Season 2\Carnivàle - 2x09 - Lincoln Highway DVD.jpg";

			Assert.IsTrue(File.Exists(path));

			using (var wand = new MagickWand(path))
			{
				wand.CurrentImage.ResizeImage(400, 150);

				wand.SaveImage(Path.Combine(SaveDirectory, "TestResize.jpg"));
				wand.SaveImage(Path.Combine(SaveDirectory, "TestResize.png"));
				wand.SaveImage(Path.Combine(SaveDirectory, "TestResize.webp"));
			}
		}

		[TestMethod()]
		public void ResizeImageTest()
		{
			var path = TestImageLogo;

			Assert.IsTrue(File.Exists(path));

			using (var wand = new MagickWand(path))
			{
				wand.CurrentImage.ResizeImage(400, 150);

				wand.SaveImage(Path.Combine(SaveDirectory, "TestResize.jpg"));
				wand.SaveImage(Path.Combine(SaveDirectory, "TestResize.png"));
				wand.SaveImage(Path.Combine(SaveDirectory, "TestResize.webp"));
			}
		}

		[TestMethod()]
		public void ExtendcanvasaroundimageTest()
		{

			var path = TestImageThumb;

			Assert.IsTrue(File.Exists(path));

			using (var wand = new MagickWand(path))
			{
				wand.OpenImage(path);
				var w = wand.CurrentImage.Width;
				var h = wand.CurrentImage.Height;

				using (PixelWand newPixelWand = new PixelWand("blue"))
				{
					wand.CurrentImage.BackgroundColor = newPixelWand;
				}
				wand.CurrentImage.ExtentImage(1024, 768, -(1024 - w) / 2, -(768 - h) / 2);
				wand.SaveImage(Path.Combine(SaveDirectory, "logo_extent.jpg"));

			}
		}

		[TestMethod()]
		public void ImageWandCreateManyTest()
		{

			using (var wand = new MagickWand())
            using (var yellowPixelWand = new PixelWand("yellow"))
            using (var blackPixelWand = new PixelWand("black", 0.5))
			{
				wand.NewImage(200, 200, "Blue");
                wand.CurrentImage.DrawRoundRectangle(10, 10, wand.CurrentImage.Width - 10, 70, 5, 5, yellowPixelWand, blackPixelWand);
				wand.NewImage(200, 200, "red");
                wand.CurrentImage.DrawRoundRectangle(10, 10, wand.CurrentImage.Width - 10, 70, 5, 5, yellowPixelWand, blackPixelWand);
				wand.NewImage(200, 200, "green");
                wand.CurrentImage.DrawRoundRectangle(10, 10, wand.CurrentImage.Width - 10, 70, 5, 5, yellowPixelWand, blackPixelWand);
				wand.SaveImages(Path.Combine(SaveDirectory, "logo_extent.jpg"));

			}
		}

		[TestMethod()]
		public void ImageWandImageClassTest()
		{

			using (var wand = new MagickWand())
            using (var yellowPixelWand = new PixelWand("yellow"))
            using (var blackPixelWand = new PixelWand("black", 0.5))
			{
				wand.NewImage(200, 200, "Blue");
                wand.CurrentImage.DrawRoundRectangle(10, 10, wand.CurrentImage.Width - 10, 70, 5, 5, yellowPixelWand, blackPixelWand);
				var t = wand.GetImage();
				//wand.Image.RotateImage("red", 45);
				//t.RotateImage("red", 45);
				t.SaveImages(Path.Combine(SaveDirectory, "logo_extent.jpg"));
				wand.SaveImages(Path.Combine(SaveDirectory, "logo_extent.jpg"));

			}
		}

		//Todo
		/*[TestMethod()]
		public void ImageWandLabelImageTests()
		{
			using (var wand = new MagickWand(200, 200, "lightblue"))
            using (var maroonPixelWand = new PixelWand(ColorName.Maroon))
			{
				wand.BackgroundColor = maroonPixelWand;

				wand.Font = "Arial";
				Debug.Print(wand.Font);
				wand.Pointsize = 72;
				wand.CurrentImage.LabelImage("Media Browser");
				wand.SaveImage(Path.Combine(SaveDirectory, "logo_extent.png"));

			}
		}*/

		/*[TestMethod()]
        public void GetImagePixelColorTest()
        {
            var path = TestImageFolder1;

            Assert.IsTrue(File.Exists(path));

            using (var wand = new MagickWand(path))
            {
                var pi = wand.CurrentImage.GetImagePixelColor(1, 1);
                Debug.Print(pi.Color);
            }
        }*/

/*		//Todo
		[TestMethod()]
		public void ColorMatrixTests()
		{
			using (var wand = new MagickWand(TestImageFolder1))
			{
				double[,] m = 
				{
				 {0.5, 0.5, 0.5, 0, 0} ,
				  {0.5, 1, 0.5, 0, 0 },
				 { 0.5, 0.5,1, 0, 0 },
				 { 0.5, 0.5, 0.5, 1, 1 } ,
				 { 0.5, 0.5, 0.5, 1, 0 }
				};
				//wand.CurrentImage.ImageVirtualPixel = VirtualPixelType.Black;
				bool t = wand.CurrentImage.ColorMatrixImage(m);
				wand.SaveImage(Path.Combine(SaveDirectory, "ColorMatrix_Out.png"));
			}
		}
        */
	}
}
