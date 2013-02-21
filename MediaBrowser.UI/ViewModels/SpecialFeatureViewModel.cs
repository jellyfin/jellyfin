using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Net;
using System;
using System.Windows.Media.Imaging;

namespace MediaBrowser.UI.ViewModels
{
    /// <summary>
    /// Class SpecialFeatureViewModel
    /// </summary>
    public class SpecialFeatureViewModel : BaseViewModel
    {
        /// <summary>
        /// Gets or sets the image download options.
        /// </summary>
        /// <value>The image download options.</value>
        public ImageOptions ImageDownloadOptions { get; set; }

        /// <summary>
        /// The _image width
        /// </summary>
        private double _imageWidth;
        /// <summary>
        /// Gets or sets the width of the image.
        /// </summary>
        /// <value>The width of the image.</value>
        public double ImageWidth
        {
            get { return _imageWidth; }

            set
            {
                _imageWidth = value;
                OnPropertyChanged("ImageWidth");
            }
        }

        /// <summary>
        /// The _image height
        /// </summary>
        private double _imageHeight;
        /// <summary>
        /// Gets or sets the height of the image.
        /// </summary>
        /// <value>The height of the image.</value>
        public double ImageHeight
        {
            get { return _imageHeight; }

            set
            {
                _imageHeight = value;
                OnPropertyChanged("ImageHeight");
            }
        }
        
        /// <summary>
        /// The _item
        /// </summary>
        private BaseItemDto _item;
        /// <summary>
        /// Gets or sets the item.
        /// </summary>
        /// <value>The item.</value>
        public BaseItemDto Item
        {
            get { return _item; }

            set
            {
                _item = value;
                OnPropertyChanged("Item");
                OnItemChanged();
            }
        }

        /// <summary>
        /// Gets the time string.
        /// </summary>
        /// <value>The time string.</value>
        public string MinutesString
        {
            get
            {
                var time = TimeSpan.FromTicks(Item.RunTimeTicks ?? 0);

                var minutes = Math.Round(time.TotalMinutes);

                if (minutes <= 1)
                {
                    return "1 minute";
                }

                return string.Format("{0} minutes", minutes);
            }
        }

        /// <summary>
        /// The _image
        /// </summary>
        private BitmapImage _image;
        /// <summary>
        /// Gets the image.
        /// </summary>
        /// <value>The image.</value>
        public BitmapImage Image
        {
            get { return _image; }
            set
            {
                _image = value;
                OnPropertyChanged("Image");
            }
        }

        /// <summary>
        /// Called when [item changed].
        /// </summary>
        private async void OnItemChanged()
        {
            var options = ImageDownloadOptions ?? new ImageOptions { };

            options.ImageType = ImageType.Primary;

            try
            {
                Image = await App.Instance.GetRemoteBitmapAsync(App.Instance.ApiClient.GetImageUrl(Item, options));
            }
            catch (HttpException)
            {
            }
        }
    }
}
