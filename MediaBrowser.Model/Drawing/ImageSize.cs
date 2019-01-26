namespace MediaBrowser.Model.Drawing
{
    /// <summary>
    /// Struct ImageSize
    /// </summary>
    public struct ImageDimensions
    {
        /// <summary>
        /// Gets or sets the height.
        /// </summary>
        /// <value>The height.</value>
        public int Height { get; set; }

        /// <summary>
        /// Gets or sets the width.
        /// </summary>
        /// <value>The width.</value>
        public int Width { get; set; }

        public bool Equals(ImageDimensions size)
        {
            return Width.Equals(size.Width) && Height.Equals(size.Height);
        }

        public override string ToString()
        {
            return string.Format("{0}-{1}", Width, Height);
        }

        public ImageDimensions(int width, int height)
        {
            Width = width;
            Height = height;
        }
    }
}
