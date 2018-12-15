using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ImageMagickSharp.InteropMarshaler;

namespace ImageMagickSharp
{
    internal static class ImageWandInterop
    {
        #region [Image Wand]

        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        internal static extern bool MagickStripImage(IntPtr wand);

        internal static bool MagickThumbnailImage(IntPtr wand, int columns, int rows, bool bestFit, bool fill)
        {
            return MagickThumbnailImage(wand, (IntPtr)columns, (IntPtr)rows, bestFit, fill);
        }

        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        private static extern bool MagickThumbnailImage(IntPtr wand, IntPtr columns, IntPtr rows, bool bestFit, bool fill);

        internal static bool MagickScaleImage(IntPtr wand, int columns, int rows)
        {
            return MagickScaleImage(wand, (IntPtr)columns, (IntPtr)rows);
        }

        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        private static extern bool MagickScaleImage(IntPtr wand, IntPtr columns, IntPtr rows);
        
        /// <summary> Magick resize image.</summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <param name="columns"> The columns. </param>
        /// <param name="rows"> The rows. </param>
        /// <param name="filterType"> Type of the filter. </param>
        /// <param name="blur"> The blur. </param>
        /// <returns> An int. </returns>
        internal static bool MagickResizeImage(IntPtr wand, int columns, int rows, int filterType, double blur)
        {
            return MagickResizeImage(wand, (IntPtr) columns, (IntPtr) rows, filterType, blur);
        }

        /// WandExport MagickBooleanType MagickResizeImage(MagickWand *wand,
        /// const size_t columns,const size_t rows,const FilterTypes filter,
        /// const double blur)
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        private static extern bool MagickResizeImage(IntPtr wand, IntPtr columns, IntPtr rows, int filterType, double blur);

        /// <summary> Magick crop image.</summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <param name="width"> The width. </param>
        /// <param name="height"> The height. </param>
        /// <param name="x"> The x coordinate. </param>
        /// <param name="y"> The y coordinate. </param>
        /// <returns> An int. </returns>
        internal static bool MagickCropImage(IntPtr wand, int width, int height, int x, int y)
        {
            return MagickCropImage(wand, (IntPtr)width, (IntPtr)height, (IntPtr)x, (IntPtr)y);
        }

        /// WandExport MagickBooleanType MagickCropImage(MagickWand *wand, 
        /// const size_t width,const size_t height,const ssize_t x,const ssize_t y)
        
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        private static extern bool MagickCropImage(IntPtr wand, IntPtr width, IntPtr height, IntPtr x, IntPtr y);

        /// <summary> Magick get image compression quality. </summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <returns> An int. </returns>
        internal static int MagickGetImageCompressionQuality(IntPtr wand)
        {
            return (int) MagickGetImageCompressionQualityInternal(wand);
        }

        /// WandExport size_t MagickGetImageCompressionQuality(MagickWand *wand)
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention, EntryPoint = "MagickGetImageCompressionQuality")]
        private static extern IntPtr MagickGetImageCompressionQualityInternal(IntPtr wand);

        /// <summary> Magick set image compression quality. </summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <param name="quality"> The quality. </param>
        /// <returns> An int. </returns>
        /// 
        internal static int MagickSetImageCompressionQuality(IntPtr wand, int quality)
        {
            return MagickSetImageCompressionQualityInternal(wand, (IntPtr)quality);
        }

        /// WandExport MagickBooleanType MagickSetImageCompressionQuality(MagickWand *wand, const size_t quality)
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention, EntryPoint = "MagickSetImageCompressionQuality")]
        private static extern int MagickSetImageCompressionQualityInternal(IntPtr wand, IntPtr quality);

        /// <summary> Magick gaussian blur image. </summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <param name="radius"> The radius. </param>
        /// <param name="sigma"> The sigma. </param>
        /// <returns> An int. </returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        private static extern int MagickGaussianBlurImage(IntPtr wand, double radius, double sigma);

		/// <summary> Magick adaptive blur image. </summary>
		/// <param name="wand"> Handle of the wand. </param>
		/// <param name="radius"> The radius. </param>
		/// <param name="sigma"> The sigma. </param>
		/// <returns> An int. </returns>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		private static extern int MagickAdaptiveBlurImage(IntPtr wand, double radius, double sigma);

        /// <summary> Magick unsharp mask image. </summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <param name="radius"> The radius. </param>
        /// <param name="sigma"> The sigma. </param>
        /// <param name="amount"> The amount. </param>
        /// <param name="threshold"> The threshold. </param>
        /// <returns> An int. </returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        private static extern int MagickUnsharpMaskImage(IntPtr wand, double radius, double sigma, double amount, double threshold);

        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        private static extern bool MagickSetImageFilename(IntPtr wand, string filename);

        /// <summary> Magick get image filename. </summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <returns> An IntPtr. </returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        private static extern IntPtr MagickGetImageFilename(IntPtr wand);

        /// <summary> Magick get image format. </summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <returns> An IntPtr. </returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        private static extern IntPtr MagickGetImageFormat(IntPtr wand);

        /// <summary> Magick set image format. </summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <param name="format"> Describes the format to use. </param>
        /// <returns> An int. </returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        private static extern int MagickSetImageFormat(IntPtr wand, IntPtr format);

        /// <summary> Magick get image BLOB. </summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <param name="length"> The length. </param>
        /// <returns> An IntPtr. </returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        private static extern IntPtr MagickGetImageBlob(IntPtr wand, out int length);

        /// <summary> Magick read image BLOB. </summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <param name="blob"> The BLOB. </param>
        /// <param name="length"> The length. </param>
        /// <returns> An int. </returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        private static extern int MagickReadImageBlob(IntPtr wand, IntPtr blob, int length);

        /// <summary> Magick reset image page. </summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <param name="page"> The page. </param>
        /// <returns> An int. </returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        private static extern int MagickResetImagePage(IntPtr wand, IntPtr page);

        /// <summary> Magick composite image. </summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <param name="sourcePtr"> Source pointer. </param>
        /// <param name="compositeOperator"> The composite operator. </param>
        /// <param name="x"> The x coordinate. </param>
        /// <param name="y"> The y coordinate. </param>
        /// <returns> An int. </returns>
        internal static bool MagickCompositeImage(IntPtr wand, IntPtr sourcePtr, CompositeOperator compositeOperator,
            int x, int y)
        {
            return MagickCompositeImageInternal(wand, sourcePtr, compositeOperator, (IntPtr)x, (IntPtr)y);
        }

        /// WandExport MagickBooleanType MagickCompositeImage(MagickWand *wand,const MagickWand *source_wand,
        /// const CompositeOperator compose,const ssize_t x,const ssize_t y)
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention, EntryPoint = "MagickCompositeImage")]
        private static extern bool MagickCompositeImageInternal(IntPtr wand, IntPtr sourcePtr, CompositeOperator compositeOperator, IntPtr x, IntPtr y);

        /// <summary> Magick rotate image. 
        /// 
        /// WandExport MagickBooleanType MagickRotateImage(MagickWand *wand,const PixelWand *background,const double degrees)
        /// 
        /// </summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <param name="background"> The background. </param>
        /// <param name="degrees"> The degrees. </param>
        /// <returns> An int. </returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        internal static extern bool MagickRotateImage(IntPtr wand, IntPtr background, double degrees);

        /// <summary> Magick transparent paint image. </summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <param name="target"> Target for the. </param>
        /// <param name="alpha"> The alpha. </param>
        /// <param name="fuzz"> The fuzz. </param>
        /// <param name="invert"> The invert. </param>
        /// <returns> An int. </returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        private static extern int MagickTransparentPaintImage(IntPtr wand, IntPtr target, double alpha, double fuzz, int invert);

        /// <summary> Magick opaque paint image. </summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <param name="target"> Target for the. </param>
        /// <param name="fill"> The fill. </param>
        /// <param name="fuzz"> The fuzz. </param>
        /// <param name="invert"> The invert. </param>
        /// <returns> An int. </returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        private static extern int MagickOpaquePaintImage(IntPtr wand, IntPtr target, IntPtr fill, double fuzz, int invert);

        /// <summary> Magick threshold image. </summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <param name="threshold"> The threshold. </param>
        /// <returns> An int. </returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        private static extern int MagickThresholdImage(IntPtr wand, double threshold);

        /// <summary> Magick adaptive threshold image. </summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <param name="width"> The width. </param>
        /// <param name="height"> The height. </param>
        /// <param name="bias"> The bias. </param>
        /// <returns> An int. </returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        private static extern int MagickAdaptiveThresholdImage(IntPtr wand, int width, int height, double bias);

        /// <summary> Magick transform image. </summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <param name="crop"> A crop geometry string. This geometry defines a subregion of the image to crop. </param>
        /// <param name="geomety"> An image geometry string. This geometry defines the final size of the image. </param>
        /// <returns> An int. </returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        private static extern IntPtr MagickTransformImage(IntPtr wand, string crop, string geomety);

		/// <summary> Magick color matrix image. </summary>
		/// <param name="wand"> Handle of the wand. </param>
		/// <param name="color_matrix"> The color matrix. </param>
		/// <returns> true if it succeeds, false if it fails. </returns>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		private static extern bool MagickColorMatrixImage(IntPtr wand,  double[,] color_matrix);

        /// <summary> Magick transform image colorspace. </summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <param name="colorspace"> The colorspace. </param>
        /// <returns> true if it succeeds, false if it fails. </returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        private static extern bool MagickTransformImageColorspace(IntPtr wand, ImageColorspaceType colorspace);

        /// <summary> Magick transpose image. </summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <returns> true if it succeeds, false if it fails. </returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        private static extern bool MagickTransposeImage(IntPtr wand);

        /// <summary> Magick transverse image. </summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <returns> true if it succeeds, false if it fails. </returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        private static extern bool MagickTransverseImage(IntPtr wand);

        /// <summary> Magick quantize image. </summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <param name="number_colors"> Number of colors. </param>
        /// <param name="colorsapceType"> Type of the colorsapce. </param>
        /// <param name="treedepth"> The treedepth. </param>
        /// <param name="dither_method"> The dither method. </param>
        /// <param name="measure_error"> The measure error. </param>
        /// <returns> An int. </returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        private static extern int MagickQuantizeImage(IntPtr wand, int number_colors, int colorsapceType, int treedepth, int dither_method, int measure_error);

        /// <summary> Magick normalize image. </summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <returns> An int. </returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        private static extern int MagickNormalizeImage(IntPtr wand);

        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        internal static extern bool MagickAutoOrientImage(IntPtr wand);
        
        /// <summary> Magick get image width. </summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <returns> An int. </returns>
        internal static int MagickGetImageWidth(IntPtr wand)
        {
            return (int)MagickGetImageWidthInternal(wand);
        }

        /// WandExport size_t MagickGetImageWidth(MagickWand *wand)
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention, EntryPoint = "MagickGetImageWidth")]
        private static extern IntPtr MagickGetImageWidthInternal(IntPtr wand);

        /// <summary> Magick get image height. </summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <returns> An int. </returns>
        internal static int MagickGetImageHeight(IntPtr wand)
        {
            return (int)MagickGetImageHeightInternal(wand);
        }

        /// WandExport size_t MagickGetImageHeight(MagickWand *wand)
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention, EntryPoint = "MagickGetImageHeight")]
        private static extern IntPtr MagickGetImageHeightInternal(IntPtr wand);

        /// <summary> Magick set image matte. </summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <param name="matte"> true to matte. </param>
        /// <returns> true if it succeeds, false if it fails. </returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        private static extern bool MagickSetImageMatte(IntPtr wand, bool matte);

        /// <summary> Magick set image matte color. </summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <param name="matteColor"> The matte color. </param>
        /// <returns> true if it succeeds, false if it fails. </returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        private static extern bool MagickSetImageMatteColor(IntPtr wand, IntPtr matteColor);

        /// <summary> Magick get image matte color. </summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <param name="matteColor"> The matte color. </param>
        /// <returns> true if it succeeds, false if it fails. </returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        private static extern bool MagickGetImageMatteColor(IntPtr wand, out IntPtr matteColor);

        /// <summary> Magick get image background color. </summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <param name="backgroundcolor"> The backgroundcolor. </param>
        /// <returns> true if it succeeds, false if it fails. </returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        private static extern bool MagickGetImageBackgroundColor(IntPtr wand, out IntPtr backgroundcolor);

        /// <summary> Magick set image background color. 
        /// 
        /// WandExport MagickBooleanType MagickSetImageBackgroundColor(MagickWand *wand,const PixelWand *background)
        /// 
        /// </summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <param name="backgroundcolor"> The backgroundcolor. </param>
        /// <returns> true if it succeeds, false if it fails. </returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        internal static extern bool MagickSetImageBackgroundColor(IntPtr wand, IntPtr backgroundcolor);

        /// <summary> Magick flip image. 
        /// 
        /// WandExport MagickBooleanType MagickFlipImage(MagickWand *wand)
        /// 
        /// </summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <returns> true if it succeeds, false if it fails. </returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        internal static extern bool MagickFlipImage(IntPtr wand);

        /// <summary> Magick flop image. </summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <returns> true if it succeeds, false if it fails. </returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        private static extern bool MagickFlopImage(IntPtr wand);

        /// <summary> Magick border image.</summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <param name="bordercolor"> The bordercolor. </param>
        /// <param name="width"> The width. </param>
        /// <param name="height"> The height. </param>
        /// <returns> true if it succeeds, false if it fails. </returns>
        internal static bool MagickBorderImage(IntPtr wand, IntPtr bordercolor, int width, int height)
        {
            return MagickBorderImage(wand, bordercolor, (IntPtr) width, (IntPtr) height);
        }

        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        private static extern bool MagickBorderImage(IntPtr wand, IntPtr bordercolor, IntPtr width, IntPtr height);

        /// <summary> Magick extent image. </summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <param name="width"> The width. </param>
        /// <param name="height"> The height. </param>
        /// <param name="x"> The x coordinate. </param>
        /// <param name="y"> The y coordinate. </param>
        /// <returns> true if it succeeds, false if it fails. </returns>
        internal static bool MagickExtentImage(IntPtr wand, int width, int height, int x, int y)
        {
            return MagickExtentImage(wand, (IntPtr) width, (IntPtr) height, (IntPtr) x, (IntPtr) y);
        } 

        /// WandExport MagickBooleanType MagickExtentImage(MagickWand *wand, const size_t width,
        /// const size_t height,const ssize_t x,const ssize_t y)
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        private static extern bool MagickExtentImage(IntPtr wand, IntPtr width, IntPtr height, IntPtr x, IntPtr y);

        /// <summary> Magick get image region. </summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <param name="width"> The width. </param>
        /// <param name="height"> The height. </param>
        /// <param name="x"> The x coordinate. </param>
        /// <param name="y"> The y coordinate. </param>
        /// <returns> An IntPtr. </returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        private static extern IntPtr MagickGetImageRegion(IntPtr wand, int width, int height, int x, int y);

        /// <summary> Magick set image extent. </summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <param name="columns"> The columns. </param>
        /// <param name="rows"> The rows. </param>
        /// <returns> true if it succeeds, false if it fails. </returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        private static extern bool MagickSetImageExtent(IntPtr wand, int columns, int rows);

		/// <summary> Magick set image extent. </summary>
		/// <param name="wand"> Handle of the wand. </param>
		/// <param name="columns"> The columns. </param>
		/// <param name="rows"> The rows. </param>
		/// <returns> true if it succeeds, false if it fails. </returns>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		private static extern bool MagickSetImageExtent(IntPtr wand, double columns, double rows);

        /// <summary> Magick trim image. 
        /// 
        /// WandExport MagickBooleanType MagickTrimImage(MagickWand *wand,const double fuzz)
        /// 
        /// </summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <param name="fuzz"> The fuzz. </param>
        /// <returns> true if it succeeds, false if it fails. </returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        internal static extern bool MagickTrimImage(IntPtr wand, double fuzz);

        /// <summary> Magick label image. </summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <param name="label"> The label. </param>
        /// <returns> true if it succeeds, false if it fails. </returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        private static extern bool MagickLabelImage(IntPtr wand, string label);

        /// <summary> Magick get image border color. </summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <param name="border_color"> The border color. </param>
        /// <returns> true if it succeeds, false if it fails. </returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        private static extern bool MagickGetImageBorderColor(IntPtr wand, out IntPtr border_color);

        /// <summary> Magick set image border color. </summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <param name="border_color"> The border color. </param>
        /// <returns> true if it succeeds, false if it fails. </returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        private static extern bool MagickSetImageBorderColor(IntPtr wand, IntPtr border_color);

        /// <summary> Magick raise image. </summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <param name="width"> The width. </param>
        /// <param name="height"> The height. </param>
        /// <param name="x"> The x coordinate. </param>
        /// <param name="y"> The y coordinate. </param>
        /// <param name="raise"> true to raise. </param>
        /// <returns> true if it succeeds, false if it fails. </returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        private static extern bool MagickRaiseImage(IntPtr wand, int width, int height, int x, int y, bool raise);

        /// <summary> Magick shadow image.</summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <param name="opacity"> The opacity. </param>
        /// <param name="sigma"> The sigma. </param>
        /// <param name="x"> The x coordinate. </param>
        /// <param name="y"> The y coordinate. </param>
        /// <returns> true if it succeeds, false if it fails. </returns>
        internal static bool MagickShadowImage(IntPtr wand, double opacity, double sigma, int x, int y)
        {
            return MagickShadowImage(wand, opacity, sigma, (IntPtr)x, (IntPtr)y);
        }

        /// WandExport MagickBooleanType MagickShadowImage(MagickWand *wand,const double opacity,
        /// const double sigma,const ssize_t x,const ssize_t y)
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        private static extern bool MagickShadowImage(IntPtr wand, double opacity, double sigma, IntPtr x, IntPtr y);

        /// <summary> Magick shade image. </summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <param name="gray"> true to gray. </param>
        /// <param name="azimuth"> The azimuth. </param>
        /// <param name="elevation"> The elevation. </param>
        /// <returns> true if it succeeds, false if it fails. </returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        private static extern bool MagickShadeImage(IntPtr wand, bool gray, double azimuth, double elevation);

        /// <summary> Magick shear image. 
        /// 
        /// WandExport MagickBooleanType MagickShearImage(MagickWand *wand,const PixelWand *background,
        /// const double x_shear,const double y_shear)
        /// 
        /// </summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <param name="background"> The background. </param>
        /// <param name="x_shear"> The shear. </param>
        /// <param name="y_shear"> The shear. </param>
        /// <returns> true if it succeeds, false if it fails. </returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        internal static extern bool MagickShearImage(IntPtr wand, IntPtr background, double x_shear, double y_shear);

		/// <summary> Magick get image gravity. 
		/// 
        /// WandExport GravityType MagickGetImageGravity(MagickWand *wand)
		/// 
		/// </summary>
		/// <param name="wand"> Handle of the wand. </param>
		/// <returns> A GravityType. </returns>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		internal static extern GravityType MagickGetImageGravity(IntPtr wand);

		/// <summary> Magick set image gravity. 
		/// 
		/// WandExport MagickBooleanType MagickSetImageGravity(MagickWand *wand,const GravityType gravity)
		/// 
		/// </summary>
		/// <param name="wand"> Handle of the wand. </param>
		/// <param name="gravity"> The gravity. </param>
		/// <returns> true if it succeeds, false if it fails. </returns>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		internal static extern bool MagickSetImageGravity(IntPtr wand, GravityType gravity);

		/// <summary> Magick get image compose. </summary>
		/// <param name="wand"> Handle of the wand. </param>
		/// <returns> A CompositeOperator. </returns>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		private static extern CompositeOperator MagickGetImageCompose(IntPtr wand);

		/// <summary> Magick set image compose. </summary>
		/// <param name="wand"> Handle of the wand. </param>
		/// <param name="compose"> The compose. </param>
		/// <returns> true if it succeeds, false if it fails. </returns>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		private static extern bool MagickSetImageCompose(IntPtr wand, CompositeOperator compose);

		/// <summary> Magick get image clip mask. </summary>
		/// <param name="wand"> Handle of the wand. </param>
		/// <returns> An IntPtr. </returns>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		private static extern IntPtr MagickGetImageClipMask(IntPtr wand);

		/// <summary> Magick set image clip mask. 
		/// 
		/// WandExport MagickBooleanType MagickSetImageClipMask(MagickWand *wand,const MagickWand *clip_mask)
		/// 
		/// </summary>
		/// <param name="wand"> Handle of the wand. </param>
		/// <param name="clip_mask"> The clip mask. </param>
		/// <returns> true if it succeeds, false if it fails. </returns>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		internal static extern bool MagickSetImageClipMask(IntPtr wand, IntPtr clip_mask);

		/// <summary> Magick negate image. 
		/// 
		/// WandExport MagickBooleanType MagickNegateImage(MagickWand *wand,const MagickBooleanType gray)
		/// 
		/// </summary>
		/// <param name="wand"> Handle of the wand. </param>
		/// <param name="gray"> true to gray. </param>
		/// <returns> true if it succeeds, false if it fails. </returns>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		internal static extern bool MagickNegateImage(IntPtr wand, bool gray);

		/// <summary> Magick get image alpha channel. 
		/// 
        /// WandExport MagickBooleanType MagickGetImageAlphaChannel(MagickWand *wand)
		/// Note: The enum here as return is okay as bool=int on all platforms. This is 
		/// a bug in imagemagick
		/// </summary>
		/// <param name="wand"> Handle of the wand. </param>
		/// <returns> An AlphaChannelType. </returns>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		internal static extern AlphaChannelType MagickGetImageAlphaChannel(IntPtr wand);

		/// <summary> Magick set image alpha channel. 
		/// 
		/// WandExport MagickBooleanType MagickSetImageAlphaChannel(MagickWand *wand,const AlphaChannelType alpha_type)
		/// 
		/// </summary>
		/// <param name="wand"> Handle of the wand. </param>
		/// <param name="alpha_type"> Type of the alpha. </param>
		/// <returns> true if it succeeds, false if it fails. </returns>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		internal static extern bool MagickSetImageAlphaChannel(IntPtr wand, AlphaChannelType alpha_type);

        /// <summary> Magick distort image.</summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <param name="method"> The method. </param>
        /// <param name="number_arguments"> Number of arguments. </param>
        /// <param name="arguments"> The arguments. </param>
        /// <param name="bestfit"> true to bestfit. </param>
        /// <returns> true if it succeeds, false if it fails. </returns>
        internal static bool MagickDistortImage(IntPtr wand, DistortImageMethodType method, int number_arguments,
            double[] arguments, bool bestfit)
        {
            return MagickDistortImage(wand, method, (IntPtr) number_arguments, arguments, bestfit);
        }

        /// WandExport MagickBooleanType MagickDistortImage(MagickWand *wand,const DistortImageMethod method,
        /// const size_t number_arguments,const double *arguments,const MagickBooleanType bestfit)
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		internal static extern bool MagickDistortImage(IntPtr wand, DistortImageMethodType method, IntPtr number_arguments, double[] arguments, bool bestfit);

		/// <summary> Magick brightness contrast image. </summary>
		/// <param name="wand"> Handle of the wand. </param>
		/// <param name="brightness"> The brightness. </param>
		/// <param name="contrast"> The contrast. </param>
		/// <returns> true if it succeeds, false if it fails. </returns>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		private static extern bool MagickBrightnessContrastImage(IntPtr wand, double brightness, double contrast);

		/// <summary> Magick colorize image. 
		/// 
		/// WandExport MagickBooleanType MagickColorizeImage(MagickWand *wand,const PixelWand *colorize,const PixelWand *opacity)
		/// 
		/// </summary>
		/// <param name="wand"> Handle of the wand. </param>
		/// <param name="colorize"> The colorize. </param>
		/// <param name="blend"> The blend. </param>
		/// <returns> true if it succeeds, false if it fails. </returns>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		internal static extern bool MagickColorizeImage(IntPtr wand, IntPtr colorize, IntPtr blend);

        #endregion

		#region [Image Wand Methods - Pixel]
		
		/// <summary> Magick get image pixel color. </summary>
		/// <param name="wand"> Handle of the wand. </param>
		/// <param name="x"> The x coordinate. </param>
		/// <param name="y"> The y coordinate. </param>
		/// <param name="color"> The color. </param>
		/// <returns> true if it succeeds, false if it fails. </returns>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		private static extern bool MagickGetImagePixelColor(IntPtr wand, int x, int y, ref IntPtr color);

        /// <summary> Magick get image virtual pixel method. 
        /// 
        /// WandExport VirtualPixelMethod MagickGetImageVirtualPixelMethod(MagickWand *wand)
        /// 
        /// </summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <returns> A PixelType. </returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        internal static extern VirtualPixelType MagickGetImageVirtualPixelMethod(IntPtr wand);

        /// <summary> Magick set image virtual pixel method. 
        /// 
        /// WandExport VirtualPixelMethod MagickSetImageVirtualPixelMethod(MagickWand *wand,const VirtualPixelMethod method)
        /// 
        /// </summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <param name="method"> The method. </param>
        /// <returns> true if it succeeds, false if it fails. </returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        internal static extern VirtualPixelType MagickSetImageVirtualPixelMethod(IntPtr wand, VirtualPixelType method);

		/// <summary> Magick annotate image. 
		/// 
		/// WandExport MagickBooleanType MagickAnnotateImage(MagickWand *wand,const DrawingWand *drawing_wand,
		/// const double x,const double y,const double angle,const char *text)
		/// 
		/// </summary>
		/// <param name="wand"> Handle of the wand. </param>
		/// <param name="drawing_wand"> The drawing wand. </param>
		/// <param name="x"> The x coordinate. </param>
		/// <param name="y"> The y coordinate. </param>
		/// <param name="angle"> The angle. </param>
		/// <param name="text"> The text. </param>
		/// <returns> true if it succeeds, false if it fails. </returns>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		internal static extern bool MagickAnnotateImage(IntPtr wand, IntPtr drawing_wand, double x, double y, double angle,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8Marshaler))] string text);

		#endregion

		#region [Image Wand Methods - Drawing]

		/// <summary> Magick draw image. 
		/// 
		/// WandExport MagickBooleanType MagickDrawImage(MagickWand *wand, const DrawingWand *drawing_wand)

		/// 
		/// </summary>
		/// <param name="wand"> Handle of the wand. </param>
		/// <param name="drawing_wand"> The drawing wand. </param>
		/// <returns> true if it succeeds, false if it fails. </returns>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		internal static extern bool MagickDrawImage(IntPtr wand, IntPtr drawing_wand);

		#endregion
	}
}
