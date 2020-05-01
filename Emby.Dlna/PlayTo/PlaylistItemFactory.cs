#pragma warning disable CS1591

using System.IO;
using System.Linq;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Session;

namespace Emby.Dlna.PlayTo
{
    public class PlaylistItemFactory
    {
        public PlaylistItem Create(Photo item, DeviceProfile profile)
        {
            var playlistItem = new PlaylistItem
            {
                StreamInfo = new StreamInfo
                {
                    ItemId = item.Id,
                    MediaType = DlnaProfileType.Photo,
                    DeviceProfile = profile
                },

                Profile = profile
            };

            var directPlay = profile.DirectPlayProfiles
                .FirstOrDefault(i => i.Type == DlnaProfileType.Photo && IsSupported(i, item));

            if (directPlay != null)
            {
                playlistItem.StreamInfo.PlayMethod = PlayMethod.DirectStream;
                playlistItem.StreamInfo.Container = Path.GetExtension(item.Path);

                return playlistItem;
            }

            var transcodingProfile = profile.TranscodingProfiles
                .FirstOrDefault(i => i.Type == DlnaProfileType.Photo);

            if (transcodingProfile != null)
            {
                playlistItem.StreamInfo.PlayMethod = PlayMethod.Transcode;
                playlistItem.StreamInfo.Container = "." + transcodingProfile.Container.TrimStart('.');
            }

            return playlistItem;
        }

        private static bool IsSupported(DirectPlayProfile profile, Photo item)
        {
            var mediaPath = item.Path;

            if (profile.Container.Length > 0)
            {
                // Check container type
                var mediaContainer = (Path.GetExtension(mediaPath) ?? string.Empty).TrimStart('.');

                if (!profile.SupportsContainer(mediaContainer))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
