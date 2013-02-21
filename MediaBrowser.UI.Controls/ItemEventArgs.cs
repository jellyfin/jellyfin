using System;

namespace MediaBrowser.UI.Controls
{
    /// <summary>
    /// Provides a generic EventArgs subclass that can hold any kind of object
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ItemEventArgs<T> : EventArgs
    {
        /// <summary>
        /// Gets or sets the argument.
        /// </summary>
        /// <value>The argument.</value>
        public T Argument { get; set; }
    }
}
