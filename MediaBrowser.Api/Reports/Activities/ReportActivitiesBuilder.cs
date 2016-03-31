using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Querying;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.Library;
namespace MediaBrowser.Api.Reports
{
    /// <summary> A report activities builder. </summary>
    /// <seealso cref="T:MediaBrowser.Api.Reports.ReportBuilderBase"/>
    public class ReportActivitiesBuilder : ReportBuilderBase
    {
        #region [Constructors]

        /// <summary>
        /// Initializes a new instance of the MediaBrowser.Api.Reports.ReportActivitiesBuilder class. </summary>
        /// <param name="libraryManager"> Manager for library. </param>
        /// <param name="userManager"> Manager for user. </param>
        public ReportActivitiesBuilder(ILibraryManager libraryManager, IUserManager userManager)
            : base(libraryManager)
        {
            _userManager = userManager;
        }

        #endregion

        #region [Private Fields]

        private readonly IUserManager _userManager; ///< Manager for user

        #endregion

        #region [Public Methods]

        /// <summary> Gets a result. </summary>
        /// <param name="queryResult"> The query result. </param>
        /// <param name="request"> The request. </param>
        /// <returns> The result. </returns>
        public ReportResult GetResult(QueryResult<ActivityLogEntry> queryResult, IReportsQuery request)
        {
            ReportDisplayType displayType = ReportHelper.GetReportDisplayType(request.DisplayType);
            List<ReportOptions<ActivityLogEntry>> options = this.GetReportOptions<ActivityLogEntry>(request,
                () => this.GetDefaultHeaderMetadata(),
                (hm) => this.GetOption(hm)).Where(x => this.DisplayTypeVisible(x.Header.DisplayType, displayType)).ToList();

            var headers = GetHeaders<ActivityLogEntry>(options);
            var rows = GetReportRows(queryResult.Items, options);

            ReportResult result = new ReportResult { Headers = headers };
            HeaderMetadata groupBy = ReportHelper.GetHeaderMetadataType(request.GroupBy);
            int i = headers.FindIndex(x => x.FieldName == groupBy);
            if (groupBy != HeaderMetadata.None && i >= 0)
            {
                var rowsGroup = rows.SelectMany(x => x.Columns[i].Name.Split(';'), (x, g) => new { Group = g.Trim(), Rows = x })
                    .GroupBy(x => x.Group)
                    .OrderBy(x => x.Key)
                    .Select(x => new ReportGroup { Name = x.Key, Rows = x.Select(r => r.Rows).ToList() });

                result.Groups = rowsGroup.ToList();
                result.IsGrouped = true;
            }
            else
            {
                result.Rows = rows;
                result.IsGrouped = false;
            }

            return result;
        }

        #endregion

        #region [Protected Internal Methods]

        /// <summary> Gets the headers. </summary>
        /// <typeparam name="H"> Type of the header. </typeparam>
        /// <param name="request"> The request. </param>
        /// <returns> The headers. </returns>
        /// <seealso cref="M:MediaBrowser.Api.Reports.ReportBuilderBase.GetHeaders{H}(H)"/>
        protected internal override List<ReportHeader> GetHeaders<H>(H request)
        {
            return this.GetHeaders<ActivityLogEntry>(request, () => this.GetDefaultHeaderMetadata(), (hm) => this.GetOption(hm));
        }

        #endregion

        #region [Private Methods]

        /// <summary> Gets default header metadata. </summary>
        /// <returns> The default header metadata. </returns>
        private List<HeaderMetadata> GetDefaultHeaderMetadata()
        {
            return new List<HeaderMetadata>
					{
                        HeaderMetadata.UserPrimaryImage,
                        HeaderMetadata.Date,
                        HeaderMetadata.User,
                        HeaderMetadata.Type,
                        HeaderMetadata.Severity,
						HeaderMetadata.Name,
                        HeaderMetadata.ShortOverview,
						HeaderMetadata.Overview,
                        //HeaderMetadata.UserId
                        //HeaderMetadata.Item,
					};
        }

        /// <summary> Gets an option. </summary>
        /// <param name="header"> The header. </param>
        /// <param name="sortField"> The sort field. </param>
        /// <returns> The option. </returns>
        private ReportOptions<ActivityLogEntry> GetOption(HeaderMetadata header, string sortField = "")
        {
            HeaderMetadata internalHeader = header;

            ReportOptions<ActivityLogEntry> option = new ReportOptions<ActivityLogEntry>()
            {
                Header = new ReportHeader
                {
                    HeaderFieldType = ReportFieldType.String,
                    SortField = sortField,
                    Type = "",
                    ItemViewType = ItemViewType.None
                }
            };

            switch (header)
            {
                case HeaderMetadata.Name:
                    option.Column = (i, r) => i.Name;
                    option.Header.SortField = "";
                    break;
                case HeaderMetadata.Overview:
                    option.Column = (i, r) => i.Overview;
                    option.Header.SortField = "";
                    option.Header.CanGroup = false;
                    break;

                case HeaderMetadata.ShortOverview:
                    option.Column = (i, r) => i.ShortOverview;
                    option.Header.SortField = "";
                    option.Header.CanGroup = false;
                    break;

                case HeaderMetadata.Type:
                    option.Column = (i, r) => i.Type;
                    option.Header.SortField = "";
                    break;

                case HeaderMetadata.Date:
                    option.Column = (i, r) => i.Date;
                    option.Header.SortField = "";
                    option.Header.HeaderFieldType = ReportFieldType.DateTime;
                    option.Header.Type = "";
                    break;

                case HeaderMetadata.UserPrimaryImage:
                    //option.Column = (i, r) => i.UserPrimaryImageTag;
                    option.Header.DisplayType = ReportDisplayType.Screen;
                    option.Header.ItemViewType = ItemViewType.UserPrimaryImage;
                    option.Header.ShowHeaderLabel = false;
                    internalHeader = HeaderMetadata.User;
                    option.Header.CanGroup = false;
                    option.Column = (i, r) =>
                    {
                        if (!string.IsNullOrEmpty(i.UserId))
                        {
                            MediaBrowser.Controller.Entities.User user = _userManager.GetUserById(i.UserId);
                            if (user != null)
                            {
                                var dto = _userManager.GetUserDto(user);
                                return dto.PrimaryImageTag;
                            }
                        }
                        return string.Empty;
                    };
                    option.Header.SortField = "";
                    break;
                case HeaderMetadata.Severity:
                    option.Column = (i, r) => i.Severity;
                    option.Header.SortField = "";
                    break;
                case HeaderMetadata.Item:
                    option.Column = (i, r) => i.ItemId;
                    option.Header.SortField = "";
                    break;
                case HeaderMetadata.User:
                    option.Column = (i, r) =>
                    {
                        if (!string.IsNullOrEmpty(i.UserId))
                        {
                            MediaBrowser.Controller.Entities.User user = _userManager.GetUserById(i.UserId);
                            if (user != null)
                                return user.Name;
                        }
                        return string.Empty;
                    };
                    option.Header.SortField = "";
                    break;
                case HeaderMetadata.UserId:
                    option.Column = (i, r) => i.UserId;
                    option.Header.SortField = "";
                    break;
            }

            option.Header.Name = GetLocalizedHeader(internalHeader);
            option.Header.FieldName = header;

            return option;
        }

        /// <summary> Gets report rows. </summary>
        /// <param name="items"> The items. </param>
        /// <param name="options"> Options for controlling the operation. </param>
        /// <returns> The report rows. </returns>
        private List<ReportRow> GetReportRows(IEnumerable<ActivityLogEntry> items, List<ReportOptions<ActivityLogEntry>> options)
        {
            var rows = new List<ReportRow>();

            foreach (ActivityLogEntry item in items)
            {
                ReportRow rRow = GetRow(item);
                foreach (ReportOptions<ActivityLogEntry> option in options)
                {
                    object itemColumn = option.Column != null ? option.Column(item, rRow) : "";
                    object itemId = option.ItemID != null ? option.ItemID(item) : "";
                    ReportItem rItem = new ReportItem
                    {
                        Name = ReportHelper.ConvertToString(itemColumn, option.Header.HeaderFieldType),
                        Id = ReportHelper.ConvertToString(itemId, ReportFieldType.Object)
                    };
                    rRow.Columns.Add(rItem);
                }

                rows.Add(rRow);
            }

            return rows;
        }

        /// <summary> Gets a row. </summary>
        /// <param name="item"> The item. </param>
        /// <returns> The row. </returns>
        private ReportRow GetRow(ActivityLogEntry item)
        {
            ReportRow rRow = new ReportRow
            {
                Id = item.Id,
                UserId = item.UserId
            };
            return rRow;
        }

        #endregion

    }
}
