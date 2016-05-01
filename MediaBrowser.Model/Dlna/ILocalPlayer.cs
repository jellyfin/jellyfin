
namespace MediaBrowser.Model.Dlna
{
    public interface ILocalPlayer
    {
        /// <summary>
        /// Determines whether this instance [can access file] the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns><c>true</c> if this instance [can access file] the specified path; otherwise, <c>false</c>.</returns>
        bool CanAccessFile(string path);
        /// <summary>
        /// Determines whether this instance [can access directory] the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns><c>true</c> if this instance [can access directory] the specified path; otherwise, <c>false</c>.</returns>
        bool CanAccessDirectory(string path);
        /// <summary>
        /// Determines whether this instance [can access URL] the specified URL.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="requiresCustomRequestHeaders">if set to <c>true</c> [requires custom request headers].</param>
        /// <returns><c>true</c> if this instance [can access URL] the specified URL; otherwise, <c>false</c>.</returns>
        bool CanAccessUrl(string url, bool requiresCustomRequestHeaders);
    }

    public interface ITranscoderSupport
    {
        bool CanEncodeToAudioCodec(string codec);
    }

    public class FullTranscoderSupport : ITranscoderSupport
    {
        public bool CanEncodeToAudioCodec(string codec)
        {
            return true;
        }
    }
}
