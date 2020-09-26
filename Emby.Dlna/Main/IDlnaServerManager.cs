using Emby.Dlna.ConnectionManager;
using Emby.Dlna.ContentDirectory;
using Emby.Dlna.MediaReceiverRegistrar;

namespace Emby.Dlna.Main
{
    /// <summary>
    /// Defines the <see cref="IDlnaServerManager" />.
    /// </summary>
    public interface IDlnaServerManager
    {
        /// <summary>
        /// Gets a value indicating whether the DLNA server is active.
        /// </summary>
        bool IsDLNAServerEnabled { get; }

        /// <summary>
        /// Gets the DLNA server' ConnectionManager instance..
        /// </summary>
        IConnectionManager? ConnectionManager { get; }

        /// <summary>
        /// Gets the DLNA server' ContentDirectory instance..
        /// </summary>
        IContentDirectory? ContentDirectory { get; }

        /// <summary>
        /// Gets the DLNA server's MediaReceiverRegistrar instance..
        /// </summary>
        IMediaReceiverRegistrar? MediaReceiverRegistrar { get; }
    }
}
