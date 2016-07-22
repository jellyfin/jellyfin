define(['jQuery', 'libraryBrowser'], function ($, libraryBrowser) {

    var defaultSortBy = "SortName";
    var topItems = 5;

    var query = {
        StartIndex: 0,
        Limit: 100,
        IncludeItemTypes: "Movie",
        HasQueryLimit: true,
        GroupBy: "None",
        ReportView: "ReportData",
        DisplayType: "Screen",
    };

    function getTable(result) {
        var html = '';
        //Report table
        html += '<table id="tblReport" data-role="table" data-mode="reflow" class="tblLibraryReport stripedTable ui-responsive table-stroke detailTable" style="display:table;">';
        html += '<thead>';

        //Report headers
        result.Headers.map(function (header) {
            var cellHtml = '<th data-priority="' + 'persist' + '">';

            if (header.ShowHeaderLabel) {
                if (header.SortField) {
                    cellHtml += '<a class="lnkColumnSort" href="#" data-sortfield="' + header.SortField + '" style="text-decoration:underline;">';
                }

                cellHtml += (header.Name || '&nbsp;');
                if (header.SortField) {
                    cellHtml += '</a>';
                    if (header.SortField === defaultSortBy) {

                        if (query.SortOrder === "Descending") {
                            cellHtml += '<span style="font-weight:bold;margin-left:5px;vertical-align:top;font-size:14px">&darr;</span>';
                        } else {
                            cellHtml += '<span style="font-weight:bold;margin-left:5px;vertical-align:top;font-size:14px;">&uarr;</span>';
                        }
                    }
                }
            }
            cellHtml += '</th>';
            html += cellHtml;
        });

        html += '</thead>';
        //Report body
        html += '<tbody>';
        if (result.IsGrouped === false) {

            result.Rows.map(function (row) {
                html += getRow(result.Headers, row);
            });
        }
        else {

            result.Groups.map(function (group) {
                html += '<tr style="background-color: rgb(51, 51, 51);">';
                html += '<th scope="rowgroup" colspan="' + result.Headers.length + '">' + (group.Name || '&nbsp;') + '</th>';
                html += '</tr>';
                group.Rows.map(function (row) {
                    html += getRow(result.Headers, row);
                });
                html += '<tr>';
                html += '<th scope="rowgroup" colspan="' + result.Headers.length + '">' + '&nbsp;' + '</th>';
                html += '</tr>';
            });
        }

        html += '</tbody>';
        html += '</table>';
        return html;
    }

    function getRow(rHeaders, rRow) {
        var html = '';
        html += '<tr>';

        for (var j = 0; j < rHeaders.length; j++) {
            var rHeader = rHeaders[j];
            var rItem = rRow.Columns[j];
            html += getItem(rHeader, rRow, rItem);
        }
        html += '</tr>';
        return html;
    }

    function getItem(rHeader, rRow, rItem) {
        var html = '';
        html += '<td>';
        switch (rHeader.ItemViewType) {
            case "None":
                html += rItem.Name;
                break;
            case "Detail":
                var id = rRow.Id;
                if (rItem.Id)
                    id = rItem.Id;
                html += '<a href="itemdetails.html?id=' + id + '">' + rItem.Name + '</a>';
                break;
            case "Edit":
                html += '<a href="edititemmetadata.html?id=' + rRow.Id + '">' + rItem.Name + '</a>';
                break;
            case "List":
                html += '<a href="itemlist.html?id=' + rRow.Id + '">' + rItem.Name + '</a>';
                break;
            case "ItemByNameDetails":
                html += '<a href="itemdetails.html?id=' + rItem.Id + '&context=' + rRow.RowType + '">' + rItem.Name + '</a>';
                break;
            case "EmbeddedImage":
                if (rRow.HasEmbeddedImage) {
                    html += '<div class="libraryReportIndicator clearLibraryReportIndicator"><div class="ui-icon-check ui-btn-icon-notext"></div></div>';
                }
                break;
            case "SubtitleImage":
                if (rRow.HasSubtitles) {
                    html += '<div class="libraryReportIndicator clearLibraryReportIndicator"><div class="ui-icon-check ui-btn-icon-notext"></div></div>';
                }
                break;
            case "TrailersImage":
                if (rRow.HasLocalTrailer) {
                    html += '<div class="libraryReportIndicator clearLibraryReportIndicator"><div class="ui-icon-check ui-btn-icon-notext"></div></div>';
                }
                break;
            case "SpecialsImage":
                if (rRow.HasSpecials) {
                    html += '<div class="libraryReportIndicator clearLibraryReportIndicator"><div class="ui-icon-check ui-btn-icon-notext"></div></div>';
                }
                break;
            case "LockDataImage":
                if (rRow.HasLockData) {
                    html += '<i class="md-icon">lock</i>';
                }
                break;
            case "TagsPrimaryImage":
                if (!rRow.HasImageTagsPrimary) {
                    html += '<a href="edititemimages.html?id=' + rRow.Id + '"><img src="css/images/editor/missingprimaryimage.png" title="Missing primary image." style="width:18px"/></a>';
                }
                break;
            case "TagsBackdropImage":
                if (!rRow.HasImageTagsBackdrop) {
                    if (rRow.RowType !== "Episode" && rRow.RowType !== "Season" && rRow.MediaType !== "Audio" && rRow.RowType !== "TvChannel" && rRow.RowType !== "MusicAlbum") {
                        html += '<a href="edititemimages.html?id=' + rRow.Id + '"><img src="css/images/editor/missingbackdrop.png" title="Missing backdrop image." style="width:18px"/></a>';
                    }
                }
                break;
            case "TagsLogoImage":
                if (!rRow.HasImageTagsLogo) {
                    if (rRow.RowType === "Movie" || rRow.RowType === "Trailer" || rRow.RowType === "Series" || rRow.RowType === "MusicArtist" || rRow.RowType === "BoxSet") {
                        html += '<a href="edititemimages.html?id=' + rRow.Id + '"><img src="css/images/editor/missinglogo.png" title="Missing logo image." style="width:18px"/></a>';
                    }
                }
                break;
            case "UserPrimaryImage":
                if (rRow.UserId) {
                    var userImage = ApiClient.getUserImageUrl(rRow.UserId, {
                        height: 24,
                        type: 'Primary'

                    });
                    if (userImage) {
                        html += '<img src="' + userImage + '" />';
                    } else {
                        html += '';
                    }
                }
                break;
            case "StatusImage":
                if (rRow.HasLockData) {
                    html += '<i class="md-icon">lock</i>';
                }

                if (!rRow.HasLocalTrailer && rRow.RowType === "Movie") {
                    html += '<i title="Missing local trailer." class="md-icon">videocam</i>';
                }

                if (!rRow.HasImageTagsPrimary) {
                    html += '<a href="edititemimages.html?id=' + rRow.Id + '"><img src="css/images/editor/missingprimaryimage.png" title="Missing primary image." style="width:18px"/></a>';
                }

                if (!rRow.HasImageTagsBackdrop) {
                    if (rRow.RowType !== "Episode" && rRow.RowType !== "Season" && rRow.MediaType !== "Audio" && rRow.RowType !== "TvChannel" && rRow.RowType !== "MusicAlbum") {
                        html += '<a href="edititemimages.html?id=' + rRow.Id + '"><img src="css/images/editor/missingbackdrop.png" title="Missing backdrop image." style="width:18px"/></a>';
                    }
                }

                if (!rRow.HasImageTagsLogo) {
                    if (rRow.RowType === "Movie" || rRow.RowType === "Trailer" || rRow.RowType === "Series" || rRow.RowType === "MusicArtist" || rRow.RowType === "BoxSet") {
                        html += '<a href="edititemimages.html?id=' + rRow.Id + '"><img src="css/images/editor/missinglogo.png" title="Missing logo image." style="width:18px"/></a>';
                    }
                }
                break;
            default:
                html += rItem.Name;
        }
        html += '</td>';
        return html;
    }

    function getStats(result) {
        var html = '';
        html += '<div class="detailSection" >';
        //html += '<div class="detailSectionHeader">If you like Africa Fever II, check these out...</div>';
        html += '<div class="detailSectionContent">';
        result.Groups.map(function (group) {
            //html += '<div class="card transparentCard detailPageSquareCard" style="vertical-align: top;">';
            //html += '<div class="card transparentCard horizontalBackdropCard"  style="vertical-align: top;">';
            //html += '<div class="card transparentCard backdropCard"  style="vertical-align: top;">';
            html += '<div class="card transparentCard bannerCard"  style="vertical-align: top;">';
            //html += '<div class="card transparentCard cardImage" style="vertical-align: top;">';
            html += '<div class="visualCardBox">';
            html += '<div class="cardBox " >';

            html += '<div class="detailSection">'
            html += '<div class="detailSectionHeader">';
            html += '<span>' + group.Header + '&nbsp;' + '</span>';
            html += '</div>';

            html += '<div class="detailSectionContent">';
            html += '<div class="childrenItemsContainer itemsContainer" style="text-align: left;">';
            html += '<ul class="itemsListview ui-listview" >';

            var l = group.Items.length - 1;
            for (var j = 0; j < topItems  ; j++) {

                html += '<li class="ui-li listItem ui-li-has-alt ui-first-child">';
                if (j <= l) {
                    var rItem = group.Items[j];
                    html += '<a class="item ui-btn"';
                    if (rItem.Id > "")
                        html += ' href="itemdetails.html?id=' + rItem.Id + '"';
                    html += '>' + rItem.Name + '&nbsp;' + '</a>';
                    html += '<a title="" class="listviewMenuButton ui-btn ui-btn-inline">' + rItem.Value + '&nbsp;' + '</a>';
                }
                else
                    html += '<a class="item ui-btn">' + '&nbsp;' + '</a>';

                html += '</li>';

            }
            html += '</ul>';
            html += '</div>';
            html += '</div>';
            html += '</div>';

            html += '</div>';
            html += '</div>';
            html += '</div>';

        });


        html += '</div>';
        //html += '</div>';
        html += '</div>';
        return html;
    }

    function ExportReport(page, e) {

        query.UserId = Dashboard.getCurrentUserId();
        query.HasQueryLimit = false;
        var url = ApiClient.getUrl("Reports/Items/Download", query);

        if (url) {
            window.location.href = url;
        }
    }

    function loadGroupByFilters(page) {

        query.UserId = Dashboard.getCurrentUserId();
        var url = "";

        url = ApiClient.getUrl("Reports/Headers", query);
        ApiClient.getJSON(url).then(function (result) {
            var selected = "None";

            $('#selectReportGroup', page).find('option').remove().end();
            $('#selectReportGroup', page).append('<option value="None"></option>');

            result.map(function (header) {
                if ((header.DisplayType === "Screen" || header.DisplayType === "ScreenExport") && header.CanGroup) {
                    if (header.FieldName.length > 0) {
                        var option = '<option value="' + header.FieldName + '">' + header.Name + '</option>';
                        $('#selectReportGroup', page).append(option);
                        if (query.GroupBy === header.FieldName)
                            selected = header.FieldName;
                    }
                }
            });
            $('#selectPageSize', page).val(selected);

        });
    }

    function renderItems(page, result) {

        window.scrollTo(0, 0);
        var html = '';

        if (query.ReportView === "ReportData" || query.ReportView === "ReportStatistics") {
            $('#selectIncludeItemTypesBox', page).show();
            $('#tabFilter', page).show();
        }
        else {
            $('#selectIncludeItemTypesBox', page).hide();
            $('#tabFilterBox', page).hide();
            $('#tabFilter', page).hide();
        }

        var pagingHtml = libraryBrowser.getQueryPagingHtml({
            startIndex: query.StartIndex,
            limit: query.Limit,
            totalRecordCount: result.TotalRecordCount,
            updatePageSizeSetting: false,
            viewButton: true,
            showLimit: false
        });

        if (query.ReportView === "ReportData" || query.ReportView === "ReportActivities") {


            $('.listTopPaging', page).html(pagingHtml).trigger('create');
            // page.querySelector('.listTopPaging').innerHTML = pagingHtml;
            $('.listTopPaging', page).show();

            $('.listBottomPaging', page).html(pagingHtml).trigger('create');
            $('.listBottomPaging', page).show();

            $('.btnNextPage', page).on('click', function () {
                query.StartIndex += query.Limit;
                reloadItems(page);
            });
            $('.btnNextPage', page).show();

            $('.btnPreviousPage', page).on('click', function () {
                query.StartIndex -= query.Limit;
                reloadItems(page);
            });
            $('.btnPreviousPage', page).show();

            $('#btnReportExport', page).show();
            $('#selectPageSizeBox', page).show();
            $('#selectReportGroupingBox', page).show();
            $('#grpReportsColumns', page).show();

            html += getTable(result);

            $('.reporContainer', page).html(html).trigger('create');

            $('.lnkColumnSort', page).on('click', function () {

                var order = this.getAttribute('data-sortfield');

                if (query.SortBy === order) {

                    if (query.SortOrder === "Descending") {

                        query.SortOrder = "Ascending";
                        query.SortBy = defaultSortBy;

                    } else {

                        query.SortOrder = "Descending";
                        query.SortBy = order;
                    }

                } else {

                    query.SortOrder = "Ascending";
                    query.SortBy = order;
                }

                query.StartIndex = 0;

                reloadItems(page);
            });
        }
        else {

            $('.listTopPaging', page).html(pagingHtml).trigger('create');
            // page.querySelector('.listTopPaging').innerHTML = pagingHtml;

            $('.listTopPaging', page).show();
            $('.listBottomPaging', page).hide();

            $('.btnNextPage', page).hide();
            $('.btnPreviousPage', page).hide();

            $('#btnReportExport', page).hide();
            $('#selectPageSizeBox', page).hide();
            $('#selectReportGroupingBox', page).hide();
            $('#grpReportsColumns', page).hide();

            html += getStats(result);
            $('.reporContainer', page).html(html).trigger('create');
        }

        $('#GroupStatus', page).hide();
        $('#GroupAirDays', page).hide();
        $('#GroupEpisodes', page).hide();
        switch (query.IncludeItemTypes) {
            case "Series":
            case "Season":
                $('#GroupStatus', page).show();
                $('#GroupAirDays', page).show();
                break;
            case "Episode":
                $('#GroupStatus', page).show();
                $('#GroupAirDays', page).show();
                $('#GroupEpisodes', page).show();
                break;
        }
        $('.viewPanel', page).refresh;
    }

    function reloadItems(page) {
        Dashboard.showLoadingMsg();

        query.UserId = Dashboard.getCurrentUserId();
        var url = "";

        switch (query.ReportView) {
            case "ReportData":
                query.HasQueryLimit = true;
                url = ApiClient.getUrl("Reports/Items", query);
                break;
            case "ReportStatistics":
                query.TopItems = topItems;
                query.HasQueryLimit = false;
                url = ApiClient.getUrl("Reports/Statistics", query);
                break;
            case "ReportActivities":
                query.HasQueryLimit = true;
                url = ApiClient.getUrl("Reports/Activities", query);
                break;
        }

        ApiClient.getJSON(url).then(function (result) {
            updateFilterControls(page);
            renderItems(page, result);
        });


        Dashboard.hideLoadingMsg();
    }

    function updateFilterControls(page) {
        $('.chkStandardFilter', page).each(function () {

            var filters = "," + (query.Filters || "");
            var filterName = this.getAttribute('data-filter');

            this.checked = filters.indexOf(',' + filterName) != -1;

        }).checkboxradio('refresh');


        $('.chkVideoTypeFilter', page).each(function () {

            var filters = "," + (query.VideoTypes || "");
            var filterName = this.getAttribute('data-filter');

            this.checked = filters.indexOf(',' + filterName) != -1;

        }).checkboxradio('refresh');

        $('.chkStatus', page).each(function () {

            var filters = "," + (query.SeriesStatus || "");
            var filterName = this.getAttribute('data-filter');

            this.checked = filters.indexOf(',' + filterName) != -1;

        }).checkboxradio('refresh');

        $('.chkAirDays', page).each(function () {

            var filters = "," + (query.AirDays || "");
            var filterName = this.getAttribute('data-filter');

            this.checked = filters.indexOf(',' + filterName) != -1;

        }).checkboxradio('refresh');

        $('#chk3D', page).checked(query.Is3D == true).checkboxradio('refresh');
        $('#chkHD', page).checked(query.IsHD == true).checkboxradio('refresh');
        $('#chkSD', page).checked(query.IsHD == false).checkboxradio('refresh');

        $('#chkSubtitle', page).checked(query.HasSubtitles == true).checkboxradio('refresh');
        $('#chkTrailer', page).checked(query.HasTrailer == true).checkboxradio('refresh');
        $('#chkMissingTrailer', page).checked(query.HasTrailer == false).checkboxradio('refresh');
        $('#chkSpecialFeature', page).checked(query.HasSpecialFeature == true).checkboxradio('refresh');
        $('#chkThemeSong', page).checked(query.HasThemeSong == true).checkboxradio('refresh');
        $('#chkThemeVideo', page).checked(query.HasThemeVideo == true).checkboxradio('refresh');

        $('#selectPageSize', page).val(query.Limit);

        //Management
        $('#chkMissingRating', page).checked(query.HasOfficialRating == false).checkboxradio('refresh');
        $('#chkMissingOverview', page).checked(query.HasOverview == false).checkboxradio('refresh');
        $('#chkIsLocked', page).checked(query.IsLocked == true).checkboxradio('refresh');
        $('#chkMissingImdbId', page).checked(query.HasImdbId == false).checkboxradio('refresh');
        $('#chkMissingTmdbId', page).checked(query.HasTmdbId == false).checkboxradio('refresh');
        $('#chkMissingTvdbId', page).checked(query.HasTvdbId == false).checkboxradio('refresh');

        //Episodes
        $('#chkSpecialEpisode', page).checked(query.ParentIndexNumber == 0).checkboxradio('refresh');
        $('#chkMissingEpisode', page).checked(query.IsMissing == true).checkboxradio('refresh');
        $('#chkFutureEpisode', page).checked(query.IsUnaired == true).checkboxradio('refresh');

        $('#selectIncludeItemTypes').val(query.IncludeItemTypes);

        // isfavorite
        if (query.IsFavorite == true) {
            $('#isFavorite').val("true");
        }
        else if (query.IsFavorite == false) {
            $('#isFavorite').val("false");
        }
        else {
            $('#isFavorite').val("-");
        }


    }

    var filtersLoaded;
    function reloadFiltersIfNeeded(page) {
        if (!filtersLoaded) {

            filtersLoaded = true;

            QueryReportFilters.loadFilters(page, Dashboard.getCurrentUserId(), query, function () {

                reloadItems(page);
            });

            QueryReportColumns.loadColumns(page, Dashboard.getCurrentUserId(), query, function () {

                reloadItems(page);
            });
        }
    }

    $(document).on('pageinit', "#libraryReportManagerPage", function () {

        var page = this;

        $('#selectIncludeItemTypes', page).on('change', function () {

            query.StartIndex = 0;
            query.ReportView = $('#selectViewType', page).val();
            query.IncludeItemTypes = this.value;
            query.SortOrder = "Ascending";
            query.ReportColumns = null;
            $('.btnReportExport', page).hide();
            filtersLoaded = false;
            loadGroupByFilters(page);
            reloadFiltersIfNeeded(page);
            reloadItems(page);
        });

        $('#selectViewType', page).on('change', function () {

            query.StartIndex = 0;
            query.ReportView = this.value;
            query.IncludeItemTypes = $('#selectIncludeItemTypes', page).val();
            query.SortOrder = "Ascending";
            filtersLoaded = false;
            query.ReportColumns = null;
            loadGroupByFilters(page);
            reloadFiltersIfNeeded(page);
            reloadItems(page);
        });

        $('#selectReportGroup', page).on('change', function () {
            query.GroupBy = this.value;
            query.StartIndex = 0;
            reloadItems(page);
        });

        $('#btnReportExportCsv', page).on('click', function (e) {

            query.ExportType = "CSV";
            ExportReport(page, e);
        });

        $('#btnReportExportExcel', page).on('click', function (e) {

            query.ExportType = "Excel";
            ExportReport(page, e);
        });

        $('#btnResetReportColumns', page).on('click', function (e) {

            query.ReportColumns = null;
            query.StartIndex = 0;
            filtersLoaded = false;
            reloadFiltersIfNeeded(page);
            reloadItems(page);
        });

        $('.viewPanel', page).on('panelopen', function () {
            reloadFiltersIfNeeded(page);
        });

        $('#selectPageSize', page).on('change', function () {
            query.Limit = parseInt(this.value);
            query.StartIndex = 0;
            reloadItems(page);
        });

        $('#isFavorite', page).on('change', function () {

            if (this.value == "true") {
                query.IsFavorite = true;
            }
            else if (this.value == "false") {
                query.IsFavorite = false;
            }
            else {
                query.IsFavorite = null;
            }
            query.StartIndex = 0;
            reloadItems(page);
        });

        $('.chkStandardFilter', this).on('change', function () {

            var filterName = this.getAttribute('data-filter');
            var filters = query.Filters || "";

            filters = (',' + filters).replace(',' + filterName, '').substring(1);

            if (this.checked) {
                filters = filters ? (filters + ',' + filterName) : filterName;
            }

            query.StartIndex = 0;
            query.Filters = filters;

            reloadItems(page);
        });

        $('.chkVideoTypeFilter', this).on('change', function () {

            var filterName = this.getAttribute('data-filter');
            var filters = query.VideoTypes || "";

            filters = (',' + filters).replace(',' + filterName, '').substring(1);

            if (this.checked) {
                filters = filters ? (filters + ',' + filterName) : filterName;
            }

            query.StartIndex = 0;
            query.VideoTypes = filters;

            reloadItems(page);
        });

        $('#chk3D', this).on('change', function () {

            query.StartIndex = 0;
            query.Is3D = this.checked ? true : null;

            reloadItems(page);
        });

        $('#chkHD', this).on('change', function () {

            query.StartIndex = 0;
            query.IsHD = this.checked ? true : null;

            reloadItems(page);
        });

        $('#chkSD', this).on('change', function () {

            query.StartIndex = 0;
            query.IsHD = this.checked ? false : null;

            reloadItems(page);
        });

        $('#chkSubtitle', this).on('change', function () {

            query.StartIndex = 0;
            query.HasSubtitles = this.checked ? true : null;

            reloadItems(page);
        });

        $('#chkTrailer', this).on('change', function () {

            query.StartIndex = 0;
            query.HasTrailer = this.checked ? true : null;

            reloadItems(page);
        });

        $('#chkMissingTrailer', this).on('change', function () {

            query.StartIndex = 0;
            query.HasTrailer = this.checked ? false : null;

            reloadItems(page);
        });

        $('#chkSpecialFeature', this).on('change', function () {

            query.StartIndex = 0;
            query.HasSpecialFeature = this.checked ? true : null;

            reloadItems(page);
        });

        $('#chkThemeSong', this).on('change', function () {

            query.StartIndex = 0;
            query.HasThemeSong = this.checked ? true : null;

            reloadItems(page);
        });

        $('#chkThemeVideo', this).on('change', function () {

            query.StartIndex = 0;
            query.HasThemeVideo = this.checked ? true : null;

            reloadItems(page);
        });

        $('#radioBasicFilters', this).on('change', function () {

            if (this.checked) {
                $('.basicFilters', page).show();
                $('.advancedFilters', page).hide();
            } else {
                $('.basicFilters', page).hide();
            }
        });

        $('#radioAdvancedFilters', this).on('change', function () {

            if (this.checked) {
                $('.advancedFilters', page).show();
                $('.basicFilters', page).hide();
            } else {
                $('.advancedFilters', page).hide();
            }
        });

        //Management
        $('#chkIsLocked', page).on('change', function () {

            query.StartIndex = 0;
            query.IsLocked = this.checked ? true : null;

            reloadItems(page);
        });

        $('#chkMissingOverview', page).on('change', function () {

            query.StartIndex = 0;
            query.HasOverview = this.checked ? false : null;

            reloadItems(page);
        });

        $('#chkMissingEpisode', page).on('change', function () {

            query.StartIndex = 0;
            query.IsMissing = this.checked ? true : false;

            reloadItems(page);
        });

        $('#chkMissingRating', page).on('change', function () {

            query.StartIndex = 0;
            query.HasOfficialRating = this.checked ? false : null;

            reloadItems(page);
        });

        $('#chkMissingImdbId', page).on('change', function () {

            query.StartIndex = 0;
            query.HasImdbId = this.checked ? false : null;

            reloadItems(page);
        });

        $('#chkMissingTmdbId', page).on('change', function () {

            query.StartIndex = 0;
            query.HasTmdbId = this.checked ? false : null;

            reloadItems(page);
        });

        $('#chkMissingTvdbId', page).on('change', function () {

            query.StartIndex = 0;
            query.HasTvdbId = this.checked ? false : null;

            reloadItems(page);
        });

        //Episodes
        $('#chkMissingEpisode', page).on('change', function () {

            query.StartIndex = 0;
            query.IsMissing = this.checked ? true : false;

            reloadItems(page);
        });

        $('#chkFutureEpisode', page).on('change', function () {

            query.StartIndex = 0;

            if (this.checked) {
                query.IsUnaired = true;
                query.IsVirtualUnaired = null;
            } else {
                query.IsUnaired = null;
                query.IsVirtualUnaired = false;
            }


            reloadItems(page);
        });

        $('#chkSpecialEpisode', page).on('change', function () {

            query.ParentIndexNumber = this.checked ? 0 : null;

            reloadItems(page);
        });

        $('.chkAirDays', this).on('change', function () {

            var filterName = this.getAttribute('data-filter');
            var filters = query.AirDays || "";

            filters = (',' + filters).replace(',' + filterName, '').substring(1);

            if (this.checked) {
                filters = filters ? (filters + ',' + filterName) : filterName;
            }

            query.AirDays = filters;
            query.StartIndex = 0;
            reloadItems(page);
        });

        $('.chkStatus', this).on('change', function () {

            var filterName = this.getAttribute('data-filter');
            var filters = query.SeriesStatus || "";

            filters = (',' + filters).replace(',' + filterName, '').substring(1);

            if (this.checked) {
                filters = filters ? (filters + ',' + filterName) : filterName;
            }

            query.SeriesStatus = filters;
            query.StartIndex = 0;
            reloadItems(page);
        });

        $(page.getElementsByClassName('viewTabButton')).on('click', function () {

            var parent = $(this).parents('.viewPanel');
            $('.viewTabButton', parent).removeClass('ui-btn-active');
            this.classList.add('ui-btn-active');

            $('.viewTab', parent).addClass('hide');
            $('.' + this.getAttribute('data-tab'), parent).removeClass('hide');
        });
    })
	.on('pageshow', "#libraryReportManagerPage", function () {

	    query.UserId = Dashboard.getCurrentUserId();
	    var page = this;
	    query.SortOrder = "Ascending";

	    QueryReportFilters.onPageShow(page, query);
	    QueryReportColumns.onPageShow(page, query);
	    $('#selectIncludeItemTypes', page).val(query.IncludeItemTypes).trigger('change');

	    updateFilterControls(page);

	    filtersLoaded = false;
	    updateFilterControls(this);
	});

    function renderOptions(page, selector, cssClass, items) {

        var elem;

        if (items.length) {

            elem = $(selector, page).show();

        } else {
            elem = $(selector, page).hide();
        }

        var html = '';

        //  style="margin: -.2em -.8em;"
        html += '<div data-role="controlgroup">';

        var index = 0;
        var idPrefix = 'chk' + selector.substring(1);

        html += items.map(function (filter) {

            var itemHtml = '';

            var id = idPrefix + index;
            var label = filter;
            var value = filter;
            var checked = false;
            if (filter.FieldName) {
                label = filter.Name;
                value = filter.FieldName;
                checked = filter.Visible;
            }
            itemHtml += '<label for="' + id + '">' + label + '</label>';
            itemHtml += '<input id="' + id + '" type="checkbox" data-filter="' + value + '" class="' + cssClass + '"';
            if (checked)
                itemHtml += ' checked="checked" ';
            itemHtml += '/>';

            index++;

            return itemHtml;

        }).join('');

        html += '</div>';

        $('.filterOptions', elem).html(html).trigger('create');
    }

    function renderFilters(page, result) {


        if (result.Tags) {
            result.Tags.length = Math.min(result.Tags.length, 50);
        }

        renderOptions(page, '.genreFilters', 'chkGenreFilter', result.Genres);
        renderOptions(page, '.officialRatingFilters', 'chkOfficialRatingFilter', result.OfficialRatings);
        renderOptions(page, '.tagFilters', 'chkTagFilter', result.Tags);
        renderOptions(page, '.yearFilters', 'chkYearFilter', result.Years);

    }

    function renderColumnss(page, result) {


        if (result.Tags) {
            result.Tags.length = Math.min(result.Tags.length, 50);
        }

        renderOptions(page, '.reportsColumns', 'chkReportColumns', result);
    }

    function onFiltersLoaded(page, query, reloadItemsFn) {

        $('.chkGenreFilter', page).on('change', function () {

            var filterName = this.getAttribute('data-filter');
            var filters = query.Genres || "";
            var delimiter = '|';

            filters = (delimiter + filters).replace(delimiter + filterName, '').substring(1);

            if (this.checked) {
                filters = filters ? (filters + delimiter + filterName) : filterName;
            }

            query.StartIndex = 0;
            query.Genres = filters;

            reloadItemsFn();
        });
        $('.chkTagFilter', page).on('change', function () {

            var filterName = this.getAttribute('data-filter');
            var filters = query.Tags || "";
            var delimiter = '|';

            filters = (delimiter + filters).replace(delimiter + filterName, '').substring(1);

            if (this.checked) {
                filters = filters ? (filters + delimiter + filterName) : filterName;
            }

            query.StartIndex = 0;
            query.Tags = filters;

            reloadItemsFn();
        });
        $('.chkYearFilter', page).on('change', function () {

            var filterName = this.getAttribute('data-filter');
            var filters = query.Years || "";
            var delimiter = ',';

            filters = (delimiter + filters).replace(delimiter + filterName, '').substring(1);

            if (this.checked) {
                filters = filters ? (filters + delimiter + filterName) : filterName;
            }

            query.StartIndex = 0;
            query.Years = filters;

            reloadItemsFn();
        });
        $('.chkOfficialRatingFilter', page).on('change', function () {

            var filterName = this.getAttribute('data-filter');
            var filters = query.OfficialRatings || "";
            var delimiter = '|';

            filters = (delimiter + filters).replace(delimiter + filterName, '').substring(1);

            if (this.checked) {
                filters = filters ? (filters + delimiter + filterName) : filterName;
            }

            query.StartIndex = 0;
            query.OfficialRatings = filters;

            reloadItemsFn();
        });
    }

    function onColumnsLoaded(page, query, reloadItemsFn) {

        $('.chkReportColumns', page).on('change', function () {

            var filterName = this.getAttribute('data-filter');
            var filters = query.ReportColumns || "";
            var delimiter = '|';

            filters = (delimiter + filters).replace(delimiter + filterName, '').substring(1);

            if (this.checked) {
                filters = filters ? (filters + delimiter + filterName) : filterName;
            }

            query.StartIndex = 0;
            query.ReportColumns = filters;

            reloadItemsFn();
        });
    }

    function loadFilters(page, userId, itemQuery, reloadItemsFn) {

        return ApiClient.getJSON(ApiClient.getUrl('Items/Filters', {

            UserId: userId,
            ParentId: itemQuery.ParentId,
            IncludeItemTypes: itemQuery.IncludeItemTypes,
            ReportView: itemQuery.ReportView


        })).then(function (result) {

            renderFilters(page, result);

            onFiltersLoaded(page, itemQuery, reloadItemsFn);
        });
    }

    function loadColumns(page, userId, itemQuery, reloadItemsFn) {

        return ApiClient.getJSON(ApiClient.getUrl('Reports/Headers', {

            UserId: userId,
            IncludeItemTypes: itemQuery.IncludeItemTypes,
            ReportView: itemQuery.ReportView

        })).then(function (result) {

            renderColumnss(page, result);
            var filters = "";
            var delimiter = '|';
            result.map(function (item) {

                if ((item.DisplayType === "Screen" || item.DisplayType === "ScreenExport"))
                    filters = filters ? (filters + delimiter + item.FieldName) : item.FieldName;
            });
            if (!itemQuery.ReportColumns)
                itemQuery.ReportColumns = filters;
            onColumnsLoaded(page, itemQuery, reloadItemsFn);
        });

    }

    function onPageShow(page, query) {
        query.Genres = null;
        query.Years = null;
        query.OfficialRatings = null;
        query.Tags = null;

    }

    function onPageReportColumnsShow(page, query) {
        query.ReportColumns = null;
    }

    window.QueryReportFilters = {
        loadFilters: loadFilters,
        onPageShow: onPageShow
    };

    window.QueryReportColumns = {
        loadColumns: loadColumns,
        onPageShow: onPageReportColumnsShow
    };
});