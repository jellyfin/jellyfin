using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageMagickSharp.Extensions
{
	internal static class CoverArtWandExtension
	{
		/// <summary> A MagickWand extension method that cover art stack. </summary>
		/// <param name="wand"> The wand. </param>
		/// <param name="xIncrement"> Amount to increment by. </param>
		/// <param name="yIncrement"> Amount to increment by. </param>
		/// <param name="width"> The width. </param>
		/// <param name="height"> The height. </param>
		/// <param name="images"> A variable-length parameters list containing images. </param>
		private static void CoverArtStack(this MagickWand wand, double xIncrement, double yIncrement, double width, double height, params string[] images)
		{
			using (var draw = new DrawingWand())
			{
				double x = 0;
				double y = 0;
				using (var wandimages = new MagickWand(images))                
				{
					foreach (ImageWand imageWand in wandimages.ImageList)
					{
					    using (var blackPixelWand = new PixelWand("black"))
					    {
                            imageWand.BorderImage(blackPixelWand, 2, 2);
                            draw.DrawComposite(CompositeOperator.AtopCompositeOp, x, y, width, height, imageWand);
                            x += xIncrement;
                            y += yIncrement;    
					    }						
					}
				}
				wand.CurrentImage.DrawImage(draw);
			}
		}

	}
}
