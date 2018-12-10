using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ImageMagickSharp.InteropMarshaler;

namespace ImageMagickSharp
{
	/// <summary> A drawing wand interop. </summary>
	internal static class DrawingWandInterop
	{
		#region [Drawing Wand]
		/// <summary> Creates a new drawing wand. 
		/// 
        /// WandExport DrawingWand *NewDrawingWand(void)
		/// 
		/// </summary>
		/// <returns> An IntPtr. </returns>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		internal static extern IntPtr NewDrawingWand();

		/// <summary> Clears the drawing wand described by wand. </summary>
		/// <param name="wand"> The wand. </param>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		private static extern void ClearDrawingWand(IntPtr wand);

		/// <summary> Destroys the drawing wand described by wand. 
		/// 
        /// WandExport DrawingWand *DestroyDrawingWand(DrawingWand *wand)
		/// 
		/// </summary>
		/// <param name="wand"> The wand. </param>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		internal static extern IntPtr DestroyDrawingWand(IntPtr wand);

		/// <summary> Clone drawing wand. </summary>
		/// <param name="wand"> The wand. </param>
		/// <returns> An IntPtr. </returns>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		private static extern IntPtr CloneDrawingWand(IntPtr wand);

		/// <summary> Draw reset vector graphics. </summary>
		/// <param name="wand"> The wand. </param>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		private static extern void DrawResetVectorGraphics(IntPtr wand);

		#endregion

		#region [Drawing Wand Methods]
		/// <summary> Draw composite. 
		/// 
		/// WandExport MagickBooleanType DrawComposite(DrawingWand *wand, const CompositeOperator compose,
		/// const double x,const double y, const double width,const double height,MagickWand *magick_wand)
		/// 
		/// </summary>
		/// <param name="wand"> The wand. </param>
		/// <param name="compose"> The compose. </param>
		/// <param name="x"> The x coordinate. </param>
		/// <param name="y"> The y coordinate. </param>
		/// <param name="width"> The width. </param>
		/// <param name="height"> The height. </param>
		/// <param name="magickwand"> The magickwand. </param>
		/// <returns> true if it succeeds, false if it fails. </returns>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		internal static extern bool DrawComposite(IntPtr wand, CompositeOperator compose, double x, double y, double width, double height, IntPtr magickwand);

		/// <summary> Draw get fill opacity. </summary>
		/// <param name="wand"> The wand. </param>
		/// <returns> A double. </returns>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		private static extern double DrawGetFillOpacity(IntPtr wand);

		/// <summary> Draw set fill opacity. </summary>
		/// <param name="wand"> The wand. </param>
		/// <param name="fill_opacity"> The fill opacity. </param>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		private static extern void DrawSetFillOpacity(IntPtr wand, double fill_opacity);

		/// <summary> Draw get opacity. </summary>
		/// <param name="wand"> The wand. </param>
		/// <returns> A double. </returns>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		private static extern double DrawGetOpacity(IntPtr wand);

		/// <summary> Draw set opacity. </summary>
		/// <param name="wand"> The wand. </param>
		/// <param name="fill_opacity"> The fill opacity. </param>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		private static extern void DrawSetOpacity(IntPtr wand, double fill_opacity);

		/// <summary> Draw skew x coordinate. </summary>
		/// <param name="wand"> The wand. </param>
		/// <param name="degrees"> The degrees. </param>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		private static extern void DrawSkewX(IntPtr wand, double degrees);

		/// <summary> Draw skew y coordinate. </summary>
		/// <param name="wand"> The wand. </param>
		/// <param name="degrees"> The degrees. </param>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		private static extern void DrawSkewY(IntPtr wand, double degrees);

		/// <summary> Draw translate. </summary>
		/// <param name="wand"> The wand. </param>
		/// <param name="x"> The x coordinate. </param>
		/// <param name="y"> The y coordinate. </param>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		private static extern void DrawTranslate(IntPtr wand, double x, double y);

		/// <summary> Draw matte. </summary>
		/// <param name="wand"> The wand. </param>
		/// <param name="x"> The x coordinate. </param>
		/// <param name="y"> The y coordinate. </param>
		/// <param name="paint_method"> The paint method. </param>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		private static extern void DrawMatte(IntPtr wand, double x, double y, PaintMethodType paint_method);

		/// <summary> Draw scale. </summary>
		/// <param name="wand"> The wand. </param>
		/// <param name="x"> The x coordinate. </param>
		/// <param name="y"> The y coordinate. </param>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		private static extern void DrawScale(IntPtr wand, double x, double y);

		/// <summary> Draw rotate. </summary>
		/// <param name="wand"> The wand. </param>
		/// <param name="degrees"> The degrees. </param>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		private static extern void DrawRotate(IntPtr wand, double degrees);

		/// <summary> Draw get gravity. 
		/// 
        /// WandExport GravityType DrawGetGravity(const DrawingWand *wand)
		///  
		/// </summary>
		/// <param name="wand"> The wand. </param>
		/// <returns> A GravityType. </returns>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		internal static extern GravityType DrawGetGravity(IntPtr wand);

		/// <summary> Draw set gravity. 
		/// 
        /// WandExport void DrawSetGravity(DrawingWand *wand,const GravityType gravity)
		/// 
		/// </summary>
		/// <param name="wand"> The wand. </param>
		/// <param name="gravity"> The gravity. </param>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		internal static extern void DrawSetGravity(IntPtr wand, GravityType gravity);

		/// <summary> Draw get stroke antialias. </summary>
		/// <param name="wand"> The wand. </param>
		/// <returns> true if it succeeds, false if it fails. </returns>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		private static extern bool DrawGetStrokeAntialias(IntPtr wand);

		/// <summary> Draw set stroke antialias. </summary>
		/// <param name="wand"> The wand. </param>
		/// <param name="stroke_antialias"> true to stroke antialias. </param>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		private static extern void DrawSetStrokeAntialias(IntPtr wand, bool stroke_antialias);

		/// <summary> Draw get vector graphics. </summary>
		/// <param name="wand"> The wand. </param>
		/// <returns> A string. </returns>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		private static extern string DrawGetVectorGraphics(IntPtr wand);

		/// <summary> Draw set vector graphics. </summary>
		/// <param name="wand"> The wand. </param>
		/// <param name="xml"> The XML. </param>
		/// <returns> true if it succeeds, false if it fails. </returns>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		private static extern bool DrawSetVectorGraphics(IntPtr wand, string xml);

		#endregion

		#region [Drawing Wand Methods Geometry]
		/// <summary> Draw arc. </summary>
		/// <param name="wand"> The wand. </param>
		/// <param name="sx"> The starting x ordinate of bounding rectangle. </param>
		/// <param name="sy"> The starting y ordinate of bounding rectangle. </param>
		/// <param name="ex"> The ending x ordinate of bounding rectangle. </param>
		/// <param name="ey"> The ending y ordinate of bounding rectangle. </param>
		/// <param name="sd"> The starting degrees of rotation. </param>
		/// <param name="ed"> The ending degrees of rotation. </param>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		private static extern void DrawArc(IntPtr wand, double sx, double sy, double ex, double ey, double sd, double ed);

		/// <summary> Draw line. </summary>
		/// <param name="wand"> The wand. </param>
		/// <param name="sx"> The starting x ordinate of bounding rectangle. </param>
		/// <param name="sy"> The starting y ordinate of bounding rectangle. </param>
		/// <param name="ex"> The ending x ordinate of bounding rectangle. </param>
		/// <param name="ey"> The ending y ordinate of bounding rectangle. </param>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		private static extern void DrawLine(IntPtr wand, double sx, double sy, double ex, double ey);

		/// <summary> Draw circle. 
		/// 
		/// WandExport void DrawCircle(DrawingWand *wand,const double ox,const double oy, const double px,const double py)
		/// 
		/// </summary>
		/// <param name="wand"> The wand. </param>
		/// <param name="ox"> The origin x ordinate. </param>
		/// <param name="oy"> The origin y ordinate. </param>
		/// <param name="px"> The perimeter x ordinate. </param>
		/// <param name="py"> The perimeter y ordinate. </param>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        internal static extern void DrawCircle(IntPtr wand, double ox, double oy, double px, double py);

		/// <summary> Draw ellipse. </summary>
		/// <param name="wand"> The wand. </param>
		/// <param name="ox"> The origin x ordinate. </param>
		/// <param name="oy"> The origin y ordinate. </param>
		/// <param name="rx"> The radius of corner in horizontal direction. </param>
		/// <param name="ry"> The radius of corner in vertical direction. </param>
		/// <param name="start"> starting rotation in degrees. </param>
		/// <param name="end"> ending rotation in degrees. </param>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		private static extern void DrawEllipse(IntPtr wand, double ox, double oy, double rx, double ry, double start, double end);

		/// <summary> Draw rectangle. 
		/// 
		/// WandExport void DrawRectangle(DrawingWand *wand,const double x1,const double y1,const double x2,const double y2)
		/// 
		/// </summary>
		/// <param name="wand"> The wand. </param>
		/// <param name="x1"> The first x value. </param>
		/// <param name="y1"> The first y value. </param>
		/// <param name="x2"> The second x value. </param>
		/// <param name="y2"> The second y value. </param>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		internal static extern void DrawRectangle(IntPtr wand, double x1, double y1, double x2, double y2);

		/// <summary> Draw round rectangle. 
		/// 
		/// WandExport void DrawRoundRectangle(DrawingWand *wand,double x1,double y1,double x2,double y2,double rx,double ry)

		/// 
		/// </summary>
		/// <param name="wand"> The wand. </param>
		/// <param name="x1"> The first x value. </param>
		/// <param name="y1"> The first y value. </param>
		/// <param name="x2"> The second x value. </param>
		/// <param name="y2"> The second y value. </param>
		/// <param name="rx"> The radius of corner in horizontal direction. </param>
		/// <param name="ry"> The radius of corner in vertical direction. </param>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		internal static extern void DrawRoundRectangle(IntPtr wand, double x1, double y1, double x2, double y2, double rx, double ry);

        /// <summary> Draw affine. </summary>
        /// <param name="wand"> The wand. </param>
        /// <param name="affine"> The affine. </param>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        private static extern void DrawAffine(IntPtr wand, double[] affine);

		#endregion

		#region [Drawing Wand Methods - Text]
		/// <summary> Draw annotation. 
		/// 
		/// WandExport void DrawAnnotation(DrawingWand *wand,const double x,const double y, const unsigned char *text)
		/// 
		/// </summary>
		/// <param name="wand"> The wand. </param>
		/// <param name="x"> The x coordinate. </param>
		/// <param name="y"> The y coordinate. </param>
		/// <param name="text"> The text. </param>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		internal static extern void DrawAnnotation(IntPtr wand, double x, double y,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8Marshaler))] string text);

		/// <summary> Draw get font. 
		/// 
        /// WandExport char *DrawGetFont(const DrawingWand *wand)
		/// 
		/// </summary>
		/// <param name="wand"> The wand. </param>
		/// <returns> A string. </returns>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		internal static extern IntPtr DrawGetFont(IntPtr wand);

		/// <summary> Draw set font. 
		/// 
		/// WandExport MagickBooleanType DrawSetFont(DrawingWand *wand, const char *font_name)
		/// 
		/// </summary>
		/// <param name="wand"> The wand. </param>
		/// <param name="font_name"> Name of the font. </param>
		/// <returns> true if it succeeds, false if it fails. </returns>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        internal static extern bool DrawSetFont(IntPtr wand, string font_name);

		/// <summary> Draw get font family. </summary>
		/// <param name="wand"> The wand. </param>
		/// <returns> A string. </returns>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		private static extern string DrawGetFontFamily(IntPtr wand);

		/// <summary> Draw set font family. </summary>
		/// <param name="wand"> The wand. </param>
		/// <param name="font_family"> The font family. </param>
		/// <returns> true if it succeeds, false if it fails. </returns>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		private static extern bool DrawSetFontFamily(IntPtr wand, string font_family);

		/// <summary> Draw get font size. 
		/// 
        /// WandExport double DrawGetFontSize(const DrawingWand *wand)
		/// 
		/// </summary>
		/// <param name="wand"> The wand. </param>
		/// <returns> A double. </returns>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		internal static extern double DrawGetFontSize(IntPtr wand);

		/// <summary> Draw set font size. 
		/// 
        /// WandExport void DrawSetFontSize(DrawingWand *wand,const double pointsize)
		/// 
		/// </summary>
		/// <param name="wand"> The wand. </param>
		/// <param name="font_size"> Size of the font. </param>
		/// <returns> true if it succeeds, false if it fails. </returns>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        internal static extern bool DrawSetFontSize(IntPtr wand, double font_size);

		/// <summary> Draw get text antialias. 
		/// 
        /// WandExport MagickBooleanType DrawGetTextAntialias(const DrawingWand *wand)
		/// 
		/// </summary>
		/// <param name="wand"> The wand. </param>
		/// <returns> true if it succeeds, false if it fails. </returns>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		internal static extern bool DrawGetTextAntialias(IntPtr wand);

		/// <summary> Draw get text antialias.
		/// 
		/// WandExport void DrawSetTextAntialias(DrawingWand *wand, const MagickBooleanType text_antialias)
		/// 
		///  </summary>
		/// <param name="wand"> The wand. </param>
		/// <param name="text_antialias"> true to text antialias. </param>
		/// <returns> true if it succeeds, false if it fails. </returns>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		internal static extern bool DrawSetTextAntialias(IntPtr wand, bool text_antialias);

		/// <summary> Draw get font stretch. </summary>
		/// <param name="wand"> The wand. </param>
		/// <returns> An int. </returns>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		private static extern int DrawGetFontStretch(IntPtr wand);

		/// <summary> Draw set font stretch. </summary>
		/// <param name="wand"> The wand. </param>
		/// <param name="font_stretch"> The font stretch. </param>
		/// <returns> true if it succeeds, false if it fails. </returns>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		private static extern bool DrawSetFontStretch(IntPtr wand, int font_stretch);

	    /// <summary> Draw get font weight. </summary>
	    /// <param name="wand"> The wand. </param>
	    /// <returns> An int. </returns>
	    internal static FontWeightType DrawGetFontWeight(IntPtr wand)
	    {
	        return (FontWeightType)DrawGetFontWeightInternal(wand);
	    }


        /// <summary>
        /// WandExport size_t DrawGetFontWeight(const DrawingWand *wand)
        /// </summary>
        /// <param name="wand"></param>
        /// <returns></returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention, EntryPoint = "DrawGetFontWeight")]
		private static extern IntPtr DrawGetFontWeightInternal(IntPtr wand);

	    /// <summary> Draw set font weight. </summary>
	    /// <param name="wand"> The wand. </param>
	    /// <param name="font_weight"> The font weight. </param>
	    /// <returns> true if it succeeds, false if it fails. </returns>
	    internal static bool DrawSetFontWeight(IntPtr wand, FontWeightType font_weight)
	    {
	        return DrawSetFontWeightInternal(wand, (IntPtr)font_weight);
	    }
        
        // WandExport void DrawSetFontWeight(DrawingWand *wand, const size_t font_weight)
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention, EntryPoint = "DrawSetFontWeight")]
        private static extern bool DrawSetFontWeightInternal(IntPtr wand, IntPtr font_weight);

		/// <summary> Draw get font style. 
		/// 
        /// WandExport StyleType DrawGetFontStyle(const DrawingWand *wand)
		/// 
		/// </summary>
		/// <param name="wand"> The wand. </param>
		/// <returns> A FontStyleType. </returns>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		internal static extern FontStyleType DrawGetFontStyle(IntPtr wand);

		/// <summary> Draw set font style. 
		/// 
        /// WandExport void DrawSetFontStyle(DrawingWand *wand,const StyleType style)
		/// 
		/// </summary>
		/// <param name="wand"> The wand. </param>
		/// <param name="style"> The style. </param>
		/// <returns> true if it succeeds, false if it fails. </returns>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        internal static extern void DrawSetFontStyle(IntPtr wand, FontStyleType style);

		/// <summary> Draw get text alignment. </summary>
		/// <param name="wand"> The wand. </param>
		/// <returns> A TextAlignType. </returns>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		private static extern TextAlignType DrawGetTextAlignment(IntPtr wand);

		/// <summary> Draw set text alignment. 
		/// 
		/// WandExport void DrawSetTextAlignment(DrawingWand *wand,const AlignType alignment)
		/// 
		/// </summary>
		/// <param name="wand"> The wand. </param>
		/// <param name="style"> The style. </param>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		internal static extern void DrawSetTextAlignment(IntPtr wand, TextAlignType style);

		/// <summary> Draw get font resolution. 
		/// 
		/// WandExport MagickBooleanType DrawGetFontResolution(const DrawingWand *wand,double *x,double *y)
		/// 
		/// </summary>
		/// <param name="wand"> The wand. </param>
		/// <param name="x"> The x resolution. </param>
		/// <param name="y"> The y resolution. </param>
		/// <returns> true if it succeeds, false if it fails. </returns>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		internal static extern bool DrawGetFontResolution(IntPtr wand, out double x, out double y);

		/// <summary> Draw set font resolution.
		/// 
		/// WandExport MagickBooleanType DrawSetFontResolution(DrawingWand *wand, const double x_resolution,const double y_resolution)
		/// 
		///  </summary>
		/// <param name="wand"> The wand. </param>
		/// <param name="x"> The x resolution. </param>
		/// <param name="y"> The y resolution. </param>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		internal static extern bool DrawSetFontResolution(IntPtr wand, double x, double y);

		#endregion

		#region [Drawing Wand Methods - Colors]
		/// <summary> Draw color. </summary>
		/// <param name="wand"> The wand. </param>
		/// <param name="x"> The x coordinate. </param>
		/// <param name="y"> The y coordinate. </param>
		/// <param name="paintmethod"> The paintmethod. </param>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		private static extern void DrawColor(IntPtr wand, double x, double y, PaintMethodType paintmethod);

		/// <summary> Draw get fill color. </summary>
		/// <param name="wand"> The wand. </param>
		/// <param name="fill_color"> The fill color. </param>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		private static extern void DrawGetFillColor(IntPtr wand, out IntPtr fill_color);

		/// <summary> Draw set fill color. 
		/// 
        /// WandExport void DrawSetFillColor(DrawingWand *wand,const PixelWand *fill_wand)
		/// 
		/// </summary>
		/// <param name="wand"> The wand. </param>
		/// <param name="fill_wand"> The fill wand. </param>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		internal static extern void DrawSetFillColor(IntPtr wand, IntPtr fill_wand);

		/// <summary> Draw get stroke color. </summary>
		/// <param name="wand"> The wand. </param>
		/// <param name="stroke_color"> The stroke color. </param>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		private static extern void DrawGetStrokeColor(IntPtr wand, out IntPtr stroke_color);

		/// <summary> Draw set stroke color. 
		/// 
		/// WandExport void DrawSetStrokeColor(DrawingWand *wand, const PixelWand *stroke_wand)
		/// 
		/// </summary>
		/// <param name="wand"> The wand. </param>
		/// <param name="stroke_wand"> The stroke wand. </param>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		internal static extern void DrawSetStrokeColor(IntPtr wand, IntPtr stroke_wand);

		/// <summary> Draw get border color. </summary>
		/// <param name="wand"> The wand. </param>
		/// <param name="border_color"> The border color. </param>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		private static extern void DrawGetBorderColor(IntPtr wand, out IntPtr border_color);

		/// <summary> Draw set border color. 
		/// 
        /// WandExport void DrawSetBorderColor(DrawingWand *wand,const PixelWand *border_wand)
		/// 
		/// </summary>
		/// <param name="wand"> The wand. </param>
		/// <param name="border_color"> The border color. </param>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		internal static extern void DrawSetBorderColor(IntPtr wand, IntPtr border_color);

		/// <summary> Draw get stroke width. </summary>
		/// <param name="wand"> The wand. </param>
		/// <param name="stroke_width"> Width of the stroke. </param>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		private static extern double DrawGetStrokeWidth(IntPtr wand);

		/// <summary> Draw set stroke width. </summary>
		/// <param name="wand"> The wand. </param>
		/// <param name="stroke_width"> Width of the stroke. </param>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		private static extern void DrawSetStrokeWidth(IntPtr wand, double stroke_width);

		/// <summary> Draw get stroke opacity. </summary>
		/// <param name="wand"> The wand. </param>
		/// <param name="stroke_opacity"> The stroke opacity. </param>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		private static extern double DrawGetStrokeOpacity(IntPtr wand);

		/// <summary> Draw set stroke opacity. </summary>
		/// <param name="wand"> The wand. </param>
		/// <param name="stroke_opacity"> The stroke opacity. </param>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		private static extern void DrawSetStrokeOpacity(IntPtr wand, double stroke_opacity);

		/// <summary> Draw get text under color. </summary>
		/// <param name="wand"> The wand. </param>
		/// <param name="under_color"> The under color. </param>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		private static extern void DrawGetTextUnderColor(IntPtr wand, out IntPtr under_color);

		/// <summary> Draw set text under color. </summary>
		/// <param name="wand"> The wand. </param>
		/// <param name="under_color"> The under color. </param>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		private static extern void DrawSetTextUnderColor(IntPtr wand, IntPtr under_color);

		#endregion

		#region [Drawing Wand Methods - Exceptions]
		/// <summary> Clear any exceptions associated with the wand. 
		/// 
        /// WandExport MagickBooleanType DrawClearException(DrawingWand *wand)
		/// 
		/// </summary>
		/// <param name="wand"> The wand. </param>
		/// <returns> An IntPtr. </returns>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		internal static extern bool DrawClearException(IntPtr wand);

		/// <summary> Draw get exception. 
		/// 
		/// WandExport char *DrawGetException(const DrawingWand *wand,ExceptionType *severity)
		/// 
		/// </summary>
		/// <param name="wand"> The wand. </param>
		/// <param name="exceptionType"> Type of the exception. </param>
		/// <returns> An IntPtr. </returns>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		internal static extern IntPtr DrawGetException(IntPtr wand, out int exceptionType);

		/// <summary> Draw get exception type. </summary>
		/// <param name="wand"> The wand. </param>
		/// <returns> An int. </returns>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		private static extern int DrawGetExceptionType(IntPtr wand);

		#endregion
	}
}
