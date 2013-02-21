using MediaBrowser.Model.DTO;
using MediaBrowser.UI.Controls;
using System;
using System.Threading;
using System.Windows;

namespace MediaBrowser.Plugins.DefaultTheme.Model
{
    public class VirtualCollection : ModelItem, IDisposable
    {
        private string _name;
        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                OnPropertyChanged("Name");
            }
        }

        private DtoBaseItem[] _items;
        public DtoBaseItem[] Items
        {
            get { return _items; }
            set
            {
                _items = value;
                OnPropertyChanged("Items");
                CurrentItemIndex = Items.Length == 0 ? -1 : 0;

                ReloadTimer();
            }
        }

        private int _currentItemIndex;
        public int CurrentItemIndex
        {
            get { return _currentItemIndex; }
            set
            {
                _currentItemIndex = value;
                OnPropertyChanged("CurrentItemIndex");
                OnPropertyChanged("CurrentItem");
            }
        }

        public DtoBaseItem CurrentItem
        {
            get { return CurrentItemIndex == -1 ? null : Items[CurrentItemIndex]; }
        }

        private Timer CurrentItemTimer { get; set; }

        private void DisposeTimer()
        {
            if (CurrentItemTimer != null)
            {
                CurrentItemTimer.Dispose();
            }
        }

        private void ReloadTimer()
        {
            DisposeTimer();

            if (Items.Length > 0)
            {
                CurrentItemTimer = new Timer(state => Application.Current.Dispatcher.InvokeAsync(() => IncrementCurrentItemIndex()), null, 5000, 5000);
            }
        }

        private void IncrementCurrentItemIndex()
        {
            var newIndex = CurrentItemIndex + 1;

            if (newIndex >= Items.Length)
            {
                newIndex = 0;
            }

            CurrentItemIndex = newIndex;
        }

        public void Dispose()
        {
            DisposeTimer();
        }
    }
}
