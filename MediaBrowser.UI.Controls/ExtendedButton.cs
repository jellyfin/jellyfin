using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MediaBrowser.UI.Controls
{
    /// <summary>
    /// This subclass simply autofocuses itself when the mouse moves over it
    /// </summary>
    public class ExtendedButton : Button
    {
        private Point? _lastMouseMovePoint;

        /// <summary>
        /// Handles OnMouseMove to auto-select the item that's being moused over
        /// </summary>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            var window = this.GetWindow();

            // If the cursor is currently hidden, don't bother reacting to it
            if (Cursor == Cursors.None || window.Cursor == Cursors.None)
            {
                return;
            }

            // Store the last position for comparison purposes
            // Even if the mouse is not moving this event will fire as elements are showing and hiding
            var pos = e.GetPosition(window);

            if (!_lastMouseMovePoint.HasValue)
            {
                _lastMouseMovePoint = pos;
                return;
            }

            if (pos == _lastMouseMovePoint)
            {
                return;
            }

            _lastMouseMovePoint = pos;

            Focus();
        }
    }
}
