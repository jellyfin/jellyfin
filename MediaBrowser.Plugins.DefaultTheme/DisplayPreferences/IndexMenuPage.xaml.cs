using MediaBrowser.Model.Net;
using MediaBrowser.UI;
using MediaBrowser.UI.Controls;
using System;
using System.Windows;
using System.Windows.Controls;

namespace MediaBrowser.Plugins.DefaultTheme.DisplayPreferences
{
    /// <summary>
    /// Interaction logic for IndexMenuPage.xaml
    /// </summary>
    public partial class IndexMenuPage : BaseDisplayPreferencesPage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IndexMenuPage" /> class.
        /// </summary>
        public IndexMenuPage()
        {
            InitializeComponent();

            chkRememberIndex.Click += chkRememberIndex_Click;
        }

        /// <summary>
        /// Handles the Click event of the chkRememberIndex control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        async void chkRememberIndex_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await MainPage.UpdateRememberIndex(chkRememberIndex.IsChecked.HasValue && chkRememberIndex.IsChecked.Value);
            }
            catch (HttpException)
            {
                App.Instance.ShowDefaultErrorMessage();
            }
        }

        /// <summary>
        /// Called when [loaded].
        /// </summary>
        protected override void OnLoaded()
        {
            chkRememberIndex.IsChecked = MainPage.DisplayPreferences.RememberIndexing;

            var index = 0;

            var currentValue = MainPage.IndexBy ?? string.Empty;

            foreach (var option in MainPage.Folder.IndexOptions)
            {
                var radio = new ExtendedRadioButton { GroupName = "Options" };

                radio.SetResourceReference(StyleProperty, "ViewMenuRadioButton");

                var textblock = new TextBlock { Text = option };

                textblock.SetResourceReference(StyleProperty, "TextBlockStyle");

                radio.Content = textblock;

                if (string.IsNullOrEmpty(MainPage.DisplayPreferences.IndexBy))
                {
                    radio.IsChecked = index == 0;
                }
                else
                {
                    radio.IsChecked = currentValue.Equals(option, StringComparison.OrdinalIgnoreCase);
                }
                
                radio.Tag = option;
                radio.Click += radio_Click;

                pnlOptions.Children.Add(radio);

                index++;
            }

            base.OnLoaded();
        }

        /// <summary>
        /// Handles the Click event of the radio control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        async void radio_Click(object sender, RoutedEventArgs e)
        {
            await MainPage.UpdateIndexOption((sender as RadioButton).Tag.ToString());
        }
    }
}
