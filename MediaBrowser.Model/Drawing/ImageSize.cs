using System.Globalization;

namespace MediaBrowser.Model.Drawing
{
    /// <summary>
    /// Struct ImageSize
    /// </summary>
    public struct ImageSize
    {
        private double _height;
        private double _width;

        /// <summary>
        /// Gets or sets the height.
        /// </summary>
        /// <value>The height.</value>
        public double Height
        {
            get
            {
                return _height;
            }
            set
            {
                _height = value;
            }
        }

        /// <summary>
        /// Gets or sets the width.
        /// </summary>
        /// <value>The width.</value>
        public double Width
        {
            get { return _width; }
            set { _width = value; }
        }

        public bool Equals(ImageSize size)
        {
            return Width.Equals(size.Width) && Height.Equals(size.Height);
        }

        public override string ToString()
        {
            return string.Format("{0}-{1}", Width, Height);
        }

        public ImageSize(string value)
        {
            _width = 0;

            _height = 0;

            ParseValue(value);
        }

        public ImageSize(int width, int height)
        {
            _width = width;
            _height = height;
        }

        public ImageSize(double width, double height)
        {
            _width = width;
            _height = height;
        }

        private void ParseValue(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                string[] parts = value.Split('-');

                if (parts.Length == 2)
                {
                    double val;

                    if (double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out val))
                    {
                        _width = val;
                    }

                    if (double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out val))
                    {
                        _height = val;
                    }
                }
            }
        }
    }
}