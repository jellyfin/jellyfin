using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ImageMagickSharp
{
    /// <summary> A magick core. </summary>
    /// <seealso cref="T:ImageMagickSharp.WandCore{ImageMagickSharp.MagickCore}"/>
    /// <seealso cref="T:System.IDisposable"/>
    /*internal class MagickCore : WandCore<MagickCore>, IDisposable
    {
        /// <summary> Initializes a new instance of the ImageMagickSharp.MagickWand class. </summary>
        private MagickCore()
        {
            Wand.EnsureInitialized();
        }

        /// <summary> Initializes a new instance of the ImageMagickSharp.MagickWand class. </summary>
        /// <param name="wand"> The wand. </param>
        private MagickCore(IntPtr wand)
        {
            Wand.EnsureInitialized();
            this.Handle = wand;
        }

        /// <summary> Acquires the image information. </summary>
        /// <returns> An IntPtr. </returns>
        private IntPtr AcquireImageInfo()
        {
            return MagickCoreInterop.AcquireImageInfo();
        }

        /// <summary> Acquires the exception information. </summary>
        /// <returns> An IntPtr. </returns>
        private IntPtr AcquireExceptionInfo()
        {
            return MagickCoreInterop.AcquireExceptionInfo();
        }

		/// <summary> Convert image command. </summary>
		/// <param name="image_info"> Information describing the image. </param>
		/// <param name="argc"> The argc. </param>
		/// <param name="argv"> The argv. </param>
		/// <param name="metadata"> The metadata. </param>
		/// <param name="exception"> The exception. </param>
		/// <returns> true if it succeeds, false if it fails. </returns>
		private bool ConvertImageCommand(IntPtr image_info, int argc, string[] argv, byte[] metadata, IntPtr exception)
        {
            return this.CheckError(MagickCoreInterop.ConvertImageCommand(image_info, argc, argv, metadata,out exception));
        }


        #region [Wand Methods - Exception]
        /// <summary> Gets the exception. </summary>
        /// <param name="exceptionSeverity"> The exception severity. </param>
        /// <returns> The exception. </returns>
        public override IntPtr GetException(out int exceptionSeverity)
        {
            IntPtr exceptionPtr = MagickWandInterop.MagickGetException(this, out exceptionSeverity);
            return exceptionPtr;
        }

        /// <summary> Clears the exception. </summary>
        ///
        /// ### <returns> An IntPtr. </returns>
        public override void ClearException()
        {
            MagickWandInterop.MagickClearException(this);
        }

        #endregion

        #region [IDisposable]

        /// <summary> List of images. </summary>
        private List<ImageWand> _ImageList = new List<ImageWand>();

        /// <summary> Finalizes an instance of the ImageMagickSharp.MagickWand class. </summary>
        ~MagickCore()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged
        /// resources. </summary>
        /// <seealso cref="M:System.IDisposable.Dispose()"/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the ImageMagickSharp.MagickWand and optionally
        /// releases the managed resources. </summary>
        /// <param name="disposing"> true to release both managed and unmanaged resources; false to
        /// release only unmanaged resources. </param>
        protected virtual void Dispose(bool disposing)
        {
            if (this.Handle != IntPtr.Zero)
            {
                this.Handle = MagickWandInterop.DestroyMagickWand(this);
                this.Handle = IntPtr.Zero;                
            }
        }

        #endregion

    }*/
}
