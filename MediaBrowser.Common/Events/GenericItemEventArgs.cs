using System;

namespace MediaBrowser.Common.Events
{
    public class GenericItemEventArgs<TItemType> : EventArgs
    {
        public TItemType Item { get; set; }
    }
}
