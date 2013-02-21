using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.UI.Controls;
using MediaBrowser.UI.ViewModels;
using System;
using System.Threading;
using System.Windows.Controls;

namespace MediaBrowser.UI.Pages
{
    /// <summary>
    /// Provides a base page for all list pages
    /// </summary>
    public abstract class BaseListPage : BaseFolderPage
    {
        /// <summary>
        /// Gets or sets the current selection timer.
        /// </summary>
        /// <value>The current selection timer.</value>
        private Timer CurrentSelectionTimer { get; set; }

        /// <summary>
        /// Subclasses must provide the list box that holds the items
        /// </summary>
        /// <value>The items list.</value>
        protected abstract ExtendedListBox ItemsList { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseListPage" /> class.
        /// </summary>
        /// <param name="itemId">The item id.</param>
        protected BaseListPage(string itemId)
            : base(itemId)
        {
        }

        /// <summary>
        /// Raises the <see cref="E:Initialized" /> event.
        /// </summary>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            ItemsList.SelectionChanged += ItemsList_SelectionChanged;
            ItemsList.ItemInvoked += ItemsList_ItemInvoked;
        }

        /// <summary>
        /// The _current item
        /// </summary>
        private BaseItemDto _currentItem;
        /// <summary>
        /// Gets or sets the current selected item
        /// </summary>
        /// <value>The current item.</value>
        public BaseItemDto CurrentItem
        {
            get { return _currentItem; }

            set
            {
                _currentItem = value;

                // Update the current item index immediately
                UpdateCurrentItemIndex(value);

                // Fire notification events after a short delay
                // We don't want backdrops and logos reloading while the user is navigating quickly
                if (CurrentSelectionTimer != null)
                {
                    CurrentSelectionTimer.Change(500, Timeout.Infinite);
                }
                else
                {
                    CurrentSelectionTimer = new Timer(CurrentItemChangedTimerCallback, value, 500, Timeout.Infinite);
                }
            }
        }

        /// <summary>
        /// Fires when the current item selection timer expires
        /// </summary>
        /// <param name="state">The state.</param>
        private void CurrentItemChangedTimerCallback(object state)
        {
            Dispatcher.InvokeAsync(() =>
            {
                // Fire notification events for the UI
                OnPropertyChanged("CurrentItem");

                // Alert subclasses
                OnCurrentItemChanged();
            });

            // Dispose the timer
            CurrentSelectionTimer.Dispose();
            CurrentSelectionTimer = null;
        }

        /// <summary>
        /// Updates the current item index based on the current selection
        /// </summary>
        /// <param name="value">The value.</param>
        private void UpdateCurrentItemIndex(BaseItemDto value)
        {
            if (value == null)
            {
                CurrentItemIndex = -1;
            }
            else
            {
                CurrentItemIndex = ItemsList.SelectedIndex;
            }
        }

        /// <summary>
        /// The _current item index
        /// </summary>
        private int _currentItemIndex;
        /// <summary>
        /// Gets of sets the index of the current item being displayed
        /// </summary>
        /// <value>The index of the current item.</value>
        public int CurrentItemIndex
        {
            get { return _currentItemIndex; }

            set
            {
                _currentItemIndex = value;
                OnPropertyChanged("CurrentItemIndex");
            }
        }

        /// <summary>
        /// Handles the list selection changed event to update the current item
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="SelectionChangedEventArgs" /> instance containing the event data.</param>
        void ItemsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                CurrentItem = (e.AddedItems[0] as DtoBaseItemViewModel).Item;
            }
            else
            {
                CurrentItem = null;
            }
        }

        /// <summary>
        /// Itemses the list_ item invoked.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        void ItemsList_ItemInvoked(object sender, ItemEventArgs<object> e)
        {
            var model = e.Argument as DtoBaseItemViewModel;

            if (model != null)
            {
                App.Instance.NavigateToItem(model.Item);
            }
        }

        /// <summary>
        /// Handles current item selection changes
        /// </summary>
        protected virtual void OnCurrentItemChanged()
        {
            if (CurrentItem != null)
            {
                SetBackdrops(CurrentItem);
            }
        }

        /// <summary>
        /// Gets called anytime a DisplayPreferences property is updated
        /// </summary>
        public override void NotifyDisplayPreferencesChanged()
        {
            base.NotifyDisplayPreferencesChanged();

            // Make sure the items list has been initialized
            if (ItemsList != null)
            {
                if (DisplayPreferences.ScrollDirection == ScrollDirection.Horizontal)
                {
                    ScrollViewer.SetHorizontalScrollBarVisibility(ItemsList, ScrollBarVisibility.Hidden);
                    ScrollViewer.SetVerticalScrollBarVisibility(ItemsList, ScrollBarVisibility.Disabled);
                }
                else
                {
                    ScrollViewer.SetHorizontalScrollBarVisibility(ItemsList, ScrollBarVisibility.Disabled);
                    ScrollViewer.SetVerticalScrollBarVisibility(ItemsList, ScrollBarVisibility.Hidden);
                }
            }
        }

    }
}
