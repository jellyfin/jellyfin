using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace ImageMagickSharp
{
	/// <summary> A wand native string. </summary>
	/// <seealso cref="T:System.IDisposable"/>
	internal static class WandNativeString
	{
		#region [Methods]
        
        /// <summary> Loads. </summary>
		/// <param name="pointer"> The pointer. </param>
		/// <param name="relinquish"> true to relinquish. </param>
		/// <returns> A string. </returns>
		internal static string Load(IntPtr pointer, bool relinquish = true)
		{
			List<byte> bytes = new List<byte>();
			byte[] buf = new byte[1];
			int index = 0;
			while (true)
			{
				Marshal.Copy(pointer + index, buf, 0, 1);
				if (buf[0] == 0)
				{
					break;
				}
				bytes.Add(buf[0]);
				++index;
			}
			if (relinquish)
			{
				MagickWandInterop.MagickRelinquishMemory(pointer);
			}
			return Encoding.UTF8.GetString(bytes.ToArray());
		}

		#endregion

	}
}
