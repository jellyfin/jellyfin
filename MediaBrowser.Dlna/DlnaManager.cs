using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Dlna;
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

        public DlnaManager(IXmlSerializer xmlSerializer, IFileSystem fileSystem)
        {
            _xmlSerializer = xmlSerializer;
            _fileSystem = fileSystem;

            //GetProfiles();
        }

        public IEnumerable<DeviceProfile> GetProfiles()
        {
            var list = new List<DeviceProfile>();

            list.Add(new DeviceProfile
            {
                Name = "Samsung TV (B Series)",
                ClientType = "DLNA",

                Identification = new DeviceIdentification
                {
                    FriendlyName = "^TV$",
                    ModelNumber = @"1\.0",
                    ModelName = "Samsung DTV DMR"
                },

                TranscodingProfiles = new[]
                {
                    new TranscodingProfile
                    {
                        Container = "mp3", 
                        Type = DlnaProfileType.Audio,                        
                    },
                     new TranscodingProfile
                    {
                        Container = "ts", 
                        Type = DlnaProfileType.Video
                    }
                },

                DirectPlayProfiles = new[]
                {
                    new DirectPlayProfile
                    {
                        Containers = new[]{"mp3"}, 
                        Type = DlnaProfileType.Audio,
                    },
                    new DirectPlayProfile
                    {
                        Containers = new[]{"mkv"}, 
                        MimeType = "x-mkv", 
                        Type = DlnaProfileType.Video
                    },
                    new DirectPlayProfile
                    {
                        Containers = new[]{"avi"}, 
                        MimeType = "x-msvideo", 
                        Type = DlnaProfileType.Video
                    },
                    new DirectPlayProfile
                    {
                        Containers = new[]{"mp4"},                       
                        Type = DlnaProfileType.Video
                    }
                }
            });

            list.Add(new DeviceProfile
            {
                Name = "Samsung TV (E/F-series)",
                ClientType = "DLNA",

                Identification = new DeviceIdentification
                {
                    FriendlyName = @"(^\[TV\][A-Z]{2}\d{2}(E|F)[A-Z]?\d{3,4}.*)|^\[TV\] Samsung|(^\[TV\]Samsung [A-Z]{2}\d{2}(E|F)[A-Z]?\d{3,4}.*)",
                    ModelNumber = @"(1\.0)|(AllShare1\.0)"
                },

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
                },

                DirectPlayProfiles = new[]
                {
                    new DirectPlayProfile
                    {
                        Containers = new[]{"mp3"}, 
                        Type = DlnaProfileType.Audio
                    },
                    new DirectPlayProfile
                    {
                        Containers = new[]{"mkv"}, 
                        MimeType = "x-mkv", 
                        Type = DlnaProfileType.Video
                    },
                    new DirectPlayProfile
                    {
                        Containers = new[]{"avi"}, 
                        MimeType = "x-msvideo", 
                        Type = DlnaProfileType.Video
                    },
                    new DirectPlayProfile
                    {
                        Containers = new[]{"mp4"},                       
                        Type = DlnaProfileType.Video
                    }
                }
            });

            list.Add(new DeviceProfile
            {
                Name = "Samsung TV (C/D-series)",
                ClientType = "DLNA",

                Identification = new DeviceIdentification
                {
                    FriendlyName = @"(^TV-\d{2}C\d{3}.*)|(^\[TV\][A-Z]{2}\d{2}(D)[A-Z]?\d{3,4}.*)|^\[TV\] Samsung",
                    ModelNumber = @"(1\.0)|(AllShare1\.0)"
                },

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
                },

                DirectPlayProfiles = new[]
                {
                    new DirectPlayProfile
                    {
                        Containers = new[]{"mp3"}, 
                        Type = DlnaProfileType.Audio
                    },                  
                    new DirectPlayProfile
                    {
                        Containers = new[]{"mkv"}, 
                        MimeType = "x-mkv", 
                        Type = DlnaProfileType.Video
                    },
                    new DirectPlayProfile
                    {
                        Containers = new[]{"avi"}, 
                        MimeType = "x-msvideo", 
                        Type = DlnaProfileType.Video
                    },
                    new DirectPlayProfile
                    {
                        Containers = new[]{"mp4"},                       
                        Type = DlnaProfileType.Video
                    }
                }
            });

            list.Add(new DeviceProfile
            {
                Name = "Xbox 360",
                ClientType = "DLNA",

                Identification = new DeviceIdentification
                {
                    ModelName = "Xbox 360"
                },

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
                },

                DirectPlayProfiles = new[]
                {
                    new DirectPlayProfile
                    {
                        Containers = new[]{"mp3"}, 
                        Type = DlnaProfileType.Audio
                    },
                    new DirectPlayProfile
                    {
                        Containers = new[]{"avi"}, 
                        MimeType = "avi", 
                        Type = DlnaProfileType.Video
                    }
                }
            });

            list.Add(new DeviceProfile
            {
                Name = "Xbox One",
                ClientType = "DLNA",

                Identification = new DeviceIdentification
                {
                    ModelName = "Xbox One",
                    FriendlyName = "Xbox-SystemOS"
                },

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
                },

                DirectPlayProfiles = new[]
                {
                    new DirectPlayProfile
                    {
                        Containers = new[]{"mp3"}, 
                        Type = DlnaProfileType.Audio
                    },
                    new DirectPlayProfile
                    {
                        Containers = new[]{"avi"}, 
                        MimeType = "x-msvideo", 
                        Type = DlnaProfileType.Video
                    }
                }
            });

            list.Add(new DeviceProfile
            {
                Name = "Sony Bravia (2012)",
                ClientType = "DLNA",

                Identification = new DeviceIdentification
                {
                    FriendlyName = @"BRAVIA KDL-\d{2}[A-Z]X\d5(\d|G).*"
                },

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
                },

                DirectPlayProfiles = new[]
                {
                    new DirectPlayProfile
                    {
                        Containers = new[]{"mp3"}, 
                        Type = DlnaProfileType.Audio
                    },
                    new DirectPlayProfile
                    {
                        Containers = new[]{"avi"}, 
                        Type = DlnaProfileType.Video,
                        MimeType = "avi"
                    }
                }
            });

            list.Add(new DeviceProfile
            {
                Name = "Sony Bravia (2013)",
                ClientType = "DLNA",

                Identification = new DeviceIdentification
                {
                    FriendlyName = @"BRAVIA (KDL-\d{2}W[689]\d{2}A.*)|(KD-\d{2}X9\d{3}A.*)"
                },

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
                        Type = DlnaProfileType.Video,
                        MimeType = "mpeg"
                    }
                },

                DirectPlayProfiles = new[]
                {
                    new DirectPlayProfile
                    {
                        Containers = new[]{"mp3"}, 
                        Type = DlnaProfileType.Audio
                    },
                    new DirectPlayProfile
                    {
                        Containers = new[]{"wma"}, 
                        Type = DlnaProfileType.Audio,
                        MimeType = "x-ms-wma"
                    },                    
                    new DirectPlayProfile
                    {
                        Containers = new[]{"avi"}, 
                        Type = DlnaProfileType.Video,
                        MimeType = "avi"
                    },
                     new DirectPlayProfile
                    {
                        Containers = new[]{"mp4"}, 
                        Type = DlnaProfileType.Video,
                        MimeType = "mp4"
                    }
                }
            });

            list.Add(new DeviceProfile
            {
                //Panasonic Viera (2011|2012) Without AVI Support
                Name = "Panasonic Viera E/S/ST/VT (2011)",
                ClientType = "DLNA",

                Identification = new DeviceIdentification
                {
                    FriendlyName = @"(VIERA (E|S)T?(3|5)0?.*)|(VIERA VT30.*)",
                    Manufacturer = "Panasonic"
                },

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
                },

                DirectPlayProfiles = new[]
                {
                    new DirectPlayProfile
                    {
                        Containers = new[]{"mp3"}, 
                        Type = DlnaProfileType.Audio
                    },
                    new DirectPlayProfile
                    {
                        Containers = new[]{"mkv"}, 
                        Type = DlnaProfileType.Video                        
                    }
                }
            });

            list.Add(new DeviceProfile
            {
                //Panasonic Viera (2011|2012) With AVI Support
                Name = "Panasonic Viera G/GT/DT/UT/VT (2011/2012)",
                ClientType = "DLNA",

                Identification = new DeviceIdentification
                {
                    FriendlyName = @"(VIERA (G|D|U)T?(3|5)0?.*)|(VIERA VT50.*)",
                    Manufacturer = "Panasonic"
                },

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
                },

                DirectPlayProfiles = new[]
                {
                    new DirectPlayProfile
                    {
                        Containers = new[]{"mp3"}, 
                        Type = DlnaProfileType.Audio
                    },
                    new DirectPlayProfile
                    {
                        Containers = new[]{"mkv"}, 
                        Type = DlnaProfileType.Video                        
                    },
                     new DirectPlayProfile
                    {
                        Containers = new[]{"avi"}, 
                        Type = DlnaProfileType.Video                        ,
                        MimeType="divx"
                    }
                }
            });

            //WDTV does not need any transcoding of the formats we support statically
            list.Add(new DeviceProfile
            {
                Name = "Philips (2010-)",
                ClientType = "DLNA",

                Identification = new DeviceIdentification
                {
                    FriendlyName = ".*PHILIPS.*",
                    ModelName = "WD TV HD Live"
                },

                DirectPlayProfiles = new[]
                {
                    new DirectPlayProfile
                    {
                        Containers = new[]{"mp3", "wma"}, 
                        Type = DlnaProfileType.Audio
                    },

                    new DirectPlayProfile
                    {
                        Containers = new[]{"avi"}, 
                        Type = DlnaProfileType.Video,
                        MimeType = "avi"
                    },

                    new DirectPlayProfile
                    {
                        Containers = new[]{"mkv"}, 
                        Type = DlnaProfileType.Video,
                        MimeType = "x-matroska"
                    }
                }
            });

            //WDTV does not need any transcoding of the formats we support statically
            list.Add(new DeviceProfile
            {
                Name = "WDTV Live",
                ClientType = "DLNA",

                Identification = new DeviceIdentification
                {
                    ModelName = "WD TV HD Live"
                },

                DirectPlayProfiles = new[]
                {
                    new DirectPlayProfile
                    {
                        Containers = new[]{"mp3", "flac", "m4a", "wma"}, 
                        Type = DlnaProfileType.Audio
                    },

                    new DirectPlayProfile
                    {
                        Containers = new[]{"avi", "mp4", "mkv", "ts"}, 
                        Type = DlnaProfileType.Video
                    }
                }
            });

            list.Add(new DeviceProfile
            {
                //Linksys DMA2100us does not need any transcoding of the formats we support statically
                Name = "Linksys DMA2100",
                ClientType = "DLNA",

                Identification = new DeviceIdentification
                {
                    ModelName = "DMA2100us"
                },

                DirectPlayProfiles = new[]
                {
                    new DirectPlayProfile
                    {
                        Containers = new[]{"mp3", "flac", "m4a", "wma"}, 
                        Type = DlnaProfileType.Audio
                    },

                    new DirectPlayProfile
                    {
                        Containers = new[]{"avi", "mp4", "mkv", "ts"}, 
                        Type = DlnaProfileType.Video
                    }
                }
            });

            list.Add(new DeviceProfile
            {                
                Name = "Denon AVR",
                ClientType = "DLNA",       
         
                Identification = new DeviceIdentification
                {
                    FriendlyName = @"Denon:\[AVR:.*",
                    Manufacturer = "Denon"
                },

                DirectPlayProfiles = new[]
                {
                    new DirectPlayProfile
                    {
                        Containers = new[]{"mp3", "flac", "m4a", "wma"}, 
                        Type = DlnaProfileType.Audio
                    },                   
                }
            });

            foreach (var item in list)
            {
                //_xmlSerializer.SerializeToFile(item, "d:\\" + _fileSystem.GetValidFilename(item.Name));
            }

            return list;
        }

        public DeviceProfile GetDefaultProfile()
        {
            return new DeviceProfile
            {
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
                },

                DirectPlayProfiles = new[]
                {
                    new DirectPlayProfile
                    {
                        Containers = new[]{"mp3", "wma"}, 
                        Type = DlnaProfileType.Audio
                    },

                    new DirectPlayProfile
                    {
                        Containers = new[]{"avi", "mp4"}, 
                        Type = DlnaProfileType.Video
                    }
                }
            };
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