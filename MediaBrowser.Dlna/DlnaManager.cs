using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Dlna.Profiles;
using MediaBrowser.Model.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MediaBrowser.Dlna
{
    public class DlnaManager : IDlnaManager
    {
        private IApplicationPaths _appPaths;
        private readonly IXmlSerializer _xmlSerializer;
        private readonly IFileSystem _fileSystem;
        private readonly IJsonSerializer _jsonSerializer;

        public DlnaManager(IXmlSerializer xmlSerializer, IFileSystem fileSystem, IJsonSerializer jsonSerializer)
        {
            _xmlSerializer = xmlSerializer;
            _fileSystem = fileSystem;
            _jsonSerializer = jsonSerializer;

            GetProfiles();
        }

        public IEnumerable<DeviceProfile> GetProfiles()
        {
            var list = new List<DeviceProfile>();

            //list.Add(new DeviceProfile
            //{
            //    Name = "Samsung TV (B Series)",
            //    ClientType = "DLNA",

            //    Identification = new DeviceIdentification
            //    {
            //        FriendlyName = "^TV$",
            //        ModelNumber = @"1\.0",
            //        ModelName = "Samsung DTV DMR"
            //    },

            //    TranscodingProfiles = new[]
            //    {
            //        new TranscodingProfile
            //        {
            //            Container = "mp3", 
            //            Type = DlnaProfileType.Audio,                        
            //        },
            //         new TranscodingProfile
            //        {
            //            Container = "ts", 
            //            Type = DlnaProfileType.Video
            //        }
            //    },

            //    DirectPlayProfiles = new[]
            //    {
            //        new DirectPlayProfile
            //        {
            //            Container = "mp3", 
            //            Type = DlnaProfileType.Audio,
            //        },
            //        new DirectPlayProfile
            //        {
            //            Container = "mkv", 
            //            Type = DlnaProfileType.Video
            //        },
            //        new DirectPlayProfile
            //        {
            //            Container = "avi", 
            //            Type = DlnaProfileType.Video
            //        },
            //        new DirectPlayProfile
            //        {
            //            Container = "mp4", 
            //            Type = DlnaProfileType.Video
            //        }
            //    },

            //    MediaProfiles = new[]
            //    {
            //        new MediaProfile
            //        {
            //            Container ="avi",
            //            MimeType = "video/x-msvideo",
            //            Type = DlnaProfileType.Video
            //        },

            //        new MediaProfile
            //        {
            //            Container ="mkv",
            //            MimeType = "video/x-mkv",
            //            Type = DlnaProfileType.Video
            //        }
            //    }
            //});

            //list.Add(new DeviceProfile
            //{
            //    Name = "Samsung TV (E/F-series)",
            //    ClientType = "DLNA",

            //    Identification = new DeviceIdentification
            //    {
            //        FriendlyName = @"(^\[TV\][A-Z]{2}\d{2}(E|F)[A-Z]?\d{3,4}.*)|^\[TV\] Samsung|(^\[TV\]Samsung [A-Z]{2}\d{2}(E|F)[A-Z]?\d{3,4}.*)",
            //        ModelNumber = @"(1\.0)|(AllShare1\.0)"
            //    },

            //    TranscodingProfiles = new[]
            //    {
            //        new TranscodingProfile
            //        {
            //            Container = "mp3", 
            //            Type = DlnaProfileType.Audio
            //        },
            //        new TranscodingProfile
            //        {
            //            Container = "ts", 
            //            Type = DlnaProfileType.Video
            //        }
            //    },

            //    DirectPlayProfiles = new[]
            //    {
            //        new DirectPlayProfile
            //        {
            //            Container = "mp3", 
            //            Type = DlnaProfileType.Audio
            //        },
            //        new DirectPlayProfile
            //        {
            //            Container = "mkv", 
            //            Type = DlnaProfileType.Video
            //        },
            //        new DirectPlayProfile
            //        {
            //            Container = "avi", 
            //            Type = DlnaProfileType.Video
            //        },
            //        new DirectPlayProfile
            //        {
            //            Container = "mp4", 
            //            Type = DlnaProfileType.Video
            //        }
            //    },

            //    MediaProfiles = new[]
            //    {
            //        new MediaProfile
            //        {
            //            Container ="avi",
            //            MimeType = "video/x-msvideo",
            //            Type = DlnaProfileType.Video
            //        },

            //        new MediaProfile
            //        {
            //            Container ="mkv",
            //            MimeType = "video/x-mkv",
            //            Type = DlnaProfileType.Video
            //        }
            //    }
            //});

            //list.Add(new DeviceProfile
            //{
            //    Name = "Samsung TV (C/D-series)",
            //    ClientType = "DLNA",

            //    Identification = new DeviceIdentification
            //    {
            //        FriendlyName = @"(^TV-\d{2}C\d{3}.*)|(^\[TV\][A-Z]{2}\d{2}(D)[A-Z]?\d{3,4}.*)|^\[TV\] Samsung",
            //        ModelNumber = @"(1\.0)|(AllShare1\.0)"
            //    },

            //    TranscodingProfiles = new[]
            //    {
            //        new TranscodingProfile
            //        {
            //            Container = "mp3", 
            //            Type = DlnaProfileType.Audio
            //        },
            //         new TranscodingProfile
            //        {
            //            Container = "ts", 
            //            Type = DlnaProfileType.Video
            //        }
            //    },

            //    DirectPlayProfiles = new[]
            //    {
            //        new DirectPlayProfile
            //        {
            //            Container = "mp3", 
            //            Type = DlnaProfileType.Audio
            //        },                  
            //        new DirectPlayProfile
            //        {
            //            Container = "mkv", 
            //            Type = DlnaProfileType.Video
            //        },
            //        new DirectPlayProfile
            //        {
            //            Container = "avi", 
            //            Type = DlnaProfileType.Video
            //        },
            //        new DirectPlayProfile
            //        {
            //            Container = "mp4", 
            //            Type = DlnaProfileType.Video
            //        }
            //    },

            //    MediaProfiles = new[]
            //    {
            //        new MediaProfile
            //        {
            //            Container ="avi",
            //            MimeType = "video/x-msvideo",
            //            Type = DlnaProfileType.Video
            //        },

            //        new MediaProfile
            //        {
            //            Container ="mkv",
            //            MimeType = "video/x-mkv",
            //            Type = DlnaProfileType.Video
            //        }
            //    }
            //});

            list.Add(new Xbox360Profile());

            list.Add(new XboxOneProfile());
            
            list.Add(new SonyBravia2010Profile());

            list.Add(new SonyBravia2011Profile());

            list.Add(new SonyBravia2012Profile());

            list.Add(new SonyBravia2013Profile());

            list.Add(new PanasonicVieraProfile());

            //list.Add(new DeviceProfile
            //{
            //    Name = "Philips (2010-)",
            //    ClientType = "DLNA",

            //    Identification = new DeviceIdentification
            //    {
            //        FriendlyName = ".*PHILIPS.*",
            //        ModelName = "WD TV HD Live"
            //    },

            //    DirectPlayProfiles = new[]
            //    {
            //        new DirectPlayProfile
            //        {
            //            Container = "mp3,wma", 
            //            Type = DlnaProfileType.Audio
            //        },

            //        new DirectPlayProfile
            //        {
            //            Container = "avi", 
            //            Type = DlnaProfileType.Video
            //        },

            //        new DirectPlayProfile
            //        {
            //            Container = "mkv", 
            //            Type = DlnaProfileType.Video
            //        }
            //    },

            //    MediaProfiles = new[]
            //    {
            //        new MediaProfile
            //        {
            //            Container ="avi",
            //            MimeType = "video/avi",
            //            Type = DlnaProfileType.Video
            //        },

            //        new MediaProfile
            //        {
            //            Container ="mkv",
            //            MimeType = "video/x-matroska",
            //            Type = DlnaProfileType.Video
            //        }
            //    }
            //});

            list.Add(new WdtvLiveProfile());

            //list.Add(new DeviceProfile
            //{
            //    // Linksys DMA2100us does not need any transcoding of the formats we support statically
            //    Name = "Linksys DMA2100",
            //    ClientType = "DLNA",

            //    Identification = new DeviceIdentification
            //    {
            //        ModelName = "DMA2100us"
            //    },

            //    DirectPlayProfiles = new[]
            //    {
            //        new DirectPlayProfile
            //        {
            //            Container = "mp3,flac,m4a,wma", 
            //            Type = DlnaProfileType.Audio
            //        },

            //        new DirectPlayProfile
            //        {
            //            Container = "avi,mp4,mkv,ts", 
            //            Type = DlnaProfileType.Video
            //        }
            //    }
            //});

            list.Add(new DenonAvrProfile());

            foreach (var item in list)
            {
                //_xmlSerializer.SerializeToFile(item, "d:\\" + _fileSystem.GetValidFilename(item.Name) + ".xml");
                //_jsonSerializer.SerializeToFile(item, "d:\\" + _fileSystem.GetValidFilename(item.Name) + ".json");
            }

            return list;
        }

        public DeviceProfile GetDefaultProfile()
        {
            return new DefaultProfile();
        }

        public DeviceProfile GetProfile(DeviceIdentification deviceInfo)
        {
            return GetProfiles().FirstOrDefault(i => IsMatch(deviceInfo, i.Identification)) ??
                GetDefaultProfile();
        }

        private bool IsMatch(DeviceIdentification deviceInfo, DeviceIdentification profileInfo)
        {
            if (!string.IsNullOrWhiteSpace(profileInfo.DeviceDescription))
            {
                if (!Regex.IsMatch(deviceInfo.DeviceDescription, profileInfo.DeviceDescription))
                    return false;
            }

            if (!string.IsNullOrWhiteSpace(profileInfo.FriendlyName))
            {
                if (!Regex.IsMatch(deviceInfo.FriendlyName, profileInfo.FriendlyName))
                    return false;
            }

            if (!string.IsNullOrWhiteSpace(profileInfo.Manufacturer))
            {
                if (!Regex.IsMatch(deviceInfo.Manufacturer, profileInfo.Manufacturer))
                    return false;
            }

            if (!string.IsNullOrWhiteSpace(profileInfo.ManufacturerUrl))
            {
                if (!Regex.IsMatch(deviceInfo.ManufacturerUrl, profileInfo.ManufacturerUrl))
                    return false;
            }

            if (!string.IsNullOrWhiteSpace(profileInfo.ModelDescription))
            {
                if (!Regex.IsMatch(deviceInfo.ModelDescription, profileInfo.ModelDescription))
                    return false;
            }

            if (!string.IsNullOrWhiteSpace(profileInfo.ModelName))
            {
                if (!Regex.IsMatch(deviceInfo.ModelName, profileInfo.ModelName))
                    return false;
            }

            if (!string.IsNullOrWhiteSpace(profileInfo.ModelNumber))
            {
                if (!Regex.IsMatch(deviceInfo.ModelNumber, profileInfo.ModelNumber))
                    return false;
            }

            if (!string.IsNullOrWhiteSpace(profileInfo.ModelUrl))
            {
                if (!Regex.IsMatch(deviceInfo.ModelUrl, profileInfo.ModelUrl))
                    return false;
            }

            if (!string.IsNullOrWhiteSpace(profileInfo.SerialNumber))
            {
                if (!Regex.IsMatch(deviceInfo.SerialNumber, profileInfo.SerialNumber))
                    return false;
            }

            return true;
        }
    }
}