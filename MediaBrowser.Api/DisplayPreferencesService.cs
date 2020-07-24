using System;
using System.Linq;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Services;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Api
{
    /// <summary>
    /// Class UpdateDisplayPreferences.
    /// </summary>
    [Route("/DisplayPreferences/{DisplayPreferencesId}", "POST", Summary = "Updates a user's display preferences for an item")]
    public class UpdateDisplayPreferences : DisplayPreferencesDto, IReturnVoid
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "DisplayPreferencesId", Description = "DisplayPreferences Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string DisplayPreferencesId { get; set; }

        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string UserId { get; set; }
    }

    [Route("/DisplayPreferences/{Id}", "GET", Summary = "Gets a user's display preferences for an item")]
    public class GetDisplayPreferences : IReturn<DisplayPreferencesDto>
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }

        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string UserId { get; set; }

        [ApiMember(Name = "Client", Description = "Client", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string Client { get; set; }
    }

    /// <summary>
    /// Class DisplayPreferencesService.
    /// </summary>
    [Authenticated]
    public class DisplayPreferencesService : BaseApiService
    {
        /// <summary>
        /// The user manager.
        /// </summary>
        private readonly IDisplayPreferencesManager _displayPreferencesManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="DisplayPreferencesService" /> class.
        /// </summary>
        /// <param name="displayPreferencesManager">The display preferences manager.</param>
        public DisplayPreferencesService(
            ILogger<DisplayPreferencesService> logger,
            IServerConfigurationManager serverConfigurationManager,
            IHttpResultFactory httpResultFactory,
            IDisplayPreferencesManager displayPreferencesManager)
            : base(logger, serverConfigurationManager, httpResultFactory)
        {
            _displayPreferencesManager = displayPreferencesManager;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public object Get(GetDisplayPreferences request)
        {
            var result = _displayPreferencesManager.GetDisplayPreferences(Guid.Parse(request.UserId), request.Client);

            if (result == null)
            {
                return null;
            }

            var dto = new DisplayPreferencesDto
            {
                Client = result.Client,
                Id = result.UserId.ToString(),
                ViewType = result.ViewType?.ToString(),
                SortBy = result.SortBy,
                SortOrder = result.SortOrder,
                IndexBy = result.IndexBy?.ToString(),
                RememberIndexing = result.RememberIndexing,
                RememberSorting = result.RememberSorting,
                ScrollDirection = result.ScrollDirection,
                ShowBackdrop = result.ShowBackdrop,
                ShowSidebar = result.ShowSidebar
            };

            foreach (var homeSection in result.HomeSections)
            {
                dto.CustomPrefs["homesection" + homeSection.Order] = homeSection.Type.ToString().ToLowerInvariant();
            }

            dto.CustomPrefs["chromecastVersion"] = result.ChromecastVersion.ToString().ToLowerInvariant();
            dto.CustomPrefs["skipForwardLength"] = result.SkipForwardLength.ToString();
            dto.CustomPrefs["skipBackLength"] = result.SkipBackwardLength.ToString();
            dto.CustomPrefs["enableNextVideoInfoOverlay"] = result.EnableNextVideoInfoOverlay.ToString();

            return ToOptimizedResult(dto);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(UpdateDisplayPreferences request)
        {
            HomeSectionType[] defaults =
            {
                HomeSectionType.SmallLibraryTiles,
                HomeSectionType.Resume,
                HomeSectionType.ResumeAudio,
                HomeSectionType.LiveTv,
                HomeSectionType.NextUp,
                HomeSectionType.LatestMedia,
                HomeSectionType.None,
            };

            var prefs = _displayPreferencesManager.GetDisplayPreferences(Guid.Parse(request.UserId), request.Client);

            prefs.ViewType = Enum.TryParse<ViewType>(request.ViewType, true, out var viewType) ? viewType : (ViewType?)null;
            prefs.IndexBy = Enum.TryParse<IndexingKind>(request.IndexBy, true, out var indexBy) ? indexBy : (IndexingKind?)null;
            prefs.ShowBackdrop = request.ShowBackdrop;
            prefs.ShowSidebar = request.ShowSidebar;
            prefs.SortBy = request.SortBy;
            prefs.SortOrder = request.SortOrder;
            prefs.RememberIndexing = request.RememberIndexing;
            prefs.RememberSorting = request.RememberSorting;
            prefs.ScrollDirection = request.ScrollDirection;
            prefs.ChromecastVersion = request.CustomPrefs.TryGetValue("chromecastVersion", out var chromecastVersion)
                ? Enum.Parse<ChromecastVersion>(chromecastVersion, true)
                : ChromecastVersion.Stable;
            prefs.EnableNextVideoInfoOverlay = request.CustomPrefs.TryGetValue("enableNextVideoInfoOverlay", out var enableNextVideoInfoOverlay)
                ? bool.Parse(enableNextVideoInfoOverlay)
                : true;
            prefs.SkipBackwardLength = request.CustomPrefs.TryGetValue("skipBackLength", out var skipBackLength) ? int.Parse(skipBackLength) : 10000;
            prefs.SkipForwardLength = request.CustomPrefs.TryGetValue("skipForwardLength", out var skipForwardLength) ? int.Parse(skipForwardLength) : 30000;
            prefs.HomeSections.Clear();

            foreach (var key in request.CustomPrefs.Keys.Where(key => key.StartsWith("homesection")))
            {
                var order = int.Parse(key.AsSpan().Slice("homesection".Length));
                if (!Enum.TryParse<HomeSectionType>(request.CustomPrefs[key], true, out var type))
                {
                    type = order < 7 ? defaults[order] : HomeSectionType.None;
                }

                prefs.HomeSections.Add(new HomeSection
                {
                    Order = order,
                    Type = type
                });
            }

            _displayPreferencesManager.SaveChanges(prefs);
        }
    }
}
