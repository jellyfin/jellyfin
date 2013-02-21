using System.Windows.Controls;
using System.Windows.Input;

namespace MediaBrowser.UI.Controls
{
    /// <summary>
    /// This subclass solves the problem of ScrollViewers eating KeyDown for all arrow keys
    /// </summary>
    public class ExtendedScrollViewer : ScrollViewer
    {
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Handled || e.OriginalSource == this)
            {
                base.OnKeyDown(e);
                return;
            } 
            
            // Don't eat left/right if horizontal scrolling is disabled
            if (e.Key == Key.Left || e.Key == Key.Right)
            {
                if (HorizontalScrollBarVisibility == ScrollBarVisibility.Disabled)
                {
                    return;
                }
            }

            // Don't eat up/down if vertical scrolling is disabled
            if (e.Key == Key.Up || e.Key == Key.Down)
            {
                if (VerticalScrollBarVisibility == ScrollBarVisibility.Disabled)
                {
                    return;
                }
            }

            // Let the base class do it's thing
            base.OnKeyDown(e);
        }
    }
}
