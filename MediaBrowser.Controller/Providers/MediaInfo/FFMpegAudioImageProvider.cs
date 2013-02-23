using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Model.Entities;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Providers.MediaInfo
{
    /// <summary>
    /// Uses ffmpeg to create video images
    /// </summary>
    public class FFMpegAudioImageProvider : BaseFFMpegImageProvider<Audio>
    {
        
        /// <summary>
        /// Fetches metadata and returns true or false indicating if any work that requires persistence was done
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="force">if set to <c>true</c> [force].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.Boolean}.</returns>
        protected override Task<bool> FetchAsyncInternal(BaseItem item, bool force, CancellationToken cancellationToken)
        {
            var audio = (Audio)item;

            if (string.IsNullOrEmpty(audio.PrimaryImagePath))
            {
                // First try to use the parent's image
                audio.PrimaryImagePath = audio.ResolveArgs.Parent.PrimaryImagePath;

                // If it's still empty see if there's an embedded image
                if (string.IsNullOrEmpty(audio.PrimaryImagePath))
                {
                    if (audio.MediaStreams != null && audio.MediaStreams.Any(s => s.Type == MediaStreamType.Video))
                    {
                        var filename = item.Id + "_" + item.DateModified.Ticks + "_primary";

                        var path = Kernel.Instance.FFMpegManager.AudioImageCache.GetResourcePath(filename, ".jpg");

                        if (!Kernel.Instance.FFMpegManager.AudioImageCache.ContainsFilePath(path))
                        {
                            return ExtractImage(audio, path, cancellationToken);
                        }

                        // Image is already in the cache
                        audio.PrimaryImagePath = path;
                    }

                }
            }

            SetLastRefreshed(item, DateTime.UtcNow);
            return TrueTaskResult;
        }

        /// <summary>
        /// Extracts the image.
        /// </summary>
        /// <param name="audio">The audio.</param>
        /// <param name="path">The path.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.Boolean}.</returns>
        private async Task<bool> ExtractImage(Audio audio, string path, CancellationToken cancellationToken)
        {
            var success = await Kernel.Instance.FFMpegManager.ExtractImage(audio, path, cancellationToken).ConfigureAwait(false);

            if (success)
            {
                audio.PrimaryImagePath = path;
                SetLastRefreshed(audio, DateTime.UtcNow);
            }
            else
            {
                SetLastRefreshed(audio, DateTime.UtcNow, ProviderRefreshStatus.Failure);
            }

            return true;
        }
    }
}
