using MediaBrowser.UI.Controls;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.UI
{
    /// <summary>
    /// Interaction logic for ImageViewerWindow.xaml
    /// </summary>
    public partial class ImageViewerWindow : BaseModalWindow
    {
        /// <summary>
        /// Gets or sets the images.
        /// </summary>
        /// <value>The images.</value>
        private IEnumerable<Tuple<Uri, string>> Images { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageViewerWindow" /> class.
        /// </summary>
        /// <param name="images">The images.</param>
        public ImageViewerWindow(IEnumerable<Tuple<Uri, string>> images)
            : base()
        {
            InitializeComponent();

            Images = images;
        }

        /// <summary>
        /// Called when [loaded].
        /// </summary>
        protected override void OnLoaded()
        {
            base.OnLoaded();

            //Image.Source = App.Instance.GetBitmapImage(Images.First().Item1);
        }
    }
}
