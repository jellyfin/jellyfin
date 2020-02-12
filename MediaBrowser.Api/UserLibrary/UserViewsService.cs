using System;
using System.Globalization;
using System.Linq;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Library;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Services;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Api.UserLibrary
{
    [Route("/Users/{UserId}/Views", "GET")]
    public class GetUserViews : IReturn<QueryResult<BaseItemDto>>
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public Guid UserId { get; set; }

        [ApiMember(Name = "IncludeExternalContent", Description = "Whether or not to include external views such as channels or live tv", IsRequired = true, DataType = "boolean", ParameterType = "query", Verb = "GET")]
        public bool? IncludeExternalContent { get; set; }
        public bool IncludeHidden { get; set; }

        public string PresetViews { get; set; }
    }

    [Route("/Users/{UserId}/GroupingOptions", "GET")]
    public class GetGroupingOptions : IReturn<SpecialViewOption[]>
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public Guid UserId { get; set; }
    }

    public class UserViewsService : BaseApiService
    {
        private readonly IUserManager _userManager;
        private readonly IUserViewManager _userViewManager;
        private readonly IDtoService _dtoService;
        private readonly IAuthorizationContext _authContext;
        private readonly ILibraryManager _libraryManager;

        public UserViewsService(
            ILogger<UserViewsService> logger,
            IServerConfigurationManager serverConfigurationManager,
            IHttpResultFactory httpResultFactory,
            IUserManager userManager,
            IUserViewManager userViewManager,
            IDtoService dtoService,
            IAuthorizationContext authContext,
            ILibraryManager libraryManager)
            : base(logger, serverConfigurationManager, httpResultFactory)
        {
            _userManager = userManager;
            _userViewManager = userViewManager;
            _dtoService = dtoService;
            _authContext = authContext;
            _libraryManager = libraryManager;
        }

        public object Get(GetUserViews request)
        {
            var query = new UserViewQuery
            {
                UserId = request.UserId
            };

            if (request.IncludeExternalContent.HasValue)
            {
                query.IncludeExternalContent = request.IncludeExternalContent.Value;
            }
            query.IncludeHidden = request.IncludeHidden;

            if (!string.IsNullOrWhiteSpace(request.PresetViews))
            {
                query.PresetViews = request.PresetViews.Split(',');
            }

            var app = _authContext.GetAuthorizationInfo(Request).Client ?? string.Empty;
            if (app.IndexOf("emby rt", StringComparison.OrdinalIgnoreCase) != -1)
            {
                query.PresetViews = new[] { CollectionType.Movies, CollectionType.TvShows };
            }

            var folders = _userViewManager.GetUserViews(query);

            var dtoOptions = GetDtoOptions(_authContext, request);
            var fields = dtoOptions.Fields.ToList();

            fields.Add(ItemFields.PrimaryImageAspectRatio);
            fields.Add(ItemFields.DisplayPreferencesId);
            fields.Remove(ItemFields.BasicSyncInfo);
            dtoOptions.Fields = fields.ToArray();

            var user = _userManager.GetUserById(request.UserId);

            var dtos = folders.Select(i => _dtoService.GetBaseItemDto(i, dtoOptions, user))
                .ToArray();

            var result = new QueryResult<BaseItemDto>
            {
                Items = dtos,
                TotalRecordCount = dtos.Length
            };

            return ToOptimizedResult(result);
        }

        public object Get(GetGroupingOptions request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var list = _libraryManager.GetUserRootFolder()
                .GetChildren(user, true)
                .OfType<Folder>()
                .Where(UserView.IsEligibleForGrouping)
                .Select(i => new SpecialViewOption
                {
                    Name = i.Name,
                    Id = i.Id.ToString("N", CultureInfo.InvariantCulture)

                })
            .OrderBy(i => i.Name)
            .ToArray();

            return ToOptimizedResult(list);
        }
    }

    class SpecialViewOption
    {
        public string Name { get; set; }
        public string Id { get; set; }
    }
}
