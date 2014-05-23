using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Notifications;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Search;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Session;
using MediaBrowser.Model.System;
using MediaBrowser.Model.Tasks;
using MediaBrowser.Model.Users;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Model.ApiClient
{
    /// <summary>
    /// Interface IApiClient
    /// </summary>
    public interface IApiClient : IDisposable
    {
        /// <summary>
        /// Occurs when [server location changed].
        /// </summary>
        event EventHandler ServerLocationChanged;

        /// <summary>
        /// Occurs when [HTTP response received].
        /// </summary>
        event EventHandler<HttpResponseEventArgs> HttpResponseReceived;

        /// <summary>
        /// Gets the API URL.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <returns>System.String.</returns>
        string GetApiUrl(string handler);

        /// <summary>
        /// Gets the game system summaries async.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{List{GameSystemSummary}}.</returns>
        Task<List<GameSystemSummary>> GetGameSystemSummariesAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Gets the async.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="url">The URL.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{``0}.</returns>
        Task<T> GetAsync<T>(string url, CancellationToken cancellationToken)
            where T : class;

        /// <summary>
        /// Gets the url needed to stream an audio file
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="ArgumentNullException">options</exception>
        string GetAudioStreamUrl(StreamOptions options);

        /// <summary>
        /// Gets the url needed to stream a video file
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="ArgumentNullException">options</exception>
        string GetVideoStreamUrl(VideoStreamOptions options);

        /// <summary>
        /// Formulates a url for streaming video using the HLS protocol
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="ArgumentNullException">options</exception>
        string GetHlsVideoStreamUrl(VideoStreamOptions options);

        /// <summary>
        /// Reports the capabilities.
        /// </summary>
        /// <param name="capabilities">The capabilities.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task ReportCapabilities(ClientCapabilities capabilities, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the index of the game players.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{List{ItemIndex}}.</returns>
        Task<List<ItemIndex>> GetGamePlayerIndex(string userId, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the index of the year.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="includeItemTypes">The include item types.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{List{ItemIndex}}.</returns>
        Task<List<ItemIndex>> GetYearIndex(string userId, string[] includeItemTypes, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the critic reviews.
        /// </summary>
        /// <param name="itemId">The item id.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="limit">The limit.</param>
        /// <returns>Task{ItemReviewsResult}.</returns>
        Task<QueryResult<ItemReview>> GetCriticReviews(string itemId, CancellationToken cancellationToken, int? startIndex = null, int? limit = null);

        /// <summary>
        /// Gets the theme songs async.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="itemId">The item id.</param>
        /// <param name="inheritFromParents">if set to <c>true</c> [inherit from parents].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{ThemeMediaResult}.</returns>
        Task<ThemeMediaResult> GetThemeSongsAsync(string userId, string itemId, bool inheritFromParents, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the search hints async.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task{SearchHintResult}.</returns>
        Task<SearchHintResult> GetSearchHintsAsync(SearchQuery query);

        /// <summary>
        /// Gets the theme videos async.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="itemId">The item id.</param>
        /// <param name="inheritFromParents">if set to <c>true</c> [inherit from parents].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{ThemeMediaResult}.</returns>
        Task<ThemeMediaResult> GetThemeVideosAsync(string userId, string itemId, bool inheritFromParents, CancellationToken cancellationToken);

        /// <summary>
        /// Gets all theme media async.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="itemId">The item id.</param>
        /// <param name="inheritFromParents">if set to <c>true</c> [inherit from parents].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{AllThemeMediaResult}.</returns>
        Task<AllThemeMediaResult> GetAllThemeMediaAsync(string userId, string itemId, bool inheritFromParents, CancellationToken cancellationToken);

        /// <summary>
        /// Marks the notifications read.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="notificationIdList">The notification id list.</param>
        /// <param name="isRead">if set to <c>true</c> [is read].</param>
        /// <returns>Task.</returns>
        Task MarkNotificationsRead(string userId, IEnumerable<string> notificationIdList, bool isRead);

        /// <summary>
        /// Gets the notifications summary.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <returns>Task{NotificationsSummary}.</returns>
        Task<NotificationsSummary> GetNotificationsSummary(string userId);

        /// <summary>
        /// Gets the notifications async.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task{NotificationResult}.</returns>
        Task<NotificationResult> GetNotificationsAsync(NotificationQuery query);

        /// <summary>
        /// Gets an image stream based on a url
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{Stream}.</returns>
        /// <exception cref="ArgumentNullException">url</exception>
        Task<Stream> GetImageStreamAsync(string url, CancellationToken cancellationToken);

        /// <summary>
        /// Gets a BaseItem
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="userId">The user id.</param>
        /// <returns>Task{BaseItemDto}.</returns>
        /// <exception cref="ArgumentNullException">id</exception>
        Task<BaseItemDto> GetItemAsync(string id, string userId);

        /// <summary>
        /// Gets the intros async.
        /// </summary>
        /// <param name="itemId">The item id.</param>
        /// <param name="userId">The user id.</param>
        /// <returns>Task{ItemsResult}.</returns>
        Task<ItemsResult> GetIntrosAsync(string itemId, string userId);

        /// <summary>
        /// Gets a BaseItem
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <returns>Task{BaseItemDto}.</returns>
        /// <exception cref="ArgumentNullException">userId</exception>
        Task<BaseItemDto> GetRootFolderAsync(string userId);

        /// <summary>
        /// Gets the users async.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task{UserDto[]}.</returns>
        Task<UserDto[]> GetUsersAsync(UserQuery query);

        /// <summary>
        /// Gets the public users async.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{UserDto[]}.</returns>
        Task<UserDto[]> GetPublicUsersAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Gets active client sessions.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task{SessionInfoDto[]}.</returns>
        Task<SessionInfoDto[]> GetClientSessionsAsync(SessionQuery query);

        /// <summary>
        /// Gets the item counts async.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task{ItemCounts}.</returns>
        Task<ItemCounts> GetItemCountsAsync(ItemCountsQuery query);

        /// <summary>
        /// Gets the episodes asynchronous.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task{ItemsResult}.</returns>
        Task<ItemsResult> GetEpisodesAsync(EpisodeQuery query);

        /// <summary>
        /// Gets the seasons asynchronous.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task{ItemsResult}.</returns>
        Task<ItemsResult> GetSeasonsAsync(SeasonQuery query);

        /// <summary>
        /// Queries for items
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task{ItemsResult}.</returns>
        /// <exception cref="ArgumentNullException">query</exception>
        Task<ItemsResult> GetItemsAsync(ItemQuery query);

        /// <summary>
        /// Gets the instant mix from song async.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task{ItemsResult}.</returns>
        Task<ItemsResult> GetInstantMixFromSongAsync(SimilarItemsQuery query);

        /// <summary>
        /// Gets the instant mix from album async.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task{ItemsResult}.</returns>
        Task<ItemsResult> GetInstantMixFromAlbumAsync(SimilarItemsQuery query);

        /// <summary>
        /// Gets the instant mix from artist async.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task{ItemsResult}.</returns>
        Task<ItemsResult> GetInstantMixFromArtistAsync(SimilarItemsByNameQuery query);

        /// <summary>
        /// Gets the instant mix from music genre async.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task{ItemsResult}.</returns>
        Task<ItemsResult> GetInstantMixFromMusicGenreAsync(SimilarItemsByNameQuery query);

        /// <summary>
        /// Gets the similar movies async.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task{ItemsResult}.</returns>
        Task<ItemsResult> GetSimilarMoviesAsync(SimilarItemsQuery query);

        /// <summary>
        /// Gets the similar trailers async.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task{ItemsResult}.</returns>
        Task<ItemsResult> GetSimilarTrailersAsync(SimilarItemsQuery query);

        /// <summary>
        /// Gets the similar series async.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task{ItemsResult}.</returns>
        Task<ItemsResult> GetSimilarSeriesAsync(SimilarItemsQuery query);

        /// <summary>
        /// Gets the similar albums async.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task{ItemsResult}.</returns>
        Task<ItemsResult> GetSimilarAlbumsAsync(SimilarItemsQuery query);

        /// <summary>
        /// Gets the similar games async.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task{ItemsResult}.</returns>
        Task<ItemsResult> GetSimilarGamesAsync(SimilarItemsQuery query);

        /// <summary>
        /// Gets the people async.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task{ItemsResult}.</returns>
        /// <exception cref="ArgumentNullException">userId</exception>
        Task<ItemsResult> GetPeopleAsync(PersonsQuery query);

        /// <summary>
        /// Gets the artists.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task{ItemsResult}.</returns>
        /// <exception cref="ArgumentNullException">userId</exception>
        Task<ItemsResult> GetArtistsAsync(ArtistsQuery query);

        /// <summary>
        /// Gets a studio
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="userId">The user id.</param>
        /// <returns>Task{BaseItemDto}.</returns>
        /// <exception cref="ArgumentNullException">userId</exception>
        Task<BaseItemDto> GetStudioAsync(string name, string userId);

        /// <summary>
        /// Gets the next up async.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task{ItemsResult}.</returns>
        Task<ItemsResult> GetNextUpEpisodesAsync(NextUpQuery query);

        /// <summary>
        /// Gets the upcoming episodes asynchronous.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task{ItemsResult}.</returns>
        Task<ItemsResult> GetUpcomingEpisodesAsync(NextUpQuery query);

        /// <summary>
        /// Gets a genre
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="userId">The user id.</param>
        /// <returns>Task{BaseItemDto}.</returns>
        /// <exception cref="ArgumentNullException">userId</exception>
        Task<BaseItemDto> GetGenreAsync(string name, string userId);

        /// <summary>
        /// Gets the genres async.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task{ItemsResult}.</returns>
        Task<ItemsResult> GetGenresAsync(ItemsByNameQuery query);

        /// <summary>
        /// Gets the music genres async.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task{ItemsResult}.</returns>
        Task<ItemsResult> GetMusicGenresAsync(ItemsByNameQuery query);

        /// <summary>
        /// Gets the game genres async.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task{ItemsResult}.</returns>
        Task<ItemsResult> GetGameGenresAsync(ItemsByNameQuery query);

        /// <summary>
        /// Gets the studios async.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task{ItemsResult}.</returns>
        Task<ItemsResult> GetStudiosAsync(ItemsByNameQuery query);

        /// <summary>
        /// Gets the music genre async.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="userId">The user id.</param>
        /// <returns>Task{BaseItemDto}.</returns>
        Task<BaseItemDto> GetMusicGenreAsync(string name, string userId);

        /// <summary>
        /// Gets the game genre async.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="userId">The user id.</param>
        /// <returns>Task{BaseItemDto}.</returns>
        Task<BaseItemDto> GetGameGenreAsync(string name, string userId);

        /// <summary>
        /// Gets the artist async.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="userId">The user id.</param>
        /// <returns>Task{BaseItemDto}.</returns>
        /// <exception cref="ArgumentNullException">name</exception>
        Task<BaseItemDto> GetArtistAsync(string name, string userId);

        /// <summary>
        /// Restarts the server.
        /// </summary>
        /// <returns>Task.</returns>
        Task RestartServerAsync();

        /// <summary>
        /// Gets the system status async.
        /// </summary>
        /// <returns>Task{SystemInfo}.</returns>
        Task<SystemInfo> GetSystemInfoAsync();

        /// <summary>
        /// Gets a person
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="userId">The user id.</param>
        /// <returns>Task{BaseItemDto}.</returns>
        /// <exception cref="ArgumentNullException">userId</exception>
        Task<BaseItemDto> GetPersonAsync(string name, string userId);

        /// <summary>
        /// Gets a list of plugins installed on the server
        /// </summary>
        /// <returns>Task{PluginInfo[]}.</returns>
        Task<PluginInfo[]> GetInstalledPluginsAsync();

        /// <summary>
        /// Gets the current server configuration
        /// </summary>
        /// <returns>Task{ServerConfiguration}.</returns>
        Task<ServerConfiguration> GetServerConfigurationAsync();

        /// <summary>
        /// Gets the scheduled tasks.
        /// </summary>
        /// <returns>Task{TaskInfo[]}.</returns>
        Task<TaskInfo[]> GetScheduledTasksAsync();

        /// <summary>
        /// Gets the scheduled task async.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>Task{TaskInfo}.</returns>
        /// <exception cref="ArgumentNullException">id</exception>
        Task<TaskInfo> GetScheduledTaskAsync(string id);

        /// <summary>
        /// Gets a user by id
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>Task{UserDto}.</returns>
        /// <exception cref="ArgumentNullException">id</exception>
        Task<UserDto> GetUserAsync(string id);

        /// <summary>
        /// Gets the parental ratings async.
        /// </summary>
        /// <returns>Task{List{ParentalRating}}.</returns>
        Task<List<ParentalRating>> GetParentalRatingsAsync();

        /// <summary>
        /// Gets local trailers for an item
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="itemId">The item id.</param>
        /// <returns>Task{ItemsResult}.</returns>
        /// <exception cref="ArgumentNullException">query</exception>
        Task<BaseItemDto[]> GetLocalTrailersAsync(string userId, string itemId);

        /// <summary>
        /// Gets special features for an item
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="itemId">The item id.</param>
        /// <returns>Task{BaseItemDto[]}.</returns>
        /// <exception cref="ArgumentNullException">userId</exception>
        Task<BaseItemDto[]> GetSpecialFeaturesAsync(string userId, string itemId);

        /// <summary>
        /// Gets the cultures async.
        /// </summary>
        /// <returns>Task{CultureDto[]}.</returns>
        Task<CultureDto[]> GetCulturesAsync();

        /// <summary>
        /// Gets the countries async.
        /// </summary>
        /// <returns>Task{CountryInfo[]}.</returns>
        Task<CountryInfo[]> GetCountriesAsync();

        /// <summary>
        /// Marks the played async.
        /// </summary>
        /// <param name="itemId">The item id.</param>
        /// <param name="userId">The user id.</param>
        /// <param name="datePlayed">The date played.</param>
        /// <returns>Task{UserItemDataDto}.</returns>
        Task<UserItemDataDto> MarkPlayedAsync(string itemId, string userId, DateTime? datePlayed);

        /// <summary>
        /// Marks the unplayed async.
        /// </summary>
        /// <param name="itemId">The item id.</param>
        /// <param name="userId">The user id.</param>
        /// <returns>Task{UserItemDataDto}.</returns>
        Task<UserItemDataDto> MarkUnplayedAsync(string itemId, string userId);

        /// <summary>
        /// Updates the favorite status async.
        /// </summary>
        /// <param name="itemId">The item id.</param>
        /// <param name="userId">The user id.</param>
        /// <param name="isFavorite">if set to <c>true</c> [is favorite].</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentNullException">itemId</exception>
        Task<UserItemDataDto> UpdateFavoriteStatusAsync(string itemId, string userId, bool isFavorite);

        /// <summary>
        /// Reports to the server that the user has begun playing an item
        /// </summary>
        /// <param name="info">The information.</param>
        /// <returns>Task{UserItemDataDto}.</returns>
        /// <exception cref="ArgumentNullException">itemId</exception>
        Task ReportPlaybackStartAsync(PlaybackStartInfo info);

        /// <summary>
        /// Reports playback progress to the server
        /// </summary>
        /// <param name="info">The information.</param>
        /// <returns>Task{UserItemDataDto}.</returns>
        /// <exception cref="ArgumentNullException">itemId</exception>
        Task ReportPlaybackProgressAsync(PlaybackProgressInfo info);

        /// <summary>
        /// Reports to the server that the user has stopped playing an item
        /// </summary>
        /// <param name="info">The information.</param>
        /// <returns>Task{UserItemDataDto}.</returns>
        /// <exception cref="ArgumentNullException">itemId</exception>
        Task ReportPlaybackStoppedAsync(PlaybackStopInfo info);

        /// <summary>
        /// Instructs antoher client to browse to a library item.
        /// </summary>
        /// <param name="sessionId">The session id.</param>
        /// <param name="itemId">The id of the item to browse to.</param>
        /// <param name="itemName">The name of the item to browse to.</param>
        /// <param name="itemType">The type of the item to browse to.</param>
        /// <returns>Task.</returns>
        Task SendBrowseCommandAsync(string sessionId, string itemId, string itemName, string itemType);

        /// <summary>
        /// Sends the playstate command async.
        /// </summary>
        /// <param name="sessionId">The session id.</param>
        /// <param name="request">The request.</param>
        /// <returns>Task.</returns>
        Task SendPlaystateCommandAsync(string sessionId, PlaystateRequest request);

        /// <summary>
        /// Sends the play command async.
        /// </summary>
        /// <param name="sessionId">The session id.</param>
        /// <param name="request">The request.</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentNullException">sessionId
        /// or
        /// request</exception>
        Task SendPlayCommandAsync(string sessionId, PlayRequest request);

        /// <summary>
        /// Sends the command asynchronous.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="command">The command.</param>
        /// <returns>Task.</returns>
        Task SendCommandAsync(string sessionId, GeneralCommand command);

        /// <summary>
        /// Sends the string.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="text">The text.</param>
        /// <returns>Task.</returns>
        Task SendString(string sessionId, string text);

        /// <summary>
        /// Sets the volume.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="volume">The volume.</param>
        /// <returns>Task.</returns>
        Task SetVolume(string sessionId, int volume);

        /// <summary>
        /// Sets the index of the audio stream.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="index">The index.</param>
        /// <returns>Task.</returns>
        Task SetAudioStreamIndex(string sessionId, int index);

        /// <summary>
        /// Sets the index of the subtitle stream.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="index">The index.</param>
        /// <returns>Task.</returns>
        Task SetSubtitleStreamIndex(string sessionId, int? index);
        
        /// <summary>
        /// Instructs the client to display a message to the user
        /// </summary>
        /// <param name="sessionId">The session id.</param>
        /// <param name="command">The command.</param>
        /// <returns>Task.</returns>
        Task SendMessageCommandAsync(string sessionId, MessageCommand command);

        /// <summary>
        /// Clears a user's rating for an item
        /// </summary>
        /// <param name="itemId">The item id.</param>
        /// <param name="userId">The user id.</param>
        /// <returns>Task{UserItemDataDto}.</returns>
        /// <exception cref="ArgumentNullException">itemId</exception>
        Task<UserItemDataDto> ClearUserItemRatingAsync(string itemId, string userId);

        /// <summary>
        /// Updates a user's rating for an item, based on likes or dislikes
        /// </summary>
        /// <param name="itemId">The item id.</param>
        /// <param name="userId">The user id.</param>
        /// <param name="likes">if set to <c>true</c> [likes].</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentNullException">itemId</exception>
        Task<UserItemDataDto> UpdateUserItemRatingAsync(string itemId, string userId, bool likes);

        /// <summary>
        /// Authenticates a user and returns the result
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="sha1Hash">The sha1 hash.</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentNullException">userId</exception>
        Task<AuthenticationResult> AuthenticateUserAsync(string username, byte[] sha1Hash);

        /// <summary>
        /// Updates the server configuration async.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentNullException">configuration</exception>
        Task UpdateServerConfigurationAsync(ServerConfiguration configuration);

        /// <summary>
        /// Updates the scheduled task triggers.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="triggers">The triggers.</param>
        /// <returns>Task{RequestResult}.</returns>
        /// <exception cref="ArgumentNullException">id</exception>
        Task UpdateScheduledTaskTriggersAsync(string id, TaskTriggerInfo[] triggers);

        /// <summary>
        /// Gets the display preferences.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="userId">The user id.</param>
        /// <param name="client">The client.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{BaseItemDto}.</returns>
        Task<DisplayPreferences> GetDisplayPreferencesAsync(string id, string userId, string client, CancellationToken cancellationToken);

        /// <summary>
        /// Updates display preferences for a user
        /// </summary>
        /// <param name="displayPreferences">The display preferences.</param>
        /// <param name="userId">The user id.</param>
        /// <param name="client">The client.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{DisplayPreferences}.</returns>
        /// <exception cref="System.ArgumentNullException">userId</exception>
        Task UpdateDisplayPreferencesAsync(DisplayPreferences displayPreferences, string userId, string client, CancellationToken cancellationToken);

        /// <summary>
        /// Posts a set of data to a url, and deserializes the return stream into T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="url">The URL.</param>
        /// <param name="args">The args.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{``0}.</returns>
        Task<T> PostAsync<T>(string url, Dictionary<string, string> args, CancellationToken cancellationToken)
            where T : class;

        /// <summary>
        /// This is a helper around getting a stream from the server that contains serialized data
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns>Task{Stream}.</returns>
        Task<Stream> GetSerializedStreamAsync(string url);

        /// <summary>
        /// Gets the json serializer.
        /// </summary>
        /// <value>The json serializer.</value>
        IJsonSerializer JsonSerializer { get; set; }

        /// <summary>
        /// Gets or sets the server host name (myserver or 192.168.x.x)
        /// </summary>
        /// <value>The name of the server host.</value>
        string ServerHostName { get; }

        /// <summary>
        /// Gets or sets the port number used by the API
        /// </summary>
        /// <value>The server API port.</value>
        int ServerApiPort { get; }

        /// <summary>
        /// Changes the server location.
        /// </summary>
        /// <param name="hostName">Name of the host.</param>
        /// <param name="apiPort">The API port.</param>
        void ChangeServerLocation(string hostName, int apiPort);

        /// <summary>
        /// Gets or sets the type of the client.
        /// </summary>
        /// <value>The type of the client.</value>
        string ClientName { get; set; }

        /// <summary>
        /// Gets or sets the name of the device.
        /// </summary>
        /// <value>The name of the device.</value>
        string DeviceName { get; set; }

        /// <summary>
        /// Gets or sets the device id.
        /// </summary>
        /// <value>The device id.</value>
        string DeviceId { get; set; }

        /// <summary>
        /// Gets or sets the current user id.
        /// </summary>
        /// <value>The current user id.</value>
        string CurrentUserId { get; set; }

        /// <summary>
        /// Gets the image URL.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="ArgumentNullException">item</exception>
        string GetImageUrl(BaseItemDto item, ImageOptions options);

        /// <summary>
        /// Gets the image URL.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        string GetImageUrl(ChannelInfoDto item, ImageOptions options);

        /// <summary>
        /// Gets the image URL.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        string GetImageUrl(RecordingInfoDto item, ImageOptions options);

        /// <summary>
        /// Gets the image URL.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        string GetImageUrl(ProgramInfoDto item, ImageOptions options);

        /// <summary>
        /// Gets the subtitle URL.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        string GetSubtitleUrl(SubtitleDownloadOptions options);
        
        /// <summary>
        /// Gets an image url that can be used to download an image from the api
        /// </summary>
        /// <param name="itemId">The Id of the item</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="ArgumentNullException">itemId</exception>
        string GetImageUrl(string itemId, ImageOptions options);

        /// <summary>
        /// Gets the user image URL.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="ArgumentNullException">user</exception>
        string GetUserImageUrl(UserDto user, ImageOptions options);

        /// <summary>
        /// Gets an image url that can be used to download an image from the api
        /// </summary>
        /// <param name="userId">The Id of the user</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="ArgumentNullException">userId</exception>
        string GetUserImageUrl(string userId, ImageOptions options);

        /// <summary>
        /// Gets the person image URL.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="ArgumentNullException">item</exception>
        string GetPersonImageUrl(BaseItemPerson item, ImageOptions options);

        /// <summary>
        /// Gets an image url that can be used to download an image from the api
        /// </summary>
        /// <param name="name">The name of the person</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="ArgumentNullException">name</exception>
        string GetPersonImageUrl(string name, ImageOptions options);

        /// <summary>
        /// Gets an image url that can be used to download an image from the api
        /// </summary>
        /// <param name="year">The year.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        string GetYearImageUrl(int year, ImageOptions options);

        /// <summary>
        /// Gets an image url that can be used to download an image from the api
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="ArgumentNullException">name</exception>
        string GetGenreImageUrl(string name, ImageOptions options);

        /// <summary>
        /// Gets the music genre image URL.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        string GetMusicGenreImageUrl(string name, ImageOptions options);

        /// <summary>
        /// Gets the game genre image URL.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        string GetGameGenreImageUrl(string name, ImageOptions options);

        /// <summary>
        /// Gets an image url that can be used to download an image from the api
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="ArgumentNullException">name</exception>
        string GetStudioImageUrl(string name, ImageOptions options);

        /// <summary>
        /// Gets the artist image URL.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="ArgumentNullException">name</exception>
        string GetArtistImageUrl(string name, ImageOptions options);

        /// <summary>
        /// This is a helper to get a list of backdrop url's from a given ApiBaseItemWrapper. If the actual item does not have any backdrops it will return backdrops from the first parent that does.
        /// </summary>
        /// <param name="item">A given item.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String[][].</returns>
        /// <exception cref="ArgumentNullException">item</exception>
        string[] GetBackdropImageUrls(BaseItemDto item, ImageOptions options);

        /// <summary>
        /// This is a helper to get the logo image url from a given ApiBaseItemWrapper. If the actual item does not have a logo, it will return the logo from the first parent that does, or null.
        /// </summary>
        /// <param name="item">A given item.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="ArgumentNullException">item</exception>
        string GetLogoImageUrl(BaseItemDto item, ImageOptions options);

        /// <summary>
        /// Gets the art image URL.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        string GetArtImageUrl(BaseItemDto item, ImageOptions options);

        /// <summary>
        /// Gets the thumb image URL.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        string GetThumbImageUrl(BaseItemDto item, ImageOptions options);

        /// <summary>
        /// Gets the live tv information asynchronous.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{LiveTvInfo}.</returns>
        Task<LiveTvInfo> GetLiveTvInfoAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Gets the live tv channels asynchronous.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{LiveTvInfo}.</returns>
        Task<QueryResult<ChannelInfoDto>> GetLiveTvChannelsAsync(ChannelQuery query, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the live tv channel asynchronous.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{ChannelInfoDto}.</returns>
        Task<ChannelInfoDto> GetLiveTvChannelAsync(string id, string userId, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the live tv recordings asynchronous.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{QueryResult{RecordingInfoDto}}.</returns>
        Task<QueryResult<RecordingInfoDto>> GetLiveTvRecordingsAsync(RecordingQuery query, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the live tv recording asynchronous.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{RecordingInfoDto}.</returns>
        Task<RecordingInfoDto> GetLiveTvRecordingAsync(string id, string userId, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the live tv recording groups asynchronous.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{QueryResult{RecordingGroupDto}}.</returns>
        Task<QueryResult<RecordingGroupDto>> GetLiveTvRecordingGroupsAsync(RecordingGroupQuery query, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the live tv recording group asynchronous.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{RecordingGroupDto}.</returns>
        Task<RecordingGroupDto> GetLiveTvRecordingGroupAsync(string id, string userId, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the live tv timers asynchronous.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{QueryResult{TimerInfoDto}}.</returns>
        Task<QueryResult<TimerInfoDto>> GetLiveTvTimersAsync(TimerQuery query, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the live tv programs asynchronous.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{QueryResult{ProgramInfoDto}}.</returns>
        Task<QueryResult<ProgramInfoDto>> GetLiveTvProgramsAsync(ProgramQuery query, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the live tv program asynchronous.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{ProgramInfoDto}.</returns>
        Task<ProgramInfoDto> GetLiveTvProgramAsync(string id, string userId, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the recommended live tv programs asynchronous.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{QueryResult{ProgramInfoDto}}.</returns>
        Task<QueryResult<ProgramInfoDto>> GetRecommendedLiveTvProgramsAsync(RecommendedProgramQuery query, CancellationToken cancellationToken);

        /// <summary>
        /// Creates the live tv timer asynchronous.
        /// </summary>
        /// <param name="timer">The timer.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task CreateLiveTvTimerAsync(BaseTimerInfoDto timer, CancellationToken cancellationToken);

        /// <summary>
        /// Updates the live tv timer asynchronous.
        /// </summary>
        /// <param name="timer">The timer.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task UpdateLiveTvTimerAsync(TimerInfoDto timer, CancellationToken cancellationToken);

        /// <summary>
        /// Creates the live tv series timer asynchronous.
        /// </summary>
        /// <param name="timer">The timer.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task CreateLiveTvSeriesTimerAsync(SeriesTimerInfoDto timer, CancellationToken cancellationToken);

        /// <summary>
        /// Updates the live tv series timer asynchronous.
        /// </summary>
        /// <param name="timer">The timer.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task UpdateLiveTvSeriesTimerAsync(SeriesTimerInfoDto timer, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the live tv timer asynchronous.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{TimerInfoDto}.</returns>
        Task<TimerInfoDto> GetLiveTvTimerAsync(string id, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the live tv series timers asynchronous.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{QueryResult{SeriesTimerInfoDto}}.</returns>
        Task<QueryResult<SeriesTimerInfoDto>> GetLiveTvSeriesTimersAsync(SeriesTimerQuery query, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the live tv series timer asynchronous.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{SeriesTimerInfoDto}.</returns>
        Task<SeriesTimerInfoDto> GetLiveTvSeriesTimerAsync(string id, CancellationToken cancellationToken);

        /// <summary>
        /// Cancels the live tv timer asynchronous.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task CancelLiveTvTimerAsync(string id, CancellationToken cancellationToken);

        /// <summary>
        /// Cancels the live tv series timer asynchronous.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task CancelLiveTvSeriesTimerAsync(string id, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes the live tv recording asynchronous.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task DeleteLiveTvRecordingAsync(string id, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the default timer information.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{SeriesTimerInfoDto}.</returns>
        Task<SeriesTimerInfoDto> GetDefaultLiveTvTimerInfo(CancellationToken cancellationToken);

        /// <summary>
        /// Gets the live tv guide information.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{GuideInfo}.</returns>
        Task<GuideInfo> GetLiveTvGuideInfo(CancellationToken cancellationToken);

        /// <summary>
        /// Gets the default timer information.
        /// </summary>
        /// <param name="programId">The program identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{SeriesTimerInfoDto}.</returns>
        Task<SeriesTimerInfoDto> GetDefaultLiveTvTimerInfo(string programId, CancellationToken cancellationToken);
    }
}