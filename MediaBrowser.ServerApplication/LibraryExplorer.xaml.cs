using MediaBrowser.Common;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace MediaBrowser.ServerApplication
{
    /// <summary>
    /// Interaction logic for LibraryExplorer.xaml
    /// </summary>
    public partial class LibraryExplorer : Window
    {
        private readonly ILogger _logger;

        private readonly IJsonSerializer _jsonSerializer;
        private readonly ILibraryManager _libraryManager;
        private readonly IDisplayPreferencesRepository _displayPreferencesManager;

        private readonly IItemRepository _itemRepository;

        /// <summary>
        /// The current user
        /// </summary>
        private User CurrentUser;
        /// <summary>
        /// Initializes a new instance of the <see cref="LibraryExplorer" /> class.
        /// </summary>
        /// <param name="jsonSerializer">The json serializer.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="appHost">The app host.</param>
        /// <param name="userManager">The user manager.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="displayPreferencesManager">The display preferences manager.</param>
        public LibraryExplorer(IJsonSerializer jsonSerializer, ILogger logger, IApplicationHost appHost, IUserManager userManager, ILibraryManager libraryManager, IDisplayPreferencesRepository displayPreferencesManager, IItemRepository itemRepo)
        {
            _logger = logger;
            _jsonSerializer = jsonSerializer;
            _libraryManager = libraryManager;
            _displayPreferencesManager = displayPreferencesManager;

            InitializeComponent();
            lblVersion.Content = "Version: " + appHost.ApplicationVersion;
            foreach (var user in userManager.Users)
                ddlProfile.Items.Add(user);
            ddlProfile.Items.Insert(0, new User { Name = "Physical" });
            ddlProfile.SelectedIndex = 0;
            ddlIndexBy.Visibility = ddlSortBy.Visibility = lblIndexBy.Visibility = lblSortBy.Visibility = Visibility.Hidden;
            _itemRepository = itemRepo;
        }

        /// <summary>
        /// Handles the Click event of the btnLoad control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {
        }

        /// <summary>
        /// Loads the tree.
        /// </summary>
        /// <returns>Task.</returns>
        private async Task LoadTree()
        {
            tvwLibrary.Items.Clear();
            lblLoading.Visibility = Visibility.Visible;
            //grab UI context so we can update within the below task
            var ui = TaskScheduler.FromCurrentSynchronizationContext();
            //this whole async thing doesn't really work in this instance since all my work pretty much needs to be on the UI thread...
            Cursor = Cursors.Wait;
            await Task.Run(() =>
                {
                    IEnumerable<BaseItem> children = CurrentUser.Name == "Physical" ? new[] { _libraryManager.RootFolder } : _libraryManager.RootFolder.GetChildren(CurrentUser, true);
                    children = OrderByName(children, CurrentUser);

                    foreach (Folder folder in children)
                    {

                        var currentFolder = folder;
                        Task.Factory.StartNew(() =>
                         {
                             var prefs = ddlProfile.SelectedItem != null ? _displayPreferencesManager.GetDisplayPreferences(currentFolder.DisplayPreferencesId, (ddlProfile.SelectedItem as User).Id, "LibraryExplorer") ?? new DisplayPreferences { SortBy = ItemSortBy.SortName } : new DisplayPreferences { SortBy = ItemSortBy.SortName };
                             var node = new TreeViewItem { Tag = currentFolder };

                             var subChildren = currentFolder.GetChildren(CurrentUser, true);
                             subChildren = OrderByName(subChildren, CurrentUser);
                             AddChildren(node, subChildren, CurrentUser);
                             node.Header = currentFolder.Name + " (" +
                                         node.Items.Count + ")";
                             tvwLibrary.Items.Add(node);
                         }, CancellationToken.None, TaskCreationOptions.None, ui);
                    }
                });
            lblLoading.Visibility = Visibility.Hidden;
            Cursor = Cursors.Arrow;

        }

        /// <summary>
        /// Orders the name of the by.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="user">The user.</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        private IEnumerable<BaseItem> OrderByName(IEnumerable<BaseItem> items, User user)
        {
            return OrderBy(items, user, ItemSortBy.SortName);
        }

        /// <summary>
        /// Orders the name of the by.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="user">The user.</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        private IEnumerable<BaseItem> OrderBy(IEnumerable<BaseItem> items, User user, string order)
        {
            return _libraryManager.Sort(items, user, new[] { order }, SortOrder.Ascending);
        }

        /// <summary>
        /// Adds the children.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="children">The children.</param>
        /// <param name="user">The user.</param>
        private void AddChildren(TreeViewItem parent, IEnumerable<BaseItem> children, User user)
        {
            foreach (var item in children)
            {
                var node = new TreeViewItem { Tag = item };
                var subFolder = item as Folder;
                if (subFolder != null)
                {
                    var prefs = _displayPreferencesManager.GetDisplayPreferences(subFolder.DisplayPreferencesId, user.Id, "LibraryExplorer");

                    AddChildren(node, OrderBy(subFolder.GetChildren(user, true), user, prefs.SortBy), user);
                    node.Header = item.Name + " (" + node.Items.Count + ")";
                }
                else
                {
                    node.Header = item.Name;
                }
                parent.Items.Add(node);
            }
        }

        /// <summary>
        /// TVWs the library_ selected item changed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        private async void tvwLibrary_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (tvwLibrary.SelectedItem != null)
            {
                var item = (BaseItem)(tvwLibrary.SelectedItem as TreeViewItem).Tag;
                lblObjType.Content = "Type: " + item.GetType().Name;

                var movie = item as Movie;

                var folder = item as Folder;
                if (folder != null)
                {
                    lblIndexBy.Visibility = ddlIndexBy.Visibility = ddlSortBy.Visibility = lblSortBy.Visibility = Visibility.Visible;
                    ddlIndexBy.ItemsSource = folder.IndexByOptionStrings;

                    ddlSortBy.ItemsSource = new[]
                        {
                            ItemSortBy.SortName, 
                            ItemSortBy.Album, 
                            ItemSortBy.AlbumArtist, 
                            ItemSortBy.Artist, 
                            ItemSortBy.CommunityRating, 
                            ItemSortBy.DateCreated, 
                            ItemSortBy.DatePlayed, 
                            ItemSortBy.PremiereDate, 
                            ItemSortBy.ProductionYear, 
                            ItemSortBy.Random, 
                            ItemSortBy.Runtime
                        };

                    var prefs = _displayPreferencesManager.GetDisplayPreferences(folder.DisplayPreferencesId, (ddlProfile.SelectedItem as User).Id, "LibraryExplorer");

                    ddlIndexBy.SelectedItem = prefs != null
                                                  ? prefs.IndexBy ?? LocalizedStrings.Instance.GetString("NoneDispPref")
                                                  : LocalizedStrings.Instance.GetString("NoneDispPref");
                    ddlSortBy.SelectedItem = prefs != null
                                                  ? prefs.SortBy ?? ItemSortBy.SortName
                                                  : ItemSortBy.SortName;
                }
                else
                {
                    lblIndexBy.Visibility = ddlIndexBy.Visibility = ddlSortBy.Visibility = lblSortBy.Visibility = Visibility.Hidden;

                }

                var json = FormatJson(_jsonSerializer.SerializeToString(item));

                if (item is IHasMediaStreams)
                {
                    var mediaStreams = _itemRepository.GetMediaStreams(new MediaStreamQuery
                    {
                        ItemId = item.Id

                    }).ToList();

                    if (mediaStreams.Count > 0)
                    {
                        json += "\n\nMedia Streams:\n\n" + FormatJson(_jsonSerializer.SerializeToString(mediaStreams));
                    }
                }

                txtData.Text = json;

                var previews = new List<PreviewItem>();
                await Task.Run(() =>
                                   {
                                       if (!string.IsNullOrEmpty(item.PrimaryImagePath))
                                       {
                                           previews.Add(new PreviewItem(item.PrimaryImagePath, "Primary"));
                                       }
                                       if (item.HasImage(ImageType.Banner))
                                       {
                                           previews.Add(new PreviewItem(item.GetImagePath(ImageType.Banner), "Banner"));
                                       }
                                       if (item.HasImage(ImageType.Logo))
                                       {
                                           previews.Add(new PreviewItem(item.GetImagePath(ImageType.Logo), "Logo"));
                                       }
                                       if (item.HasImage(ImageType.Art))
                                       {
                                           previews.Add(new PreviewItem(item.GetImagePath(ImageType.Art), "Art"));
                                       }
                                       if (item.HasImage(ImageType.Thumb))
                                       {
                                           previews.Add(new PreviewItem(item.GetImagePath(ImageType.Thumb), "Thumb"));
                                       }
                                       previews.AddRange(
                                           item.GetImages(ImageType.Backdrop).Select(
                                               image => new PreviewItem(image.Path, "Backdrop")));
                                   });
                lstPreviews.ItemsSource = previews;
                lstPreviews.Items.Refresh();
            }
        }

        /// <summary>
        /// The INDEN t_ STRING
        /// </summary>
        private const string INDENT_STRING = "    ";
        /// <summary>
        /// Formats the json.
        /// </summary>
        /// <param name="str">The STR.</param>
        /// <returns>System.String.</returns>
        private static string FormatJson(string str)
        {
            var indent = 0;
            var quoted = false;
            var sb = new StringBuilder();
            for (var i = 0; i < str.Length; i++)
            {
                var ch = str[i];
                switch (ch)
                {
                    case '{':
                    case '[':
                        sb.Append(ch);
                        if (!quoted)
                        {
                            sb.AppendLine();
                            Enumerable.Range(0, ++indent).ForEach(item => sb.Append(INDENT_STRING));
                        }
                        break;
                    case '}':
                    case ']':
                        if (!quoted)
                        {
                            sb.AppendLine();
                            Enumerable.Range(0, --indent).ForEach(item => sb.Append(INDENT_STRING));
                        }
                        sb.Append(ch);
                        break;
                    case '"':
                        sb.Append(ch);
                        bool escaped = false;
                        var index = i;
                        while (index > 0 && str[--index] == '\\')
                            escaped = !escaped;
                        if (!escaped)
                            quoted = !quoted;
                        break;
                    case ',':
                        sb.Append(ch);
                        if (!quoted)
                        {
                            sb.AppendLine();
                            Enumerable.Range(0, indent).ForEach(item => sb.Append(INDENT_STRING));
                        }
                        break;
                    case ':':
                        sb.Append(ch);
                        if (!quoted)
                            sb.Append(" ");
                        break;
                    default:
                        sb.Append(ch);
                        break;
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Handles the SelectionChanged event of the ddlProfile control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="SelectionChangedEventArgs" /> instance containing the event data.</param>
        private void ddlProfile_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CurrentUser = ddlProfile.SelectedItem as User;
            if (CurrentUser != null)
                LoadTree().ConfigureAwait(false);
        }

        /// <summary>
        /// Handles the Click event of the btnRefresh control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            if (tvwLibrary.SelectedItem != null)
            {
                var item = ((TreeViewItem)tvwLibrary.SelectedItem).Tag as BaseItem;
                if (item != null)
                {
                    item.RefreshMetadata(new MetadataRefreshOptions { ReplaceAllMetadata = cbxForce.IsChecked.Value }, CancellationToken.None);
                    tvwLibrary_SelectedItemChanged(this, null);
                }
            }
        }

        /// <summary>
        /// Handles the SelectionChanged event of the ddlIndexBy control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="SelectionChangedEventArgs" /> instance containing the event data.</param>
        private async void ddlIndexBy_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ddlIndexBy.SelectedItem != null)
            {
                var treeItem = tvwLibrary.SelectedItem as TreeViewItem;
                var folder = treeItem != null
                                 ? treeItem.Tag as Folder
                                 : null;
                var prefs = folder != null ? _displayPreferencesManager.GetDisplayPreferences(folder.DisplayPreferencesId, CurrentUser.Id, "LibraryExplorer") : new DisplayPreferences { SortBy = ItemSortBy.SortName };
                if (folder != null && prefs.IndexBy != ddlIndexBy.SelectedItem as string)
                {
                    //grab UI context so we can update within the below task
                    var ui = TaskScheduler.FromCurrentSynchronizationContext();
                    Cursor = Cursors.Wait;
                    await Task.Factory.StartNew(() =>
                                                    {
                                                        using (
                                                            new Profiler("Explorer full index expansion for " +
                                                                         folder.Name, _logger))
                                                        {
                                                            //re-build the current item's children as an index
                                                            prefs.IndexBy = ddlIndexBy.SelectedItem as string;
                                                            treeItem.Items.Clear();
                                                            AddChildren(treeItem, OrderBy(folder.GetChildren(CurrentUser, true), CurrentUser, prefs.SortBy), CurrentUser);
                                                            treeItem.Header = folder.Name + "(" +
                                                                              treeItem.Items.Count + ")";
                                                            Cursor = Cursors.Arrow;

                                                        }
                                                    }, CancellationToken.None, TaskCreationOptions.None,
                                                ui);

                }
            }
        }

        /// <summary>
        /// Handles the SelectionChanged event of the ddlSortBy control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="SelectionChangedEventArgs" /> instance containing the event data.</param>
        private async void ddlSortBy_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ddlSortBy.SelectedItem != null)
            {
                var treeItem = tvwLibrary.SelectedItem as TreeViewItem;
                var folder = treeItem != null
                                 ? treeItem.Tag as Folder
                                 : null;
                var prefs = folder != null ? _displayPreferencesManager.GetDisplayPreferences(folder.DisplayPreferencesId, CurrentUser.Id, "LibraryExplorer") : new DisplayPreferences();
                if (folder != null && prefs.SortBy != ddlSortBy.SelectedItem as string)
                {
                    //grab UI context so we can update within the below task
                    var ui = TaskScheduler.FromCurrentSynchronizationContext();
                    Cursor = Cursors.Wait;
                    await Task.Factory.StartNew(() =>
                                                    {
                                                        using (
                                                            new Profiler("Explorer sorting by " + ddlSortBy.SelectedItem + " for " +
                                                                         folder.Name, _logger))
                                                        {
                                                            //re-sort
                                                            prefs.SortBy = ddlSortBy.SelectedItem as string;
                                                            treeItem.Items.Clear();
                                                            AddChildren(treeItem, OrderBy(folder.GetChildren(CurrentUser, true), CurrentUser, prefs.SortBy ?? ItemSortBy.SortName), CurrentUser);
                                                            treeItem.Header = folder.Name + "(" +
                                                                              treeItem.Items.Count + ")";
                                                            Cursor = Cursors.Arrow;

                                                        }
                                                    }, CancellationToken.None, TaskCreationOptions.None,
                                                ui);

                }
            }
        }

    }

    /// <summary>
    /// Class PreviewItem
    /// </summary>
    public class PreviewItem
    {

        /// <summary>
        /// The preview
        /// </summary>
        private readonly string preview;
        /// <summary>
        /// The name
        /// </summary>
        private readonly string name;

        /// <summary>
        /// Gets the preview.
        /// </summary>
        /// <value>The preview.</value>
        public string Preview
        {
            get { return preview; }
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get { return name; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PreviewItem" /> class.
        /// </summary>
        /// <param name="p">The p.</param>
        /// <param name="n">The n.</param>
        public PreviewItem(string p, string n)
        {
            preview = p;
            name = n;
        }
    }

    /// <summary>
    /// Class Extensions
    /// </summary>
    static class Extensions
    {
        /// <summary>
        /// Fors the each.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ie">The ie.</param>
        /// <param name="action">The action.</param>
        public static void ForEach<T>(this IEnumerable<T> ie, Action<T> action)
        {
            foreach (var i in ie)
            {
                action(i);
            }
        }
    }
    #region ItemToImageConverter

    /// <summary>
    /// Class ItemToImageConverter
    /// </summary>
    [ValueConversion(typeof(string), typeof(bool))]
    public class ItemToImageConverter : IValueConverter
    {
        /// <summary>
        /// The instance
        /// </summary>
        public static ItemToImageConverter Instance =
            new ItemToImageConverter();

        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <param name="value">The value produced by the binding source.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A converted value. If the method returns null, the valid null value is used.</returns>
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            var item = value as BaseItem ?? new Folder();
            switch (item.DisplayMediaType)
            {
                case "DVD":
                case "HD DVD":
                case "Blu-ray":
                case "Blu-Ray":
                case "Movie":
                    {
                        var uri = new Uri
                            ("pack://application:,,,/Resources/Images/movie.png");
                        var source = new BitmapImage(uri);
                        return source;
                    }
                case "Series":
                    {
                        var uri = new Uri
                            ("pack://application:,,,/Resources/Images/series.png");
                        var source = new BitmapImage(uri);
                        return source;
                    }
                case "Season":
                    {
                        var uri = new Uri
                            ("pack://application:,,,/Resources/Images/season.png");
                        var source = new BitmapImage(uri);
                        return source;
                    }
                case "Episode":
                    {
                        var uri = new Uri
                            ("pack://application:,,,/Resources/Images/episode.png");
                        var source = new BitmapImage(uri);
                        return source;
                    }
                case "BoxSet":
                    {
                        var uri = new Uri
                            ("pack://application:,,,/Resources/Images/boxset.png");
                        var source = new BitmapImage(uri);
                        return source;
                    }
                case "Audio":
                    {
                        var uri = new Uri
                            ("pack://application:,,,/Resources/Images/audio.png");
                        var source = new BitmapImage(uri);
                        return source;
                    }
                case "Person":
                    {
                        var uri = new Uri
                            ("pack://application:,,,/Resources/Images/persons.png");
                        var source = new BitmapImage(uri);
                        return source;
                    }
                case "MusicArtist":
                    {
                        var uri = new Uri
                            ("pack://application:,,,/Resources/Images/artist.png");
                        var source = new BitmapImage(uri);
                        return source;
                    }
                case "MusicAlbum":
                    {
                        var uri = new Uri
                            ("pack://application:,,,/Resources/Images/album.png");
                        var source = new BitmapImage(uri);
                        return source;
                    }
                case "Trailer":
                    {
                        var uri = new Uri
                            ("pack://application:,,,/Resources/Images/trailer.png");
                        var source = new BitmapImage(uri);
                        return source;
                    }
                case "None":
                    {
                        Uri uri;
                        if (item is Movie)
                            uri = new Uri("pack://application:,,,/Resources/Images/movie.png");
                        else if (item is Series)
                            uri = new Uri("pack://application:,,,/Resources/Images/series.png");
                        else if (item is BoxSet)
                            uri = new Uri("pack://application:,,,/Resources/Images/boxset.png");
                        else
                            uri = new Uri("pack://application:,,,/Resources/Images/folder.png");

                        return new BitmapImage(uri);
                    }
                default:
                    {
                        var uri = new Uri("pack://application:,,,/Resources/Images/folder.png");
                        var source = new BitmapImage(uri);
                        return source;
                    }
            }
        }

        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <param name="value">The value that is produced by the binding target.</param>
        /// <param name="targetType">The type to convert to.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A converted value. If the method returns null, the valid null value is used.</returns>
        /// <exception cref="System.NotSupportedException">Cannot convert back</exception>
        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("Cannot convert back");
        }
    }

    #endregion // ItemToImageConverter
}
