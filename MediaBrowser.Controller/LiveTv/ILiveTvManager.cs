using System;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Querying;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Events;

namespace MediaBrowser.Controller.LiveTv
{
    /// <summary>
    /// Manages all live tv services installed on the server
    /// </summary>
    public interface ILiveTvManager
    {
        /// <summary>
        /// Gets the services.
        /// </summary>
        /// <value>The services.</value>
        IReadOnlyList<ILiveTvService> Services { get; }

        /// <summary>
        /// Gets the new timer defaults asynchronous.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{TimerInfo}.</returns>
        Task<SeriesTimerInfoDto> GetNewTimerDefaults(CancellationToken cancellationToken);

        /// <summary>
        /// Gets the new timer defaults.
        /// </summary>
        /// <param name="programId">The program identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{SeriesTimerInfoDto}.</returns>
        Task<SeriesTimerInfoDto> GetNewTimerDefaults(string programId, CancellationToken cancellationToken);
        
        /// <summary>
        /// Deletes the recording.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>Task.</returns>
        Task DeleteRecording(string id);

        /// <summary>
        /// Deletes the recording.
        /// </summary>
        /// <param name="recording">The recording.</param>
        /// <returns>Task.</returns>
        Task DeleteRecording(BaseItem recording);
        
        /// <summary>
        /// Cancels the timer.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>Task.</returns>
        Task CancelTimer(string id);

        /// <summary>
        /// Cancels the series timer.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>Task.</returns>
        Task CancelSeriesTimer(string id);

        /// <summary>
        /// Adds the parts.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="tunerHosts">The tuner hosts.</param>
        /// <param name="listingProviders">The listing providers.</param>
        void AddParts(IEnumerable<ILiveTvService> services, IEnumerable<ITunerHost> tunerHosts, IEnumerable<IListingsProvider> listingProviders);

        /// <summary>
        /// Gets the recording.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="user">The user.</param>
        /// <returns>Task{RecordingInfoDto}.</returns>
        Task<BaseItemDto> GetRecording(string id, DtoOptions options, CancellationToken cancellationToken, User user = null);
        
        /// <summary>
        /// Gets the timer.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{TimerInfoDto}.</returns>
        Task<TimerInfoDto> GetTimer(string id, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the series timer.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{TimerInfoDto}.</returns>
        Task<SeriesTimerInfoDto> GetSeriesTimer(string id, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the recordings.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>QueryResult{RecordingInfoDto}.</returns>
        Task<QueryResult<BaseItemDto>> GetRecordings(RecordingQuery query, DtoOptions options, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the timers.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{QueryResult{TimerInfoDto}}.</returns>
        Task<QueryResult<TimerInfoDto>> GetTimers(TimerQuery query, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the series timers.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{QueryResult{SeriesTimerInfoDto}}.</returns>
        Task<QueryResult<SeriesTimerInfoDto>> GetSeriesTimers(SeriesTimerQuery query, CancellationToken cancellationToken);
        
        /// <summary>
        /// Gets the channel.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>Channel.</returns>
        LiveTvChannel GetInternalChannel(string id);
        
        /// <summary>
        /// Gets the recording.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>LiveTvRecording.</returns>
        Task<BaseItem> GetInternalRecording(string id, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the recording stream.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{Stream}.</returns>
        Task<MediaSourceInfo> GetRecordingStream(string id, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the channel stream.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="mediaSourceId">The media source identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{StreamResponseInfo}.</returns>
        Task<MediaSourceInfo> GetChannelStream(string id, string mediaSourceId, CancellationToken cancellationToken);
        
        /// <summary>
        /// Gets the program.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="user">The user.</param>
        /// <returns>Task{ProgramInfoDto}.</returns>
        Task<BaseItemDto> GetProgram(string id, CancellationToken cancellationToken, User user = null);

        /// <summary>
        /// Gets the programs.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>IEnumerable{ProgramInfo}.</returns>
        Task<QueryResult<BaseItemDto>> GetPrograms(ProgramQuery query, DtoOptions options, CancellationToken cancellationToken);

        /// <summary>
        /// Updates the timer.
        /// </summary>
        /// <param name="timer">The timer.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task UpdateTimer(TimerInfoDto timer, CancellationToken cancellationToken);

        /// <summary>
        /// Updates the timer.
        /// </summary>
        /// <param name="timer">The timer.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task UpdateSeriesTimer(SeriesTimerInfoDto timer, CancellationToken cancellationToken);

        /// <summary>
        /// Creates the timer.
        /// </summary>
        /// <param name="timer">The timer.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task CreateTimer(TimerInfoDto timer, CancellationToken cancellationToken);

        /// <summary>
        /// Creates the series timer.
        /// </summary>
        /// <param name="timer">The timer.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task CreateSeriesTimer(SeriesTimerInfoDto timer, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the recording groups.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{QueryResult{RecordingGroupDto}}.</returns>
        Task<QueryResult<BaseItemDto>> GetRecordingGroups(RecordingGroupQuery query, CancellationToken cancellationToken);

        /// <summary>
        /// Closes the live stream.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task CloseLiveStream(string id, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the guide information.
        /// </summary>
        /// <returns>GuideInfo.</returns>
        GuideInfo GetGuideInfo();

        /// <summary>
        /// Gets the recommended programs.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{QueryResult{ProgramInfoDto}}.</returns>
        Task<QueryResult<BaseItemDto>> GetRecommendedPrograms(RecommendedProgramQuery query, DtoOptions options, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the recommended programs internal.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task&lt;QueryResult&lt;LiveTvProgram&gt;&gt;.</returns>
        Task<QueryResult<LiveTvProgram>> GetRecommendedProgramsInternal(RecommendedProgramQuery query, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the live tv information.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{LiveTvInfo}.</returns>
        Task<LiveTvInfo> GetLiveTvInfo(CancellationToken cancellationToken);

        /// <summary>
        /// Resets the tuner.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task ResetTuner(string id, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the live tv folder.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>BaseItemDto.</returns>
        Task<Folder> GetInternalLiveTvFolder(CancellationToken cancellationToken);

        /// <summary>
        /// Gets the live tv folder.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>BaseItemDto.</returns>
        Task<BaseItemDto> GetLiveTvFolder(string userId, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the enabled users.
        /// </summary>
        /// <returns>IEnumerable{User}.</returns>
        IEnumerable<User> GetEnabledUsers();

        /// <summary>
        /// Gets the internal channels.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task&lt;QueryResult&lt;LiveTvChannel&gt;&gt;.</returns>
        Task<QueryResult<LiveTvChannel>> GetInternalChannels(LiveTvChannelQuery query,
            CancellationToken cancellationToken);

        /// <summary>
        /// Gets the internal recordings.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task&lt;QueryResult&lt;BaseItem&gt;&gt;.</returns>
        Task<QueryResult<BaseItem>> GetInternalRecordings(RecordingQuery query, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the recording media sources.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task&lt;IEnumerable&lt;MediaSourceInfo&gt;&gt;.</returns>
        Task<IEnumerable<MediaSourceInfo>> GetRecordingMediaSources(string id, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the channel media sources.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task&lt;IEnumerable&lt;MediaSourceInfo&gt;&gt;.</returns>
        Task<IEnumerable<MediaSourceInfo>> GetChannelMediaSources(string id, CancellationToken cancellationToken);

        /// <summary>
        /// Adds the information to recording dto.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="dto">The dto.</param>
        /// <param name="user">The user.</param>
        void AddInfoToRecordingDto(BaseItem item, BaseItemDto dto, User user = null);

        /// <summary>
        /// Adds the information to program dto.
        /// </summary>
        /// <param name="programs">The programs.</param>
        /// <param name="fields">The fields.</param>
        /// <param name="user">The user.</param>
        /// <returns>Task.</returns>
        Task AddInfoToProgramDto(List<Tuple<BaseItem,BaseItemDto>> programs, List<ItemFields> fields, User user = null);
      
        /// <summary>
        /// Saves the tuner host.
        /// </summary>
        Task<TunerHostInfo> SaveTunerHost(TunerHostInfo info, bool dataSourceChanged = true);
        /// <summary>
        /// Saves the listing provider.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <param name="validateLogin">if set to <c>true</c> [validate login].</param>
        /// <param name="validateListings">if set to <c>true</c> [validate listings].</param>
        /// <returns>Task.</returns>
        Task<ListingsProviderInfo> SaveListingProvider(ListingsProviderInfo info, bool validateLogin, bool validateListings);

        void DeleteListingsProvider(string id);

        Task<TunerChannelMapping> SetChannelMapping(string providerId, string tunerChannelNumber, string providerChannelNumber);

        TunerChannelMapping GetTunerChannelMapping(ChannelInfo channel, List<NameValuePair> mappings, List<ChannelInfo> providerChannels);

        /// <summary>
        /// Gets the lineups.
        /// </summary>
        /// <param name="providerType">Type of the provider.</param>
        /// <param name="providerId">The provider identifier.</param>
        /// <param name="country">The country.</param>
        /// <param name="location">The location.</param>
        /// <returns>Task&lt;List&lt;NameIdPair&gt;&gt;.</returns>
        Task<List<NameIdPair>> GetLineups(string providerType, string providerId, string country, string location);

        /// <summary>
        /// Gets the registration information.
        /// </summary>
        /// <param name="channelId">The channel identifier.</param>
        /// <param name="programId">The program identifier.</param>
        /// <param name="feature">The feature.</param>
        /// <returns>Task&lt;MBRegistrationRecord&gt;.</returns>
        Task<MBRegistrationRecord> GetRegistrationInfo(string channelId, string programId, string feature);

        /// <summary>
        /// Adds the channel information.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="options">The options.</param>
        /// <param name="user">The user.</param>
        void AddChannelInfo(List<Tuple<BaseItemDto, LiveTvChannel>> items, DtoOptions options, User user);

        /// <summary>
        /// Called when [recording file deleted].
        /// </summary>
        /// <param name="recording">The recording.</param>
        /// <returns>Task.</returns>
        Task OnRecordingFileDeleted(BaseItem recording);

        /// <summary>
        /// Gets the sat ini mappings.
        /// </summary>
        /// <returns>List&lt;NameValuePair&gt;.</returns>
        List<NameValuePair> GetSatIniMappings();

        Task<List<ChannelInfo>> GetSatChannelScanResult(TunerHostInfo info, CancellationToken cancellationToken);

        Task<List<ChannelInfo>> GetChannelsForListingsProvider(string id, CancellationToken cancellationToken);
        Task<List<ChannelInfo>> GetChannelsFromListingsProviderData(string id, CancellationToken cancellationToken);

        List<IListingsProvider> ListingProviders { get;}

        event EventHandler<GenericEventArgs<TimerEventInfo>> SeriesTimerCancelled;
        event EventHandler<GenericEventArgs<TimerEventInfo>> TimerCancelled;
        event EventHandler<GenericEventArgs<TimerEventInfo>> TimerCreated;
        event EventHandler<GenericEventArgs<TimerEventInfo>> SeriesTimerCreated;
    }
}
