using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageMagickSharp;
using ImageMagickSharp.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Diagnostics;
namespace ImageMagickSharp.Tests
{
	[TestClass()]
	public class CoverArtWandTests : BaseTest
	{

		[TestMethod()]
		public void CoverArtWandRotateTests()
		{
			using (var wand = new MagickWand(this.TestImageFolder1))
			{
				wand.CurrentImage.RotateImage(new PixelWand("transparent", 1), 30);
				//wand.CurrentImage.TrimImage(10);
				wand.SaveImage(Path.Combine(SaveDirectory, "logo_extent.png"));
			}
		}

		//Todo
		[TestMethod()]
		public void CoverArtWand3DTests()
		{
			using (var wand = new MagickWand(TestImageFolder1))
			{
				var t = wand.CloneMagickWand();
				t.CurrentImage.ShearImage(new PixelWand(ColorHEX.None, 1), 0, 10);
				t.CurrentImage.ExtentImage(t.CurrentImage.Width + 50, t.CurrentImage.Height + 50, -25, -25);
				//RaiseImage
				//wand.CurrentImage.ShadeImage(true, 5, 6);
				//
				wand.CurrentImage.TrimImage(100);
				t.SaveImage(Path.Combine(SaveDirectory, "logo_extent.png"));

			}
		}

		[TestMethod()]
		public void CoverArtWandShadowTests()
		{
			using (var wand = new MagickWand(TestImageFolder1))
			{
				using (MagickWand nailclone = wand.CloneMagickWand())
                using (var blackPixelWand = new PixelWand(ColorName.Black))
				{
                    nailclone.CurrentImage.BackgroundColor = blackPixelWand;
					nailclone.CurrentImage.ShadowImage(80, 5, 5, 5);
					nailclone.CurrentImage.CompositeImage(wand, CompositeOperator.CopyCompositeOp, 0, 0);
					nailclone.SaveImage(Path.Combine(SaveDirectory, "logo_extent.png"));
				}
			}
		}

		/*[TestMethod()]
		public void CoverArtWandStackTests()
		{
			using (var wand = new MagickWand(1000, 1500, "White"))
			{
				wand.CoverArtStack(60, 60, 0, 0, this.TestImageFolder1, this.TestImageFolder2, this.TestImageFolder3);
				wand.SaveImage(Path.Combine(SaveDirectory, "StackOutput.png"));
			}
		}*/


	}
}
