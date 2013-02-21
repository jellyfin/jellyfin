using MediaBrowser.Common.Logging;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MediaBrowser.ServerApplication.Controls
{
    /// <summary>
    /// Interaction logic for MultiItemUpdateNotification.xaml
    /// </summary>
    public partial class MultiItemUpdateNotification : UserControl
    {
        /// <summary>
        /// The logger
        /// </summary>
        private static readonly ILogger Logger = LogManager.GetLogger("MultiItemUpdateNotification");

        /// <summary>
        /// Gets the children changed event args.
        /// </summary>
        /// <value>The children changed event args.</value>
        private List<BaseItem> Items
        {
            get { return DataContext as List<BaseItem>; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiItemUpdateNotification" /> class.
        /// </summary>
        public MultiItemUpdateNotification()
        {
            InitializeComponent();

            Loaded += MultiItemUpdateNotification_Loaded;
        }

        /// <summary>
        /// Handles the Loaded event of the MultiItemUpdateNotification control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        void MultiItemUpdateNotification_Loaded(object sender, RoutedEventArgs e)
        {
            header.Text = string.Format("{0} New Items!", Items.Count);

            PopulateItems();
        }

        /// <summary>
        /// Populates the items.
        /// </summary>
        private void PopulateItems()
        {
            itemsPanel.Children.Clear();

            var items = Items;

            const int maxItemsToDisplay = 8;
            var index = 0;

            foreach (var item in items)
            {
                if (index >= maxItemsToDisplay)
                {
                    break;
                }

                // Try our best to find an image
                var path = GetImagePath(item);

                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                Image img;

                try
                {
                    img = App.Instance.GetImage(path);
                }
                catch (FileNotFoundException)
                {
                    Logger.Error("Image file not found {0}", path);
                    continue;
                }

                img.Stretch = Stretch.Uniform;
                img.Margin = new Thickness(0, 0, 5, 5);
                img.ToolTip = ItemUpdateNotification.GetDisplayName(item, true);
                RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.Fant);
                itemsPanel.Children.Add(img);

                index++;
            }
        }



        /// <summary>
        /// Gets the image path.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        internal static string GetImagePath(BaseItem item)
        {
            // Try our best to find an image
            var path = item.PrimaryImagePath;

            if (string.IsNullOrEmpty(path) && item.BackdropImagePaths != null)
            {
                path = item.BackdropImagePaths.FirstOrDefault();
            }

            if (string.IsNullOrEmpty(path))
            {
                path = item.GetImage(ImageType.Thumb);
            }

            if (string.IsNullOrEmpty(path))
            {
                path = item.GetImage(ImageType.Art);
            }

            if (string.IsNullOrEmpty(path))
            {
                path = item.GetImage(ImageType.Logo);
            }

            if (string.IsNullOrEmpty(path))
            {
                path = item.GetImage(ImageType.Disc);
            }

            return path;
        }
    }
}
