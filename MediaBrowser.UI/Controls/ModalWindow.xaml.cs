using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace MediaBrowser.UI.Controls
{
    /// <summary>
    /// Interaction logic for ModalWindow.xaml
    /// </summary>
    public partial class ModalWindow : BaseModalWindow
    {
        public MessageBoxResult MessageBoxResult { get; set; }

        public UIElement TextContent
        {
            set
            {
                pnlContent.Children.Clear();

                var textBlock = value as TextBlock;

                if (textBlock != null)
                {
                    textBlock.SetResourceReference(TextBlock.StyleProperty, "ModalTextStyle");
                }
                pnlContent.Children.Add(value);
            }
        }

        public string Text
        {
            set { TextContent = new TextBlock { Text = value }; }
        }

        private MessageBoxButton _button;
        public MessageBoxButton Button
        {
            get { return _button; }
            set
            {
                _button = value;
                UpdateButtonVisibility();
                OnPropertyChanged("Button");
            }
        }

        private MessageBoxIcon _messageBoxImage;
        public MessageBoxIcon MessageBoxImage
        {
            get { return _messageBoxImage; }
            set
            {
                _messageBoxImage = value;
                OnPropertyChanged("MessageBoxImage");
            }
        }

        private string _caption;
        public string Caption
        {
            get { return _caption; }
            set
            {
                _caption = value;
                txtCaption.Visibility = string.IsNullOrEmpty(value) ? Visibility.Collapsed : Visibility.Visible;
                OnPropertyChanged("Caption");
            }
        }

        public ModalWindow()
            : base()
        {
            InitializeComponent();
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            btnOk.Click += btnOk_Click;
            btnCancel.Click += btnCancel_Click;
            btnYes.Click += btnYes_Click;
            btnNo.Click += btnNo_Click;
        }

        void btnNo_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult = MessageBoxResult.No;
            CloseModal();
        }

        void btnYes_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult = MessageBoxResult.Yes;
            CloseModal();
        }

        void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult = MessageBoxResult.Cancel;
            CloseModal();
        }

        void btnOk_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult = MessageBoxResult.OK;
            CloseModal();
        }

        private void UpdateButtonVisibility()
        {
            btnYes.Visibility = Button == MessageBoxButton.YesNo || Button == MessageBoxButton.YesNoCancel
                                    ? Visibility.Visible
                                    : Visibility.Collapsed;

            btnNo.Visibility = Button == MessageBoxButton.YesNo || Button == MessageBoxButton.YesNoCancel
                            ? Visibility.Visible
                            : Visibility.Collapsed;

            btnOk.Visibility = Button == MessageBoxButton.OK || Button == MessageBoxButton.OKCancel
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            btnCancel.Visibility = Button == MessageBoxButton.OKCancel || Button == MessageBoxButton.YesNoCancel
            ? Visibility.Visible
            : Visibility.Collapsed;
        }
    }

    /// <summary>
    /// I had to make my own enum that essentially clones MessageBoxImage
    /// Some of the options share the same enum int value, and this was preventing databinding from working properly.
    /// </summary>
    public enum MessageBoxIcon
    {
        // Summary:
        //     No icon is displayed.
        None,
        //
        // Summary:
        //     The message box contains a symbol consisting of white X in a circle with
        //     a red background.
        Error,
        //
        // Summary:
        //     The message box contains a symbol consisting of a white X in a circle with
        //     a red background.
        Hand,
        //
        // Summary:
        //     The message box contains a symbol consisting of white X in a circle with
        //     a red background.
        Stop,
        //
        // Summary:
        //     The message box contains a symbol consisting of a question mark in a circle.
        Question,
        //
        // Summary:
        //     The message box contains a symbol consisting of an exclamation point in a
        //     triangle with a yellow background.
        Exclamation,
        //
        // Summary:
        //     The message box contains a symbol consisting of an exclamation point in a
        //     triangle with a yellow background.
        Warning,
        //
        // Summary:
        //     The message box contains a symbol consisting of a lowercase letter i in a
        //     circle.
        Information,
        //
        // Summary:
        //     The message box contains a symbol consisting of a lowercase letter i in a
        //     circle.
        Asterisk
    }
}
