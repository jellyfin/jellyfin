using System;

namespace MediaBrowser.Common.Events
{
    /// <summary>
    /// Provides a generic EventArgs subclass that can hold any kind of object
    /// </summary>
    public class GenericEventArgs<T> : EventArgs
    {
        public T Argument { get; set; }
    }
}
