using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageMagickSharp
{
    /// <summary> A wand base. </summary>
    public abstract class WandBase
    {

		#region [Constructors]

		/// <summary> Initializes a new instance of the MagickBase class. </summary>
		/// <param name="magickWand"> . </param>
		protected WandBase(MagickWand magickWand)
		{
			_MagickWand = magickWand;
		}

		#endregion

		#region [Private Fields]

		/// <summary> The magick wand. </summary>
		private MagickWand _MagickWand;

		#endregion

		#region [private Properties]

		/// <summary> Gets the magick wand. </summary>
		/// <value> The magick wand. </value>
		internal MagickWand MagickWand
		{
			get { return _MagickWand; }
		}

		#endregion
     
    }
}
