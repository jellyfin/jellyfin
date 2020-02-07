#pragma warning disable CS1591
#pragma warning disable SA1600

namespace MediaBrowser.Model.Sync
{
    public enum SyncCategory
    {
        /// <summary>
        /// The latest
        /// </summary>
        Latest = 0,
        /// <summary>
        /// The next up
        /// </summary>
        NextUp = 1,
        /// <summary>
        /// The resume
        /// </summary>
        Resume = 2
    }
}
