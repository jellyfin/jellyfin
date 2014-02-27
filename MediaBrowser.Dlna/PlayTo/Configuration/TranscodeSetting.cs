using System;

namespace MediaBrowser.Dlna.PlayTo.Configuration
{
    public class TranscodeSettings
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
        /// The default transcoding settings
        /// </summary>
        private static readonly TranscodeSettings[] DefaultTranscodingSettings =
        { 
            new TranscodeSettings { Container = "mkv", TargetContainer = "ts" }, 
            new TranscodeSettings { Container = "flac", TargetContainer = "mp3" },
            new TranscodeSettings { Container = "m4a", TargetContainer = "mp3" }
        };

        public static TranscodeSettings[] GetDefaultTranscodingSettings()
        {
            return DefaultTranscodingSettings;
        }

        /// <summary>
        /// Gets the profile settings.
        /// </summary>
        /// <param name="deviceProperties">The device properties.</param>
        /// <returns>The TranscodeSettings for the device</returns>
        public static TranscodeSettings[] GetProfileSettings(DeviceProperties deviceProperties)
        {
            foreach (var profile in PlayToConfiguration.Profiles)
            {
                if (!string.IsNullOrEmpty(profile.FriendlyName))
                {
                    if (!string.Equals(deviceProperties.Name, profile.FriendlyName, StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                if (!string.IsNullOrEmpty(profile.ModelNumber))
                {
                    if (!string.Equals(deviceProperties.ModelNumber, profile.ModelNumber, StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                if (!string.IsNullOrEmpty(profile.ModelName))
                {
                    if (!string.Equals(deviceProperties.ModelName, profile.ModelName, StringComparison.OrdinalIgnoreCase))
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
