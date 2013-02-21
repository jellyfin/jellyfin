using MediaBrowser.Model.Dto;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MediaBrowser.UI.Pages
{
    /// <summary>
    /// Provides a common base page for all pages
    /// </summary>
    public abstract class BasePage : Page, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public virtual void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        protected override void OnInitialized(EventArgs e)
        {
            Loaded += BasePageLoaded;
            Unloaded += BasePage_Unloaded;

            base.OnInitialized(e);

            DataContext = this;
        }

        void BasePage_Unloaded(object sender, RoutedEventArgs e)
        {
            OnUnloaded();
        }

        void BasePageLoaded(object sender, RoutedEventArgs e)
        {
            OnLoaded();
        }

        protected virtual void OnLoaded()
        {
            // Give focus to the first element
            MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
        }

        protected virtual void OnUnloaded()
        {
        }

        /// <summary>
        /// Sets the backdrop based on a BaseItemDto
        /// </summary>
        public void SetBackdrops(BaseItemDto item)
        {
            App.Instance.ApplicationWindow.SetBackdrops(item);
        }

        /// <summary>
        /// Clears current backdrops
        /// </summary>
        public void ClearBackdrops()
        {
            App.Instance.ApplicationWindow.ClearBackdrops();
        }
    }
}
