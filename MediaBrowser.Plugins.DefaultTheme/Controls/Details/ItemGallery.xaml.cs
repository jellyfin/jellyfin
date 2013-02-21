using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Net;
using MediaBrowser.UI;
using MediaBrowser.UI.Controller;
using MediaBrowser.UI.Controls;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace MediaBrowser.Plugins.DefaultTheme.Controls.Details
{
    /// <summary>
    /// Interaction logic for ItemGallery.xaml
    /// </summary>
    public partial class ItemGallery : BaseDetailsControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ItemGallery" /> class.
        /// </summary>
        public ItemGallery()
            : base()
        {
            InitializeComponent();
            lstItems.ItemInvoked += lstItems_ItemInvoked;
        }

        /// <summary>
        /// LSTs the items_ item invoked.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        void lstItems_ItemInvoked(object sender, ItemEventArgs<object> e)
        {
            var img = (BitmapImage)e.Argument;

            var index = Images.IndexOf(img);

            //App.Instance.OpenImageViewer(new Uri(ImageUrls[index]), Item.Name);
        }

        /// <summary>
        /// The _images
        /// </summary>
        private List<BitmapImage> _images;
        /// <summary>
        /// Gets or sets the images.
        /// </summary>
        /// <value>The images.</value>
        public List<BitmapImage> Images
        {
            get { return _images; }
            set
            {
                _images = value;
                lstItems.ItemsSource = value;
                OnPropertyChanged("Images");
            }
        }

        /// <summary>
        /// Gets or sets the image urls.
        /// </summary>
        /// <value>The image urls.</value>
        private List<string> ImageUrls { get; set; }

        /// <summary>
        /// Called when [item changed].
        /// </summary>
        protected override async void OnItemChanged()
        {
            ImageUrls = GetImages(Item);

            var tasks = ImageUrls.Select(GetImage);

            var results = await Task.WhenAll(tasks);

            Images = results.Where(i => i != null).ToList();
        }

        /// <summary>
        /// Gets the image.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns>Task{BitmapImage}.</returns>
        private async Task<BitmapImage> GetImage(string url)
        {
            try
            {
                return await App.Instance.GetRemoteBitmapAsync(url);
            }
            catch (HttpException)
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the images.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>List{System.String}.</returns>
        internal static List<string> GetImages(BaseItemDto item)
        {
            var images = new List<string> { };

            if (item.BackdropCount > 0)
            {
                for (var i = 0; i < item.BackdropCount; i++)
                {
                    images.Add(UIKernel.Instance.ApiClient.GetImageUrl(item, new ImageOptions
                    {
                        ImageType = ImageType.Backdrop,
                        ImageIndex = i
                    }));
                }
            }

            if (item.HasThumb)
            {
                images.Add(UIKernel.Instance.ApiClient.GetImageUrl(item, new ImageOptions
                {
                    ImageType = ImageType.Thumb
                }));
            }

            if (item.HasArtImage)
            {
                images.Add(UIKernel.Instance.ApiClient.GetImageUrl(item, new ImageOptions
                {
                    ImageType = ImageType.Art
                }));
            }

            if (item.HasDiscImage)
            {
                images.Add(UIKernel.Instance.ApiClient.GetImageUrl(item, new ImageOptions
                {
                    ImageType = ImageType.Disc
                }));
            }

            if (item.HasMenuImage)
            {
                images.Add(UIKernel.Instance.ApiClient.GetImageUrl(item, new ImageOptions
                {
                    ImageType = ImageType.Menu
                }));
            }

            if (item.HasBoxImage)
            {
                images.Add(UIKernel.Instance.ApiClient.GetImageUrl(item, new ImageOptions
                {
                    ImageType = ImageType.Box
                }));
            }

            return images;
        }
    }
}
