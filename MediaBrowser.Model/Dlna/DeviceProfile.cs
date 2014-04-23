using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace MediaBrowser.Model.Dlna
{
    [XmlRoot("Profile")]
    public class DeviceProfile
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        [XmlIgnore]
        public string Id { get; set; }

        [XmlIgnore]
        public DeviceProfileType ProfileType { get; set; }

        /// <summary>
        /// Gets or sets the identification.
        /// </summary>
        /// <value>The identification.</value>
        public DeviceIdentification Identification { get; set; }

        public string FriendlyName { get; set; }
        public string Manufacturer { get; set; }
        public string ManufacturerUrl { get; set; }
        public string ModelName { get; set; }
        public string ModelDescription { get; set; }
        public string ModelNumber { get; set; }
        public string ModelUrl { get; set; }
        public string SerialNumber { get; set; }
        public bool IgnoreTranscodeByteRangeRequests { get; set; }
        public bool EnableAlbumArtInDidl { get; set; }

        public string SupportedMediaTypes { get; set; }

        public string UserId { get; set; }

        public string AlbumArtPn { get; set; }

        public int? MaxAlbumArtWidth { get; set; }
        public int? MaxAlbumArtHeight { get; set; }

        public int? MaxIconWidth { get; set; }
        public int? MaxIconHeight { get; set; }

        public int? MaxBitrate { get; set; }
        
        /// <summary>
        /// Controls the content of the X_DLNADOC element in the urn:schemas-dlna-org:device-1-0 namespace.
        /// </summary>
        public string XDlnaDoc { get; set; }
        /// <summary>
        /// Controls the content of the X_DLNACAP element in the urn:schemas-dlna-org:device-1-0 namespace.
        /// </summary>
        public string XDlnaCap { get; set; }
        /// <summary>
        /// Controls the content of the aggregationFlags element in the urn:schemas-sonycom:av namespace.
        /// </summary>
        public string SonyAggregationFlags { get; set; }

        public string ProtocolInfo { get; set; }

        public int TimelineOffsetSeconds { get; set; }
        public bool RequiresPlainVideoItems { get; set; }
        public bool RequiresPlainFolders { get; set; }

        /// <summary>
        /// Gets or sets the direct play profiles.
        /// </summary>
        /// <value>The direct play profiles.</value>
        public DirectPlayProfile[] DirectPlayProfiles { get; set; }

        /// <summary>
        /// Gets or sets the transcoding profiles.
        /// </summary>
        /// <value>The transcoding profiles.</value>
        public TranscodingProfile[] TranscodingProfiles { get; set; }

        public ContainerProfile[] ContainerProfiles { get; set; }

        public CodecProfile[] CodecProfiles { get; set; }
        public ResponseProfile[] ResponseProfiles { get; set; }

        public DeviceProfile()
        {
            DirectPlayProfiles = new DirectPlayProfile[] { };
            TranscodingProfiles = new TranscodingProfile[] { };
            ResponseProfiles = new ResponseProfile[] { };
            CodecProfiles = new CodecProfile[] { };
            ContainerProfiles = new ContainerProfile[] { };

            SupportedMediaTypes = "Audio,Photo,Video";
        }

        public List<string> GetSupportedMediaTypes()
        {
            return (SupportedMediaTypes ?? string.Empty).Split(',').Where(i => !string.IsNullOrEmpty(i)).ToList();
        }

        public TranscodingProfile GetAudioTranscodingProfile(string container, string audioCodec)
        {
            container = (container ?? string.Empty).TrimStart('.');

            return TranscodingProfiles.FirstOrDefault(i =>
            {
                if (i.Type != DlnaProfileType.Audio)
                {
                    return false;
                }

                if (!string.Equals(container, i.Container, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                if (!i.GetAudioCodecs().Contains(audioCodec ?? string.Empty))
                {
                    return false;
                }

                return true;
            });
        }

        public TranscodingProfile GetVideoTranscodingProfile(string container, string audioCodec, string videoCodec)
        {
            container = (container ?? string.Empty).TrimStart('.');

            return TranscodingProfiles.FirstOrDefault(i =>
            {
                if (i.Type != DlnaProfileType.Video)
                {
                    return false;
                }

                if (!string.Equals(container, i.Container, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                if (!i.GetAudioCodecs().Contains(audioCodec ?? string.Empty))
                {
                    return false;
                }

                if (!string.Equals(videoCodec, i.VideoCodec, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                return true;
            });
        }

        public ResponseProfile GetAudioMediaProfile(string container, string audioCodec)
        {
            container = (container ?? string.Empty).TrimStart('.');

            return ResponseProfiles.FirstOrDefault(i =>
            {
                if (i.Type != DlnaProfileType.Audio)
                {
                    return false;
                }

                var containers = i.GetContainers().ToList();
                if (containers.Count > 0 && !containers.Contains(container))
                {
                    return false;
                }

                var audioCodecs = i.GetAudioCodecs().ToList();
                if (audioCodecs.Count > 0 && !audioCodecs.Contains(audioCodec ?? string.Empty))
                {
                    return false;
                }

                return true;
            });
        }

        public ResponseProfile GetVideoMediaProfile(string container, string audioCodec, string videoCodec)
        {
            container = (container ?? string.Empty).TrimStart('.');

            return ResponseProfiles.FirstOrDefault(i =>
            {
                if (i.Type != DlnaProfileType.Video)
                {
                    return false;
                }

                var containers = i.GetContainers().ToList();
                if (containers.Count > 0 && !containers.Contains(container))
                {
                    return false;
                }

                var audioCodecs = i.GetAudioCodecs().ToList();
                if (audioCodecs.Count > 0 && !audioCodecs.Contains(audioCodec ?? string.Empty))
                {
                    return false;
                }

                var videoCodecs = i.GetVideoCodecs().ToList();
                if (videoCodecs.Count > 0 && !videoCodecs.Contains(videoCodec ?? string.Empty))
                {
                    return false;
                }

                return true;
            });
        }

        public ResponseProfile GetPhotoMediaProfile(string container)
        {
            container = (container ?? string.Empty).TrimStart('.');

            return ResponseProfiles.FirstOrDefault(i =>
            {
                if (i.Type != DlnaProfileType.Photo)
                {
                    return false;
                }

                var containers = i.GetContainers().ToList();
                if (containers.Count > 0 && !containers.Contains(container))
                {
                    return false;
                }

                return true;
            });
        }
    }
}
