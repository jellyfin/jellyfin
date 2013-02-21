using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Net;
using MediaBrowser.UI;
using MediaBrowser.UI.Controls;
using MediaBrowser.UI.ViewModels;
using System;
using System.Windows;

namespace MediaBrowser.Plugins.DefaultTheme.Controls
{
    /// <summary>
    /// Interaction logic for BaseItemTile.xaml
    /// </summary>
    public partial class HomePageTile : BaseUserControl
    {
        /// <summary>
        /// Gets the view model.
        /// </summary>
        /// <value>The view model.</value>
        public DtoBaseItemViewModel ViewModel
        {
            get { return DataContext as DtoBaseItemViewModel; }
        }

        /// <summary>
        /// Gets the item.
        /// </summary>
        /// <value>The item.</value>
        private BaseItemDto Item
        {
            get { return ViewModel.Item; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HomePageTile" /> class.
        /// </summary>
        public HomePageTile()
        {
            InitializeComponent();

            DataContextChanged += BaseItemTile_DataContextChanged;
        }

        /// <summary>
        /// Handles the DataContextChanged event of the BaseItemTile control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs" /> instance containing the event data.</param>
        void BaseItemTile_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            OnItemChanged();
        }

        /// <summary>
        /// Called when [item changed].
        /// </summary>
        private void OnItemChanged()
        {
            ReloadImage();
        }

        /// <summary>
        /// Reloads the image.
        /// </summary>
        private void ReloadImage()
        {
            if (Item.HasPrimaryImage)
            {
                var url = App.Instance.ApiClient.GetImageUrl(Item, new ImageOptions
                {
                    ImageType = ImageType.Primary,
                    Height = 225
                });

                SetImage(url);
            }
            else if (Item.BackdropCount > 0)
            {
                var url = App.Instance.ApiClient.GetImageUrl(Item, new ImageOptions
                {
                    ImageType = ImageType.Backdrop,
                    Height = 225,
                    Width = 400
                });

                SetImage(url);
            }
            else if (Item.HasThumb)
            {
                var url = App.Instance.ApiClient.GetImageUrl(Item, new ImageOptions
                {
                    ImageType = ImageType.Thumb,
                    Height = 225,
                    Width = 400
                });

                SetImage(url);
            }
            else
            {
                SetDefaultImage();
            }
        }

        /// <summary>
        /// Sets the image.
        /// </summary>
        /// <param name="url">The URL.</param>
        private async void SetImage(string url)
        {
            try
            {
                image.Source = await App.Instance.GetRemoteBitmapAsync(url);
            }
            catch (HttpException)
            {
                SetDefaultImage();
            }
        }

        private void SetDefaultImage()
        {
            var imageUri = new Uri("../Resources/Images/VideoDefault.png", UriKind.Relative);
            image.Source = App.Instance.GetBitmapImage(imageUri);
        }
    }
}
