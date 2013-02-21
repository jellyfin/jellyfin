using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace MediaBrowser.UI.Controls
{
    /// <summary>
    /// Extends the ListBox to provide auto-focus behavior when items are moused over
    /// This also adds an ItemInvoked event that is fired when an item is clicked or invoked using the enter key
    /// </summary>
    public class ExtendedListBox : ListBox
    {
        /// <summary>
        /// Fired when an item is clicked or invoked using the enter key
        /// </summary>
        public event EventHandler<ItemEventArgs<object>> ItemInvoked;

        /// <summary>
        /// Called when [item invoked].
        /// </summary>
        /// <param name="boundObject">The bound object.</param>
        protected virtual void OnItemInvoked(object boundObject)
        {
            if (ItemInvoked != null)
            {
                ItemInvoked(this, new ItemEventArgs<object> { Argument = boundObject });
            }
        }

        /// <summary>
        /// The _auto focus
        /// </summary>
        private bool _autoFocus = true;
        /// <summary>
        /// Gets or sets a value indicating if the first list item should be auto-focused on load
        /// </summary>
        /// <value><c>true</c> if [auto focus]; otherwise, <c>false</c>.</value>
        public bool AutoFocus
        {
            get { return _autoFocus; }
            set
            {
                _autoFocus = value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtendedListBox" /> class.
        /// </summary>
        public ExtendedListBox()
            : base()
        {
            ItemContainerGenerator.StatusChanged += ItemContainerGeneratorStatusChanged;
        }

        /// <summary>
        /// The mouse down object
        /// </summary>
        private object mouseDownObject;

        /// <summary>
        /// Invoked when an unhandled <see cref="E:System.Windows.Input.Mouse.PreviewMouseDown" /> attached routed event reaches an element in its route that is derived from this class. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseButtonEventArgs" /> that contains the event data. The event data reports that one or more mouse buttons were pressed.</param>
        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);

            // Get the item that the mouse down event occurred on
            mouseDownObject = GetBoundListItemObject((DependencyObject)e.OriginalSource);
        }

        /// <summary>
        /// Invoked when an unhandled <see cref="E:System.Windows.UIElement.MouseLeftButtonUp" /> routed event reaches an element in its route that is derived from this class. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseButtonEventArgs" /> that contains the event data. The event data reports that the left mouse button was released.</param>
        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);

            // If the mouse up event occurred on the same item as the mousedown event, then fire ItemInvoked
            if (mouseDownObject != null)
            {
                var boundObject = GetBoundListItemObject((DependencyObject)e.OriginalSource);

                if (mouseDownObject == boundObject)
                {
                    mouseDownObject = null;
                    OnItemInvoked(boundObject);
                }
            }
        }

        /// <summary>
        /// The key down object
        /// </summary>
        private object keyDownObject;

        /// <summary>
        /// Responds to the <see cref="E:System.Windows.UIElement.KeyDown" /> event.
        /// </summary>
        /// <param name="e">Provides data for <see cref="T:System.Windows.Input.KeyEventArgs" />.</param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (!e.IsRepeat)
                {
                    // Get the item that the keydown event occurred on
                    keyDownObject = GetBoundListItemObject((DependencyObject)e.OriginalSource);
                }

                e.Handled = true;
            }

            base.OnKeyDown(e);
        }

        /// <summary>
        /// Invoked when an unhandled <see cref="E:System.Windows.Input.Keyboard.KeyUp" /> attached event reaches an element in its route that is derived from this class. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.KeyEventArgs" /> that contains the event data.</param>
        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);

            // Fire ItemInvoked when enter is pressed on an item
            if (e.Key == Key.Enter)
            {
                if (!e.IsRepeat)
                {
                    // If the keyup event occurred on the same item as the keydown event, then fire ItemInvoked
                    if (keyDownObject != null)
                    {
                        var boundObject = GetBoundListItemObject((DependencyObject)e.OriginalSource);

                        if (keyDownObject == boundObject)
                        {
                            keyDownObject = null;
                            OnItemInvoked(boundObject);
                        }
                    }
                }

                e.Handled = true;
            }
        }

        /// <summary>
        /// The _last mouse move point
        /// </summary>
        private Point? _lastMouseMovePoint;

        /// <summary>
        /// Handles OnMouseMove to auto-select the item that's being moused over
        /// </summary>
        /// <param name="e">Provides data for <see cref="T:System.Windows.Input.MouseEventArgs" />.</param>
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

            var dep = (DependencyObject)e.OriginalSource;

            while ((dep != null) && !(dep is ListBoxItem))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }

            if (dep != null)
            {
                var listBoxItem = dep as ListBoxItem;

                if (!listBoxItem.IsFocused)
                {
                    listBoxItem.Focus();
                }
            }
        }

        /// <summary>
        /// Gets the datacontext for a given ListBoxItem
        /// </summary>
        /// <param name="dep">The dep.</param>
        /// <returns>System.Object.</returns>
        private object GetBoundListItemObject(DependencyObject dep)
        {
            while ((dep != null) && !(dep is ListBoxItem))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }

            if (dep == null)
            {
                return null;
            }

            return ItemContainerGenerator.ItemFromContainer(dep);
        }

        /// <summary>
        /// Autofocuses the first list item when the list is loaded
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        void ItemContainerGeneratorStatusChanged(object sender, EventArgs e)
        {
            if (ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated && AutoFocus)
            {
                Dispatcher.InvokeAsync(OnContainersGenerated);
            }
        }

        /// <summary>
        /// Called when [containers generated].
        /// </summary>
        void OnContainersGenerated()
        {
            var index = 0;

            if (index >= 0)
            {
                var item = ItemContainerGenerator.ContainerFromIndex(index) as ListBoxItem;

                if (item != null)
                {
                    item.Focus();
                    ItemContainerGenerator.StatusChanged -= ItemContainerGeneratorStatusChanged;
                }
            }
        }
    }
}
