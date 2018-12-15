using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ImageMagickSharp.InteropMarshaler;

namespace ImageMagickSharp
{
    /// <summary> A pixel wand interop. </summary>
    internal static class PixelWandInterop
    {

        #region [Pixel Wand]
        /// <summary> Creates a new pixel wand. 
        /// 
        /// WandExport PixelWand *NewPixelWand(void)
        /// 
        /// </summary>
        /// <returns> An IntPtr. </returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        internal static extern IntPtr NewPixelWand();

        /// <summary> Destroys the pixel wand described by wand. </summary>
        /// <param name="wand"> Handle of the wand. </param>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        private static extern void ClearPixelWand(IntPtr wand);

        /// <summary> Clone pixel wand. </summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <returns> An IntPtr. </returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        private static extern IntPtr ClonePixelWand(IntPtr wand);

        /// <summary> Destroys the pixel wand described by wand. 
        /// 
        /// WandExport PixelWand *DestroyPixelWand(PixelWand *wand)
        /// 
        /// </summary>
        /// <param name="wand"> Handle of the wand. </param>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        internal static extern IntPtr DestroyPixelWand(IntPtr wand);

        #endregion

        #region [Pixel Wand Color]
        /// <summary> Pixel set color. 
        /// 
        /// WandExport MagickBooleanType PixelSetColor(PixelWand *wand,const char *color)
        /// 
        /// </summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <param name="color"> The color. </param>
        /// <returns> An int. </returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        internal static extern bool PixelSetColor(IntPtr wand,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8Marshaler))] string color);

        /// <summary> Pixel get color as string. 
        /// 
        /// WandExport char *PixelGetColorAsString(const PixelWand *wand)
        /// 
        /// </summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <returns> An IntPtr. </returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        internal static extern IntPtr PixelGetColorAsString(IntPtr wand);

        /// <summary> Pixel get color as normalized string. 
        /// 
        /// WandExport char *PixelGetColorAsNormalizedString(const PixelWand *wand)
        /// 
        /// </summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <returns> An IntPtr. </returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        internal static extern IntPtr PixelGetColorAsNormalizedString(IntPtr wand);

        /// <summary> Pixel get alpha. 
        /// 
        /// WandExport double PixelGetAlpha(const PixelWand *wand)
        /// 
        /// </summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <returns> A double. </returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        internal static extern double PixelGetAlpha(IntPtr wand);

        /// <summary> Pixel set alpha. 
        /// 
        /// WandExport void PixelSetAlpha(PixelWand *wand,const double alpha)
        /// 
        /// </summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <param name="value"> The value. </param>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        internal static extern void PixelSetAlpha(IntPtr wand, double value);

        /// <summary> Pixel get opacity. 
        /// 
        /// WandExport double PixelGetOpacity(const PixelWand *wand)
        /// 
        /// </summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <returns> A double. </returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        internal static extern double PixelGetOpacity(IntPtr wand);

        /// <summary> Pixel set opacity. 
        /// 
        /// WandExport void PixelSetOpacity(PixelWand *wand,const double opacity)
        /// 
        /// </summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <param name="value"> The value. </param>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        internal static extern void PixelSetOpacity(IntPtr wand, double value);

        /// <summary> Pixel get red. </summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <returns> A double. </returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        private static extern double PixelGetRed(IntPtr wand);

        /// <summary> Pixel set red. </summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <param name="value"> The value. </param>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        private static extern void PixelSetRed(IntPtr wand, double value);

        /// <summary> Pixel get green. </summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <returns> A double. </returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        private static extern double PixelGetGreen(IntPtr wand);

        /// <summary> Pixel set green. </summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <param name="value"> The value. </param>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        private static extern void PixelSetGreen(IntPtr wand, double value);

        /// <summary> Pixel get blue. </summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <returns> A double. </returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        private static extern double PixelGetBlue(IntPtr wand);

        /// <summary> Pixel set blue. </summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <param name="value"> The value. </param>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        private static extern void PixelSetBlue(IntPtr wand, double value);

        #endregion

        #region [Wand Methods - Exception]
        /// <summary> Pixel clear exception. 
        /// 
        /// WandExport MagickBooleanType PixelClearException(PixelWand *wand)
        /// 
        /// </summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <returns> An int. </returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        internal static extern bool PixelClearException(IntPtr wand);

        /// <summary> Pixel get exception. 
        /// 
        /// WandExport char *PixelGetException(const PixelWand *wand,ExceptionType *severity)
        /// 
        /// </summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <param name="exceptionType"> Type of the exception. </param>
        /// <returns> An IntPtr. </returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        internal static extern IntPtr PixelGetException(IntPtr wand, out int exceptionType);

        /// <summary> Pixel get exception type. 
        /// 
        /// WandExport ExceptionType PixelGetExceptionType(const PixelWand *wand)
        /// 
        /// </summary>
        /// <param name="wand"> Handle of the wand. </param>
        /// <returns> An int. </returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        internal static extern int PixelGetExceptionType(IntPtr wand);

        #endregion


    }
}
