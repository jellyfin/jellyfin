using MediaBrowser.Model.DTO;
using MediaBrowser.UI.Controller;
using System;
using System.Threading.Tasks;

namespace MediaBrowser.UI.Pages
{
    public class BaseLoginPage : BasePage
    {
        private DtoUser[] _users;
        public DtoUser[] Users
        {
            get { return _users; }

            set
            {
                _users = value;
                OnPropertyChanged("Users");
            }
        }

        protected override async Task LoadData()
        {
            Users = await UIKernel.Instance.ApiClient.GetAllUsersAsync().ConfigureAwait(false);
        }

        protected void UserClicked(DtoUser user)
        {
            App.Instance.CurrentUser = user;
            //App.Instance.Navigate(new Uri("/Pages/HomePage.xaml", UriKind.Relative));
        }
    }
}
