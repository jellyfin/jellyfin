using MediaBrowser.Model.Logging;
using System;
using System.IO;
using System.Xml.Serialization;
namespace MediaBrowser.Dlna.PlayTo.Configuration
{
    public class PlayToConfiguration
    {
        [XmlIgnore]
        public static PlayToConfiguration Instance
        {
            get;
            private set;
        }

        private static readonly string[] _supportedStaticFormats = { "mp3", "flac", "m4a", "wma", "avi", "mp4", "mkv", "ts" };  
        
        [XmlIgnore]
        public string[] SupportedStaticFormats
        {
            get
            {
                return _supportedStaticFormats;
            }
        }

        public DlnaProfile[] Profiles
        { get; set; }

        public static DlnaProfile[] GetDefaultProfiles()
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
                    new TranscodeSetting {Container = "mkv", MimeType = "x-mkv"},
                    new TranscodeSetting {Container = "flac", TargetContainer = "mp3"},
                    new TranscodeSetting {Container = "m4a", TargetContainer = "mp3"}
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
                    new TranscodeSetting {Container = "mkv", MimeType = "x-mkv"},
                    new TranscodeSetting {Container = "flac", TargetContainer = "mp3"},
                    new TranscodeSetting {Container = "m4a", TargetContainer = "mp3"}
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
                    new TranscodeSetting {Container = "mkv", MimeType = "x-mkv"},
                    new TranscodeSetting {Container = "flac", TargetContainer = "mp3"},
                    new TranscodeSetting {Container = "m4a", TargetContainer = "mp3"}
                }
            };

            var profile3 = new DlnaProfile
            {
                Name = "Xbox 360 [Profile]",
                ClientType = "DLNA",
                ModelName = "Xbox 360",
                TranscodeSettings = new[]
                {
                    new TranscodeSetting {Container = "mkv", TargetContainer = "ts"},
                    new TranscodeSetting {Container = "flac", TargetContainer = "mp3"},
                    new TranscodeSetting {Container = "m4a", TargetContainer = "mp3"}
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
                    new TranscodeSetting {Container = "mkv", TargetContainer = "ts"},
                    new TranscodeSetting {Container = "flac", TargetContainer = "mp3"},
                    new TranscodeSetting {Container = "m4a", TargetContainer = "mp3"}
                }
            };

            var profile5 = new DlnaProfile
            {
                Name = "Sony Bravia TV (2012)",
                ClientType = "TV",
                FriendlyName = @"BRAVIA KDL-\d{2}[A-Z]X\d5(\d|G).*",
                TranscodeSettings = TranscodeSetting.GetDefaultTranscodingSettings()
            };

            //WDTV does not need any transcoding of the formats we support statically
            var profile6 = new DlnaProfile
            {
                Name = "WDTV Live [Profile]",
                ClientType = "DLNA",
                ModelName = "WD TV HD Live",
                TranscodeSettings = new TranscodeSetting[] { }
            };

            var profile7 = new DlnaProfile
           {
               //Linksys DMA2100us does not need any transcoding of the formats we support statically
               Name = "Linksys DMA2100 [Profile]",
               ClientType = "DLNA",
               ModelName = "DMA2100us",
               TranscodeSettings = new TranscodeSetting[] { }
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

        public static void Load(string path, ILogger logger)
        {
            if (!File.Exists(path))
            {
               Instance = CreateNewSettingsFile(path, logger);

            }
            else
            {
                try
                {
                    XmlSerializer deserializer = new XmlSerializer(typeof(PlayToConfiguration));
                    using (var textReader = new StreamReader(path))
                    {
                        var configuration = (PlayToConfiguration)deserializer.Deserialize(textReader);
                        Instance = configuration;
                        textReader.Close();
                    }
                }
                catch (Exception e)
                {
                    // Something went wrong with the loading of the file
                    // Maybe a user created a faulty config? 
                    // Delete the file and use default settings
                    logger.ErrorException("Error loading PlayTo configuration", e);
                    Instance = CreateNewSettingsFile(path, logger);
                }
            } 
        }

        private static PlayToConfiguration CreateNewSettingsFile(string path, ILogger logger)
        {
            var defaultConfig = new PlayToConfiguration();
            defaultConfig.Profiles = PlayToConfiguration.GetDefaultProfiles();

            try
            {
                if (File.Exists(path))
                    File.Delete(path);

                XmlSerializer serializer = new XmlSerializer(typeof(PlayToConfiguration));

                using (var fileStream = new StreamWriter(path))
                {
                    serializer.Serialize(fileStream, defaultConfig);
                    fileStream.Close();
                }
            }
            catch(Exception e)
            {
                //Something went wrong deleting or creating the file, Log and continue with the default profile unsaved
                logger.ErrorException("Error creating default PlayTo configuration", e);
            }
            return defaultConfig;
        }

    }
}
