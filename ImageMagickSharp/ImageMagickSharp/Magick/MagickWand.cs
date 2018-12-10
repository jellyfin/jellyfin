using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ImageMagickSharp
{
	/// <summary> A magick wand. </summary>
	/// <seealso cref="T:ImageMagickSharp.WandBase"/>
	/// <seealso cref="T:System.IDisposable"/>
	public class MagickWand : WandCore<MagickWand>, IDisposable
	{
		#region [Constructors]

		/// <summary> Initializes a new instance of the ImageMagickSharp.MagickWand class. </summary>
		private MagickWand()
		{
			Wand.EnsureInitialized();
			this.Handle = MagickWandInterop.NewMagickWand();
		}

		/// <summary> Initializes a new instance of the ImageMagickSharp.MagickWand class. </summary>
		/// <param name="wand"> The wand. </param>
		private MagickWand(IntPtr wand)
		{
			Wand.EnsureInitialized();
			this.Handle = wand;
		}

		/// <summary> Initializes a new instance of the ImageMagickSharp.MagickWand class. </summary>
		/// <param name="width"> The width. </param>
		/// <param name="height"> The height. </param>
		/// <param name="color"> The color. </param>
		public MagickWand(int width, int height, string color)
		{
			Wand.EnsureInitialized();
			this.Handle = MagickWandInterop.NewMagickWand();
			if (this.Handle == IntPtr.Zero)
			{
				throw new Exception("Error acquiring wand.");
			}

			this.NewImage(width, height, color);
		}

		public MagickWand(int width, int height)
		{
			Wand.EnsureInitialized();
			this.Handle = MagickWandInterop.NewMagickWand();
			if (this.Handle == IntPtr.Zero)
			{
				throw new Exception("Error acquiring wand.");
			}

			this.Size = new WandSize(width, height);
		}

		/// <summary> Initializes a new instance of the ImageMagickSharp.MagickWand class. </summary>
		/// <exception cref="Exception"> Thrown when an exception error condition occurs. </exception>
		/// <param name="width"> The width. </param>
		/// <param name="height"> The height. </param>
		/// <param name="pixelWand"> The pixel wand. </param>
		public MagickWand(int width, int height, PixelWand pixelWand)
		{
			Wand.EnsureInitialized();
			this.Handle = MagickWandInterop.NewMagickWand();
			if (this.Handle == IntPtr.Zero)
			{
				throw new Exception("Error acquiring wand.");
			}

			this.NewImage(width, height, pixelWand);
		}

		/// <summary> Initializes a new instance of the ImageMagickSharp.MagickWand class. </summary>
		/// <exception cref="Exception"> Thrown when an exception error condition occurs. </exception>
		/// <param name="path"> Full pathname of the file. </param>
		public MagickWand(string path)
		{
			Wand.EnsureInitialized();
			this.Handle = MagickWandInterop.NewMagickWand();
			if (this.Handle == IntPtr.Zero)
			{
				throw new Exception("Error acquiring wand.");
			}

			this.OpenImage(path);
		}

		public MagickWand(params string[] paths)
		{
			Wand.EnsureInitialized();
			this.Handle = MagickWandInterop.NewMagickWand();
			if (this.Handle == IntPtr.Zero)
			{
				throw new Exception("Error acquiring wand.");
			}

			this.OpenImages(paths);
		}

		#endregion

		#region [Magick Wand Properties]

		/// <summary> Gets the current image. </summary>
		/// <value> The current image. </value>
		public ImageWand CurrentImage
		{
			get
			{
				return this._ImageList[this.IteratorIndex];
			}
		}

		/// <summary> Gets a list of images. </summary>
		/// <value> A List of images. </value>
		public IEnumerable<ImageWand> ImageList
		{
			get { return _ImageList; }
		}

		/// <summary> Gets or sets the pointsize. </summary>
		/// <value> The pointsize. </value>
		/*private double Pointsize
		{
			get { return MagickWandInterop.MagickGetPointsize(this); }
			set { MagickWandInterop.MagickSetPointsize(this, value); }
		}

		/// <summary> Gets or sets the gravity. </summary>
		/// <value> The gravity. </value>
		private GravityType Gravity
		{
			get { return MagickWandInterop.MagickGetGravity(this); }
			set { this.CheckError(MagickWandInterop.MagickSetGravity(this, value)); }
		}

		/// <summary> Gets or sets a value indicating whether the antialias. </summary>
		/// <value> true if antialias, false if not. </value>
		private bool Antialias
		{
			get { return MagickWandInterop.MagickGetAntialias(this); }
			set { MagickWandInterop.MagickSetAntialias(this, value); }
		}

		/// <summary> Gets or sets the font. </summary>
		/// <value> The font. </value>
		private string Font
		{
			get { return WandNativeString.Load(MagickWandInterop.MagickGetFont(this)); }
			set { MagickWandInterop.MagickSetFont(this, value); }
		}*/

		/// <summary> Gets or sets the size. </summary>
		/// <value> The size. </value
		private WandSize Size
		{
            /* NOT WORKING
			get
			{
				int width;
				int height;
				MagickWandInterop.MagickGetSize(this, out width, out height);
				return new WandSize(width, height);
			}*/
			set
			{
				MagickWandInterop.MagickSetSize(this, value.Width, value.Height);
			}
		}

		/// <summary> Gets or sets the color of the background. </summary>
		/// <value> The color of the background. </value>
		/*private PixelWand BackgroundColor
		{
			/* NOT WORKINGget
			{
				IntPtr background = MagickWandInterop.MagickGetBackgroundColor(this);
				return new PixelWand(background);
			}*/
			/*set { this.CheckError(MagickWandInterop.MagickSetBackgroundColor(this, value)); }
		}*/
		#endregion


		#region [Magick Wand Methods]

		/// <summary> Clone magick wand. </summary>
		/// <returns> A MagickWand. </returns>
		public MagickWand CloneMagickWand()
		{
			MagickWand wand = new MagickWand(MagickWandInterop.CloneMagickWand(this));
			wand.ReloadImageList();
			return wand;
		}

		/// <summary> Reload image list. </summary>
		private void ReloadImageList()
		{
			this._ImageList.Clear();
			for (int i = 0; i < this.GetNumberImages(); i++)
			{
				this._ImageList.Add(new ImageWand(this, i));
			}
		}

		/// <summary>
		/// ClearMagickWand() clears resources associated with the wand, leaving the wand blank, and
		/// ready to be used for a new set of images. </summary>
		/*private void ClearMagickWand()
		{
			MagickWandInterop.ClearMagickWand(this);
			this._ImageList.Clear();
		}*/

		#endregion

		#region [Magick Wand Methods - Image]

		/// <summary> Creates a new image. </summary>
		/// <param name="width"> The width. </param>
		/// <param name="height"> The height. </param>
		/// <param name="pixelWand"> The pixel wand. </param>
		private void NewImage(int width, int height, PixelWand pixelWand)
		{
			this.CheckError(MagickWandInterop.MagickNewImage(this, width, height, pixelWand));
			this._ImageList.Add(new ImageWand(this, this.IteratorIndex));
		}

		/// <summary> Adds an image. </summary>
		/// <param name="wand"> The wand. </param>
		/// <returns> true if it succeeds, false if it fails. </returns>
		public bool AddImage(MagickWand wand, bool prepent = false)
		{
			if (prepent)
				this.SetFirstIterator();
			else
				this.SetLastIterator();
			bool result = this.CheckError(MagickWandInterop.MagickAddImage(this, wand));
			this.ReloadImageList();
			return result;
		}

        /// <summary> Adds the images to 'wands'. </summary>
        /// <param name="prepent"> true to prepent. </param>
        /// <param name="wands"> A variable-length parameters list containing wands. </param>
        /// <returns> true if it succeeds, false if it fails. </returns>
        /*private bool AddImages(bool prepent, params MagickWand[] wands)
        {
            if (prepent)
                this.SetFirstIterator();
            else
                this.SetLastIterator();

            bool result = true;
            foreach (MagickWand wand in wands)
            {
                result = result & this.CheckError(MagickWandInterop.MagickAddImage(this, wand));
            }
           
            this.ReloadImageList();
            return result;
        }*/

		/// <summary> Creates a new image. </summary>
		/// <param name="width"> The width. </param>
		/// <param name="height"> The height. </param>
		/// <param name="backgroundColor"> The background color. </param>
		internal void NewImage(int width, int height, string backgroundColor)
		{
			using (var pixelWand = new PixelWand(backgroundColor))
			{
				this.NewImage(width, height, pixelWand);
			}
		}

		public bool OpenImage(string path)
		{
			bool checkErrorBool =this.CheckErrorBool(MagickWandInterop.MagickReadImage(this, path));

			if (checkErrorBool)
				this._ImageList.Add(new ImageWand(this, this.IteratorIndex));
			return checkErrorBool;
		}

		/// <summary> Opens the images. </summary>
		/// <param name="paths"> A variable-length parameters list containing paths. </param>
		private void OpenImages(params string[] paths)
		{
			foreach (string path in paths)
			{
				this.OpenImage(path);
			}
		}

		/// <summary>
		/// MagickPingImage() is like MagickReadImage() except the only valid information returned is the
		/// image width, height, size, and format. It is designed to efficiently obtain this information
		/// from a file without reading the entire image sequence into memory. </summary>
		/// <param name="file_name"> Filename of the file. </param>
		/// <returns> true if it succeeds, false if it fails. </returns>
		public bool PingImage(string path)
		{
			bool checkErrorBool = this.CheckErrorBool(MagickWandInterop.MagickPingImage(this, path));

			if (checkErrorBool)
				this._ImageList.Add(new ImageWand(this, this.IteratorIndex));
			return checkErrorBool;
		}
		/// <summary> Removes the image. </summary>
		/// <returns> true if it succeeds, false if it fails. </returns>
		/*private bool RemoveImage()
		{
			bool checkErrorBool = this.CheckErrorBool(MagickWandInterop.MagickRemoveImage(this));
			if (checkErrorBool)
				this._ImageList.RemoveAt(this.IteratorIndex);
			return checkErrorBool;
		}*/

		/// <summary> Removes the image. </summary>
		/// <param name="indexs"> A variable-length parameters list containing indexs. </param>
		/*private void RemoveImage(params int[] indexs)
		{
			foreach (int index in indexs)
			{
				this.IteratorIndex = index;
				this.RemoveImage();
			}
		}*/

		/// <summary> Saves an image. </summary>
		/// <param name="path"> Full pathname of the file. </param>
		public void SaveImage(string path)
		{
            this.CheckError(MagickWandInterop.MagickWriteImage(this, path));
		}

		/// <summary> Saves the images. </summary>
		/// <param name="path"> Full pathname of the file. </param>
		/// <param name="adjoin"> true to adjoin. </param>
		public void SaveImages(string path, bool adjoin = false)
		{
            this.CheckError(MagickWandInterop.MagickWriteImages(this, path, adjoin));
		}

		/// <summary> Gets or sets the size of the page. </summary>
		/// <value> The size of the page. </value>
		/*private WandRectangle PageSize
		{
			get
			{
				int width;
				int height;
				int x;
				int y;
				MagickWandInterop.MagickGetPage(this, out width, out height, out x, out y);
				return new WandRectangle(x, y, width, height);
			}
			set
			{
				MagickWandInterop.MagickSetPage(this, value.Width, value.Height, value.X, value.Y);
			}
		}

		/// <summary> Combine images. </summary>
		/// <param name="channel"> The channel. </param>
		/// <returns> A MagickWand. </returns>
		private MagickWand CombineImages(int channel)
		{
			MagickWand wand = new MagickWand(MagickWandInterop.MagickCombineImages(this, channel));
			wand.ReloadImageList();
			return wand;
		}

		/// <summary> Merge image layers. </summary>
		/// <param name="method"> The method. </param>
		/// <returns> A MagickWand. </returns>
		private MagickWand MergeImageLayers(ImageLayerType method)
		{
			MagickWand wand = new MagickWand(MagickWandInterop.MagickMergeImageLayers(this, method));
			wand.ReloadImageList();
			return wand;
		}*/

		/// <summary> Appends the images. </summary>
		/// <param name="stack"> true to stack. </param>
		/// <returns> A MagickWand. </returns>
		public MagickWand AppendImages(bool stack = false)
		{
			this.ResetIterator();
			MagickWand wand = new MagickWand(MagickWandInterop.MagickAppendImages(this, stack));
			wand.ReloadImageList();
			return wand;
		}

		/// <summary> Queries font metrics. </summary>
		/// <param name="drawing_wand"> The drawing wand. </param>
		/// <param name="text"> The text. </param>
		/// <param name="multiline"> true to multiline. </param>
		/// <returns> The font metrics. </returns>
		public FontMetrics QueryFontMetrics(IntPtr drawing_wand, string text, bool multiline = false)
		{
			double[] rowArray = new double[13];
            // TODO: Memory leak
			IntPtr metrics;
			if (multiline)
                metrics = MagickWandInterop.MagickQueryMultilineFontMetrics(this, drawing_wand, text);
			else
				metrics = MagickWandInterop.MagickQueryFontMetrics(this, drawing_wand, text);
			Marshal.Copy(metrics, rowArray, 0, 13);

			return new FontMetrics(rowArray);
		}

		#endregion

		#region [Magick Wand Methods - Iterator]

		/// <summary> Gets or sets the zero-based index of the iterator. </summary>
		/// <value> The iterator index. </value>
		internal int IteratorIndex
		{
			get { return (int)MagickWandInterop.MagickGetIteratorIndex(this); }
			set { MagickWandInterop.MagickSetIteratorIndex(this, (IntPtr)value); }
		}

		/// <summary> Gets a value indicating whether this object has next image. </summary>
		/// <value> true if this object has next image, false if not. </value>
		/*private bool HasNextImage
		{
			get { return this.CheckError(MagickWandInterop.MagickHasNextImage(this)); }
		}

		/// <summary> Gets a value indicating whether this object has previous image. </summary>
		/// <value> true if this object has previous image, false if not. </value>
		private bool HasPreviousImage
		{
			get { return this.CheckError(MagickWandInterop.MagickHasPreviousImage(this)); }
		}

		/// <summary> Iterator set image. </summary>
		/// <param name="source"> Source for the. </param>
		/// <param name="target"> Target for the. </param>
		private void IteratorSetImage(MagickWand target)
		{
			this.CheckError(MagickWandInterop.MagickSetImage(this, target.Handle));
		}*/

		/// <summary> Resets the iterator. </summary>
		private void ResetIterator()
		{
			MagickWandInterop.MagickResetIterator(this);
		}

		/// <summary> Sets first iterator. </summary>
		public void SetFirstIterator()
		{
			MagickWandInterop.MagickSetFirstIterator(this);
		}

		/// <summary> Gets the image. </summary>
		/// <returns> The image. </returns>
		internal MagickWand GetImage()
		{
			return new MagickWand(MagickWandInterop.MagickGetImage(this));
		}

		private void SetLastIterator()
		{
			MagickWandInterop.MagickSetLastIterator(this);
		}

		/// <summary> Gets number images. </summary>
		/// <returns> The number images. </returns>
		private int GetNumberImages()
		{
			return (int)MagickWandInterop.MagickGetNumberImages(this);
		}

		/// <summary> Determines if we can next image. </summary>
		/// <returns> true if it succeeds, false if it fails. </returns>
		/*private bool NextImage()
		{
			return this.CheckError(MagickWandInterop.MagickNextImage(this));
		}

		/// <summary> Determines if we can previous image. </summary>
		/// <returns> true if it succeeds, false if it fails. </returns>
		private bool PreviousImage()
		{
			return this.CheckError(MagickWandInterop.MagickPreviousImage(this));
		}*/

		#endregion

		#region [Private Methods]



		#endregion

		#region [Wand Methods - Exception]

		/// <summary> Gets the exception. </summary>
		/// <returns> The exception. </returns>
        public override IntPtr GetException(out int exceptionSeverity)
		{
			IntPtr exceptionPtr = MagickWandInterop.MagickGetException(this, out exceptionSeverity);
			return exceptionPtr;
		}

		/// <summary> Clears the exception. </summary>
		/// <returns> An IntPtr. </returns>
        public override void ClearException()
		{
			MagickWandInterop.MagickClearException(this);
		}

		#endregion


		#region [IDisposable]

		private List<ImageWand> _ImageList = new List<ImageWand>();

		/// <summary> Finalizes an instance of the ImageMagickSharp.MagickWand class. </summary>
		~MagickWand()
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

	}
}
