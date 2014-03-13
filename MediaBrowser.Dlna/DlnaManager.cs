using MediaBrowser.Controller.Dlna;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MediaBrowser.Dlna
{
    public class DlnaManager : IDlnaManager
    {
        public IEnumerable<DlnaProfile> GetProfiles()
        {
            var profile0 = new DlnaProfile
            {
                Name = "Samsung TV (B Series) [Profile]",
                ClientType = "DLNA",
                FriendlyName = "^TV$",
                ModelNumber = @"1\.0",
                ModelName = "Samsung DTV DMR",

                TranscodingProfiles = new[]
                {
                    new TranscodingProfile
                    {
                        Container = "mp3", 
                        Type = DlnaProfileType.Audio
                    }
                },

                DirectPlayProfiles = new[]
                {
                    new DirectPlayProfile
                    {
                        Containers = new[]{"mkv"}, 
                        MimeType = "x-mkv", 
                        Type = DlnaProfileType.Video
                    }
                }
            };

            var profile1 = new DlnaProfile
            {
                Name = "Samsung TV (E/F-series) [Profile]",
                ClientType = "DLNA",
                FriendlyName = @"(^\[TV\][A-Z]{2}\d{2}(E|F)[A-Z]?\d{3,4}.*)|^\[TV\] Samsung",
                ModelNumber = @"(1\.0)|(AllShare1\.0)",

                TranscodingProfiles = new[]
                {
                    new TranscodingProfile
                    {
                        Container = "mp3", 
                        Type = DlnaProfileType.Audio
                    }
                },

                DirectPlayProfiles = new[]
                {
                    new DirectPlayProfile
                    {
                        Containers = new[]{"mkv"}, 
                        MimeType = "x-mkv", 
                        Type = DlnaProfileType.Video
                    }
                }
            };

            var profile2 = new DlnaProfile
            {
                Name = "Samsung TV (C/D-series) [Profile]",
                ClientType = "DLNA",
                FriendlyName = @"(^TV-\d{2}C\d{3}.*)|(^\[TV\][A-Z]{2}\d{2}(D)[A-Z]?\d{3,4}.*)|^\[TV\] Samsung",
                ModelNumber = @"(1\.0)|(AllShare1\.0)",
                TranscodingProfiles = new[]
                {
                    new TranscodingProfile
                    {
                        Container = "mp3", 
                        Type = DlnaProfileType.Audio
                    }
                },

                DirectPlayProfiles = new[]
                {
                    new DirectPlayProfile
                    {
                        Containers = new[]{"mkv"}, 
                        MimeType = "x-mkv", 
                        Type = DlnaProfileType.Video
                    }
                }
            };

            var profile3 = new DlnaProfile
            {
                Name = "Xbox 360 [Profile]",
                ClientType = "DLNA",
                ModelName = "Xbox 360",
                TranscodingProfiles = new[]
                {
                    new TranscodingProfile
                    {
                        Container = "mp3", 
                        Type = DlnaProfileType.Audio
                    },
                    new TranscodingProfile
                    {
                        Container = "ts", 
                        Type = DlnaProfileType.Video
                    }
                }
            };

            var profile4 = new DlnaProfile
            {
                Name = "Xbox One [Profile]",
                ModelName = "Xbox One",
                ClientType = "DLNA",
                FriendlyName = "Xbox-SystemOS",
                TranscodingProfiles = new[]
                {
                    new TranscodingProfile
                    {
                        Container = "mp3", 
                        Type = DlnaProfileType.Audio
                    },
                    new TranscodingProfile
                    {
                        Container = "ts", 
                        Type = DlnaProfileType.Video
                    }
                }
            };

            var profile5 = new DlnaProfile
            {
                Name = "Sony Bravia TV (2012)",
                ClientType = "TV",
                FriendlyName = @"BRAVIA KDL-\d{2}[A-Z]X\d5(\d|G).*"
            };

            //WDTV does not need any transcoding of the formats we support statically
            var profile6 = new DlnaProfile
            {
                Name = "WDTV Live [Profile]",
                ClientType = "DLNA",
                ModelName = "WD TV HD Live"
            };

            var profile7 = new DlnaProfile
            {
                //Linksys DMA2100us does not need any transcoding of the formats we support statically
                Name = "Linksys DMA2100 [Profile]",
                ClientType = "DLNA",
                ModelName = "DMA2100us"
            };

            return new[] 
            {
                profile0,
                profile1,
                profile2,
                profile3,
                profile4,
                profile5,
                profile6,
                profile7
            };
        }

        public DlnaProfile GetDefaultProfile()
        {
            return new DlnaProfile();
        }

        public DlnaProfile GetProfile(string friendlyName, string modelName, string modelNumber)
        {
            foreach (var profile in GetProfiles())
            {
                if (!string.IsNullOrEmpty(profile.FriendlyName))
                {
                    if (!Regex.IsMatch(friendlyName, profile.FriendlyName))
                        continue;
                }

                if (!string.IsNullOrEmpty(profile.ModelNumber))
                {
                    if (!Regex.IsMatch(modelNumber, profile.ModelNumber))
                        continue;
                }

                if (!string.IsNullOrEmpty(profile.ModelName))
                {
                    if (!Regex.IsMatch(modelName, profile.ModelName))
                        continue;
                }

                return profile;

            }
            return GetDefaultProfile();
        }
    }
}
