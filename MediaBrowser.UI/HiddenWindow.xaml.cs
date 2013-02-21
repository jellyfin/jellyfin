using System.Windows;
using MediaBrowser.UI.Controller;

namespace MediaBrowser.UI
{
    /// <summary>
    /// Interaction logic for HiddenWindow.xaml
    /// </summary>
    public partial class HiddenWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HiddenWindow" /> class.
        /// </summary>
        public HiddenWindow()
        {
            InitializeComponent();

            Loaded += HiddenWindow_Loaded;
        }

        /// <summary>
        /// Handles the Loaded event of the HiddenWindow control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        void HiddenWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Title += " " + UIKernel.Instance.ApplicationVersion.ToString();
        }
    }
}
