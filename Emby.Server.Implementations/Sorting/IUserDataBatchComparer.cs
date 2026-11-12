using System;
using System.Collections.Generic;
using MediaBrowser.Controller.Entities;

namespace Emby.Server.Implementations.Sorting
{
    /// <summary>
    /// A user comparer that can compare from user data read for the whole list up front.
    /// </summary>
    internal interface IUserDataBatchComparer
    {
        /// <summary>
        /// Gets or sets the user data of the items being sorted, keyed by item id.
        /// </summary>
        IReadOnlyDictionary<Guid, UserItemData>? UserDataById { get; set; }
    }
}
