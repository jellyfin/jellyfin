using System;
using System.Windows;

namespace MediaBrowser.UI.Controls
{
    /// <summary>
    /// Class BaseModalWindow
    /// </summary>
    public class BaseModalWindow : BaseWindow
    {
        /// <summary>
        /// Shows the modal.
        /// </summary>
        /// <param name="owner">The owner.</param>
        public void ShowModal(Window owner)
        {
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            WindowStartupLocation = WindowStartupLocation.Manual;
            AllowsTransparency = true;

            Width = owner.Width;
            Height = owner.Height;
            Top = owner.Top;
            Left = owner.Left;
            WindowState = owner.WindowState;
            Owner = owner;

            ShowDialog();
        }

        /// <summary>
        /// Called when [browser back].
        /// </summary>
        protected override void OnBrowserBack()
        {
            base.OnBrowserBack();

            CloseModal();
        }

        /// <summary>
        /// Raises the <see cref="E:System.Windows.FrameworkElement.Initialized" /> event. This method is invoked whenever <see cref="P:System.Windows.FrameworkElement.IsInitialized" /> is set to true internally.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.RoutedEventArgs" /> that contains the event data.</param>
        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            DataContext = this;
        }

        /// <summary>
        /// Closes the modal.
        /// </summary>
        protected virtual void CloseModal()
        {
            Close();
        }
    }
}
