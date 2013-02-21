using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Net;
using MediaBrowser.UI;
using MediaBrowser.UI.Controls;
using MediaBrowser.UI.ViewModels;
using Microsoft.Expression.Media.Effects;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MediaBrowser.Plugins.DefaultTheme.Controls
{
    /// <summary>
    /// Interaction logic for MultiItemTile.xaml
    /// </summary>
    public partial class MultiItemTile : BaseUserControl
    {
        /// <summary>
        /// The _image width
        /// </summary>
        private int _imageWidth;
        /// <summary>
        /// Gets or sets the width of the image.
        /// </summary>
        /// <value>The width of the image.</value>
        public int ImageWidth
        {
            get { return _imageWidth; }
            set
            {
                _imageWidth = value;
                mainGrid.Width = value;
            }
        }

        /// <summary>
        /// The _image height
        /// </summary>
        private int _imageHeight;
        /// <summary>
        /// Gets or sets the height of the image.
        /// </summary>
        /// <value>The height of the image.</value>
        public int ImageHeight
        {
            get { return _imageHeight; }
            set
            {
                _imageHeight = value;
                mainGrid.Height = value;
            }
        }

        /// <summary>
        /// The _effects
        /// </summary>
        TransitionEffect[] _effects = new TransitionEffect[]
			{ 
				new BlindsTransitionEffect { Orientation = BlindOrientation.Horizontal },
				new BlindsTransitionEffect { Orientation = BlindOrientation.Vertical },
	            new CircleRevealTransitionEffect { },
				new FadeTransitionEffect { },
				new SlideInTransitionEffect {  SlideDirection= SlideDirection.TopToBottom},
				new SlideInTransitionEffect {  SlideDirection= SlideDirection.RightToLeft},
				new WipeTransitionEffect { WipeDirection = WipeDirection.RightToLeft},
				new WipeTransitionEffect { WipeDirection = WipeDirection.TopLeftToBottomRight}
			};

        /// <summary>
        /// Gets or sets the random.
        /// </summary>
        /// <value>The random.</value>
        private Random Random { get; set; }

        /// <summary>
        /// Gets the collection.
        /// </summary>
        /// <value>The collection.</value>
        public ItemCollectionViewModel Collection
        {
            get { return DataContext as ItemCollectionViewModel; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiItemTile" /> class.
        /// </summary>
        public MultiItemTile()
        {
            InitializeComponent();

            Random = new Random(Guid.NewGuid().GetHashCode());

            mainGrid.Width = ImageWidth;
            mainGrid.Height = ImageHeight;
            DataContextChanged += BaseItemTile_DataContextChanged;
        }

        /// <summary>
        /// Handles the DataContextChanged event of the BaseItemTile control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs" /> instance containing the event data.</param>
        void BaseItemTile_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            OnCurrentItemChanged();

            if (Collection != null)
            {
                Collection.PropertyChanged += Collection_PropertyChanged;
            }
        }

        /// <summary>
        /// Handles the PropertyChanged event of the Collection control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PropertyChangedEventArgs" /> instance containing the event data.</param>
        void Collection_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("CurrentItem"))
            {
                OnCurrentItemChanged();
            }
        }

        /// <summary>
        /// Called when [current item changed].
        /// </summary>
        private async void OnCurrentItemChanged()
        {
            if (Collection == null)
            {
                // Setting this to null doesn't seem to clear out the content
                transitionControl.Content = new FrameworkElement();
                txtName.Text = null;
                return;
            }

            var currentItem = Collection.CurrentItem;

            if (currentItem == null)
            {
                // Setting this to null doesn't seem to clear out the content
                transitionControl.Content = new FrameworkElement();
                txtName.Text = Collection.Name;
                return;
            }

            var img = new Image
            {
                Stretch = Stretch.Uniform,
                Width = ImageWidth,
                Height = ImageHeight
            };

            var url = GetImageSource(currentItem);

            if (!string.IsNullOrEmpty(url))
            {
                try
                {
                    img.Source = await App.Instance.GetRemoteBitmapAsync(url);
                    txtName.Text = Collection.Name ?? currentItem.Name;
                }
                catch (HttpException)
                {
                }
            }

            transitionControl.TransitionType = _effects[Random.Next(0, _effects.Length)];
            transitionControl.Content = img;
        }

        /// <summary>
        /// Gets the image source.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>Uri.</returns>
        private string GetImageSource(BaseItemDto item)
        {
            if (item != null)
            {
                if (item.BackdropCount > 0)
                {
                    return App.Instance.ApiClient.GetImageUrl(item, new ImageOptions
                    {
                        ImageType = ImageType.Backdrop,
                        Height = ImageHeight,
                        Width = ImageWidth
                    });
                }

                if (item.HasThumb)
                {
                    return App.Instance.ApiClient.GetImageUrl(item, new ImageOptions
                    {
                        ImageType = ImageType.Thumb,
                        Height = ImageHeight,
                        Width = ImageWidth
                    });
                }
                
                if (item.HasPrimaryImage)
                {
                    return App.Instance.ApiClient.GetImageUrl(item, new ImageOptions
                    {
                        ImageType = ImageType.Primary,
                        Height = ImageHeight
                    });
                }
            }

            return null;
        }
    }
}
