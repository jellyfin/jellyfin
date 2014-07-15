
namespace MediaBrowser.Model.Sync
{
    public enum SyncJobStatus
    {
        /// <summary>
        /// The queued
        /// </summary>
        Queued = 0,
        /// <summary>
        /// The transcoding
        /// </summary>
        Transcoding = 1,
        /// <summary>
        /// The transcoding failed
        /// </summary>
        TranscodingFailed = 2,
        /// <summary>
        /// The transcoding completed
        /// </summary>
        TranscodingCompleted = 3,
        /// <summary>
        /// The transfering
        /// </summary>
        Transfering = 4,
        /// <summary>
        /// The transfer failed
        /// </summary>
        TransferFailed = 4,
        /// <summary>
        /// The completed
        /// </summary>
        Completed = 6
    }
}
