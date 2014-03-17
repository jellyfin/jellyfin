using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using System;
using System.IO;
using System.Linq;

namespace MediaBrowser.Dlna.PlayTo
{
    public class PlaylistItem
    {
        public string ItemId { get; set; }

        public bool Transcode { get; set; }

        public bool IsVideo { get; set; }

        public bool IsAudio { get; set; }

        public string FileFormat { get; set; }

        public string MimeType { get; set; }

        public int PlayState { get; set; }

        public string StreamUrl { get; set; }

        public string DlnaHeaders { get; set; }

        public string Didl { get; set; }

        public long StartPositionTicks { get; set; }

        public static PlaylistItem Create(BaseItem item, DeviceProfile profile)
        {
            var playlistItem = new PlaylistItem
            {
                ItemId = item.Id.ToString()
            };

            DlnaProfileType profileType;
            if (string.Equals(item.MediaType, MediaType.Video, StringComparison.OrdinalIgnoreCase))
            {
                playlistItem.IsVideo = true;
                profileType = DlnaProfileType.Video;
            }
            else
            {
                playlistItem.IsAudio = true;
                profileType = DlnaProfileType.Audio;
            }

            var path = item.Path;

            var directPlay = profile.DirectPlayProfiles.FirstOrDefault(i => i.Type == profileType && IsSupported(i, path));

            if (directPlay != null)
            {
                playlistItem.Transcode = false;
                playlistItem.FileFormat = Path.GetExtension(path);
                playlistItem.MimeType = directPlay.MimeType;
                return playlistItem;
            }

            var transcodingProfile = profile.TranscodingProfiles.FirstOrDefault(i => i.Type == profileType && IsSupported(profile, i, path));

            if (transcodingProfile != null)
            {
                playlistItem.Transcode = true;
                //Just to make sure we have a "." for the url, remove it in case a user adds it or not
                playlistItem.FileFormat = "." + transcodingProfile.Container.TrimStart('.');

                playlistItem.MimeType = transcodingProfile.MimeType;
            }

            return playlistItem;
        }

        private static bool IsSupported(DirectPlayProfile profile, string path)
        {
            var mediaContainer = Path.GetExtension(path);

            if (!profile.Containers.Any(i => string.Equals("." + i.TrimStart('.'), mediaContainer, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            // Placeholder for future conditions

            // TODO: Support codec list as additional restriction

            return true;
        }

        private static bool IsSupported(DeviceProfile profile, TranscodingProfile transcodingProfile, string path)
        {
            // Placeholder for future conditions
            return true;
        }
    }
}