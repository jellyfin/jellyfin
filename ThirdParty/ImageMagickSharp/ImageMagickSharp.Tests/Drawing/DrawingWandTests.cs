using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageMagickSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Diagnostics;
namespace ImageMagickSharp.Tests
{
	[TestClass()]
	public class DrawingWandTests : BaseTest
	{
		[TestMethod()]
		public void DrawingWandCustomFontTest()
		{
			//using (var wand = new MagickWand(TestImageBackdrop))
			using (var wand = new MagickWand(600, 200, "#ffffff"))
			{
				//wand.NewImage(400, 200, new PixelWand("white"));
				//wand.OpenImage(TestImageBackdrop); 
				using (var draw = new DrawingWand())
				{
					using (PixelWand pixel = new PixelWand("red"))
					{
						draw.FillColor = pixel;
						draw.Font = CustomFonts.WedgieRegular;
						draw.FontSize = 40;
						draw.FontStyle = FontStyleType.NormalStyle;
						draw.TextAlignment = TextAlignType.LeftAlign;
						draw.FontWeight = FontWeightType.BoldStyle;
						draw.TextAntialias = true;
						draw.DrawAnnotation(0, 40, "Media Browser");
						draw.BorderColor = new PixelWand("red");
						//draw.Font = "Times-New-Roman";
						//pixel.Color = "Red";
						//pixel.Opacity = 0.8;
						//draw.FillColor = pixel;
						//draw.DrawAnnotation(60, 120, "Tavares");
						Debug.WriteLine(draw);
						wand.CurrentImage.DrawImage(draw);
					}

				}
				//Debug.WriteLine(wand.GetNumberImages());
				//wand.Image.TrimImage(10);
				wand.SaveImage(Path.Combine(SaveDirectory, "logo_extent.jpg"));

			}
		}

		[TestMethod()]
		public void DrawingWandAnnotationTest()
		{
			//using (var wand = new MagickWand(TestImageBackdrop))
			using (var wand = new MagickWand(400, 100, "#ffffff"))
			{
				//wand.NewImage(400, 200, new PixelWand("white"));
				//wand.OpenImage(TestImageBackdrop); 
				using (var draw = new DrawingWand())
				{
					using (PixelWand pixel = new PixelWand("black"))
					{
						draw.FillColor = pixel;
						draw.Font = "Arial";
						draw.FontSize = 20;
						draw.FontStyle = FontStyleType.NormalStyle;
						draw.TextAlignment = TextAlignType.LeftAlign;
						draw.FontWeight = FontWeightType.BoldStyle;
						draw.TextAntialias = true;
						draw.DrawAnnotation(0, 20, "Media Browser");
						draw.BorderColor = new PixelWand("red");
						//draw.Font = "Times-New-Roman";
						//pixel.Color = "Red";
						//pixel.Opacity = 0.8;
						//draw.FillColor = pixel;
						//draw.DrawAnnotation(60, 120, "Tavares");
						Debug.WriteLine(draw);
						wand.CurrentImage.DrawImage(draw);
					}
					
				}
				//Debug.WriteLine(wand.GetNumberImages());
				//wand.Image.TrimImage(10);
				wand.SaveImage(Path.Combine(SaveDirectory, "logo_extent.jpg"));

			}
		}

		[TestMethod()]
		public void DrawingWandRectangleTest()
		{
			using (var wand = new MagickWand(TestImageBackdrop))
			{
				//wand.NewImage(400, 200, new PixelWand("white"));
				//wand.OpenImage(TestImageBackdrop); 
				using (var draw = new DrawingWand())
				{
					using (PixelWand pixel = new PixelWand())
					{

						pixel.Color = "red";
						draw.StrokeColor = pixel;
						pixel.Color = "black";
						pixel.Opacity = 0.5;
						draw.FillColor = pixel;
						draw.DrawRectangle(0, 0, wand.CurrentImage.Width - 1, 120);

						pixel.Color = "transparent";
						draw.StrokeColor = pixel;
						pixel.Color = "white";
						draw.FillColor = pixel;
						draw.Font = "Verdana";
						draw.FontSize = 120;
						draw.FontStyle = FontStyleType.NormalStyle;
						draw.TextAlignment = TextAlignType.LeftAlign;
						draw.FontWeight = FontWeightType.BoldStyle;
						draw.TextAntialias = true;
						draw.DrawAnnotation(10, 100, "Media Browser");

			
						
						
						draw.FillColor = pixel;
						wand.CurrentImage.DrawImage(draw);
					}

				}
				//Debug.WriteLine(wand.GetNumberImages());
				//wand.Image.TrimImage(10);
				wand.SaveImage(Path.Combine(SaveDirectory, "logo_extent.jpg"));

			}
		}

		[TestMethod()]
		public void DrawingWandCircleTest()
		{
			using (var wand = new MagickWand(TestImageBackdrop))
			{
				//wand.NewImage(400, 200, new PixelWand("white"));
				//wand.OpenImage(TestImageBackdrop); 
				using (var draw = new DrawingWand())
				{
					using (PixelWand pixel = new PixelWand())
					{

						pixel.Color = "red";
						draw.StrokeColor = pixel;
						pixel.Color = "black";
						pixel.Opacity = 0.3;
						draw.FillColor = pixel;
						draw.DrawCircle(400, 400, 300, 300);

						pixel.Color = "transparent";
						draw.StrokeColor = pixel;
						pixel.Color = "white";
						draw.FillColor = pixel;
						draw.Font = "Verdana";
						draw.FontSize = 120;
						draw.FontStyle = FontStyleType.NormalStyle;
						draw.TextAlignment = TextAlignType.LeftAlign;
						draw.FontWeight = FontWeightType.BoldStyle;
						draw.TextAntialias = true;
						draw.DrawAnnotation(10, 100, "Media Browser");




						draw.FillColor = pixel;
						wand.CurrentImage.DrawImage(draw);
					}

				}
				//Debug.WriteLine(wand.GetNumberImages());
				//wand.Image.TrimImage(10);
				wand.SaveImage(Path.Combine(SaveDirectory, "logo_extent.jpg"));

			}
		}
	}
}
