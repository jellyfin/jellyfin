using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ImageMagickSharp
{
	internal class Constants
	{

		#region [Constants]


		/// <summary> The wand calling convention. </summary>
        internal const CallingConvention WandCallingConvention = CallingConvention.Cdecl;

		/// <summary> The wand library. </summary>
        internal const string WandLibrary = @"CORE_RL_Wand_";

		/// <summary> The magick false. </summary>
        internal const int MagickFalse = 0;

		#endregion

	}
}
