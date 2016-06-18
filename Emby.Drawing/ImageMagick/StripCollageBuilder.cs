using ImageMagickSharp;
using MediaBrowser.Common.Configuration;
using System;
using System.Collections.Generic;
using CommonIO;

namespace Emby.Drawing.ImageMagick
{
    public class StripCollageBuilder
    {
        private readonly IApplicationPaths _appPaths;
        private readonly IFileSystem _fileSystem;

        public StripCollageBuilder(IApplicationPaths appPaths, IFileSystem fileSystem)
        {
            _appPaths = appPaths;
            _fileSystem = fileSystem;
        }

        public void BuildPosterCollage(List<string> paths, string outputPath, int width, int height)
        {
            using (var wand = BuildPosterCollageWand(paths, width, height))
            {
                wand.SaveImage(outputPath);
            }
        }

        public void BuildSquareCollage(List<string> paths, string outputPath, int width, int height)
        {
            using (var wand = BuildSquareCollageWand(paths, width, height))
            {
                wand.SaveImage(outputPath);
            }
        }

        public void BuildThumbCollage(List<string> paths, string outputPath, int width, int height)
        {
            using (var wand = BuildThumbCollageWand(paths, width, height))
            {
                wand.SaveImage(outputPath);
            }
        }

        private MagickWand BuildPosterCollageWand(List<string> paths, int width, int height)
        {
            var inputPaths = ImageHelpers.ProjectPaths(paths, 3);
            using (var wandImages = new MagickWand(inputPaths.ToArray()))
            {
                var wand = new MagickWand(width, height);
                wand.OpenImage("gradient:#111111-#111111");
                using (var draw = new DrawingWand())
                {
                    var iSlice = Convert.ToInt32(width * 0.3);
                    int iTrans = Convert.ToInt32(height * .25);
                    int iHeight = Convert.ToInt32(height * .65);
                    var horizontalImagePadding = Convert.ToInt32(width * 0.0366);

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
                        {
                            using (var blackPixelWand = new PixelWand(ColorName.Black))
                            {
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
                                        wand.CurrentImage.CompositeImage(wandList.AppendImages(true), CompositeOperator.AtopCompositeOp, ex, Convert.ToInt32(height * .05));
                                    }
                                }
                            }
                        }
                    }
                }

                return wand;
            }
        }

        private MagickWand BuildThumbCollageWand(List<string> paths, int width, int height)
        {
            var inputPaths = ImageHelpers.ProjectPaths(paths, 4);
            using (var wandImages = new MagickWand(inputPaths.ToArray()))
            {
                var wand = new MagickWand(width, height);
                wand.OpenImage("gradient:#111111-#111111");
                using (var draw = new DrawingWand())
                {
                    var iSlice = Convert.ToInt32(width * 0.24125);
                    int iTrans = Convert.ToInt32(height * .25);
                    int iHeight = Convert.ToInt32(height * .70);
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
                        {
                            using (var blackPixelWand = new PixelWand(ColorName.Black))
                            {
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
                                        wand.CurrentImage.CompositeImage(wandList.AppendImages(true), CompositeOperator.AtopCompositeOp, ex, Convert.ToInt32(height * .045));
                                    }
                                }
                            }
                        }
                    }
                }

                return wand;
            }
        }

        private MagickWand BuildSquareCollageWand(List<string> paths, int width, int height)
        {
            var inputPaths = ImageHelpers.ProjectPaths(paths, 4);
            var outputWand = new MagickWand(width, height, new PixelWand("none", 1));
            var imageIndex = 0;
            var cellWidth = width/2;
            var cellHeight = height/2;
            for (var x = 0; x < 2; x++)
            {
                for (var y = 0; y < 2; y++)
                {
                    using (var temp = new MagickWand(inputPaths[imageIndex]))
                    {
                        temp.CurrentImage.ScaleImage(cellWidth, cellHeight);
                        // draw this image into the strip at the next position
                        var xPos = x*cellWidth;
                        var yPos = y*cellHeight;
                        outputWand.CurrentImage.CompositeImage(temp, CompositeOperator.OverCompositeOp, xPos, yPos);
                    }
                    imageIndex++;
                }
            }

            return outputWand;
        }
    }
}
