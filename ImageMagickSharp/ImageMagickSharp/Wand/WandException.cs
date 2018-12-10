using System;
using System.Linq;

namespace ImageMagickSharp
{
	/// <summary> Exception for signalling wand errors. </summary>
	/// <seealso cref="T:System.Exception"/>
	internal class WandException : Exception 
	{
		#region [Constructors]

		/// <summary>
		/// Initializes a new instance of the ImageMagickSharp.WandException class. </summary>
		/// <param name="wand"> Handle of the wand. </param>
		internal WandException(IWandCore wand)
			: base(DecodeException(wand))
		{
		}

		#endregion

		#region [Private Methods]

		/// <summary> Decode exception. </summary>
		/// <param name="wand"> Handle of the wand. </param>
		/// <returns> A string. </returns>
		private static string DecodeException(IWandCore wand)
		{
			int exceptionSeverity;

			IntPtr exceptionPtr = wand.GetException(out exceptionSeverity);
			wand.ClearException();
			return WandNativeString.Load(exceptionPtr);
		}

		#endregion

	}
}
