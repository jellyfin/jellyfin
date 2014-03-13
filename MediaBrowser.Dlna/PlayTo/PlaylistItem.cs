using MediaBrowser.Controller.Entities;
using MediaBrowser.Dlna.PlayTo.Configuration;
using System;

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

        public static PlaylistItem GetBasicConfig(BaseItem item, TranscodeSettings[] profileTranscodings)
        {

            var playlistItem = new PlaylistItem();
            playlistItem.ItemId = item.Id.ToString();

            if (string.Equals(item.MediaType, Model.Entities.MediaType.Video, StringComparison.OrdinalIgnoreCase))
            {
                playlistItem.IsVideo = true;
            }
            else
            {
                playlistItem.IsAudio = true;
            }


            var path = item.Path.ToLower();

            //Check the DlnaProfile associated with the renderer
            if (profileTranscodings != null)
            {
                foreach (TranscodeSettings transcodeSetting in profileTranscodings)
                {
                    if (string.IsNullOrWhiteSpace(transcodeSetting.Container))
                        continue;
                    if (path.EndsWith(transcodeSetting.Container) && !string.IsNullOrWhiteSpace(transcodeSetting.TargetContainer))
                    {
                        playlistItem.Transcode = true;
                        playlistItem.FileFormat = transcodeSetting.TargetContainer;
                        
                        if (string.IsNullOrWhiteSpace(transcodeSetting.MimeType))
                            playlistItem.MimeType = transcodeSetting.MimeType;
                        
                        return playlistItem;
                    }
                    if (path.EndsWith(transcodeSetting.Container) && !string.IsNullOrWhiteSpace(transcodeSetting.MimeType))
                    {
                        playlistItem.Transcode = false;
                        playlistItem.FileFormat = transcodeSetting.Container;
                        playlistItem.MimeType = transcodeSetting.MimeType;
                        return playlistItem;
                    }
                }
            }
            if (playlistItem.IsVideo)
            {

                //Check to see if we support serving the format statically
                foreach (string supported in PlayToConfiguration.SupportedStaticFormats)
                {
                    if (path.EndsWith(supported))
                    {
                        playlistItem.Transcode = false;
                        playlistItem.FileFormat = supported;
                        return playlistItem;
                    }
                }

                playlistItem.Transcode = true;
                playlistItem.FileFormat = "ts";
            }
            else
            {
                foreach (string supported in PlayToConfiguration.SupportedStaticFormats)
                {
                    if (path.EndsWith(supported))
                    {
                        playlistItem.Transcode = false;
                        playlistItem.FileFormat = supported;
                        return playlistItem;
                    }
                }

                playlistItem.Transcode = true;
                playlistItem.FileFormat = "mp3";
            }

            return playlistItem;
        }
    }
}
