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
	public class MediaBrowserWandTests : BaseTest
	{
		//Todo
		[TestMethod()]
		public void MediaBrowserCollectionImageTest()
		{
			using (var wand = new MagickWand(TestImageBackdrop))
			{
				var w = wand.CurrentImage.Width;
				var h = wand.CurrentImage.Height;

				wand.CurrentImage.AlphaChannel = AlphaChannelType.DeactivateAlphaChannel;
				using (var mwr = wand.CloneMagickWand())
				{
					mwr.CurrentImage.ResizeImage(w, h / 2, FilterTypes.LanczosFilter, 1);
					using (var mwg = new MagickWand(w, h / 2))
					{
						mwg.OpenImage(TestImageBackdrop);
						mwr.CurrentImage.CompositeImage(mwg, CompositeOperator.CopyOpacityCompositeOp, 0, 0);
						wand.AddImage(mwr);
						var t = wand.AppendImages(true);
                        t.SaveImage(Path.Combine(SaveDirectory, Guid.NewGuid().ToString() + ".jpg"));
                    }
				}
			}

		}

        [TestMethod()]
        public void MediaBrowserScaleImageTest()
        {
            using (var wand = new MagickWand(TestImageBackdrop))
            {
                var w = wand.CurrentImage.Width;
                var h = wand.CurrentImage.Height;

                using (var mwr = wand.CloneMagickWand())
                {
                    var newW = 1280;
                    var newH = 720;
                    mwr.CurrentImage.ScaleImage(newW, newH);
                    mwr.CurrentImage.StripImage();
                    mwr.SaveImage(Path.Combine(SaveDirectory, Guid.NewGuid().ToString() + ".jpg"));
                }
            }

        }

        [TestMethod()]
        public void MediaBrowserResizeImageTest()
        {
            using (var wand = new MagickWand(TestImageBackdrop))
            {
                var w = wand.CurrentImage.Width;
                var h = wand.CurrentImage.Height;

                using (var mwr = wand.CloneMagickWand())
                {
                    var newW = 1280;
                    var newH = 720;
                    mwr.CurrentImage.ResizeImage(newW, newH);
                    mwr.CurrentImage.StripImage();
                    mwr.SaveImage(Path.Combine(SaveDirectory, Guid.NewGuid().ToString() + ".jpg"));
                }
            }

        }

        [TestMethod()]
        public void MediaBrowserThumbnailImageTest()
        {
            using (var wand = new MagickWand(TestImageBackdrop))
            {
                var w = wand.CurrentImage.Width;
                var h = wand.CurrentImage.Height;

                using (var mwr = wand.CloneMagickWand())
                {
                    var newW = 1280;
                    var newH = 720;
                    mwr.CurrentImage.MagickThumbnailImage(newW, newH, true, false);
                    mwr.CurrentImage.StripImage();
                    mwr.SaveImage(Path.Combine(SaveDirectory, Guid.NewGuid().ToString() + ".jpg"));
                }
            }

        }

        [TestMethod()]
        public void MediaBrowserResizePerformanceImageTest()
        {
            for (var i = 0; i < 100; i++)
            {
                using (var wand = new MagickWand(TestImageBackdrop))
                {
                    var w = wand.CurrentImage.Width;
                    var h = wand.CurrentImage.Height;

                    using (var mwr = wand.CloneMagickWand())
                    {
                        var newW = 1280;
                        var newH = 720;
                        mwr.CurrentImage.ResizeImage(newW, newH, FilterTypes.CatromFilter);
                        mwr.SaveImage(Path.Combine(SaveDirectory, Guid.NewGuid().ToString() + ".jpg"));
                    }
                }
            }
        }

        [TestMethod()]
        public void MediaBrowserScalePerformanceImageTest()
        {
            for (var i = 0; i < 100; i++)
            {
                using (var wand = new MagickWand(TestImageBackdrop))
                {
                    var w = wand.CurrentImage.Width;
                    var h = wand.CurrentImage.Height;

                    using (var mwr = wand.CloneMagickWand())
                    {
                        var newW = 1280;
                        var newH = 720;
                        mwr.CurrentImage.ScaleImage(newW, newH);
                        mwr.SaveImage(Path.Combine(SaveDirectory, Guid.NewGuid().ToString() + ".jpg"));
                    }
                }
            }
        }

        [TestMethod()]
        public void MediaBrowserThumbnailPerformanceImageTest()
        {
            for (var i = 0; i < 100; i++)
            {
                using (var wand = new MagickWand(TestImageBackdrop))
                {
                    var w = wand.CurrentImage.Width;
                    var h = wand.CurrentImage.Height;

                    using (var mwr = wand.CloneMagickWand())
                    {
                        var newW = 1280;
                        var newH = 720;
                        mwr.CurrentImage.MagickThumbnailImage(newW, newH, true, true);
                        mwr.SaveImage(Path.Combine(SaveDirectory, Guid.NewGuid().ToString() + ".jpg"));
                    }
                }
            }
        }

        [TestMethod()]
		public void MediaBrowserClipMaskTest()
		{

			var dest = new MagickWand(100, 100);
			var mask = new MagickWand();
			var src = new MagickWand(100, 100);

			dest.OpenImage(this.TestImageFolder1);
			mask.OpenImage(this.TestImageFolder2);
			mask.CurrentImage.NegateImage(false);
			mask.SaveImage(Path.Combine(SaveDirectory, "TestImageBackdropMask.png"));
			dest.CurrentImage.SetImageClipMask(mask);
			src.OpenImage(this.TestImageBackdrop);
			dest.CurrentImage.CompositeImage(src, CompositeOperator.OverCompositeOp, 0, 0);
			dest.SaveImage(Path.Combine(SaveDirectory, "TestImageBackdrop.png"));

		}


		[TestMethod()]
		public void MediaBrowserWandRoundCornersTest()
		{
			var cofactor = 15;
			using (var wand = new MagickWand(TestImageBackdrop).RoundCorners(cofactor))
				wand.SaveImage(Path.Combine(SaveDirectory, "TestImageBackdrop.png"));

		}

		[TestMethod()]
		public void MediaBrowserWandTextTests()
		{
			using (var wand = new MagickWand(TestImageBackdrop))
            using (var yellowPixelWand = new PixelWand("yellow"))            
            using (var whitePixelWand = new PixelWand("white"))
            using (var blackPixelWand = new PixelWand("black", 0.5))
			{

                wand.CurrentImage.DrawRoundRectangle(10, 10, wand.CurrentImage.Width - 10, 70, 5, 5, yellowPixelWand, blackPixelWand);

                wand.CurrentImage.DrawCircle(400, 300, 500, 400, yellowPixelWand, blackPixelWand);
                wand.CurrentImage.DrawCircle(400, 400, 60, yellowPixelWand, blackPixelWand);

                wand.CurrentImage.DrawRectangle(0, wand.CurrentImage.Height - 70, wand.CurrentImage.Width - 1, wand.CurrentImage.Height, yellowPixelWand, blackPixelWand);
                wand.CurrentImage.DrawText("Media Browser", 10, wand.CurrentImage.Height - 10, "Arial", 60, whitePixelWand, FontWeightType.BoldStyle);

				wand.SaveImage(Path.Combine(SaveDirectory, "TestImageBackdrop.jpg"));
			}
		}

		[TestMethod()]
		public void MediaBrowserWandOverlayTests()
		{
			using (var wand = new MagickWand(TestImageBackdrop))
			{
				using (MagickWand wandComposit = new MagickWand(TestImageLogo))
				{
					//draw.FillOpacity = 0.5;
					wand.CurrentImage.OverlayImage(CompositeOperator.AtopCompositeOp, 560, 660, wandComposit.CurrentImage.Width, wandComposit.CurrentImage.Height, wandComposit);
				}

				wand.SaveImage(Path.Combine(SaveDirectory, "TestImageBackdrop.jpg"));
			}
		}

		[TestMethod()]
		public void MediaBrowserWandCropWhitespaceTests()
		{
			using (var wand = new MagickWand(TestImageLogo))
			{
				wand.CurrentImage.TrimImage(10);
				wand.SaveImage(Path.Combine(SaveDirectory, "TestImageBackdrop.png"));
			}
		}
	}
}
