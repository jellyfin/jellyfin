using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Net;
using MediaBrowser.UI.Controls;
using System;
using System.Threading.Tasks;

namespace MediaBrowser.UI.Pages
{
    /// <summary>
    /// Provides a base page for theme login pages
    /// </summary>
    public abstract class BaseLoginPage : BasePage
    {
        /// <summary>
        /// The _users
        /// </summary>
        private UserDto[] _users;
        /// <summary>
        /// Gets or sets the users.
        /// </summary>
        /// <value>The users.</value>
        public UserDto[] Users
        {
            get { return _users; }

            set
            {
                _users = value;
                OnPropertyChanged("Users");
            }
        }

        /// <summary>
        /// Subclasses must provide the list that holds the users
        /// </summary>
        /// <value>The items list.</value>
        protected abstract ExtendedListBox ItemsList { get; }

        /// <summary>
        /// Raises the <see cref="E:Initialized" /> event.
        /// </summary>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected override async void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            ItemsList.ItemInvoked += ItemsList_ItemInvoked;

            try
            {
                Users = await App.Instance.ApiClient.GetAllUsersAsync();
            }
            catch (HttpException)
            {
                App.Instance.ShowErrorMessage("There was an error retrieving the list of users from the server.");
            }
        }

        /// <summary>
        /// Called when [loaded].
        /// </summary>
        protected override void OnLoaded()
        {
            base.OnLoaded();
            ClearBackdrops();
        }

        /// <summary>
        /// Logs in a user when one is selected
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        async void ItemsList_ItemInvoked(object sender, ItemEventArgs<object> e)
        {
            var user = (UserDto)e.Argument;

            try
            {
                await LoginUser(user);
            }
            catch (HttpException ex)
            {
                if (ex.StatusCode.HasValue && ex.StatusCode.Value == System.Net.HttpStatusCode.Unauthorized)
                {
                    App.Instance.ShowErrorMessage("Invalid username or password. Please try again.", caption: "Login Failure");
                }
                else
                {
                    App.Instance.ShowDefaultErrorMessage();
                }
            }
        }

        /// <summary>
        /// Logs in a user and verifies their password
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="password">The password.</param>
        /// <returns>Task{AuthenticationResult}.</returns>
        protected async Task LoginUser(UserDto user, string password)
        {
            await App.Instance.ApiClient.AuthenticateUserAsync(user.Id, password);

            App.Instance.CurrentUser = user;

            App.Instance.NavigateToHomePage();
        }

        /// <summary>
        /// Logs in a user who does not have a password
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>Task{AuthenticationResult}.</returns>
        protected Task LoginUser(UserDto user)
        {
            return LoginUser(user, null);
        }
    }
}
