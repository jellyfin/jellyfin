using System;
using System.Text.RegularExpressions;


namespace MediaBrowser.Dlna.PlayTo.Configuration
{
    public class TranscodeSetting
    {
        /// <summary>
        /// Gets or sets the container.
        /// </summary>
        /// <value>
        /// The container.
        /// </value>
        public string Container { get; set; }

        /// <summary>
        /// Gets or sets the target container.
        /// </summary>
        /// <value>
        /// The target container.
        /// </value>
        public string TargetContainer { get; set; }

        /// <summary>
        /// Gets or sets the Mimetype to enforce
        /// </summary>
        /// <value>
        /// The MimeType.
        /// </value>
        public string MimeType { get; set; }

        /// <summary>
        /// The default transcoding settings
        /// </summary>
        private static readonly TranscodeSetting[] DefaultTranscodingSettings =
        { 
            new TranscodeSetting { Container = "mkv", TargetContainer = "ts" }, 
            new TranscodeSetting { Container = "flac", TargetContainer = "mp3" },
            new TranscodeSetting { Container = "m4a", TargetContainer = "mp3" }
        };

        public static TranscodeSetting[] GetDefaultTranscodingSettings()
        {
            return DefaultTranscodingSettings;
        }

        /// <summary>
        /// Gets the profile settings.
        /// </summary>
        /// <param name="deviceProperties">The device properties.</param>
        /// <returns>The TranscodeSettings for the device</returns>
        public static TranscodeSetting[] GetProfileSettings(DeviceProperties deviceProperties)
        {
            foreach (var profile in PlayToConfiguration.Instance.Profiles)
            {
                if (!string.IsNullOrEmpty(profile.FriendlyName))
                {
                    if (!Regex.IsMatch(deviceProperties.Name, profile.FriendlyName))
                        continue;
                }

                if (!string.IsNullOrEmpty(profile.ModelNumber))
                {
                    if (!Regex.IsMatch(deviceProperties.ModelNumber, profile.ModelNumber))
                        continue;
                }

                if (!string.IsNullOrEmpty(profile.ModelName))
                {
                    if (!Regex.IsMatch(deviceProperties.ModelName, profile.ModelName))
                        continue;
                }

                deviceProperties.DisplayName = profile.Name;
                deviceProperties.ClientType = profile.ClientType;
                return profile.TranscodeSettings;

            }

            // Since we don't have alot of info about different devices we go down the safe
            // route abd use the default transcoding settings if no profile exist
            return GetDefaultTranscodingSettings();
        }
    }
}
