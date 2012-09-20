using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MediaBrowser.UI.Controls
{
    /// <summary>
    /// Provides a ScrollViewer that can be scrolled by dragging the mouse
    /// </summary>
    public class EnhancedScrollViewer : ScrollViewer
    {
        private Point _scrollTarget;
        private Point _scrollStartPoint;
        private Point _scrollStartOffset;
        private const int PixelsToMoveToBeConsideredScroll = 5;
        
        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            if (IsMouseOver)
            {
                // Save starting point, used later when determining how much to scroll.
                _scrollStartPoint = e.GetPosition(this);
                _scrollStartOffset.X = HorizontalOffset;
                _scrollStartOffset.Y = VerticalOffset;

                // Update the cursor if can scroll or not.
                Cursor = (ExtentWidth > ViewportWidth) ||
                    (ExtentHeight > ViewportHeight) ?
                    Cursors.ScrollAll : Cursors.Arrow;

                CaptureMouse();
            }
            
            base.OnPreviewMouseDown(e);
        }

        protected override void OnPreviewMouseMove(MouseEventArgs e)
        {
            if (IsMouseCaptured)
            {
                Point currentPoint = e.GetPosition(this);

                // Determine the new amount to scroll.
                var delta = new Point(_scrollStartPoint.X - currentPoint.X, _scrollStartPoint.Y - currentPoint.Y);

                if (Math.Abs(delta.X) < PixelsToMoveToBeConsideredScroll &&
                    Math.Abs(delta.Y) < PixelsToMoveToBeConsideredScroll)
                    return;

                _scrollTarget.X = _scrollStartOffset.X + delta.X;
                _scrollTarget.Y = _scrollStartOffset.Y + delta.Y;

                // Scroll to the new position.
                ScrollToHorizontalOffset(_scrollTarget.X);
                ScrollToVerticalOffset(_scrollTarget.Y);
            }
            
            base.OnPreviewMouseMove(e);
        }

        protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
        {
            if (IsMouseCaptured)
            {
                Cursor = Cursors.Arrow;
                ReleaseMouseCapture();
            }
            
            base.OnPreviewMouseUp(e);
        }
    }
}
