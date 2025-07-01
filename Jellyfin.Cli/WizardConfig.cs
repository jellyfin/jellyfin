using Jellyfin.Api.Models.StartupDtos;
using MediaBrowser.Common.Net;

namespace Jellyfin.Cli
{
    /// <summary>
    /// Class containing the aggregated configuration for the wizard.
    /// </summary>
    public class WizardConfig
    {
        private string? username;
        private string? password;
        private string? preferredDisplayLanguage;
        private string? preferredMetadataCountryRegion;
        private string? preferredMetadataLanguage;
        private bool? enableRemoteAccess;

        /// <summary>
        /// Initializes a new instance of the <see cref="WizardConfig"/> class.
        /// </summary>
        /// <param name="firstUser">Instance of the <see cref="StartupUserDto"/> class.</param>
        /// <param name="startupConfiguration">Instance of the <see cref="StartupConfigurationDto"/> class.</param>
        /// <param name="networkConfiguration">Instance of the <see cref="NetworkConfiguration"/> class.</param>
        /// <returns>WizardConfig.</returns>
        public static WizardConfig FromDtos(StartupUserDto? firstUser, StartupConfigurationDto? startupConfiguration, NetworkConfiguration? networkConfiguration)
        {
            return new WizardConfig
            {
                username = firstUser?.Name,
                password = firstUser?.Password,
                preferredDisplayLanguage = startupConfiguration?.UICulture,
                preferredMetadataCountryRegion = startupConfiguration?.MetadataCountryCode,
                preferredMetadataLanguage = startupConfiguration?.PreferredMetadataLanguage,
                enableRemoteAccess = networkConfiguration?.EnableRemoteAccess,
            };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WizardConfig"/> class.
        /// </summary>
        /// <param name="options">Instance of the <see cref="Options.Wizard"/> class.</param>
        /// <returns>WizardConfig.</returns>
        public static WizardConfig FromOptions(Options.Wizard options)
        {
            return new WizardConfig
            {
                username = options.Username,
                password = options.Password,
                preferredDisplayLanguage = options.PreferredDisplayLanguage,
                preferredMetadataCountryRegion = options.PreferredMetadataCountryRegion,
                preferredMetadataLanguage = options.PreferredMetadataLanguage,
                enableRemoteAccess = options.EnableRemoteAccess,
            };
        }

        /// <summary>
        /// Pretty prints the config.
        /// </summary>
        /// <returns>string.</returns>
        public string[] AsLines()
        {
            var redactedPassword = password?.StartsWith("$PBKDF2", System.StringComparison.Ordinal) ?? true ? password : "<new password redacted>";
            return [
              $"Username:                        {username}",
              $"Password:                        {redactedPassword}",
              $"PreferredDisplayLanguage:        {preferredDisplayLanguage}",
              $"PreferredMetadataCountryRegion:  {preferredMetadataCountryRegion}",
              $"PreferredMetadataLanguage:       {preferredMetadataLanguage}",
              $"EnableRemoteAccess:              {enableRemoteAccess}",
            ];
        }

        /// <summary>
        /// Merges the current config with another one, only overriding if fields that are not null in the other config.
        /// </summary>
        /// <param name="other">Instance of the <see cref="WizardConfig"/> class.</param>
        public void Merge(WizardConfig other)
        {
            username = other.username ?? username;
            password = other.password ?? password;
            preferredDisplayLanguage = other.preferredDisplayLanguage ?? preferredDisplayLanguage;
            preferredMetadataCountryRegion = other.preferredMetadataCountryRegion ?? preferredMetadataCountryRegion;
            preferredMetadataLanguage = other.preferredMetadataLanguage ?? preferredMetadataLanguage;
            enableRemoteAccess = other.enableRemoteAccess ?? enableRemoteAccess;
        }

        /// <summary>
        /// Returns an instance of <see cref="StartupUserDto" /> with fields filled from the config.
        /// </summary>
        /// <returns>StartupUserDto.</returns>
        public StartupUserDto GetStartupUserDto()
        {
            return new StartupUserDto
            {
                Name = username,
                Password = password
            };
        }

        /// <summary>
        /// Returns an instance of <see cref="StartupConfigurationDto" /> with fields filled from the config.
        /// </summary>
        /// <returns>StartupConfigurationDto.</returns>
        public StartupConfigurationDto GetStartupConfigurationDto()
        {
            return new StartupConfigurationDto
            {
                UICulture = preferredDisplayLanguage,
                MetadataCountryCode = preferredMetadataCountryRegion,
                PreferredMetadataLanguage = preferredMetadataLanguage
            };
        }

        /// <summary>
        /// Returns an instance of <see cref="StartupRemoteAccessDto" /> with fields filled from the config.
        /// </summary>
        /// <returns>StartupRemoteAccessDto.</returns>
        public StartupRemoteAccessDto GetStartupRemoteAccessDto()
        {
            return new StartupRemoteAccessDto
            {
                EnableRemoteAccess = enableRemoteAccess ?? false,
            };
        }
    }
}
