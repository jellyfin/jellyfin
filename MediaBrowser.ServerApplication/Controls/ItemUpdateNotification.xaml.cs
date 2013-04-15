using System.Linq;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MediaBrowser.ServerApplication.Controls
{
    /// <summary>
    /// Interaction logic for ItemUpdateNotification.xaml
    /// </summary>
    public partial class ItemUpdateNotification : UserControl
    {
        /// <summary>
        /// The logger
        /// </summary>
        private readonly ILogger Logger;

        /// <summary>
        /// Gets the children changed event args.
        /// </summary>
        /// <value>The children changed event args.</value>
        private BaseItem Item
        {
            get { return DataContext as BaseItem; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemUpdateNotification" /> class.
        /// </summary>
        public ItemUpdateNotification(ILogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            Logger = logger;
            
            InitializeComponent();

            Loaded += ItemUpdateNotification_Loaded;
        }

        /// <summary>
        /// Handles the Loaded event of the ItemUpdateNotification control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        void ItemUpdateNotification_Loaded(object sender, RoutedEventArgs e)
        {
            DisplayItem(Item);
        }

        /// <summary>
        /// Gets the display name.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="includeParentName">if set to <c>true</c> [include parent name].</param>
        /// <returns>System.String.</returns>
        internal static string GetDisplayName(BaseItem item, bool includeParentName)
        {
            var name = item.Name;

            if (item.ProductionYear.HasValue && !(item is Episode))
            {
                name += string.Format(" ({0})", item.ProductionYear);
            }

            var episode = item as Episode;
            if (episode != null)
            {
                var indexNumbers = new List<int>();

                if (episode.Season.IndexNumber.HasValue)
                {
                    indexNumbers.Add(episode.Season.IndexNumber.Value);
                }
                if (episode.IndexNumber.HasValue)
                {
                    indexNumbers.Add(episode.IndexNumber.Value);
                }
                var indexNumber = string.Join(".", indexNumbers.ToArray());

                name = string.Format("{0} - {1}", indexNumber, name);

                if (includeParentName)
                {
                    name = episode.Series.Name + " - " + name;
                }
            }

            if (includeParentName)
            {
                var season = item as Season;

                if (season != null)
                {
                    name = season.Series.Name + " - " + name;
                }
            }

            return name;
        }

        /// <summary>
        /// Displays the parent title.
        /// </summary>
        /// <param name="item">The item.</param>
        private void DisplayParentTitle(BaseItem item)
        {
            if (!(item is Episode || item is Season))
            {
                txtParentName.Visibility = Visibility.Collapsed;
                imgParentLogo.Visibility = Visibility.Collapsed;
                return;
            }

            var series = item is Episode ? (item as Episode).Series : (item as Season).Series;

            var logo = series.GetImage(ImageType.Logo);

            if (string.IsNullOrEmpty(logo))
            {
                imgParentLogo.Visibility = Visibility.Collapsed;
                txtParentName.Visibility = Visibility.Visible;
            }
            else
            {
                imgParentLogo.Visibility = Visibility.Visible;
                txtParentName.Visibility = Visibility.Collapsed;
                imgParentLogo.Source = App.Instance.GetBitmapImage(logo);
            }

            txtParentName.Text = series.Name;
        }

        /// <summary>
        /// Displays the title.
        /// </summary>
        /// <param name="item">The item.</param>
        private void DisplayTitle(BaseItem item)
        {
            txtName.Text = GetDisplayName(item, false);
        }

        /// <summary>
        /// Displays the item.
        /// </summary>
        /// <param name="item">The item.</param>
        private void DisplayItem(BaseItem item)
        {
            DisplayParentTitle(item);
            DisplayTitle(item);
            DisplayRating(item);

            var path = GetImagePath(item);

            if (string.IsNullOrEmpty(path))
            {
                img.Visibility = Visibility.Collapsed;
            }
            else
            {
                img.Visibility = Visibility.Visible;

                try
                {
                    img.Source = App.Instance.GetBitmapImage(path);
                }
                catch (FileNotFoundException)
                {
                    Logger.Error("Image file not found {0}", path);
                }
            }

            if (string.IsNullOrEmpty(item.Overview))
            {
                txtOverview.Visibility = Visibility.Collapsed;
            }
            else
            {
                txtOverview.Visibility = Visibility.Visible;
                txtOverview.Text = item.Overview;
            }

            if (item.Taglines == null || item.Taglines.Count == 0)
            {
                txtTagline.Visibility = Visibility.Collapsed;
            }
            else
            {
                txtTagline.Visibility = Visibility.Visible;
                txtTagline.Text = item.Taglines[0];
            }

            if (!item.PremiereDate.HasValue)
            {
                txtPremeireDate.Visibility = Visibility.Collapsed;
            }
            else
            {
                txtPremeireDate.Visibility = Visibility.Visible;
                txtPremeireDate.Text = "Premiered " + item.PremiereDate.Value.ToLocalTime().ToShortDateString();
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

        /// <summary>
        /// Displays the rating.
        /// </summary>
        /// <param name="item">The item.</param>
        private void DisplayRating(BaseItem item)
        {
            if (!item.CommunityRating.HasValue)
            {
                pnlRating.Visibility = Visibility.Collapsed;
                return;
            }

            pnlRating.Children.Clear();
            pnlRating.Visibility = Visibility.Visible;

            var rating = item.CommunityRating.Value;

            for (var i = 0; i < 10; i++)
            {
                Image image;
                if (rating < i - 1)
                {
                    image = App.Instance.GetImage(new Uri("../Resources/Images/starEmpty.png", UriKind.Relative));
                }
                else if (rating < i)
                {
                    image = App.Instance.GetImage(new Uri("../Resources/Images/starHalf.png", UriKind.Relative));
                }
                else
                {
                    image = App.Instance.GetImage(new Uri("../Resources/Images/starFull.png", UriKind.Relative));
                }

                RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.Fant);

                image.Stretch = Stretch.Uniform;
                image.Height = 16;

                pnlRating.Children.Add(image);
            }
        }
    }
}
