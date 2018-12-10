/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ImageMagickSharp
{
    /// <summary> A pixel iterator interop. </summary>
    internal class PixelIteratorInterop
    {
        #region [PixelIterator Wand]
		
		/// <summary> Creates a new pixel iterator. </summary>
		/// <returns> An IntPtr. </returns>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		internal static extern IntPtr NewPixelIterator();

		/// <summary> Creates a new pixel iterator. </summary>
		/// <param name="wand"> The wand. </param>
		/// <returns> An IntPtr. </returns>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		internal static extern IntPtr NewPixelIterator(IntPtr wand);

        /// <summary> Clears the pixel iterator described by wand. </summary>
        /// <param name="wand"> The wand. </param>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        private static extern void ClearPixelIterator(IntPtr wand);

        /// <summary> Clone pixel iterator. </summary>
        /// <param name="wand"> The wand. </param>
        /// <returns> An IntPtr. </returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        private static extern IntPtr ClonePixelIterator(IntPtr wand);

        /// <summary> Destroys the pixel iterator described by wand. </summary>
        /// <param name="wand"> The wand. </param>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        private static extern void DestroyPixelIterator(IntPtr wand);

        #endregion

        #region [PixelIterator Wand - Properties]
        /// <summary> Pixel get iterator row. </summary>
        /// <param name="wand"> The wand. </param>
        /// <returns> An int. </returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        private static extern int PixelGetIteratorRow(IntPtr wand);

        /// <summary> Pixel set iterator row. </summary>
        /// <param name="wand"> The wand. </param>
        /// <param name="row"> The row. </param>
        /// <returns> true if it succeeds, false if it fails. </returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        private static extern bool PixelSetIteratorRow(IntPtr wand, int row);

        #endregion

        #region [PixelIterator Wand - Methods]
        /// <summary> Query if 'wand' is pixel iterator. </summary>
        /// <param name="wand"> The wand. </param>
        /// <returns> true if pixel iterator, false if not. </returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        private static extern bool IsPixelIterator(IntPtr wand);

        /// <summary> Creates a new pixel region iterator. </summary>
        /// <param name="wand"> The wand. </param>
        /// <param name="x"> The x coordinate. </param>
        /// <param name="y"> The y coordinate. </param>
        /// <param name="width"> The width. </param>
        /// <param name="height"> The height. </param>
        /// <returns> An IntPtr. </returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        internal static extern IntPtr NewPixelRegionIterator(IntPtr wand, int x, int y, int width, int height);

        /// <summary> Pixel get current iterator row. </summary>
        /// <param name="wand"> The wand. </param>
        /// <param name="number_wands"> Number of wands. </param>
        /// <returns> An IntPtr. </returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        private static extern IntPtr PixelGetCurrentIteratorRow(IntPtr wand, out int number_wands);

		/// <summary> Pixel get next iterator row. </summary>
		/// <param name="wand"> The wand. </param>
		/// <param name="number_wands"> Number of wands. </param>
		/// <returns> An IntPtr. </returns>
		[DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		private static extern IntPtr PixelGetNextIteratorRow(IntPtr wand, out int number_wands);

        /// <summary> Pixel get previous iterator row. </summary>
        /// <param name="wand"> The wand. </param>
        /// <param name="number_wands"> Number of wands. </param>
        /// <returns> An IntPtr. </returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        private static extern IntPtr PixelGetPreviousIteratorRow(IntPtr wand,out int number_wands);

        /// <summary> Pixel reset iterator. </summary>
        /// <param name="wand"> The wand. </param>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        private static extern void PixelResetIterator(IntPtr wand);

        /// <summary> Pixel set first iterator row. </summary>
        /// <param name="wand"> The wand. </param>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        private static extern void PixelSetFirstIteratorRow(IntPtr wand);

        /// <summary> Pixel set last iterator row. </summary>
        /// <param name="wand"> The wand. </param>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        private static extern void PixelSetLastIteratorRow(IntPtr wand);

        /// <summary> Pixel synchronise iterator. </summary>
        /// <param name="wand"> The wand. </param>
        /// <returns> true if it succeeds, false if it fails. </returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        private static extern bool PixelSyncIterator(IntPtr wand);

        #endregion

        #region [Wand Methods - Exception]
        /// <summary> Pixel clear iterator exception. </summary>
        /// <param name="wand"> The wand. </param>
        /// <returns> An int. </returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        private static extern int PixelClearIteratorException(IntPtr wand);

        /// <summary> Pixel get iterator exception. </summary>
        /// <param name="wand"> The wand. </param>
        /// <param name="exceptionType"> Type of the exception. </param>
        /// <returns> An IntPtr. </returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        private static extern IntPtr PixelGetIteratorException(IntPtr wand, out int exceptionType);

        /// <summary> Pixel get iterator exception type. </summary>
        /// <param name="wand"> The wand. </param>
        /// <returns> An int. </returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
        private static extern int PixelGetIteratorExceptionType(IntPtr wand);

        #endregion

    }
}
*/