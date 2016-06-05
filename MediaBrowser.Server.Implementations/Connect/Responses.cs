using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Connect;

namespace MediaBrowser.Server.Implementations.Connect
{
    public class ServerRegistrationResponse
    {
        public string Id { get; set; }
        public string Url { get; set; }
        public string Name { get; set; }
        public string AccessKey { get; set; }
    }

    public class UpdateServerRegistrationResponse
    {
        public string Id { get; set; }
        public string Url { get; set; }
        public string Name { get; set; }
    }

    public class GetConnectUserResponse
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public bool IsActive { get; set; }
        public string ImageUrl { get; set; }
    }

    public class ServerUserAuthorizationResponse
    {
        public string Id { get; set; }
        public string ServerId { get; set; }
        public string UserId { get; set; }
        public string AccessToken { get; set; }
        public string DateCreated { get; set; }
        public bool IsActive { get; set; }
        public string AcceptStatus { get; set; }
        public string UserType { get; set; }
        public string UserImageUrl { get; set; }
        public string UserName { get; set; }
    }

    public class ConnectUserPreferences
    {
        public string[] PreferredAudioLanguages { get; set; }
        public bool PlayDefaultAudioTrack { get; set; }
        public string[] PreferredSubtitleLanguages { get; set; }
        public SubtitlePlaybackMode SubtitleMode { get; set; }
        public bool GroupMoviesIntoBoxSets { get; set; }

        public ConnectUserPreferences()
        {
            PreferredAudioLanguages = new string[] { };
            PreferredSubtitleLanguages = new string[] { };
        }

        public static ConnectUserPreferences FromUserConfiguration(UserConfiguration config)
        {
            return new ConnectUserPreferences
            {
                PlayDefaultAudioTrack = config.PlayDefaultAudioTrack,
                SubtitleMode = config.SubtitleMode,
                PreferredAudioLanguages = string.IsNullOrWhiteSpace(config.AudioLanguagePreference) ? new string[] { } : new[] { config.AudioLanguagePreference },
                PreferredSubtitleLanguages = string.IsNullOrWhiteSpace(config.SubtitleLanguagePreference) ? new string[] { } : new[] { config.SubtitleLanguagePreference }
            };
        }

        public void MergeInto(UserConfiguration config)
        {

        }
    }

    public class UserPreferencesDto<T>
    {
        public T data { get; set; }
    }

    public class ConnectAuthorizationInternal : ConnectAuthorization
    {
        public string AccessToken { get; set; }
    }
}
