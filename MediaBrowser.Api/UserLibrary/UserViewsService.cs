using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Library;
using MediaBrowser.Model.Querying;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Services;

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
        public string UserId { get; set; }

        [ApiMember(Name = "IncludeExternalContent", Description = "Whether or not to include external views such as channels or live tv", IsRequired = true, DataType = "boolean", ParameterType = "query", Verb = "POST")]
        public bool? IncludeExternalContent { get; set; }

        public string PresetViews { get; set; }
    }

    [Route("/Users/{UserId}/GroupingOptions", "GET")]
    public class GetGroupingOptions : IReturn<List<SpecialViewOption>>
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string UserId { get; set; }
    }

    public class UserViewsService : BaseApiService
    {
        private readonly IUserManager _userManager;
        private readonly IUserViewManager _userViewManager;
        private readonly IDtoService _dtoService;
        private readonly IAuthorizationContext _authContext;

        public UserViewsService(IUserManager userManager, IUserViewManager userViewManager, IDtoService dtoService, IAuthorizationContext authContext)
        {
            _userManager = userManager;
            _userViewManager = userViewManager;
            _dtoService = dtoService;
            _authContext = authContext;
        }

        public async Task<object> Get(GetUserViews request)
        {
            var query = new UserViewQuery
            {
                UserId = request.UserId
            };

            if (request.IncludeExternalContent.HasValue)
            {
                query.IncludeExternalContent = request.IncludeExternalContent.Value;
            }

            if (!string.IsNullOrWhiteSpace(request.PresetViews))
            {
                query.PresetViews = request.PresetViews.Split(',');
            }

            var app = _authContext.GetAuthorizationInfo(Request).Client ?? string.Empty;
            if (app.IndexOf("emby rt", StringComparison.OrdinalIgnoreCase) != -1)
            {
                query.PresetViews = new[] { CollectionType.Music, CollectionType.Movies, CollectionType.TvShows };
            }
            //query.PresetViews = new[] { CollectionType.Music, CollectionType.Movies, CollectionType.TvShows };

            var folders = await _userViewManager.GetUserViews(query, CancellationToken.None).ConfigureAwait(false);

            var dtoOptions = GetDtoOptions(_authContext, request);
            dtoOptions.Fields.Add(ItemFields.PrimaryImageAspectRatio);
            dtoOptions.Fields.Add(ItemFields.DisplayPreferencesId);
            dtoOptions.Fields.Remove(ItemFields.SyncInfo);
            dtoOptions.Fields.Remove(ItemFields.BasicSyncInfo);

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

        public async Task<object> Get(GetGroupingOptions request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var views = user.RootFolder
                .GetChildren(user, true)
                .OfType<Folder>()
                .Where(UserView.IsEligibleForGrouping)
                .ToList();

            var list = views
                .Select(i => new SpecialViewOption
                {
                    Name = i.Name,
                    Id = i.Id.ToString("N")

                })
            .OrderBy(i => i.Name)
            .ToList();

            return ToOptimizedResult(list);
        }
    }

    class SpecialViewOption
    {
        public string Name { get; set; }
        public string Id { get; set; }
    }
}
