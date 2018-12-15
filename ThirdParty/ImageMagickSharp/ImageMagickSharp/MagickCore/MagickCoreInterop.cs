using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ImageMagickSharp
{
    /// <summary> A magick core interop. </summary>
    /*internal class MagickCoreInterop
    {
        /// <summary> Acquires the image information. </summary>
        /// <returns> An IntPtr. </returns>
        [DllImport("CORE_RL_magick_.dll", CallingConvention = Constants.WandCallingConvention)]
        private static extern IntPtr AcquireImageInfo();

        /// <summary> Acquires the exception information. </summary>
        /// <returns> An IntPtr. </returns>
        [DllImport("CORE_RL_magick_.dll", CallingConvention = Constants.WandCallingConvention)]
        private static extern IntPtr AcquireExceptionInfo();

		/// <summary> Convert image command. </summary>
		/// <param name="image_info"> Information describing the image. </param>
		/// <param name="argc"> The argc. </param>
		/// <param name="argv"> The argv. </param>
		/// <param name="metadata"> The metadata. </param>
		/// <param name="exception"> The exception. </param>
		/// <returns> true if it succeeds, false if it fails. </returns>
        [DllImport(Constants.WandLibrary, CallingConvention = Constants.WandCallingConvention)]
		private static extern bool ConvertImageCommand(IntPtr image_info, int argc, string[] argv, byte[] metadata, out IntPtr exception);
    }*/
}
