using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageMagickSharp
{
	/// <summary> A wand size. </summary>
	internal struct WandSize
	{
		#region [Constructors]
	
		/// <summary>
		/// Initializes a new instance of the WandSize structure.
		/// </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		internal WandSize(int width, int height)
			: this()
		{
			Width = width;
			Height = height;
		}

		#endregion

		#region [private Properties]
		/// <summary> Gets or sets the width. </summary>
		/// <value> The width. </value>
		internal int Width { get; set; }

		/// <summary> Gets or sets the height. </summary>
		/// <value> The height. </value>
		internal int Height { get; set; }

		#endregion

	}

	/// <summary> A wand size double. </summary>
	internal struct WandSizeD
	{
		#region [Constructors]
		/// <summary>
		/// Initializes a new instance of the WandSizeD structure.
		/// </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		private WandSizeD(double width, double height)
			: this()
		{
			Width = width;
			Height = height;
		}

		#endregion

		#region [private Properties]
		/// <summary> Gets or sets the width. </summary>
		/// <value> The width. </value>
		private double Width { get; set; }

		/// <summary> Gets or sets the height. </summary>
		/// <value> The height. </value>
		private double Height { get; set; }

		#endregion

	}

	/// <summary> A wand point. </summary>
	internal struct WandPoint
	{
		#region [Constructors]
		
		/// <summary> Initializes a new instance of the WandPoint structure. </summary>
		/// <param name="x"> The x coordinate. </param>
		/// <param name="y"> The y coordinate. </param>
		private WandPoint(int x, int y)
			: this()
		{
			X = x;
			Y = y;
		}

		#endregion

		#region [Properties]
		/// <summary> Gets or sets the x coordinate. </summary>
		/// <value> The x coordinate. </value>
		private int X { get; set; }

		/// <summary> Gets or sets the y coordinate. </summary>
		/// <value> The y coordinate. </value>
		private int Y { get; set; }

		#endregion

	}

	/// <summary> A wand point double. </summary>
	public struct WandPointD
	{
		#region [Constructors]
		
		/// <summary>
		/// Initializes a new instance of the ImageMagickSharp.Global.WandPointD struct. </summary>
		/// <param name="x"> The x coordinate. </param>
		/// <param name="y"> The y coordinate. </param>
		public WandPointD(double x, double y)
			: this()
		{
			X = x;
			Y = y;
		}

		#endregion

		#region [Properties]
		/// <summary> Gets or sets the x coordinate. </summary>
		/// <value> The x coordinate. </value>
		internal double X { get; set; }

		/// <summary> Gets or sets the y coordinate. </summary>
		/// <value> The y coordinate. </value>
		internal double Y { get; set; }

		#endregion

	}

	internal struct WandRectangle
	{
		#region [Constructors]
		
		/// <summary>
		/// Initializes a new instance of the ImageMagickSharp.Global.WandRectangle struct. </summary>
		/// <param name="x"> The x coordinate. </param>
		/// <param name="y"> The y coordinate. </param>
		/// <param name="width"> The width. </param>
		/// <param name="height"> The height. </param>
		internal WandRectangle(int x, int y, int width, int height)
			: this()
		{
			X = x;
			Y = y;
			Width = width;
			Height = height;
		}
		#endregion

		#region [private Properties]

		/// <summary> Gets or sets the x coordinate. </summary>
		/// <value> The x coordinate. </value>
        internal int X { get; set; }

		/// <summary> Gets or sets the y coordinate. </summary>
		/// <value> The y coordinate. </value>
        internal int Y { get; set; }

		/// <summary> Gets or sets the width. </summary>
		/// <value> The width. </value>
		internal int Width { get; set; }

		/// <summary> Gets or sets the height. </summary>
		/// <value> The height. </value>
        internal int Height { get; set; }
		#endregion
	}

	internal struct WandRectangleD
	{
		#region [Constructors]
		/// <summary>
		/// Initializes a new instance of the ImageMagickSharp.Global.WandRectangleD struct. </summary>
		/// <param name="x"> The x coordinate. </param>
		/// <param name="y"> The y coordinate. </param>
		/// <param name="width"> The width. </param>
		/// <param name="height"> The height. </param>
		private WandRectangleD(double x, double y, double width, double height)
			: this()
		{
			X = x;
			Y = y;
			Width = width;
			Height = height;
		}

		#endregion

		#region [private Properties]

		/// <summary> Gets or sets the x coordinate. </summary>
		/// <value> The x coordinate. </value>
		private double X { get; set; }

		/// <summary> Gets or sets the y coordinate. </summary>
		/// <value> The y coordinate. </value>
		private double Y { get; set; }

		/// <summary> Gets or sets the width. </summary>
		/// <value> The width. </value>
		private double Width { get; set; }

		/// <summary> Gets or sets the height. </summary>
		/// <value> The height. </value>
		private double Height { get; set; }

		#endregion
	}
}
