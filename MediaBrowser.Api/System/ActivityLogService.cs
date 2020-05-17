using System;
using System.Globalization;
using System.Linq;
using Jellyfin.Data.Entities;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Services;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Api.System
{
    [Route("/System/ActivityLog/Entries", "GET", Summary = "Gets activity log entries")]
    public class GetActivityLogs : IReturn<QueryResult<ActivityLogEntry>>
    {
        /// <summary>
        /// Skips over a given number of items within the results. Use for paging.
        /// </summary>
        /// <value>The start index.</value>
        [ApiMember(Name = "StartIndex", Description = "Optional. The record index to start at. All items with a lower index will be dropped from the results.", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? StartIndex { get; set; }

        /// <summary>
        /// The maximum number of items to return
        /// </summary>
        /// <value>The limit.</value>
        [ApiMember(Name = "Limit", Description = "Optional. The maximum number of records to return", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? Limit { get; set; }

        [ApiMember(Name = "MinDate", Description = "Optional. The minimum date. Format = ISO", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string MinDate { get; set; }

        public bool? HasUserId { get; set; }
    }

    [Authenticated(Roles = "Admin")]
    public class ActivityLogService : BaseApiService
    {
        private readonly IActivityManager _activityManager;

        public ActivityLogService(
            ILogger<ActivityLogService> logger,
            IServerConfigurationManager serverConfigurationManager,
            IHttpResultFactory httpResultFactory,
            IActivityManager activityManager)
            : base(logger, serverConfigurationManager, httpResultFactory)
        {
            _activityManager = activityManager;
        }

        public object Get(GetActivityLogs request)
        {
            DateTime? minDate = string.IsNullOrWhiteSpace(request.MinDate) ?
                (DateTime?)null :
                DateTime.Parse(request.MinDate, null, DateTimeStyles.RoundtripKind).ToUniversalTime();

            var filterFunc = new Func<IQueryable<ActivityLog>, IQueryable<ActivityLog>>(
                entries => entries.Where(entry => entry.DateCreated >= minDate));

            var result = _activityManager.GetPagedResult(filterFunc, request.StartIndex, request.Limit);

            return ToOptimizedResult(result);
        }
    }
}
