using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageMagickSharp.Extensions
{
    /// <summary> A media browser wand extension. </summary>
    public static class MediaBrowserWandExtension
    {
        /// <summary> A MagickWand extension method that draw text. </summary>
        /// <param name="wand"> The wand to act on. </param>
        /// <param name="text"> The text. </param>
        /// <param name="x"> The x coordinate. </param>
        /// <param name="y"> The y coordinate. </param>
        /// <param name="fontName"> Name of the font. </param>
        /// <param name="fontSize"> Size of the font. </param>
        /// <param name="fontColor"> The font color. </param>
        /// <param name="fontWeight"> The font weight. </param>
        internal static void DrawText(this ImageWand wand, string text, double x, double y, string fontName, double fontSize, PixelWand fontColor, FontWeightType fontWeight)
        {
            using (var draw = new DrawingWand())
            {
                using (fontColor)
                {
                    draw.FillColor = fontColor;
                    draw.Font = fontName;
                    draw.FontSize = fontSize;
                    draw.FontWeight = fontWeight;
                    draw.TextAntialias = true;
                    draw.DrawAnnotation(x, y, text);
                    wand.DrawImage(draw);
                }
            }
        }

        /// <summary> A MagickWand extension method that draw rectangle. </summary>
        /// <param name="wand"> The wand to act on. </param>
        /// <param name="x1"> The first x value. </param>
        /// <param name="y1"> The first y value. </param>
        /// <param name="x2"> The second x value. </param>
        /// <param name="y2"> The second y value. </param>
        /// <param name="strokeColor"> The stroke color. </param>
        /// <param name="fillcolor"> The fillcolor. </param>
        internal static void DrawRectangle(this ImageWand wand, double x1, double y1, double x2, double y2, PixelWand strokeColor, PixelWand fillcolor)
        {
            using (var draw = new DrawingWand())
            {
                draw.StrokeColor = strokeColor;
                draw.FillColor = fillcolor;
                draw.DrawRectangle(x1, y1, x2, y2);
                wand.DrawImage(draw);
            }
        }

        /// <summary> A MagickWand extension method that draw round rectangle. </summary>
        /// <param name="wand"> The wand to act on. </param>
        /// <param name="x1"> The first x value. </param>
        /// <param name="y1"> The first y value. </param>
        /// <param name="x2"> The second x value. </param>
        /// <param name="y2"> The second y value. </param>
        /// <param name="rx"> The receive. </param>
        /// <param name="ry"> The ry. </param>
        /// <param name="strokeColor"> The stroke color. </param>
        /// <param name="fillcolor"> The fillcolor. </param>
        internal static void DrawRoundRectangle(this ImageWand wand, double x1, double y1, double x2, double y2, double rx, double ry, PixelWand strokeColor, PixelWand fillcolor)
        {
            using (var draw = new DrawingWand())
            {
                draw.StrokeColor = strokeColor;
                draw.FillColor = fillcolor;
                draw.DrawRoundRectangle(x1, y1, x2, y2, rx, ry);
                wand.DrawImage(draw);
            }
        }

        /// <summary> A MagickWand extension method that draw circle. </summary>
        /// <param name="wand"> The wand to act on. </param>
        /// <param name="ox"> The ox. </param>
        /// <param name="oy"> The oy. </param>
        /// <param name="px"> The px. </param>
        /// <param name="py"> The py. </param>
        /// <param name="strokeColor"> The stroke color. </param>
        /// <param name="fillcolor"> The fillcolor. </param>
        internal static void DrawCircle(this ImageWand wand, double ox, double oy, double px, double py, PixelWand strokeColor, PixelWand fillcolor)
        {
            using (var draw = new DrawingWand())
            {
                draw.StrokeColor = strokeColor;
                draw.FillColor = fillcolor;
                draw.DrawCircle(ox, oy, px, py);
                wand.DrawImage(draw);
            }
        }

        /// <summary> A MagickWand extension method that draw circle. </summary>
        /// <param name="wand"> The wand to act on. </param>
        /// <param name="x"> The x coordinate. </param>
        /// <param name="y"> The y coordinate. </param>
        /// <param name="p"> The double to process. </param>
        /// <param name="strokeColor"> The stroke color. </param>
        /// <param name="fillcolor"> The fillcolor. </param>
        internal static void DrawCircle(this ImageWand wand, double x, double y, double p, PixelWand strokeColor, PixelWand fillcolor)
        {
            using (var draw = new DrawingWand())
            {
                draw.StrokeColor = strokeColor;
                draw.FillColor = fillcolor;
                draw.DrawCircle(x + p, y + p, x + p, y + p * 2);
                wand.DrawImage(draw);
            }
        }

        /// <summary> A MagickWand extension method that overlay image. </summary>
        /// <param name="wand"> The wand to act on. </param>
        /// <param name="compose"> The compose. </param>
        /// <param name="x"> The x coordinate. </param>
        /// <param name="y"> The y coordinate. </param>
        /// <param name="width"> The width. </param>
        /// <param name="height"> The height. </param>
        /// <param name="magickwand"> The magickwand. </param>
        internal static void OverlayImage(this ImageWand wand, CompositeOperator compose, double x, double y, double width, double height, MagickWand magickwand)
        {
            using (var draw = new DrawingWand())
            {
                draw.DrawComposite(compose, x, y, width, height, magickwand.CurrentImage);
                wand.DrawImage(draw);
            }
        }

        /// <summary> Round corners. </summary>
        /// <param name="wand"> The wand to act on. </param>
        /// <param name="cofactor"> The cofactor. </param>
        /// <returns> A MagickWand. </returns>
        public static MagickWand RoundCorners(this MagickWand wand, Double cofactor)
        {
            var currentWidth = wand.CurrentImage.Width;
            var currentHeight = wand.CurrentImage.Height;

            var newWand = new MagickWand(currentWidth, currentHeight, new PixelWand(ColorName.None, 1));
            
            using (var whitePixelWand = new PixelWand(ColorName.White))
            using (var draw = new DrawingWand(whitePixelWand))
            {
                draw.DrawRoundRectangle(0, 0, currentWidth, currentHeight, cofactor, cofactor);
                newWand.CurrentImage.DrawImage(draw);
                newWand.CurrentImage.CompositeImage(wand, CompositeOperator.SrcInCompositeOp, 0, 0);
                return newWand;
            }
        }

        /// <summary>
        /// Media browser collection image.
        /// </summary>
        /// <param name="wandImages">The wand images.</param>
        /// <returns>A MagickWand.</returns>
        internal static MagickWand MediaBrowserCollectionImage(MagickWand wandImages)
        {
            int width = 1920;
            int height = 1080;

            var wand = new MagickWand(width, height);
            wand.OpenImage("gradient:#000000-#202020");
            using (var draw = new DrawingWand())
            {
                var iSlice = Convert.ToInt32(width * .1166666667);
                int iTrans = Convert.ToInt32(height * .25);
                int iHeight = Convert.ToInt32(height * .62);
                var horizontalImagePadding = Convert.ToInt32(width * 0.0125);

                foreach (var element in wandImages.ImageList)
                {
                    using (var blackPixelWand = new PixelWand(ColorName.Black))
                    {
                        int iWidth = (int)Math.Abs(iHeight * element.Width / element.Height);
                        element.Gravity = GravityType.CenterGravity;
                        element.BackgroundColor = blackPixelWand;
                        element.ResizeImage(iWidth, iHeight, FilterTypes.LanczosFilter);
                        int ix = (int)Math.Abs((iWidth - iSlice) / 2);
                        element.CropImage(iSlice, iHeight, ix, 0);

                        element.ExtentImage(iSlice, iHeight, 0 - horizontalImagePadding, 0);    
                    }
                    
                }

                wandImages.SetFirstIterator();
                using (var wandList = wandImages.AppendImages())
                {
                    wandList.CurrentImage.TrimImage(1);
                    using (var mwr = wandList.CloneMagickWand())
                    using (var blackPixelWand = new PixelWand(ColorName.Black))
                    using (var greyPixelWand = new PixelWand(ColorName.Grey70))
                    {
                        mwr.CurrentImage.ResizeImage(wandList.CurrentImage.Width, (wandList.CurrentImage.Height / 2), FilterTypes.LanczosFilter, 1);
                        mwr.CurrentImage.FlipImage();

                        mwr.CurrentImage.AlphaChannel = AlphaChannelType.DeactivateAlphaChannel;
                        mwr.CurrentImage.ColorizeImage(blackPixelWand, greyPixelWand);

                        using (var mwg = new MagickWand(wandList.CurrentImage.Width, iTrans))
                        {
                            mwg.OpenImage("gradient:black-none");
                            var verticalSpacing = Convert.ToInt32(height * 0.01111111111111111111111111111111);
                            mwr.CurrentImage.CompositeImage(mwg, CompositeOperator.CopyOpacityCompositeOp, 0, verticalSpacing);

                            wandList.AddImage(mwr);
                            int ex = (int)(wand.CurrentImage.Width - mwg.CurrentImage.Width) / 2;
                            wand.CurrentImage.CompositeImage(wandList.AppendImages(true), CompositeOperator.AtopCompositeOp, ex, Convert.ToInt32(height * .08));
                        }
                    }
                }
            }

            return wand;

        }

        internal static MagickWand MediaBrowserPosterCollectionImage(MagickWand wandImages)
        {
            int width = 600;
            int height = 900;

            var wand = new MagickWand(width, height);
            wand.OpenImage("gradient:#000000-#202020");
            using (var draw = new DrawingWand())
            {
                var iSlice = Convert.ToInt32(width * 0.225);
                int iTrans = Convert.ToInt32(height * .25);
                int iHeight = Convert.ToInt32(height * .65);
                var horizontalImagePadding = Convert.ToInt32(width * 0.0275);

                foreach (var element in wandImages.ImageList)
                {
                    using (var blackPixelWand = new PixelWand(ColorName.Black))
                    {
                        int iWidth = (int)Math.Abs(iHeight * element.Width / element.Height);
                        element.Gravity = GravityType.CenterGravity;
                        element.BackgroundColor = blackPixelWand;
                        element.ResizeImage(iWidth, iHeight, FilterTypes.LanczosFilter);
                        int ix = (int)Math.Abs((iWidth - iSlice) / 2);
                        element.CropImage(iSlice, iHeight, ix, 0);

                        element.ExtentImage(iSlice, iHeight, 0 - horizontalImagePadding, 0);    
                    }                    
                }

                wandImages.SetFirstIterator();
                using (var wandList = wandImages.AppendImages())
                using (var blackPixelWand = new PixelWand(ColorName.Black))
                using (var greyPixelWand = new PixelWand(ColorName.Grey70))
                {
                    wandList.CurrentImage.TrimImage(1);
                    using (var mwr = wandList.CloneMagickWand())
                    {
                        mwr.CurrentImage.ResizeImage(wandList.CurrentImage.Width, (wandList.CurrentImage.Height / 2), FilterTypes.LanczosFilter, 1);
                        mwr.CurrentImage.FlipImage();

                        mwr.CurrentImage.AlphaChannel = AlphaChannelType.DeactivateAlphaChannel;
                        mwr.CurrentImage.ColorizeImage(blackPixelWand, greyPixelWand);

                        using (var mwg = new MagickWand(wandList.CurrentImage.Width, iTrans))
                        {
                            mwg.OpenImage("gradient:black-none");
                            var verticalSpacing = Convert.ToInt32(height * 0.01111111111111111111111111111111);
                            mwr.CurrentImage.CompositeImage(mwg, CompositeOperator.CopyOpacityCompositeOp, 0, verticalSpacing);

                            wandList.AddImage(mwr);
                            int ex = (int)(wand.CurrentImage.Width - mwg.CurrentImage.Width) / 2;
                            wand.CurrentImage.CompositeImage(wandList.AppendImages(true), CompositeOperator.AtopCompositeOp, ex, Convert.ToInt32(height * .05));
                        }
                    }
                }
            }

            return wand;

        }

        internal static MagickWand MediaBrowserPosterCollectionImageWithText(MagickWand wandImages, string label, string font)
        {
            int width = 600;
            int height = 900;

            var wand = new MagickWand(width, height);
            wand.OpenImage("gradient:#111111-#111111");
            using (var draw = new DrawingWand())
            {
                using (var fcolor = new PixelWand(ColorName.White))
                {
                    draw.FillColor = fcolor;
                    draw.Font = font;
                    draw.FontSize = 60;
                    draw.FontWeight = FontWeightType.LightStyle;
                    draw.TextAntialias = true;
                }

                var fontMetrics = wand.QueryFontMetrics(draw, label);
                var textContainerY = Convert.ToInt32(height * .145);
                wand.CurrentImage.AnnotateImage(draw, (width - fontMetrics.TextWidth) / 2, textContainerY, 0.0, label);

                var iSlice = Convert.ToInt32(width * 0.225);
                int iTrans = Convert.ToInt32(height * 0.2);
                int iHeight = Convert.ToInt32(height * 0.48296296296296296296296296296296);
                var horizontalImagePadding = Convert.ToInt32(width * 0.0275);

                foreach (var element in wandImages.ImageList)
                {
                    using (var nonePixelWand = new PixelWand("none", 1))
                    {
                        int iWidth = (int)Math.Abs(iHeight * element.Width / element.Height);
                        element.Gravity = GravityType.CenterGravity;
                        element.BackgroundColor = nonePixelWand;
                        element.ResizeImage(iWidth, iHeight, FilterTypes.LanczosFilter);
                        int ix = (int)Math.Abs((iWidth - iSlice) / 2);
                        element.CropImage(iSlice, iHeight, ix, 0);

                        element.ExtentImage(iSlice, iHeight, 0 - horizontalImagePadding, 0);    
                    }                    
                }

                wandImages.SetFirstIterator();
                using (var wandList = wandImages.AppendImages())
                {
                    wandList.CurrentImage.TrimImage(1);
                    using (var mwr = wandList.CloneMagickWand())
                    using (var blackPixelWand = new PixelWand(ColorName.Black))
                    using (var greyPixelWand = new PixelWand(ColorName.Grey60))
                    {
                        mwr.CurrentImage.ResizeImage(wandList.CurrentImage.Width, (wandList.CurrentImage.Height / 2), FilterTypes.LanczosFilter, 1);
                        mwr.CurrentImage.FlipImage();

                        mwr.CurrentImage.AlphaChannel = AlphaChannelType.DeactivateAlphaChannel;
                        mwr.CurrentImage.ColorizeImage(blackPixelWand, greyPixelWand);

                        using (var mwg = new MagickWand(wandList.CurrentImage.Width, iTrans))
                        {
                            mwg.OpenImage("gradient:black-none");
                            var verticalSpacing = Convert.ToInt32(height * 0.01111111111111111111111111111111);
                            mwr.CurrentImage.CompositeImage(mwg, CompositeOperator.DstInCompositeOp, 0, verticalSpacing);

                            wandList.AddImage(mwr);
                            int ex = (int)(wand.CurrentImage.Width - mwg.CurrentImage.Width) / 2;
                            wand.CurrentImage.CompositeImage(wandList.AppendImages(true), CompositeOperator.AtopCompositeOp, ex, Convert.ToInt32(height * 0.24851851851851851851851851851852));
                        }
                    }
                }
            }

            return wand;

        }

        internal static MagickWand MediaBrowserSquareCollectionImage(MagickWand wandImages)
        {
            int width = 540;
            int height = 540;

            var wand = new MagickWand(width, height);
            wand.OpenImage("gradient:#000000-#202020");
            using (var draw = new DrawingWand())
            {
                var iSlice = Convert.ToInt32(width * .225);
                int iTrans = Convert.ToInt32(height * .25);
                int iHeight = Convert.ToInt32(height * .63);
                var horizontalImagePadding = Convert.ToInt32(width * 0.02);

                foreach (var element in wandImages.ImageList)
                {
                    using (var blackPixelWand = new PixelWand(ColorName.Black))
                    {
                        int iWidth = (int)Math.Abs(iHeight * element.Width / element.Height);
                        element.Gravity = GravityType.CenterGravity;
                        element.BackgroundColor = blackPixelWand;
                        element.ResizeImage(iWidth, iHeight, FilterTypes.LanczosFilter);
                        int ix = (int)Math.Abs((iWidth - iSlice) / 2);
                        element.CropImage(iSlice, iHeight, ix, 0);

                        element.ExtentImage(iSlice, iHeight, 0 - horizontalImagePadding, 0);    
                    }                    
                }

                wandImages.SetFirstIterator();
                using (var wandList = wandImages.AppendImages())
                {
                    wandList.CurrentImage.TrimImage(1);
                    using (var mwr = wandList.CloneMagickWand())
                    using (var blackPixelWand = new PixelWand(ColorName.Black))
                    using (var greyPixelWand = new PixelWand(ColorName.Grey70))
                    {
                        mwr.CurrentImage.ResizeImage(wandList.CurrentImage.Width, (wandList.CurrentImage.Height / 2), FilterTypes.LanczosFilter, 1);
                        mwr.CurrentImage.FlipImage();

                        mwr.CurrentImage.AlphaChannel = AlphaChannelType.DeactivateAlphaChannel;
                        mwr.CurrentImage.ColorizeImage(blackPixelWand, greyPixelWand);

                        using (var mwg = new MagickWand(wandList.CurrentImage.Width, iTrans))
                        {
                            mwg.OpenImage("gradient:black-none");
                            var verticalSpacing = Convert.ToInt32(height * 0.01111111111111111111111111111111);
                            mwr.CurrentImage.CompositeImage(mwg, CompositeOperator.CopyOpacityCompositeOp, 0, verticalSpacing);

                            wandList.AddImage(mwr);
                            int ex = (int)(wand.CurrentImage.Width - mwg.CurrentImage.Width) / 2;
                            wand.CurrentImage.CompositeImage(wandList.AppendImages(true), CompositeOperator.AtopCompositeOp, ex, Convert.ToInt32(height * .07));
                        }
                    }
                }
            }

            return wand;

        }

        internal static MagickWand MediaBrowserSquareCollectionImageWithText(MagickWand wandImages, string label, string font)
        {
            int width = 540;
            int height = 540;

            var wand = new MagickWand(width, height);
            wand.OpenImage("gradient:#111111-#111111");
            using (var draw = new DrawingWand())
            {
                using (var fcolor = new PixelWand(ColorName.White))
                {
                    draw.FillColor = fcolor;
                    draw.Font = font;
                    draw.FontSize = 50;
                    draw.FontWeight = FontWeightType.LightStyle;
                    draw.TextAntialias = true;
                }

                var fontMetrics = wand.QueryFontMetrics(draw, label);
                var textContainerY = Convert.ToInt32(height * .165);
                wand.CurrentImage.AnnotateImage(draw, (width - fontMetrics.TextWidth) / 2, textContainerY, 0.0, label);

                var iSlice = Convert.ToInt32(width * .225);
                int iTrans = Convert.ToInt32(height * 0.2);
                int iHeight = Convert.ToInt32(height * 0.46296296296296296296296296296296);
                var horizontalImagePadding = Convert.ToInt32(width * 0.02);

                foreach (var element in wandImages.ImageList)
                {
                    using (var nonePixelWand = new PixelWand("none", 1))
                    {
                        int iWidth = (int)Math.Abs(iHeight * element.Width / element.Height);
                        element.Gravity = GravityType.CenterGravity;
                        element.BackgroundColor = nonePixelWand;
                        element.ResizeImage(iWidth, iHeight, FilterTypes.LanczosFilter);
                        int ix = (int)Math.Abs((iWidth - iSlice) / 2);
                        element.CropImage(iSlice, iHeight, ix, 0);

                        element.ExtentImage(iSlice, iHeight, 0 - horizontalImagePadding, 0);    
                    }                    
                }

                wandImages.SetFirstIterator();
                using (var wandList = wandImages.AppendImages())
                {
                    wandList.CurrentImage.TrimImage(1);
                    using (var mwr = wandList.CloneMagickWand())
                    using (var blackPixelWand = new PixelWand(ColorName.Black))
                    using (var greyPixelWand = new PixelWand(ColorName.Grey60))
                    {
                        mwr.CurrentImage.ResizeImage(wandList.CurrentImage.Width, (wandList.CurrentImage.Height / 2), FilterTypes.LanczosFilter, 1);
                        mwr.CurrentImage.FlipImage();

                        mwr.CurrentImage.AlphaChannel = AlphaChannelType.DeactivateAlphaChannel;
                        mwr.CurrentImage.ColorizeImage(blackPixelWand, greyPixelWand);

                        using (var mwg = new MagickWand(wandList.CurrentImage.Width, iTrans))
                        {
                            mwg.OpenImage("gradient:black-none");
                            var verticalSpacing = Convert.ToInt32(height * 0.01111111111111111111111111111111);
                            mwr.CurrentImage.CompositeImage(mwg, CompositeOperator.DstInCompositeOp, 0, verticalSpacing);

                            wandList.AddImage(mwr);
                            int ex = (int)(wand.CurrentImage.Width - mwg.CurrentImage.Width) / 2;
                            wand.CurrentImage.CompositeImage(wandList.AppendImages(true), CompositeOperator.AtopCompositeOp, ex, Convert.ToInt32(height * 0.26851851851851851851851851851852));
                        }
                    }
                }
            }

            return wand;

        }

        internal static MagickWand MediaBrowserCollectionImageWithText(MagickWand wandImages, string label, string font)
        {
            int width = 960;
            int height = 540;

            var wand = new MagickWand(width, height);
            wand.OpenImage("gradient:#111111-#111111");
            using (var draw = new DrawingWand())
            {
                using (var fcolor = new PixelWand(ColorName.White))
                {
                    draw.FillColor = fcolor;
                    draw.Font = font;
                    draw.FontSize = 50;
                    draw.FontWeight = FontWeightType.LightStyle;
                    draw.TextAntialias = true;
                }

                var fontMetrics = wand.QueryFontMetrics(draw, label);
                var textContainerY = Convert.ToInt32(height * .165);
                wand.CurrentImage.AnnotateImage(draw, (width - fontMetrics.TextWidth) / 2, textContainerY, 0.0, label);

                var iSlice = Convert.ToInt32(width * .1166666667);
                int iTrans = Convert.ToInt32(height * 0.2);
                int iHeight = Convert.ToInt32(height * 0.46296296296296296296296296296296);
                var horizontalImagePadding = Convert.ToInt32(width * 0.0125);

                foreach (var element in wandImages.ImageList)
                {
                    using (var nonePixelWand = new PixelWand("none", 1))
                    {
                        int iWidth = (int)Math.Abs(iHeight * element.Width / element.Height);
                        element.Gravity = GravityType.CenterGravity;
                        element.BackgroundColor = nonePixelWand;
                        element.ResizeImage(iWidth, iHeight, FilterTypes.LanczosFilter);
                        int ix = (int)Math.Abs((iWidth - iSlice) / 2);
                        element.CropImage(iSlice, iHeight, ix, 0);

                        element.ExtentImage(iSlice, iHeight, 0 - horizontalImagePadding, 0);    
                    }                    
                }

                wandImages.SetFirstIterator();
                using (var wandList = wandImages.AppendImages())
                {
                    wandList.CurrentImage.TrimImage(1);
                    using (var mwr = wandList.CloneMagickWand())
                    using (var blackPixelWand = new PixelWand(ColorName.Black))
                    using (var greyPixelWand = new PixelWand(ColorName.Grey60))
                    {
                        mwr.CurrentImage.ResizeImage(wandList.CurrentImage.Width, (wandList.CurrentImage.Height / 2), FilterTypes.LanczosFilter, 1);
                        mwr.CurrentImage.FlipImage();

                        mwr.CurrentImage.AlphaChannel = AlphaChannelType.DeactivateAlphaChannel;
                        mwr.CurrentImage.ColorizeImage(blackPixelWand, greyPixelWand);

                        using (var mwg = new MagickWand(wandList.CurrentImage.Width, iTrans))
                        {
                            mwg.OpenImage("gradient:black-none");
                            var verticalSpacing = Convert.ToInt32(height * 0.01111111111111111111111111111111);
                            mwr.CurrentImage.CompositeImage(mwg, CompositeOperator.DstInCompositeOp, 0, verticalSpacing);

                            wandList.AddImage(mwr);
                            int ex = (int)(wand.CurrentImage.Width - mwg.CurrentImage.Width) / 2;
                            wand.CurrentImage.CompositeImage(wandList.AppendImages(true), CompositeOperator.AtopCompositeOp, ex, Convert.ToInt32(height * 0.26851851851851851851851851851852));
                        }
                    }
                }
            }

            return wand;

        }

    }

}
