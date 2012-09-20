using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;

namespace MediaBrowser.UI.Pages
{
    public abstract class BasePage : Page, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        protected Uri Uri
        {
            get
            {
                return NavigationService.CurrentSource;
            }
        }

        protected MainWindow MainWindow
        {
            get
            {
                return App.Instance.MainWindow as MainWindow;
            }
        }

        private NameValueCollection _queryString;
        protected NameValueCollection QueryString
        {
            get
            {
                if (_queryString == null)
                {
                    string url = Uri.ToString();

                    int index = url.IndexOf('?');

                    if (index == -1)
                    {
                        _queryString = new NameValueCollection();
                    }
                    else
                    {
                        _queryString = HttpUtility.ParseQueryString(url.Substring(index + 1));
                    }
                }

                return _queryString;
            }
        }

        protected BasePage()
            : base()
        {
            Loaded += BasePageLoaded;
        }

        async void BasePageLoaded(object sender, RoutedEventArgs e)
        {
            await LoadData();

            DataContext = this;
        }

        protected abstract Task LoadData();
    }
}
