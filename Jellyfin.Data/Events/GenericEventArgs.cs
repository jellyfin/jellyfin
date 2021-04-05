using System;

namespace Jellyfin.Data.Events
{
    /// <summary>
    /// Provides a generic EventArgs subclass that can hold any kind of object.
    /// </summary>
    /// <typeparam name="T">The type of this event.</typeparam>
    public class GenericEventArgs<T> : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GenericEventArgs{T}"/> class.
        /// </summary>
        /// <param name="arg">The argument.</param>
        public GenericEventArgs(T arg)
        {
            Argument = arg;
        }

        /// <summary>
        /// Gets or sets the argument.
        /// This must be settable for Rebus to work.
        /// </summary>
        /// <value>The argument.</value>
        public T Argument { get; set; }
    }
}
