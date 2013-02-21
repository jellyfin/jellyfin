using MediaBrowser.Model.Dto;
using System;
using System.Threading;
using System.Windows;

namespace MediaBrowser.UI.ViewModels
{
    /// <summary>
    /// Represents a view model that contains multiple items.
    /// This should be used if you want to display a button or list item that holds more than one item, 
    /// and cycle through them periodically.
    /// </summary>
    public class ItemCollectionViewModel : BaseViewModel, IDisposable
    {
        private int RotationPeriodMs { get; set; }

        public ItemCollectionViewModel(int rotationPeriodMs = 10000, int rotationDevaiationMs = 2000)
            : base()
        {
            if (rotationDevaiationMs > 0)
            {
                rotationPeriodMs += new Random(Guid.NewGuid().GetHashCode()).Next(0 - rotationDevaiationMs, rotationDevaiationMs);
            }

            RotationPeriodMs = rotationPeriodMs;
        }

        /// <summary>
        /// Gets the timer that updates the current item
        /// </summary>
        private Timer CurrentItemTimer { get; set; }

        private string _name;
        /// <summary>
        /// Gets or sets the name of the collection
        /// </summary>
        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                OnPropertyChanged("Name");
            }
        }

        private BaseItemDto[] _items;
        /// <summary>
        /// Gets or sets the list of items
        /// </summary>
        public BaseItemDto[] Items
        {
            get { return _items; }
            set
            {
                _items = value ?? new BaseItemDto[] { };
                OnPropertyChanged("Items");
                CurrentItemIndex = Items.Length == 0 ? -1 : 0;

                ReloadTimer();
            }
        }

        private int _currentItemIndex;
        /// <summary>
        /// Gets or sets the index of the current item
        /// </summary>
        public int CurrentItemIndex
        {
            get { return _currentItemIndex; }
            set
            {
                _currentItemIndex = value;
                OnPropertyChanged("CurrentItemIndex");
                OnPropertyChanged("CurrentItem");
                OnPropertyChanged("NextItem");
            }
        }

        /// <summary>
        /// Gets the current item
        /// </summary>
        public BaseItemDto CurrentItem
        {
            get { return CurrentItemIndex == -1 ? null : Items[CurrentItemIndex]; }
        }

        /// <summary>
        /// Gets the next item
        /// </summary>
        public BaseItemDto NextItem
        {
            get
            {
                if (CurrentItem == null || CurrentItemIndex == -1)
                {
                    return null;
                }
                var index = CurrentItemIndex + 1;

                if (index >= Items.Length)
                {
                    index = 0;
                }

                return Items[index];
            }
        }

        /// <summary>
        /// Disposes the timer
        /// </summary>
        private void DisposeTimer()
        {
            if (CurrentItemTimer != null)
            {
                CurrentItemTimer.Dispose();
            }
        }

        /// <summary>
        /// Reloads the timer
        /// </summary>
        private void ReloadTimer()
        {
            DisposeTimer();

            // Don't bother unless there's at least two items
            if (Items.Length > 1)
            {
                CurrentItemTimer = new Timer(state => Application.Current.Dispatcher.InvokeAsync(IncrementCurrentItemIndex), null, RotationPeriodMs, RotationPeriodMs);
            }
        }

        /// <summary>
        /// Increments current item index, or resets it back to zero if we're at the end of the list
        /// </summary>
        private void IncrementCurrentItemIndex()
        {
            var newIndex = CurrentItemIndex + 1;

            if (newIndex >= Items.Length)
            {
                newIndex = 0;
            }

            CurrentItemIndex = newIndex;
        }

        /// <summary>
        /// Disposes the collection
        /// </summary>
        public void Dispose()
        {
            DisposeTimer();
        }
    }
}
