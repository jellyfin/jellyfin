using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Net;
using MediaBrowser.Plugins.DefaultTheme.Resources;
using MediaBrowser.UI;
using MediaBrowser.UI.Controls;
using MediaBrowser.UI.Pages;
using MediaBrowser.UI.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.DefaultTheme.Pages
{
    /// <summary>
    /// Interaction logic for HomePage.xaml
    /// </summary>
    public partial class HomePage : BaseHomePage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HomePage" /> class.
        /// </summary>
        public HomePage()
        {
            InitializeComponent();

            lstCollectionFolders.ItemInvoked += lstCollectionFolders_ItemInvoked;
        }

        /// <summary>
        /// The _favorite items
        /// </summary>
        private ItemCollectionViewModel _favoriteItems;
        /// <summary>
        /// Gets or sets the favorite items.
        /// </summary>
        /// <value>The favorite items.</value>
        public ItemCollectionViewModel FavoriteItems
        {
            get { return _favoriteItems; }

            set
            {
                _favoriteItems = value;
                OnPropertyChanged("FavoriteItems");
            }
        }

        /// <summary>
        /// The _resumable items
        /// </summary>
        private ItemCollectionViewModel _resumableItems;
        /// <summary>
        /// Gets or sets the resumable items.
        /// </summary>
        /// <value>The resumable items.</value>
        public ItemCollectionViewModel ResumableItems
        {
            get { return _resumableItems; }

            set
            {
                _resumableItems = value;
                OnPropertyChanged("ResumableItems");
            }
        }

        /// <summary>
        /// The _recently added items
        /// </summary>
        private ItemCollectionViewModel _recentlyAddedItems;
        /// <summary>
        /// Gets or sets the recently added items.
        /// </summary>
        /// <value>The recently added items.</value>
        public ItemCollectionViewModel RecentlyAddedItems
        {
            get { return _recentlyAddedItems; }

            set
            {
                _recentlyAddedItems = value;
                OnPropertyChanged("RecentlyAddedItems");
            }
        }

        /// <summary>
        /// The _recently played items
        /// </summary>
        private ItemCollectionViewModel _recentlyPlayedItems;
        /// <summary>
        /// Gets or sets the recently played items.
        /// </summary>
        /// <value>The recently played items.</value>
        public ItemCollectionViewModel RecentlyPlayedItems
        {
            get { return _recentlyPlayedItems; }

            set
            {
                _recentlyPlayedItems = value;
                OnPropertyChanged("RecentlyPlayedItems");
            }
        }

        /// <summary>
        /// The _spotlight items
        /// </summary>
        private ItemCollectionViewModel _spotlightItems;
        /// <summary>
        /// Gets or sets the spotlight items.
        /// </summary>
        /// <value>The spotlight items.</value>
        public ItemCollectionViewModel SpotlightItems
        {
            get { return _spotlightItems; }

            set
            {
                _spotlightItems = value;
                OnPropertyChanged("SpotlightItems");
            }
        }

        /// <summary>
        /// The _top picks
        /// </summary>
        private ItemCollectionViewModel _topPicks;
        /// <summary>
        /// Gets or sets the top picks.
        /// </summary>
        /// <value>The top picks.</value>
        public ItemCollectionViewModel TopPicks
        {
            get { return _topPicks; }

            set
            {
                _topPicks = value;
                OnPropertyChanged("TopPicks");
            }
        }

        /// <summary>
        /// LSTs the collection folders_ item invoked.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        void lstCollectionFolders_ItemInvoked(object sender, ItemEventArgs<object> e)
        {
            var model = e.Argument as DtoBaseItemViewModel;

            if (model != null)
            {
                App.Instance.NavigateToItem(model.Item);
            }
        }

        /// <summary>
        /// Called when [loaded].
        /// </summary>
        protected override void OnLoaded()
        {
            base.OnLoaded();

            AppResources.Instance.SetDefaultPageTitle();
        }

        /// <summary>
        /// Gets called anytime the Folder gets refreshed
        /// </summary>
        protected override void OnFolderChanged()
        {
            base.OnFolderChanged();

            Task.Run(() => RefreshSpecialItems());
        }

        /// <summary>
        /// Refreshes the special items.
        /// </summary>
        /// <returns>Task.</returns>
        private async Task RefreshSpecialItems()
        {
            var tasks = new List<Task>();

            tasks.Add(RefreshFavoriteItemsAsync());

            // In-Progress Items
            if (Folder.ResumableItemCount > 0)
            {
                tasks.Add(RefreshResumableItemsAsync());
            }
            else
            {
                SetResumableItems(new BaseItemDto[] { });
            }

            // Recently Added Items
            if (Folder.RecentlyAddedItemCount > 0)
            {
                tasks.Add(RefreshRecentlyAddedItemsAsync());
            }
            else
            {
                SetRecentlyAddedItems(new BaseItemDto[] { });
            }

            // Recently Played Items
            if (Folder.RecentlyPlayedItemCount > 0)
            {
                tasks.Add(RefreshRecentlyPlayedItemsAsync());
            }
            else
            {
                SetRecentlyPlayedItems(new BaseItemDto[] { });
            }

            tasks.Add(RefreshTopPicksAsync());
            tasks.Add(RefreshSpotlightItemsAsync());

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        /// <summary>
        /// Refreshes the favorite items async.
        /// </summary>
        /// <returns>Task.</returns>
        private async Task RefreshFavoriteItemsAsync()
        {
            var query = new ItemQuery
            {
                Filters = new[] { ItemFilter.IsFavorite },
                ImageTypes = new[] { ImageType.Backdrop, ImageType.Thumb },
                UserId = App.Instance.CurrentUser.Id,
                ParentId = Folder.Id,
                Limit = 10,
                SortBy = new[] { ItemSortBy.Random },
                Recursive = true
            };

            try
            {
                var result = await App.Instance.ApiClient.GetItemsAsync(query).ConfigureAwait(false);

                SetFavoriteItems(result.Items);
            }
            catch (HttpException)
            {
                // Already logged in lower levels
                // Don't allow the entire screen to fail
            }
        }

        /// <summary>
        /// Refreshes the resumable items async.
        /// </summary>
        /// <returns>Task.</returns>
        private async Task RefreshResumableItemsAsync()
        {
            var query = new ItemQuery
            {
                Filters = new[] { ItemFilter.IsResumable },
                ImageTypes = new[] { ImageType.Backdrop, ImageType.Thumb },
                UserId = App.Instance.CurrentUser.Id,
                ParentId = Folder.Id,
                Limit = 10,
                SortBy = new[] { ItemSortBy.DatePlayed },
                SortOrder = SortOrder.Descending,
                Recursive = true
            };

            try
            {
                var result = await App.Instance.ApiClient.GetItemsAsync(query).ConfigureAwait(false);

                SetResumableItems(result.Items);
            }
            catch (HttpException)
            {
                // Already logged in lower levels
                // Don't allow the entire screen to fail
            }
        }

        /// <summary>
        /// Refreshes the recently played items async.
        /// </summary>
        /// <returns>Task.</returns>
        private async Task RefreshRecentlyPlayedItemsAsync()
        {
            var query = new ItemQuery
            {
                Filters = new[] { ItemFilter.IsRecentlyPlayed },
                ImageTypes = new[] { ImageType.Backdrop, ImageType.Thumb },
                UserId = App.Instance.CurrentUser.Id,
                ParentId = Folder.Id,
                Limit = 10,
                SortBy = new[] { ItemSortBy.DatePlayed },
                SortOrder = SortOrder.Descending,
                Recursive = true
            };

            try
            {
                var result = await App.Instance.ApiClient.GetItemsAsync(query).ConfigureAwait(false);
                SetRecentlyPlayedItems(result.Items);
            }
            catch (HttpException)
            {
                // Already logged in lower levels
                // Don't allow the entire screen to fail
            }
        }

        /// <summary>
        /// Refreshes the recently added items async.
        /// </summary>
        /// <returns>Task.</returns>
        private async Task RefreshRecentlyAddedItemsAsync()
        {
            var query = new ItemQuery
            {
                Filters = new[] { ItemFilter.IsRecentlyAdded, ItemFilter.IsNotFolder },
                ImageTypes = new[] { ImageType.Backdrop, ImageType.Thumb },
                UserId = App.Instance.CurrentUser.Id,
                ParentId = Folder.Id,
                Limit = 10,
                SortBy = new[] { ItemSortBy.DateCreated },
                SortOrder = SortOrder.Descending,
                Recursive = true
            };

            try
            {
                var result = await App.Instance.ApiClient.GetItemsAsync(query).ConfigureAwait(false);
                SetRecentlyAddedItems(result.Items);
            }
            catch (HttpException)
            {
                // Already logged in lower levels
                // Don't allow the entire screen to fail
            }
        }

        /// <summary>
        /// Refreshes the top picks async.
        /// </summary>
        /// <returns>Task.</returns>
        private async Task RefreshTopPicksAsync()
        {
            var query = new ItemQuery
            {
                ImageTypes = new[] { ImageType.Backdrop, ImageType.Thumb },
                Filters = new[] { ItemFilter.IsRecentlyAdded, ItemFilter.IsNotFolder },
                UserId = App.Instance.CurrentUser.Id,
                ParentId = Folder.Id,
                Limit = 10,
                SortBy = new[] { ItemSortBy.Random },
                SortOrder = SortOrder.Descending,
                Recursive = true
            };

            try
            {
                var result = await App.Instance.ApiClient.GetItemsAsync(query).ConfigureAwait(false);

                TopPicks = new ItemCollectionViewModel { Items = result.Items, Name = "Top Picks" };
            }
            catch (HttpException)
            {
                // Already logged in lower levels
                // Don't allow the entire screen to fail
            }
        }

        /// <summary>
        /// Refreshes the spotlight items async.
        /// </summary>
        /// <returns>Task.</returns>
        private async Task RefreshSpotlightItemsAsync()
        {
            var query = new ItemQuery
            {
                ImageTypes = new[] { ImageType.Backdrop },
                ExcludeItemTypes = new[] { "Season" },
                UserId = App.Instance.CurrentUser.Id,
                ParentId = Folder.Id,
                Limit = 10,
                SortBy = new[] { ItemSortBy.Random },
                Recursive = true
            };

            try
            {
                var result = await App.Instance.ApiClient.GetItemsAsync(query).ConfigureAwait(false);

                SpotlightItems = new ItemCollectionViewModel(rotationPeriodMs: 6000, rotationDevaiationMs: 1000) { Items = result.Items };
            }
            catch (HttpException)
            {
                // Already logged in lower levels
                // Don't allow the entire screen to fail
            }
        }

        /// <summary>
        /// Sets the favorite items.
        /// </summary>
        /// <param name="items">The items.</param>
        private void SetFavoriteItems(BaseItemDto[] items)
        {
            FavoriteItems = new ItemCollectionViewModel { Items = items, Name = "Favorites" };
        }

        /// <summary>
        /// Sets the resumable items.
        /// </summary>
        /// <param name="items">The items.</param>
        private void SetResumableItems(BaseItemDto[] items)
        {
            ResumableItems = new ItemCollectionViewModel { Items = items, Name = "Resume" };
        }

        /// <summary>
        /// Sets the recently played items.
        /// </summary>
        /// <param name="items">The items.</param>
        private void SetRecentlyPlayedItems(BaseItemDto[] items)
        {
            RecentlyPlayedItems = new ItemCollectionViewModel { Items = items, Name = "Recently Played" };
        }

        /// <summary>
        /// Sets the recently added items.
        /// </summary>
        /// <param name="items">The items.</param>
        private void SetRecentlyAddedItems(BaseItemDto[] items)
        {
            RecentlyAddedItems = new ItemCollectionViewModel { Items = items, Name = "Recently Added" };
        }
    }
}
