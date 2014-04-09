using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Dlna.PlayTo
{
    internal class DidlBuilder
    {
        const string CRLF = "\r\n";
        const string UNKNOWN = "Unknown";

        const string DIDL_START = @"<item id=""{0}"" parentID=""{1}"" restricted=""1"" xmlns=""urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/"">" + CRLF;
        const string DIDL_TITLE = @"  <dc:title xmlns:dc=""http://purl.org/dc/elements/1.1/"">{0}</dc:title>" + CRLF;
        const string DIDL_ARTIST = @"<upnp:artist xmlns:upnp=""urn:schemas-upnp-org:metadata-1-0/upnp/"">{0}</upnp:artist>" + CRLF;
        const string DIDL_ALBUM = @"<upnp:album xmlns:upnp=""urn:schemas-upnp-org:metadata-1-0/upnp/"">{0}</upnp:album>" + CRLF;
        const string DIDL_TRACKNUM = @"<upnp:originalTrackNumber xmlns:upnp=""urn:schemas-upnp-org:metadata-1-0/upnp/"">{0}</upnp:originalTrackNumber>" + CRLF;
        const string DIDL_VIDEOCLASS = @"  <upnp:class xmlns:upnp=""urn:schemas-upnp-org:metadata-1-0/upnp/"">object.item.videoItem</upnp:class>" + CRLF;
        const string DIDL_AUDIOCLASS = @"  <upnp:class xmlns:upnp=""urn:schemas-upnp-org:metadata-1-0/upnp/"">object.item.audioItem.musicTrack</upnp:class>" + CRLF;
        const string DIDL_IMAGE = @"  <upnp:albumArtURI dlna:profileID=""JPEG_TN"" xmlns:dlna=""urn:schemas-dlna-org:metadata-1-0/"" xmlns:upnp=""urn:schemas-upnp-org:metadata-1-0/upnp/"">{0}</upnp:albumArtURI>" + CRLF +
                                               @"  <upnp:icon xmlns:upnp=""urn:schemas-upnp-org:metadata-1-0/upnp/"">{0}</upnp:icon>" + CRLF;
        const string DIDL_RELEASEDATE = @"  <dc:date xmlns:dc=""http://purl.org/dc/elements/1.1/"">{0}</dc:date>" + CRLF;
        const string DIDL_GENRE = @"  <upnp:genre xmlns:upnp=""urn:schemas-upnp-org:metadata-1-0/upnp/"">{0}</upnp:genre>" + CRLF;
        const string DESCRIPTION = @"  <dc:description xmlns:dc=""http://purl.org/dc/elements/1.1/"">{0}</dc:description>" + CRLF;
        const string DIDL_VIDEO_RES = @"  <res bitrate=""{0}"" duration=""{1}"" protocolInfo=""http-get:*:video/x-msvideo:DLNA.ORG_PN=AVI;DLNA.ORG_OP=01;DLNA.ORG_CI=0;DLNA.ORG_FLAGS=01500000000000000000000000000000"" resolution=""{2}x{3}"">{4}</res>" + CRLF;
        const string DIDL_AUDIO_RES = @"  <res bitrate=""{0}"" duration=""{1}"" nrAudioChannels=""2"" protocolInfo=""http-get:*:audio/mp3:DLNA.ORG_OP=01;DLNA.ORG_CI=0;DLNA.ORG_FLAGS=01500000000000000000000000000000"" sampleFrequency=""{2}"">{3}</res>" + CRLF;
        const string DIDL_IMAGE_RES = @"  <res protocolInfo=""http-get:*:image/jpeg:DLNA.ORG_PN=JPEG_TN;DLNA.ORG_OP=00;DLNA.ORG_CI=1;DLNA.ORG_FLAGS=00D00000000000000000000000000000"" resolution=""212x320"">{0}</res>" + CRLF;
        const string DIDL_ALBUMIMAGE_RES = @"  <res protocolInfo=""http-get:*:image/jpeg:DLNA.ORG_PN=JPEG_TN;DLNA.ORG_OP=00;DLNA.ORG_CI=1;DLNA.ORG_FLAGS=00D00000000000000000000000000000"" resolution=""320x320"">{0}</res>" + CRLF;
        const string DIDL_RATING = @"  <upnp:rating xmlns:upnp=""urn:schemas-upnp-org:metadata-1-0/upnp/"">{0}</upnp:rating>" + CRLF;
        const string DIDL_END = "</item>";

        /// <summary>
        /// Builds a Didl MetaData object for the specified dto.
        /// </summary>
        /// <param name="dto">The dto.</param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="serverAddress">The server address.</param>
        /// <param name="streamUrl">The stream URL.</param>
        /// <param name="streams">The streams.</param>
        /// <returns>System.String.</returns>
        public static string Build(BaseItem dto, string userId, string serverAddress, string streamUrl, IEnumerable<MediaStream> streams, bool includeImageRes)
        {
            string response = string.Format(DIDL_START, dto.Id, userId);
            response += string.Format(DIDL_TITLE, dto.Name.Replace("&", "and"));
            if (IsVideo(dto))
                response += DIDL_VIDEOCLASS;
            else
                response += DIDL_AUDIOCLASS;

            var imageUrl = GetImageUrl(dto, serverAddress);

            if (!string.IsNullOrWhiteSpace(imageUrl))
            {
                response += string.Format(DIDL_IMAGE, imageUrl);
            }
            response += string.Format(DIDL_RELEASEDATE, GetDateString(dto.PremiereDate));

            //TODO Add genres to didl;
            response += string.Format(DIDL_GENRE, UNKNOWN);

            if (IsVideo(dto))
            {
                response += string.Format(DESCRIPTION, UNKNOWN);
                response += GetVideoDIDL(dto, streamUrl, streams);

                if (includeImageRes && !string.IsNullOrWhiteSpace(imageUrl))
                {
                    response += string.Format(DIDL_IMAGE_RES, imageUrl);
                }
            }
            else
            {
                var audio = dto as Audio;

                if (audio != null)
                {
                    response += string.Format(DIDL_ARTIST, audio.Artists.FirstOrDefault() ?? UNKNOWN);
                    response += string.Format(DIDL_ALBUM, audio.Album);

                    response += string.Format(DIDL_TRACKNUM, audio.IndexNumber ?? 0);
                }

                response += GetAudioDIDL(dto, streamUrl, streams);

                if (includeImageRes && !string.IsNullOrWhiteSpace(imageUrl))
                {
                    response += string.Format(DIDL_ALBUMIMAGE_RES, imageUrl);
                }
            }

            response += DIDL_END;

            return response;
        }

        private static string GetVideoDIDL(BaseItem dto, string streamUrl, IEnumerable<MediaStream> streams)
        {
            var videostream = streams.Where(stream => stream.Type == MediaStreamType.Video).OrderBy(s => s.IsDefault ? 0 : 1).FirstOrDefault();

            if (videostream == null)
            {
                // TOOD: ???
                return string.Empty;
            }

            return string.Format(DIDL_VIDEO_RES, 
                videostream.BitRate.HasValue ? videostream.BitRate.Value / 10 : 0, 
                GetDurationString(dto), 
                videostream.Width ?? 0, 
                videostream.Height ?? 0, 
                streamUrl);
        }

        private static string GetAudioDIDL(BaseItem dto, string streamUrl, IEnumerable<MediaStream> streams)
        {
            var audiostream = streams.Where(stream => stream.Type == MediaStreamType.Audio).OrderBy(s => s.IsDefault ? 0 : 1).FirstOrDefault();

            if (audiostream == null)
            {
                // TOOD: ???
                return string.Empty;
            }

            return string.Format(DIDL_AUDIO_RES, 
                audiostream.BitRate.HasValue ? audiostream.BitRate.Value / 10 : 16000, 
                GetDurationString(dto), 
                audiostream.SampleRate ?? 0, 
                streamUrl);
        }

        private static string GetImageUrl(BaseItem dto, string serverAddress)
        {
            const ImageType imageType = ImageType.Primary;

            if (!dto.HasImage(imageType))
            {
                dto = dto.Parents.FirstOrDefault(i => i.HasImage(imageType));
            }

            return dto == null ? null : string.Format("{0}/Items/{1}/Images/{2}", serverAddress, dto.Id, imageType);
        }

        private static string GetDurationString(BaseItem dto)
        {
            var duration = TimeSpan.FromTicks(dto.RunTimeTicks.HasValue ? dto.RunTimeTicks.Value : 0);

            // TODO: Bad format string?
            return string.Format("{0}:{1:00}:2{00}.000", duration.Hours, duration.Minutes, duration.Seconds);
        }

        private static string GetDateString(DateTime? date)
        {
            if (!date.HasValue)
                return UNKNOWN;

            return string.Format("{0}-{1:00}-{2:00}", date.Value.Year, date.Value.Month, date.Value.Day);
        }

        private static bool IsVideo(BaseItem item)
        {
            return string.Equals(item.MediaType, MediaType.Video, StringComparison.OrdinalIgnoreCase);
        }
    }
}
