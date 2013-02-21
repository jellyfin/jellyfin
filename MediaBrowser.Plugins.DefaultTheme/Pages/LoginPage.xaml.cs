using MediaBrowser.Plugins.DefaultTheme.Resources;
using MediaBrowser.UI.Controls;
using MediaBrowser.UI.Pages;

namespace MediaBrowser.Plugins.DefaultTheme.Pages
{
    /// <summary>
    /// Interaction logic for LoginPage.xaml
    /// </summary>
    public partial class LoginPage : BaseLoginPage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LoginPage" /> class.
        /// </summary>
        public LoginPage()
            : base()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Subclasses must provide the list that holds the users
        /// </summary>
        /// <value>The items list.</value>
        protected override ExtendedListBox ItemsList
        {
            get
            {
                return lstUsers;
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
    }
}
