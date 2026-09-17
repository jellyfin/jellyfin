using System;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// The collections of rows a <see cref="BaseItem"/> owns outright, which saving the item
    /// rewrites from whatever the instance holds.
    /// </summary>
    /// <remarks>
    /// A query only reads these when the caller's <c>DtoOptions</c> asks for them, and an unread
    /// collection is indistinguishable from an empty one by its contents alone. Items track which
    /// of these they have actually read so the save path can leave the rest of the stored rows
    /// alone instead of taking "nothing here" as "delete everything".
    /// </remarks>
    [Flags]
    public enum OwnedItemRows
    {
        /// <summary>
        /// Nothing has been read.
        /// </summary>
        None = 0,

        /// <summary>
        /// The item's images.
        /// </summary>
        Images = 1,

        /// <summary>
        /// The item's external provider ids.
        /// </summary>
        Providers = 2,

        /// <summary>
        /// The metadata fields locked against refresh.
        /// </summary>
        LockedFields = 4
    }
}
