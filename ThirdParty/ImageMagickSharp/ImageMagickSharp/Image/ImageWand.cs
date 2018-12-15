using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ImageMagickSharp
{
	/// <summary> An image wand. </summary>
	/// <seealso cref="T:ImageMagickSharp.WandBase"/>
	public class ImageWand : WandBase
	{

		#region [Constructors]
		/// <summary> Initializes a new instance of the MagickBase class. </summary>
		/// <param name="magickWand"> . </param>
		/// <param name="index"> The index. </param>
		internal ImageWand(MagickWand magickWand, int index)
			: base(magickWand)
		{
			this.Index = index;
		}

		#endregion

		#region [Image Wand Properties]
		/// <summary> Activates the image wand described by wandAction. </summary>
		/// <typeparam name="T"> Generic type parameter. </typeparam>
		/// <param name="wandAction"> The wand action. </param>
		/// <returns> A T. </returns>
		private T ActivateImageWand<T>(Func<T> wandAction)
		{
			this.MagickWand.IteratorIndex = this.Index;
			return wandAction();
		}

		/// <summary> Activates the image wand. </summary>
		/// <param name="wandAction"> The wand action. </param>
		private void ActivateImageWand(Action wandAction)
		{
			this.MagickWand.IteratorIndex = this.Index;
			wandAction();
		}

		/// <summary> Activates the image wand. </summary>
		private void ActivateImageWand()
		{
			this.MagickWand.IteratorIndex = this.Index;
		}

		/// <summary> Gets the zero-based index of this object. </summary>
		/// <value> The index. </value>
		private int Index { get; set; }

		/// <summary> Gets or sets the filename of the file. </summary>
		/// <value> The filename. </value>
		/*private string Filename
		{
			get { return this.ActivateImageWand(() => WandNativeString.Load(ImageWandInterop.MagickGetImageFilename(this.MagickWand))); }
			set { this.ActivateImageWand(() => this.MagickWand.CheckError(ImageWandInterop.MagickSetImageFilename(this.MagickWand, value))); }
		}*/

		/// <summary> Gets the width. </summary>
		/// <value> The width. </value>
		public int Width
		{
			get { return this.ActivateImageWand(() => ImageWandInterop.MagickGetImageWidth(this.MagickWand)); }
		}

		/// <summary> Gets the height. </summary>
		/// <value> The height. </value>
        public int Height
		{
			get { return this.ActivateImageWand(() => ImageWandInterop.MagickGetImageHeight(this.MagickWand)); }
		}

		/// <summary> Gets or sets the image compression quality. </summary>
		/// <value> The image compression quality. </value>
		public int CompressionQuality
		{
			get { return this.ActivateImageWand(() => ImageWandInterop.MagickGetImageCompressionQuality(this.MagickWand)); }
			set { this.ActivateImageWand(() => this.MagickWand.CheckError(ImageWandInterop.MagickSetImageCompressionQuality(this.MagickWand, value))); }
		}

		/// <summary> Gets or sets the compose. </summary>
		/// <value> The compose. </value>
		/*private CompositeOperator Compose
		{
			get { return this.ActivateImageWand(() => ImageWandInterop.MagickGetImageCompose(this.MagickWand)); }
			set { this.ActivateImageWand(() => this.MagickWand.CheckError(ImageWandInterop.MagickSetImageCompose(this.MagickWand, value))); }
		}*/

		/// <summary> Gets or sets the image virtual pixel. </summary>
		/// <value> The image virtual pixel. </value>
		public VirtualPixelType ImageVirtualPixel
		{
			get { return this.ActivateImageWand(() => ImageWandInterop.MagickGetImageVirtualPixelMethod(this.MagickWand)); }
			set { this.ActivateImageWand(() => ImageWandInterop.MagickSetImageVirtualPixelMethod(this.MagickWand, value)); }
		}

		/// <summary> Gets or sets the alpha channel. </summary>
		/// <value> The alpha channel. </value>
		public AlphaChannelType AlphaChannel
		{
			get { return this.ActivateImageWand(() => ImageWandInterop.MagickGetImageAlphaChannel(this.MagickWand)); }
			set { this.ActivateImageWand(() => this.MagickWand.CheckError(ImageWandInterop.MagickSetImageAlphaChannel(this.MagickWand, value))); }
		}

		/// <summary> Gets or sets the gravity. </summary>
		/// <value> The gravity. </value>
		public GravityType Gravity
		{
			get { return this.ActivateImageWand(() => ImageWandInterop.MagickGetImageGravity(this.MagickWand)); }
			set { this.ActivateImageWand(() => this.MagickWand.CheckError(ImageWandInterop.MagickSetImageGravity(this.MagickWand, value))); }
		}

		/// <summary> Gets or sets the color of the background. </summary>
		/// <value> The color of the background. </value>
		public PixelWand BackgroundColor
		{
			/* DOESNOT WORK
             * get
			{
				IntPtr background;
				this.ActivateImageWand();
				this.MagickWand.CheckError(ImageWandInterop.MagickGetImageBackgroundColor(this.MagickWand, out background));
				return new PixelWand(background);
			}*/
			set { this.ActivateImageWand(() => this.MagickWand.CheckError(ImageWandInterop.MagickSetImageBackgroundColor(this.MagickWand, value))); }
		}

		/// <summary> Gets or sets the color of the image border. </summary>
		/// <value> The color of the image border. </value>
		/* NOT WORKING
        private PixelWand ImageBorderColor
		{
			get
			{
				IntPtr background;
				this.ActivateImageWand();
				this.MagickWand.CheckError(ImageWandInterop.MagickGetImageBorderColor(this.MagickWand, out background));
				return new PixelWand(background);
			}
			set { this.ActivateImageWand(() => this.MagickWand.CheckError(ImageWandInterop.MagickSetImageBorderColor(this.MagickWand, value))); }
		}
        */

		/// <summary> Gets or sets the color of the image matte. </summary>
		/// <value> The color of the image matte. </value>
		/* NOT WORKING
		private PixelWand MatteColor
		{
			get
			{
				IntPtr color;
				this.ActivateImageWand();
				this.MagickWand.CheckError(ImageWandInterop.MagickGetImageMatteColor(this.MagickWand, out color));
				return new PixelWand(color);
			}
			set { this.MagickWand.CheckError(ImageWandInterop.MagickSetImageMatteColor(this.MagickWand, value)); }
		}
        */

		/// <summary> Sets a value indicating whether the matte. </summary>
		/// <value> true if matte, false if not. </value>
		/*private bool Matte
		{
			set { this.ActivateImageWand(() => this.MagickWand.CheckError(ImageWandInterop.MagickSetImageMatte(this.MagickWand, value))); }
		}

		/// <summary> Gets or sets the format to use. </summary>
		/// <value> The format. </value>
		private string Format
		{
			get { return this.ActivateImageWand(() => WandNativeString.Load(ImageWandInterop.MagickGetImageFormat(this.MagickWand))); }
			set
			{
				using (var formatString = new WandNativeString(value))
				{
					this.ActivateImageWand(() => this.MagickWand.CheckError(ImageWandInterop.MagickSetImageFormat(this.MagickWand, formatString.Pointer)));
				}
			}
		}*/
		#endregion

		#region [Image Wand Methods]
        public void ScaleImage(int width, int height)
        {
            this.ActivateImageWand(() => this.MagickWand.CheckError((ImageWandInterop.MagickScaleImage(this.MagickWand, width, height))));
        }

        public void AutoOrientImage()
        {
            this.ActivateImageWand(() => this.MagickWand.CheckError((ImageWandInterop.MagickAutoOrientImage(this.MagickWand))));
        }

        public void StripImage()
        {
            this.ActivateImageWand(() => this.MagickWand.CheckError((ImageWandInterop.MagickStripImage(this.MagickWand))));
        }
        
        /// <summary> Resize image. </summary>
		/// <param name="width"> The width. </param>
		/// <param name="height"> The height. </param>
		/// <param name="filter"> Specifies the filter. </param>
		/// <param name="blur"> The blur. </param>
		public void ResizeImage(int width, int height, FilterTypes filter, double blur = 1.0)
		{
			this.ActivateImageWand(() => this.MagickWand.CheckError((ImageWandInterop.MagickResizeImage(this.MagickWand, width, height, (int)filter, blur))));
		}

		/// <summary> Resize image. </summary>
		/// <param name="width"> The width. </param>
		/// <param name="height"> The height. </param>
		public void ResizeImage(int width, int height)
		{
			this.ActivateImageWand(() => ResizeImage(width, height, FilterTypes.LanczosFilter));
		}

        public void MagickThumbnailImage(int width, int height, bool bestFit, bool fill)
        {
            this.ActivateImageWand(() => this.MagickWand.CheckError((ImageWandInterop.MagickThumbnailImage(this.MagickWand, width, height, bestFit, fill))));
        }
        
        /// <summary> Crop image. </summary>
		/// <param name="width"> The width. </param>
		/// <param name="height"> The height. </param>
		/// <param name="x"> The x coordinate. </param>
		/// <param name="y"> The y coordinate. </param>
		public void CropImage(int width, int height, int x, int y)
		{
			this.ActivateImageWand(() => this.MagickWand.CheckError(ImageWandInterop.MagickCropImage(this.MagickWand, width, height, x, y)));
		}

		/// <summary> Rotate image. </summary>
		/// <param name="background"> The background. </param>
		/// <param name="degrees"> The degrees. </param>
		public void RotateImage(PixelWand background, double degrees)
		{
			this.ActivateImageWand(() => this.MagickWand.CheckError(ImageWandInterop.MagickRotateImage(this.MagickWand, background, degrees)));
		}

		/// <summary> Transform image. </summary>
		/// <param name="crop"> The crop. </param>
		/// <param name="geomety"> The geomety. </param>
		/// <returns> A MagickWand. </returns>
		/*private MagickWand TransformImage(string crop, string geomety)
		{
			return this.ActivateImageWand(() => new MagickWand(ImageWandInterop.MagickTransformImage(this.MagickWand, crop, geomety)));
		}*/

		/// <summary> Transform image colorspace. </summary>
		/// <param name="colorspace"> The colorspace. </param>
		/// <returns> true if it succeeds, false if it fails. </returns>
		/*private bool TransformImageColorspace(ImageColorspaceType colorspace)
		{
			return this.ActivateImageWand(() => this.MagickWand.CheckError(ImageWandInterop.MagickTransformImageColorspace(this.MagickWand, colorspace)));
		}

		/// <summary> Determines if we can transpose image. </summary>
		/// <returns> true if it succeeds, false if it fails. </returns>
		private bool TransposeImage()
		{
			return this.ActivateImageWand(() => this.MagickWand.CheckError(ImageWandInterop.MagickTransposeImage(this.MagickWand)));
		}

		/// <summary> Determines if we can transverse image. </summary>
		/// <returns> true if it succeeds, false if it fails. </returns>
		private bool TransverseImage()
		{
			return this.ActivateImageWand(() => this.MagickWand.CheckError(ImageWandInterop.MagickTransverseImage(this.MagickWand)));
		}

		/// <summary> Thumbnail image. </summary>
		/// <param name="columns"> The columns. </param>
		/// <param name="rows"> The rows. </param>
		/// <returns> true if it succeeds, false if it fails. </returns>
		private bool ThumbnailImage(int columns, int rows)
		{
			return this.ActivateImageWand(() => this.MagickWand.CheckError(ImageWandInterop.MagickThumbnailImage(this.MagickWand, columns, rows)));
		}*/

		/// <summary> Gets image region. </summary>
		/// <param name="width"> The width. </param>
		/// <param name="height"> The height. </param>
		/// <param name="x"> The x coordinate. </param>
		/// <param name="y"> The y coordinate. </param>
		/// <returns> The image region. </returns>
		/* NOT WORKING
		private MagickWand GetImageRegion(int width, int height, int x, int y)
		{
			return this.ActivateImageWand(() => new MagickWand(ImageWandInterop.MagickGetImageRegion(this.MagickWand, width, height, x, y)));
		}
         * */
		/// <summary> Flip image. </summary>
		public void FlipImage()
		{
			this.ActivateImageWand(() => this.MagickWand.CheckError(ImageWandInterop.MagickFlipImage(this.MagickWand)));
		}

		/// <summary> Flop image. </summary>
		/*private void FlopImage()
		{
			this.ActivateImageWand(() => this.MagickWand.CheckError(ImageWandInterop.MagickFlopImage(this.MagickWand)));
		}

		/// <summary> Transparents. </summary>
		/// <param name="target"> Target for the. </param>
		/// <param name="alpha"> The alpha. </param>
		/// <param name="fuzz"> The fuzz. </param>
		/// <param name="invert"> true to invert. </param>
		private void Transparent(PixelWand target, double alpha, double fuzz, bool invert)
		{
			this.ActivateImageWand(() => this.MagickWand.CheckError(ImageWandInterop.MagickTransparentPaintImage(this.MagickWand, target, alpha, fuzz, invert ? 1 : 0)));
		}

		/// <summary> Fills. </summary>
		/// <param name="target"> Target for the. </param>
		/// <param name="fill"> The fill. </param>
		/// <param name="fuzz"> The fuzz. </param>
		/// <param name="invert"> true to invert. </param>
		private void Fill(PixelWand target, PixelWand fill, double fuzz, bool invert)
		{
			this.ActivateImageWand(() => this.MagickWand.CheckError(ImageWandInterop.MagickOpaquePaintImage(this.MagickWand, target, fill, fuzz, invert ? 1 : 0)));
		}

		/// <summary> Sets image extent. </summary>
		/// <param name="width"> The width. </param>
		/// <param name="height"> The height. </param>
		/// <param name="x"> The x coordinate. </param>
		/// <param name="y"> The y coordinate. </param>
		/// <returns> true if it succeeds, false if it fails. </returns>
		private bool SetImageExtent(int width, int height, int x, int y)
		{
			return this.ActivateImageWand(() => this.MagickWand.CheckErrorBool(ImageWandInterop.MagickSetImageExtent(this.MagickWand, width, height)));
		}

		/// <summary> Sets image extent. </summary>
		/// <param name="width"> The width. </param>
		/// <param name="height"> The height. </param>
		/// <param name="x"> The x coordinate. </param>
		/// <param name="y"> The y coordinate. </param>
		/// <returns> true if it succeeds, false if it fails. </returns>
		private bool SetImageExtent(double width, double height, double x, double y)
		{
			return this.ActivateImageWand(() => this.MagickWand.CheckErrorBool(ImageWandInterop.MagickSetImageExtent(this.MagickWand, width, height)));
		}*/

		/// <summary> Extent image. </summary>
		/// <param name="width"> The width. </param>
		/// <param name="height"> The height. </param>
		/// <param name="x"> The x coordinate. </param>
		/// <param name="y"> The y coordinate. </param>
		/// <returns> true if it succeeds, false if it fails. </returns>
		public bool ExtentImage(int width, int height, int x, int y)
		{
			return this.ActivateImageWand(() => this.MagickWand.CheckErrorBool(ImageWandInterop.MagickExtentImage(this.MagickWand, width, height, x, y)));
		}

		/// <summary> Trim image. </summary>
		/// <param name="fuzz"> The fuzz. By default target must match a particular pixel color exactly.
		/// However, in many cases two colors may differ by a small amount. The fuzz member of image
		/// defines how much tolerance is acceptable to consider two colors as the same. For example, set
		/// fuzz to 10 and the color red at intensities of 100 and 102 respectively are now interpreted
		/// as the same color for the purposes of the floodfill. </param>
		/// <returns> true if it succeeds, false if it fails. </returns>
		public bool TrimImage(double fuzz)
		{
			return this.ActivateImageWand(() => this.MagickWand.CheckErrorBool(ImageWandInterop.MagickTrimImage(this.MagickWand, fuzz)));
		}

		/// <summary> Color matrix image. </summary>
		/// <param name="matrix"> The matrix. </param>
		/// <returns> true if it succeeds, false if it fails. </returns>
		/*private bool ColorMatrixImage(double[,] matrix)
		{
			return this.ActivateImageWand(() => this.MagickWand.CheckErrorBool(ImageWandInterop.MagickColorMatrixImage(this.MagickWand, matrix)));
		}*/

		/// <summary> Composite image. </summary>
		/// <param name="sourcePtr"> Source pointer. </param>
		/// <param name="compositeOperator"> The composite operator. </param>
		/// <param name="x"> The x coordinate. </param>
		/// <param name="y"> The y coordinate. </param>
		/// <returns> true if it succeeds, false if it fails. </returns>
		public bool CompositeImage(MagickWand sourcePtr, CompositeOperator compositeOperator, int x, int y)
		{
			return this.ActivateImageWand(() => this.MagickWand.CheckErrorBool(ImageWandInterop.MagickCompositeImage(this.MagickWand, sourcePtr, compositeOperator, x, y)));
		}

		/// <summary> Label image. </summary>
		/// <param name="label"> The label. </param>
		/// <returns> true if it succeeds, false if it fails. </returns>
		/*private bool LabelImage(string label)
		{
			return this.ActivateImageWand(() => this.MagickWand.CheckErrorBool(ImageWandInterop.MagickLabelImage(this.MagickWand, label)));
		}

		/// <summary> Raises the image event. </summary>
		/// <param name="width"> The width. </param>
		/// <param name="height"> The height. </param>
		/// <param name="x"> The x coordinate. </param>
		/// <param name="y"> The y coordinate. </param>
		/// <param name="raise"> true to raise. </param>
		/// <returns> true if it succeeds, false if it fails. </returns>
		private bool RaiseImage(int width, int height, int x, int y, bool raise)
		{
			return this.ActivateImageWand(() => this.MagickWand.CheckErrorBool(ImageWandInterop.MagickRaiseImage(this.MagickWand, width, height, x, y, raise)));
		}*/

		/// <summary> Border image. </summary>
		/// <param name="bordercolor"> The bordercolor. </param>
		/// <param name="width"> The width. </param>
		/// <param name="height"> The height. </param>
		/// <returns> true if it succeeds, false if it fails. </returns>
		internal bool BorderImage(PixelWand bordercolor, int width, int height)
		{
			return this.ActivateImageWand(() => this.MagickWand.CheckErrorBool(ImageWandInterop.MagickBorderImage(this.MagickWand, bordercolor, width, height)));
		}

		/// <summary> Shadow image. </summary>
		/// <param name="opacity"> The opacity. </param>
		/// <param name="sigma"> The sigma. </param>
		/// <param name="x"> The x coordinate. </param>
		/// <param name="y"> The y coordinate. </param>
		/// <returns> true if it succeeds, false if it fails. </returns>
		internal bool ShadowImage(double opacity, double sigma, int x, int y)
		{
			return this.ActivateImageWand(() => this.MagickWand.CheckErrorBool(ImageWandInterop.MagickShadowImage(this.MagickWand, opacity, sigma, x, y)));

		}

		/// <summary>
		/// Shade image. shines a distant light on an image to create a three-dimensional effect. You
		/// control the positioning of the light with azimuth and elevation; azimuth is measured in
		/// degrees off the x axis and elevation is measured in pixels above the Z axis. </summary>
		/// <param name="gray"> true to gray. </param>
		/// <param name="azimuth"> The azimuth. </param>
		/// <param name="elevation"> The elevation. </param>
		/// <returns> true if it succeeds, false if it fails. </returns>
		/*private bool ShadeImage(bool gray, double azimuth, double elevation)
		{
			return this.ActivateImageWand(() => this.MagickWand.CheckErrorBool(ImageWandInterop.MagickShadeImage(this.MagickWand, gray, azimuth, elevation)));
		}*/

		/// <summary> Shear image. </summary>
		/// <param name="background"> The background. </param>
		/// <param name="x_shear"> The shear. </param>
		/// <param name="y_shear"> The shear. </param>
		/// <returns> true if it succeeds, false if it fails. </returns>
		internal bool ShearImage(PixelWand background, double x_shear, double y_shear)
		{
			return this.ActivateImageWand(() => this.MagickWand.CheckErrorBool(ImageWandInterop.MagickShearImage(this.MagickWand, background, x_shear, y_shear)));
		}

		/// <summary> Sets image clip mask. </summary>
		/// <param name="clip_mask"> The clip mask. </param>
		/// <returns> true if it succeeds, false if it fails. </returns>
		internal bool SetImageClipMask(MagickWand clip_mask)
		{
			return this.ActivateImageWand(() => this.MagickWand.CheckError(ImageWandInterop.MagickSetImageClipMask(this.MagickWand, clip_mask)));
		}

		/// <summary> Gets image clip mask. </summary>
		/// <returns> The image clip mask. </returns>
		/*private MagickWand GetImageClipMask()
		{
			return this.ActivateImageWand(() => new MagickWand(ImageWandInterop.MagickGetImageClipMask(this.MagickWand)));
		}*/

		/// <summary> Gets image pixel color. </summary>
		/// <param name="x"> The x coordinate. </param>
		/// <param name="y"> The y coordinate. </param>
		/// <returns> The image pixel color. </returns>
		/* NOT WORKING 
         * private PixelWand GetImagePixelColor(int x, int y)
		{

			PixelIterator pi = new PixelIterator(this.MagickWand, x, y, 1, 1);
			var pw = pi.GetCurrentPixelIteratorRow();

			if (pw == null)
				return null;
			return pw.FirstOrDefault();

		}*/

		/// <summary> Negate image. </summary>
		/// <param name="gray"> true to gray. </param>
		/// <returns> true if it succeeds, false if it fails. </returns>
		internal bool NegateImage(bool gray)
		{
			return this.ActivateImageWand(() => this.MagickWand.CheckError(ImageWandInterop.MagickNegateImage(this.MagickWand, gray)));
		}

		/// <summary> Distort image. </summary>
		/// <param name="method"> The method. </param>
		/// <param name="arguments"> The arguments. </param>
		/// <param name="bestfit"> true to bestfit. </param>
		/// <returns> true if it succeeds, false if it fails. </returns>
		public bool DistortImage(DistortImageMethodType method, double[] arguments, bool bestfit)
		{
		    return this.ActivateImageWand(() =>
		    {
		        bool rValue =
		            this.MagickWand.CheckError(ImageWandInterop.MagickDistortImage(this.MagickWand, method, arguments.Length,
		                arguments, bestfit));
		        return rValue;
		    });
		}

	    /// <summary> Annotate image. </summary>
	    /// <param name="drawing_wand"> The drawing wand. </param>
	    /// <param name="x"> The x coordinate. </param>
	    /// <param name="y"> The y coordinate. </param>
	    /// <param name="angle"> The angle. </param>
	    /// <param name="text"> The text. </param>
	    /// <returns> true if it succeeds, false if it fails. </returns>
	    public bool AnnotateImage(IntPtr drawing_wand, double x, double y, double angle, string text)
	    {
	        return
	            this.MagickWand.CheckErrorBool(ImageWandInterop.MagickAnnotateImage(this.MagickWand, drawing_wand, x, y,
	                angle, text));
	    }

	    /// <summary> Gaussian blur image. </summary>
		/// <param name="radius"> The radius. </param>
		/// <param name="sigma"> The sigma. </param>
		/*private void GaussianBlurImage(double radius, double sigma)
		{
			bool checkErrorBool = false;
			checkErrorBool = this.MagickWand.CheckErrorBool(ImageWandInterop.MagickGaussianBlurImage(this.MagickWand, radius, sigma));

		}

		/// <summary> Adaptive blur image. </summary>
		/// <param name="radius"> The radius. </param>
		/// <param name="sigma"> The sigma. </param>
		private void AdaptiveBlurImage(double radius, double sigma)
		{
			bool checkErrorBool = false;
			checkErrorBool = this.MagickWand.CheckErrorBool(ImageWandInterop.MagickAdaptiveBlurImage(this.MagickWand, radius, sigma));

		}

		/// <summary> Threshold image. </summary>
		/// <param name="threshold"> The threshold. </param>
		private void ThresholdImage(double threshold)
		{
			bool checkErrorBool = false;
			checkErrorBool = this.MagickWand.CheckErrorBool(ImageWandInterop.MagickThresholdImage(this.MagickWand, threshold));

		}

		/// <summary> Adaptive threshold image. </summary>
		/// <param name="width"> The width. </param>
		/// <param name="height"> The height. </param>
		/// <param name="bias"> The bias. </param>
		private void AdaptiveThresholdImage(int width, int height, double bias)
		{
			bool checkErrorBool = false;
			checkErrorBool = this.MagickWand.CheckErrorBool(ImageWandInterop.MagickAdaptiveThresholdImage(this.MagickWand, width,height, bias));

		}

		/// <summary> Brightness contrast image. </summary>
		/// <param name="brightness"> The brightness. </param>
		/// <param name="contrast"> The contrast. </param>
		private void BrightnessContrastImage(double brightness, double contrast)
		{
			bool checkErrorBool = false;
			checkErrorBool = this.MagickWand.CheckErrorBool(ImageWandInterop.MagickBrightnessContrastImage(this.MagickWand, brightness, contrast));

		}*/

		/// <summary> Colorize image. </summary>
		/// <param name="colorize"> The colorize. </param>
		/// <param name="blend"> The blend. </param>
		public void ColorizeImage(PixelWand colorize, PixelWand blend)
		{
			bool checkErrorBool = false;
			checkErrorBool = this.MagickWand.CheckErrorBool(ImageWandInterop.MagickColorizeImage(this.MagickWand, colorize, blend));

		}

		#endregion

		#region [Image Wand Methods - Drawing]

		/// <summary> Draw image. </summary>
		/// <param name="drawing_wand"> The drawing wand. </param>
		/// <returns> true if it succeeds, false if it fails. </returns>
		public bool DrawImage(DrawingWand drawing_wand)
		{
			return this.ActivateImageWand(() => this.MagickWand.CheckError(ImageWandInterop.MagickDrawImage(this.MagickWand, drawing_wand)));
		}

		#endregion

	}
}
