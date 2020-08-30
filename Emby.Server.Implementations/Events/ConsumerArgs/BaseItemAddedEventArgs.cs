using System;
using Jellyfin.Data.Events;
using MediaBrowser.Controller.Entities;

namespace Emby.Server.Implementations.Events.ConsumerArgs
{
    /// <summary>
    /// An event that occurs when a base item is added.
    /// </summary>
    public class BaseItemAddedEventArgs : GenericEventArgs<BaseItem>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseItemAddedEventArgs"/> class.
        /// </summary>
        /// <param name="arg">The base item.</param>
        public BaseItemAddedEventArgs(BaseItem arg) : base(arg)
        {
        }
    }
}
