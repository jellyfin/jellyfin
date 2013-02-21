using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Net;
using MediaBrowser.UI;
using MediaBrowser.UI.Controls;
using MediaBrowser.UI.Converters;
using MediaBrowser.UI.ViewModels;
using System;
using System.ComponentModel;
using System.Windows;

namespace MediaBrowser.Plugins.DefaultTheme.Controls
{
    /// <summary>
    /// Interaction logic for BaseItemTile.xaml
    /// </summary>
    public partial class BaseItemTile : BaseUserControl
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
        /// Initializes a new instance of the <see cref="BaseItemTile" /> class.
        /// </summary>
        public BaseItemTile()
        {
            InitializeComponent();

            DataContextChanged += BaseItemTile_DataContextChanged;
            Loaded += BaseItemTile_Loaded;
            Unloaded += BaseItemTile_Unloaded;
        }

        /// <summary>
        /// Handles the Unloaded event of the BaseItemTile control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        void BaseItemTile_Unloaded(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }
        }

        /// <summary>
        /// Handles the Loaded event of the BaseItemTile control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        void BaseItemTile_Loaded(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
                ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
        }

        /// <summary>
        /// Handles the DataContextChanged event of the BaseItemTile control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs" /> instance containing the event data.</param>
        void BaseItemTile_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            OnItemChanged();

            if (ViewModel != null)
            {
                ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
                ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
        }

        /// <summary>
        /// Handles the PropertyChanged event of the ViewModel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PropertyChangedEventArgs" /> instance containing the event data.</param>
        void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ReloadImage();
        }

        /// <summary>
        /// Called when [item changed].
        /// </summary>
        private void OnItemChanged()
        {
            ReloadImage();

            var visibility = Item.HasPrimaryImage && !Item.IsType("Episode") ? Visibility.Collapsed : Visibility.Visible;

            if (Item.IsType("Person") || Item.IsType("IndexFolder"))
            {
                visibility = Visibility.Visible;
            }

            txtName.Visibility = visibility;

            var name = Item.Name;

            if (Item.IndexNumber.HasValue)
            {
                name = Item.IndexNumber + " - " + name;
            }

            txtName.Text = name;
        }

        /// <summary>
        /// Reloads the image.
        /// </summary>
        private async void ReloadImage()
        {
            mainGrid.Height = ViewModel.ParentDisplayPreferences.PrimaryImageHeight;
            mainGrid.Width = ViewModel.ParentDisplayPreferences.PrimaryImageWidth;

            if (Item.HasPrimaryImage)
            {
                var url = ViewModel.GetImageUrl(ViewModel.ParentDisplayPreferences.PrimaryImageType);

                border.Background = null;
                
                try
                {
                    image.Source = await App.Instance.GetRemoteBitmapAsync(url);
                }
                catch (HttpException)
                {
                    SetDefaultImage();
                }
            }
            else
            {
                SetDefaultImage();
            }
        }

        /// <summary>
        /// Sets the default image.
        /// </summary>
        private void SetDefaultImage()
        {
            if (Item.IsAudio || Item.IsType("MusicAlbum") || Item.IsType("MusicArtist"))
            {
                var imageUri = new Uri("../Resources/Images/AudioDefault.png", UriKind.Relative);

                border.Background = MetroTileBackgroundConverter.GetRandomBackground();
                image.Source = App.Instance.GetBitmapImage(imageUri);
            }
            else
            {
                var imageUri = new Uri("../Resources/Images/VideoDefault.png", UriKind.Relative);

                border.Background = MetroTileBackgroundConverter.GetRandomBackground();
                image.Source = App.Instance.GetBitmapImage(imageUri);
            }
        }
    }
}
