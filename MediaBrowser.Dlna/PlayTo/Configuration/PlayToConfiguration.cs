namespace MediaBrowser.Dlna.PlayTo.Configuration
{
    public class PlayToConfiguration
    {
        private static readonly string[] _supportedStaticFormats = { "mp3", "flac", "m4a", "wma", "avi", "mp4", "mkv", "ts" };
        public static string[] SupportedStaticFormats
        {
            get
            {
                return _supportedStaticFormats;
            }
        }

        private static readonly DlnaProfile[] _profiles = GetDefaultProfiles();
        public static DlnaProfile[] Profiles
        {
            get
            {
                return _profiles;
            }
        }

        private static DlnaProfile[] GetDefaultProfiles()
        {
            var profile0 = new DlnaProfile
            {
                Name = "Samsung TV (B Series) [Profile]",
                ClientType = "DLNA",
                FriendlyName = "^TV$",
                ModelNumber = @"1\.0",
                ModelName = "Samsung DTV DMR",
                TranscodeSettings = new[]
                {
                    new TranscodeSettings {Container = "mkv", MimeType = "x-mkv"},
                    new TranscodeSettings {Container = "flac", TargetContainer = "mp3"},
                    new TranscodeSettings {Container = "m4a", TargetContainer = "mp3"}
                }
            };

            var profile1 = new DlnaProfile
            {
                Name = "Samsung TV (E/F-series) [Profile]",
                ClientType = "DLNA",
                FriendlyName = @"(^\[TV\][A-Z]{2}\d{2}(E|F)[A-Z]?\d{3,4}.*)|^\[TV\] Samsung",
                ModelNumber = @"(1\.0)|(AllShare1\.0)",
                TranscodeSettings = new[]
                {
                    new TranscodeSettings {Container = "mkv", MimeType = "x-mkv"},
                    new TranscodeSettings {Container = "flac", TargetContainer = "mp3"},
                    new TranscodeSettings {Container = "m4a", TargetContainer = "mp3"}
                }
            };

            var profile2 = new DlnaProfile
            {
                Name = "Samsung TV (C/D-series) [Profile]",
                ClientType = "DLNA",
                FriendlyName = @"(^TV-\d{2}C\d{3}.*)|(^\[TV\][A-Z]{2}\d{2}(D)[A-Z]?\d{3,4}.*)|^\[TV\] Samsung",
                ModelNumber = @"(1\.0)|(AllShare1\.0)",
                TranscodeSettings = new[]
                {
                    new TranscodeSettings {Container = "mkv", MimeType = "x-mkv"},
                    new TranscodeSettings {Container = "flac", TargetContainer = "mp3"},
                    new TranscodeSettings {Container = "m4a", TargetContainer = "mp3"}
                }
            };

            var profile3 = new DlnaProfile
            {
                Name = "Xbox 360 [Profile]",
                ClientType = "DLNA",
                ModelName = "Xbox 360",
                TranscodeSettings = new[]
                {
                    new TranscodeSettings {Container = "mkv", TargetContainer = "ts"},
                    new TranscodeSettings {Container = "flac", TargetContainer = "mp3"},
                    new TranscodeSettings {Container = "m4a", TargetContainer = "mp3"}
                }
            };

            var profile4 = new DlnaProfile
            {
                Name = "Xbox One [Profile]",
                ModelName = "Xbox One",
                ClientType = "DLNA",
                FriendlyName = "Xbox-SystemOS",
                TranscodeSettings = new[]
                {
                    new TranscodeSettings {Container = "mkv", TargetContainer = "ts"},
                    new TranscodeSettings {Container = "flac", TargetContainer = "mp3"},
                    new TranscodeSettings {Container = "m4a", TargetContainer = "mp3"}
                }
            };

            var profile5 = new DlnaProfile
            {
                Name = "Sony Bravia TV (2012)",
                ClientType = "TV",
                FriendlyName = @"BRAVIA KDL-\d{2}[A-Z]X\d5(\d|G).*",
                TranscodeSettings = TranscodeSettings.GetDefaultTranscodingSettings()
            };

            //WDTV does not need any transcoding of the formats we support statically
            var profile6 = new DlnaProfile
            {
                Name = "WDTV Live [Profile]",
                ClientType = "DLNA",
                ModelName = "WD TV HD Live",
                TranscodeSettings = new TranscodeSettings[] { }
            };

            var profile7 = new DlnaProfile
           {
               //Linksys DMA2100us does not need any transcoding of the formats we support statically
               Name = "Linksys DMA2100 [Profile]",
               ClientType = "DLNA",
               ModelName = "DMA2100us",
               TranscodeSettings = new TranscodeSettings[] { }
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
    }
}
