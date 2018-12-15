using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageMagickSharp
{
	/// <summary> A pixel wand. </summary>
	/// <seealso cref="T:ImageMagickSharp.WandBase"/>
	/// <seealso cref="T:System.IDisposable"/>
    public class PixelWand : WandCore<DrawingWand>, IDisposable
	{
		#region [Constructors]

		/// <summary> Initializes a new instance of the ImageMagickSharp.PixelWand class. </summary>
		/// <param name="color"> The color. </param>
		public PixelWand(string color, double opacity = 0)
			: this()
		{
			this.Color = color;
			this.Opacity = opacity;
		}

		/// <summary> Initializes a new instance of the ImageMagickSharp.PixelWand class. </summary>
		/// <exception cref="Exception"> Thrown when an exception error condition occurs. </exception>
		public PixelWand()
		{
			this.Handle = PixelWandInterop.NewPixelWand();
			if (this.Handle == IntPtr.Zero)
			{
				throw new Exception("Error acquiring pixel wand.");
			}
		}
		/// <summary>
		/// Initializes a new instance of the PixelWand class.
		/// </summary>
		/// <param name="handle"></param>
		private PixelWand(IntPtr handle)
			: base(handle)
		{

		}

		#endregion

		#region [Properties]
		/// <summary> Gets or sets the color. </summary>
		/// <value> The color. </value>
		public string Color
		{
			get { return WandNativeString.Load(PixelWandInterop.PixelGetColorAsString(this)); }
			set { this.CheckError(PixelWandInterop.PixelSetColor(this, value)); }
		}

		/// <summary> Gets the color of the normalized. </summary>
		/// <value> The color of the normalized. </value>
		private string NormalizedColor
		{
			get
			{
				return WandNativeString.Load(PixelWandInterop.PixelGetColorAsNormalizedString(this));
			}
		}

		#endregion

		#region [Methods]
		/// <summary> From a RGB. </summary>
		/// <param name="alpha"> The alpha. </param>
		/// <param name="red"> The red. </param>
		/// <param name="green"> The green. </param>
		/// <param name="blue"> The blue. </param>
		/// <returns> A PixelWand. </returns>
		/*private static PixelWand FromARGB(double alpha, double red, double green, double blue)
		{
			return new PixelWand()
			{
				Alpha = alpha,
				Red = red,
				Green = green,
				Blue = blue,
			};
		}

		/// <summary> From RGB. </summary>
		/// <param name="red"> The red. </param>
		/// <param name="green"> The green. </param>
		/// <param name="blue"> The blue. </param>
		/// <returns> A PixelWand. </returns>
		private static PixelWand FromRGB(double red, double green, double blue)
		{
			return new PixelWand()
			{
				Red = red,
				Green = green,
				Blue = blue,
			};
		}*/

		#endregion

		#region [Pixel Wand]

		/// <summary> Clears the pixel wand. </summary>
/*		private void ClearPixelWand()
		{
			PixelWandInterop.ClearPixelWand(this);
		}

		/// <summary> Clone pixel wand. </summary>
		/// <returns> A PixelWand. </returns>
		private PixelWand ClonePixelWand()
		{
			return new PixelWand(PixelWandInterop.ClonePixelWand(this));
		}*/

		/// <summary> Destroys the pixel wand. </summary>
		private void DestroyPixelWand()
		{
			PixelWandInterop.DestroyPixelWand(this);
		}

		#endregion

		#region [Pixel Wand Properties - RGB]
		/// <summary> Gets or sets the alpha. </summary>
		/// <value> The alpha. </value>
		private double Alpha
		{
			get { return PixelWandInterop.PixelGetAlpha(this); }
			set { PixelWandInterop.PixelSetAlpha(this, value); }
		}

		/// <summary> Gets or sets the opacity. </summary>
		/// <value> The opacity. </value>
		public double Opacity
		{
			get { return PixelWandInterop.PixelGetOpacity(this); }
			set { PixelWandInterop.PixelSetOpacity(this, value); }
		}

		/// <summary> Gets or sets the red. </summary>
		/// <value> The red. </value>
		/*private double Red
		{
			get { return PixelWandInterop.PixelGetRed(this); }
			set { PixelWandInterop.PixelSetRed(this, value); }
		}

		/// <summary> Gets or sets the green. </summary>
		/// <value> The green. </value>
		private double Green
		{
			get { return PixelWandInterop.PixelGetGreen(this); }
			set { PixelWandInterop.PixelSetGreen(this, value); }
		}

		/// <summary> Gets or sets the blue. </summary>
		/// <value> The blue. </value>
		private double Blue
		{
			get { return PixelWandInterop.PixelGetBlue(this); }
			set { PixelWandInterop.PixelSetBlue(this, value); }
		}*/
		#endregion

		#region [Wand Methods - Exception]

		/// <summary> Gets the exception. </summary>
		/// <returns> The exception. </returns>
        public override IntPtr GetException(out int exceptionSeverity)
		{
			IntPtr exceptionPtr = PixelWandInterop.PixelGetException(this, out exceptionSeverity);
			return exceptionPtr;
		}

		/// <summary> Clears the exception. </summary>
		/// <returns> An IntPtr. </returns>
        public override void ClearException()
		{
			PixelWandInterop.PixelClearException(this);
		}

		#endregion

		#region [Pixel Wand Operators]
		
		/// <summary> Implicit cast that converts the given PixelWand to a string. </summary>
		/// <param name="wand"> The wand. </param>
		/// <returns> The result of the operation. </returns>
		public static implicit operator string(PixelWand wand)
		{
			return wand.Color;
		}

		#endregion

		#region [IDisposable]
		/// <summary> Finalizes an instance of the ImageMagickSharp.MagickWand class. </summary>
		~PixelWand()
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
		/// Releases the unmanaged resources used by the ImageMagickSharp.PixelWand and optionally
		/// releases the managed resources. </summary>
		/// <param name="disposing"> true to release both managed and unmanaged resources; false to
		/// release only unmanaged resources. </param>
		protected virtual void Dispose(bool disposing)
		{
            if (this.Handle != IntPtr.Zero)
			{
				PixelWandInterop.DestroyPixelWand(this);
				this.Handle = IntPtr.Zero;				

			}
		}
		#endregion

	}
}
