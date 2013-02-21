using MediaBrowser.Model.Entities;
using System.Windows;

namespace MediaBrowser.Plugins.DefaultTheme.DisplayPreferences
{
    /// <summary>
    /// Interaction logic for MainPage.xaml
    /// </summary>
    public partial class MainPage : BaseDisplayPreferencesPage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MainPage" /> class.
        /// </summary>
        public MainPage()
        {
            InitializeComponent();

            btnScroll.Click += btnScroll_Click;
            btnIncrease.Click += btnIncrease_Click;
            btnDecrease.Click += btnDecrease_Click;
            ViewMenuButton.Click += ViewMenuButton_Click;
            SortMenuButton.Click += SortMenuButton_Click;
            IndexMenuButton.Click += IndexMenuButton_Click;
        }

        /// <summary>
        /// Handles the Click event of the IndexMenuButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        void IndexMenuButton_Click(object sender, RoutedEventArgs e)
        {
            DisplayPreferencesWindow.NavigateToIndexMenu();
        }

        /// <summary>
        /// Handles the Click event of the SortMenuButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        void SortMenuButton_Click(object sender, RoutedEventArgs e)
        {
            DisplayPreferencesWindow.NavigateToSortMenu();
        }

        /// <summary>
        /// Called when [loaded].
        /// </summary>
        protected override void OnLoaded()
        {
            base.OnLoaded();

            UpdateFields();
        }

        /// <summary>
        /// Handles the Click event of the ViewMenuButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        void ViewMenuButton_Click(object sender, RoutedEventArgs e)
        {
            DisplayPreferencesWindow.NavigateToViewMenu();
        }

        /// <summary>
        /// Handles the Click event of the btnDecrease control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        void btnDecrease_Click(object sender, RoutedEventArgs e)
        {
            MainPage.DisplayPreferences.DecreaseImageSize();
            MainPage.NotifyDisplayPreferencesChanged();
        }

        /// <summary>
        /// Handles the Click event of the btnIncrease control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        void btnIncrease_Click(object sender, RoutedEventArgs e)
        {
            MainPage.DisplayPreferences.IncreaseImageSize();
            MainPage.NotifyDisplayPreferencesChanged();
        }

        /// <summary>
        /// Handles the Click event of the btnScroll control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        void btnScroll_Click(object sender, RoutedEventArgs e)
        {
            MainPage.DisplayPreferences.ScrollDirection = MainPage.DisplayPreferences.ScrollDirection == ScrollDirection.Horizontal
                                                     ? ScrollDirection.Vertical
                                                     : ScrollDirection.Horizontal;

            MainPage.NotifyDisplayPreferencesChanged();
           
            UpdateFields();
        }

        /// <summary>
        /// Updates the fields.
        /// </summary>
        private void UpdateFields()
        {
            var displayPreferences = MainPage.DisplayPreferences;

            btnScroll.Visibility = displayPreferences.ViewType == ViewTypes.Poster
                                       ? Visibility.Visible
                                       : Visibility.Collapsed;

            txtScrollDirection.Text = displayPreferences.ScrollDirection == ScrollDirection.Horizontal ? "Scroll: Horizontal" : "Scroll: Vertical";
        }
    }
}
