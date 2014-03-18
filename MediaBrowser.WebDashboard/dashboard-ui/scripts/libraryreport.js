(function ($, document, window) {

    var defaultSortBy = "SortName";

    // The base query options
    var query = {

        SortBy: defaultSortBy,
        SortOrder: "Ascending",
        Recursive: true,
        Fields: "MediaStreams,DateCreated,Settings,Studios",
        StartIndex: 0,
        IncludeItemTypes: "Movie",
        IsMissing: false,
        IsVirtualUnaired: false,
        Limit: 300,
        CollapseBoxSetItems: false
    };

    function getHeaderCells(reportType) {

        switch (reportType) {

            case 'Season':
                {
                    return [
                        {},
                        { name: 'Series', sortField: 'SeriesSortName,SortName' },
                        { name: 'Season', sortField: 'SortName' },
                        { name: 'Date Added', sortField: 'DateCreated,SortName' }
                    ];
                }
            case 'Series':
                {
                    return [
                        {},
                        { name: 'Name', sortField: 'SortName' },
                        { name: 'Network', sortField: 'Studio,SortName' },
                        { name: 'Date Added', sortField: 'DateCreated,SortName' },
                        { name: 'Year', sortField: 'ProductionYear,PremiereDate,SortName' },
                        { name: 'Parental Rating', sortField: 'OfficialRating,SortName' },
                        { name: 'Community Rating', sortField: 'CommunityRating,SortName' },
                        { name: 'Runtime', sortField: 'Runtime,SortName' },
                        { name: 'Trailers' },
                        { name: 'Specials' }
                    ];
                }
            case 'Game':
                {
                    return [
                        {},
                        { name: 'Name', sortField: 'SortName' },
                        { name: 'Game System', sortField: 'GameSystem,SortName' },
                        { name: 'Date Added', sortField: 'DateCreated,GameSystem,SortName' },
                        { name: 'Release Date', sortField: 'ProductionYear,PremiereDate,GameSystem,SortName' },
                        { name: 'Parental Rating', sortField: 'OfficialRating,GameSystem,SortName' },
                        { name: 'Community Rating', sortField: 'CommunityRating,GameSystem,SortName' },
                        { name: 'Players', sortField: 'Players,GameSystem,SortName' },
                        { name: 'Trailers' }
                    ];
                }
            case 'Audio':
                {
                    return [
                        {},
                        { name: 'Album Artist', sortField: 'AlbumArtist,Album,SortName' },
                        { name: 'Album', sortField: 'Album,SortName' },
                        { name: 'Disc' },
                        { name: 'Track' },
                        { name: 'Name', sortField: 'Name' },
                        { name: 'Date Added', sortField: 'DateCreated,SortName' },
                        { name: 'Release Date', sortField: 'ProductionYear,PremiereDate,SortName' },
                        { name: 'Runtime', sortField: 'Runtime,SortName' },
                        { name: 'Audio' },
                        { name: 'Embedded Image' }
                    ];
                }
            case 'Episode':
                {
                    return [
                        {},
                        { name: 'Series', sortField: 'SeriesSortName,SortName' },
                        { name: 'Season' },
                        { name: 'Name', sortField: 'SortName' },
                        { name: 'Date Added', sortField: 'DateCreated,SortName' },
                        { name: 'Release Date', sortField: 'ProductionYear,PremiereDate,SortName' },
                        { name: 'Community Rating', sortField: 'CommunityRating,SortName' },
                        { name: 'Runtime', sortField: 'Runtime,SortName' },
                        { name: 'Video' },
                        { name: 'Resolution' },
                        { name: 'Audio' },
                        { name: 'Subtitles' }
                    ];
                }
            case 'BoxSet':
                {
                    return [
                        {},
                        { name: 'Name', sortField: 'SortName' },
                        { name: 'Date Added', sortField: 'DateCreated,SortName' },
                        { name: 'Release Date', sortField: 'ProductionYear,PremiereDate,SortName' },
                        { name: 'Parental Rating', sortField: 'OfficialRating,SortName' },
                        { name: 'Community Rating', sortField: 'CommunityRating,SortName' },
                        { name: 'Trailers' }
                    ];
                }
            case 'Book':
                {
                    return [
                        {},
                        { name: 'Series' },
                        { name: 'Name', sortField: 'SortName' },
                        { name: 'Date Added', sortField: 'DateCreated,SortName' },
                        { name: 'Release Date', sortField: 'ProductionYear,PremiereDate,SortName' },
                        { name: 'Parental Rating', sortField: 'OfficialRating,SortName' },
                        { name: 'Community Rating', sortField: 'CommunityRating,SortName' }
                    ];
                }
            case 'MusicArtist':
                {
                    return [
                        {},
                        { name: 'Name', sortField: 'SortName' },
                        { name: 'Date Added', sortField: 'DateCreated,SortName' }
                    ];
                }
            case 'MusicAlbum':
                {
                    return [
                        {},
                        { name: 'Album Artist', sortField: 'AlbumArtist,SortName' },
                        { name: 'Name', sortField: 'SortName' },
                        { name: 'Date Added', sortField: 'DateCreated,SortName' },
                        { name: 'Release Date', sortField: 'ProductionYear,PremiereDate,SortName' },
                        { name: 'Parental Rating', sortField: 'OfficialRating,SortName' },
                        { name: 'Community Rating', sortField: 'CommunityRating,SortName' },
                        { name: 'Runtime', sortField: 'Runtime,SortName' }
                    ];
                }
            default:
                {
                    return [
                        {},
                        { name: 'Name', sortField: 'SortName' },
                        { name: 'Date Added', sortField: 'DateCreated,SortName' },
                        { name: 'Release Date', sortField: 'ProductionYear,PremiereDate,SortName' },
                        { name: 'Parental Rating', sortField: 'OfficialRating,SortName' },
                        { name: 'Community Rating', sortField: 'CommunityRating,SortName' },
                        { name: 'Runtime', sortField: 'Runtime,SortName' },
                        { name: 'Video' },
                        { name: 'Resolution' },
                        { name: 'Audio' },
                        { name: 'Subtitles' },
                        { name: 'Trailers' },
                        { name: 'Specials' }
                    ];
                }
        }
    }

    function getDefaultSortOrder(reportType) {

        switch (reportType) {

            case 'Season':
                {
                    return "SeriesSortName,SortName";
                }
            case 'Series':
                {
                    return "SortName";
                }
            case 'Game':
                {
                    return "GameSystem,SortName";
                }
            case 'Audio':
                {
                    return "AlbumArtist,Album,SortName";
                }
            case 'Episode':
                {
                    return "SeriesSortName,SortName";
                }
            case 'BoxSet':
                {
                    return "SortName";
                }
            case 'Book':
                {
                    return "SortName";
                }
            case 'MusicArtist':
                {
                    return "SortName";
                }
            case 'MusicAlbum':
                {
                    return "AlbumArtist,SortName";
                }
            default:
                {
                    return "SortName";
                }
        }
    }

    function getItemCellsHtml(item, headercells) {

        return headercells.map(function (cell) {

            var html = '';
            html += '<td>';

            var stream;

            switch (cell.type || cell.name) {

                case 'Album Artist':
                    {
                        html += item.AlbumArtist || '&nbsp;';
                        break;
                    }
                case 'Album':
                    {
                        html += item.Album || '&nbsp;';
                        break;
                    }
                case 'Series':
                    {
                        html += item.SeriesName || '&nbsp;';
                        break;
                    }
                case 'Game System':
                    {
                        html += item.GameSystem || '&nbsp;';
                        break;
                    }
                case 'Network':
                    {
                        html += item.Studios.length ? item.Studios[0].Name : '&nbsp;';
                        break;
                    }
                case 'Disc':
                    {
                        html += item.ParentIndexNumber == null ? '' : item.ParentIndexNumber;
                        break;
                    }
                case 'Track':
                    {
                        html += item.IndexNumber == null ? '' : item.IndexNumber;
                        break;
                    }
                case 'Players':
                    {
                        html += item.Players || '&nbsp;';
                        break;
                    }
                case 'Audio':
                    {
                        stream = (item.MediaStreams || []).filter(function (s) {

                            return s.Type == 'Audio';

                        })[0];

                        if (stream) {

                            var name = (stream.Codec || '').toUpperCase();
                            html += name == 'DCA' ? (stream.Profile || '').toUpperCase() : name;
                        }
                        break;
                    }
                case 'Video':
                    {
                        stream = (item.MediaStreams || []).filter(function (s) {

                            return s.Type == 'Video';

                        })[0];

                        if (stream) {
                            html += (stream.Codec || '').toUpperCase();
                        }
                        break;
                    }
                case 'Resolution':
                    {
                        stream = (item.MediaStreams || []).filter(function (s) {

                            return s.Type == 'Video';

                        })[0];

                        if (stream && stream.Width) {
                            html += stream.Width + "*" + (stream.Height || "-");
                        }
                        break;
                    }
                case 'Embedded Image':
                    {
                        if ((item.MediaStreams || []).filter(function (s) {

                            return s.Type == 'Video';

                        }).length) {
                            html += '<div class="libraryReportIndicator clearLibraryReportIndicator"><div class="ui-icon-check ui-btn-icon-notext"></div></div>';
                        }
                        break;
                    }
                case 'Subtitles':
                    {
                        var hasSubtitles = (item.MediaStreams || []).filter(function (s) {

                            return s.Type == 'Subtitle';

                        }).length;

                        if (hasSubtitles) {
                            html += '<div class="libraryReportIndicator clearLibraryReportIndicator"><div class="ui-icon-check ui-btn-icon-notext"></div></div>';
                        }
                        break;
                    }
                case 'Runtime':
                    {
                        if (item.RunTimeTicks) {
                            html += Dashboard.getDisplayTime(item.RunTimeTicks);
                        } else {
                            html += '&nbsp;';
                        }
                        break;
                    }
                case 'Trailers':
                    {
                        if (item.LocalTrailerCount) {

                            html += '<div class="libraryReportIndicator clearLibraryReportIndicator"><div class="ui-icon-check ui-btn-icon-notext"></div></div>';
                        }
                        break;
                    }
                case 'Specials':
                    {
                        if (item.SpecialFeatureCount) {

                            html += '<div class="libraryReportIndicator clearLibraryReportIndicator"><div class="ui-icon-check ui-btn-icon-notext"></div></div>';
                        }
                        break;
                    }

                case 'Season':
                    {
                        if (item.Type == "Episode") {
                            html += item.ParentIndexNumber == null ? '' : ('Season ' + item.ParentIndexNumber);
                        } else {
                            html += '<a href="edititemmetadata.html?id=' + item.Id + '">' + LibraryBrowser.getPosterViewDisplayName(item, false, false) + '</a>';
                        }
                        break;
                    }

                case 'Name':
                    {
                        html += '<a href="edititemmetadata.html?id=' + item.Id + '">' + LibraryBrowser.getPosterViewDisplayName(item, false, false) + '</a>';
                        break;
                    }
                case 'Community Rating':
                    {
                        html += item.CommunityRating || '&nbsp;';
                        break;
                    }
                case 'Parental Rating':
                    {
                        html += item.OfficialRating || '&nbsp;';
                        break;
                    }

                case 'Year':
                case 'Release Date':
                    {
                        if (item.PremiereDate && item.Type != "Series") {
                            try {
                                var date = parseISO8601Date(item.PremiereDate, { toLocal: true });

                                html += date.toLocaleDateString();
                            }
                            catch (e) {
                                html += '&nbsp;';
                            }
                        }
                        else if (item.ProductionYear) {
                            html += item.ProductionYear;

                            if (item.Status == "Continuing") {
                                html += "-Present";
                            }
                            else if (item.EndDate) {

                                try {

                                    var endYear = parseISO8601Date(item.EndDate, { toLocal: true }).getFullYear();

                                    if (endYear != item.ProductionYear) {
                                        html += "-" + parseISO8601Date(item.EndDate, { toLocal: true }).getFullYear();
                                    }

                                }
                                catch (e) {
                                    console.log("Error parsing date: " + item.EndDate);
                                }
                            }

                        } else {
                            html += '&nbsp;';
                        }
                        break;
                    }
                case 'Date Added':
                    {
                        if (item.DateCreated) {
                            try {
                                html += parseISO8601Date(item.DateCreated, { toLocal: true }).toLocaleDateString();
                            }
                            catch (e) {
                                html += '&nbsp;';
                            }
                        }
                        break;
                    }
                default:
                    {
                        if (item.LockData) {
                            html += '<img src="css/images/editor/lock.png" />';
                        }
                        if (item.IsUnidentified) {
                            html += '<div class="libraryReportIndicator"><div class="ui-icon-alert ui-btn-icon-notext"></div></div>';
                        }

                        if (!item.LocalTrailerCount && item.Type == "Movie") {
                            html += '<img src="css/images/editor/missingtrailer.png" title="Missing local trailer." />';
                        }

                        if (!item.ImageTags || !item.ImageTags.Primary) {
                            html += '<a href="edititemimages.html?id=' + item.Id + '"><img src="css/images/editor/missingprimaryimage.png" title="Missing primary image." /></a>';
                        }

                        if (!item.BackdropImageTags || !item.BackdropImageTags.length) {
                            if (item.Type !== "Episode" && item.Type !== "Season" && item.MediaType !== "Audio" && item.Type !== "TvChannel" && item.Type !== "MusicAlbum") {
                                html += '<a href="edititemimages.html?id=' + item.Id + '"><img src="css/images/editor/missingbackdrop.png" title="Missing backdrop image." /></a>';
                            }
                        }

                        if (!item.ImageTags || !item.ImageTags.Logo) {
                            if (item.Type == "Movie" || item.Type == "Trailer" || item.Type == "Series" || item.Type == "MusicArtist" || item.Type == "BoxSet") {
                                html += '<a href="edititemimages.html?id=' + item.Id + '"><img src="css/images/editor/missinglogo.png" title="Missing logo image." /></a>';
                            }
                        }

                        break;
                    }
            }

            html += '</td>';
            return html;

        }).join('');
    }

    function getReportHtml(items, reportType, currentSortField, currentSortDirection) {

        var html = '';

        html += '<table id="tblReport" data-role="table" data-mode="reflow" class="tblLibraryReport stripedTable ui-responsive table-stroke detailTable" style="display:table;">';

        html += '<thead>';
        html += '<tr>';

        var cells = getHeaderCells(reportType);

        html += cells.map(function (c) {

            var cellHtml = '<th data-priority="' + (c.priority || 'persist') + '">';

            if (c.sortField) {
                cellHtml += '<a class="lnkColumnSort" href="#" data-sortfield="' + c.sortField + '" style="text-decoration:underline;">';
            }

            cellHtml += (c.name || '&nbsp;');

            if (c.sortField) {

                cellHtml += '</a>';

                if (c.sortField == currentSortField) {

                    if (currentSortDirection == "Descending") {
                        cellHtml += '<span style="font-weight:bold;margin-left:5px;vertical-align:top;font-size:12px;">&darr;</span>';
                    } else {
                        cellHtml += '<span style="font-weight:bold;margin-left:5px;vertical-align:top;font-size:12px;">&uarr;</span>';
                    }
                }
            }

            cellHtml += '</th>';

            return cellHtml;

        }).join('');

        html += '</tr>';
        html += '</thead>';

        html += '<tbody>';

        for (var i = 0, length = items.length; i < length; i++) {

            var item = items[i];

            html += '<tr>';
            html += getItemCellsHtml(item, cells);
            html += '</tr>';
        }

        html += '</tbody>';

        html += '</table>';

        return html;
    }

    function renderItems(page, result, reportType) {

        // Scroll back up so they can see the results from the beginning
        $(document).scrollTop(0);

        $('.listTopPaging', page).html(LibraryBrowser.getPagingHtml(query, result.TotalRecordCount, false, [], false)).trigger('create');

        updateFilterControls(page);

        $('.listBottomPaging', page).html(LibraryBrowser.getPagingHtml(query, result.TotalRecordCount, false, [], false)).trigger('create');

        $('.reportContainer', page).html(getReportHtml(result.Items, reportType, query.SortBy, query.SortOrder)).trigger('create');

        $('.btnNextPage', page).on('click', function () {
            query.StartIndex += query.Limit;
            reloadItems(page);
        });

        $('.btnPreviousPage', page).on('click', function () {
            query.StartIndex -= query.Limit;
            reloadItems(page);
        });

        $('.lnkColumnSort', page).on('click', function () {

            var order = this.getAttribute('data-sortfield');

            if (query.SortBy == order) {

                if (query.SortOrder == "Descending") {

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

    function reloadItems(page) {

        var url = ApiClient.getUrl("Items", query);
        var reportType = $('#selectView', page).val();

        $.getJSON(url).done(function (result) {

            renderItems(page, result, reportType);

        });
    }

    function updateFilterControls(page) {

        $('#selectView').val(query.IncludeItemTypes).selectmenu('refresh');

        $('.chkVideoTypeFilter', page).each(function () {

            var filters = "," + (query.VideoTypes || "");
            var filterName = this.getAttribute('data-filter');

            this.checked = filters.indexOf(',' + filterName) != -1;

        }).checkboxradio('refresh');

        $('#chk3D', page).checked(query.Is3D == true).checkboxradio('refresh');
        $('#chkHD', page).checked(query.IsHD == true).checkboxradio('refresh');
        $('#chkSD', page).checked(query.IsHD == false).checkboxradio('refresh');

        $('#chkSubtitle', page).checked(query.HasSubtitles == true).checkboxradio('refresh');
        $('#chkTrailer', page).checked(query.HasTrailer == true).checkboxradio('refresh');
        $('#chkSpecialFeature', page).checked(query.HasSpecialFeature == true).checkboxradio('refresh');
        $('#chkThemeSong', page).checked(query.HasThemeSong == true).checkboxradio('refresh');
        $('#chkThemeVideo', page).checked(query.HasThemeVideo == true).checkboxradio('refresh');
        $('#chkIsPlaceHolder', page).checked(query.IsPlaceHolder == true).checkboxradio('refresh');

        $('#chkMissingRating', page).checked(query.HasOfficialRating == false).checkboxradio('refresh');
        $('#chkMissingOverview', page).checked(query.HasOverview == false).checkboxradio('refresh');
        $('#chkYearMismatch', page).checked(query.IsYearMismatched == true).checkboxradio('refresh');

        $('#chkIsUnidentified', page).checked(query.IsUnidentified == true).checkboxradio('refresh');
        $('#chkIsLocked', page).checked(query.IsLocked == true).checkboxradio('refresh');

        $('#chkSpecialEpisode', page).checked(query.ParentIndexNumber == 0).checkboxradio('refresh');
        $('#chkMissingEpisode', page).checked(query.IsMissing == true).checkboxradio('refresh');
        $('#chkFutureEpisode', page).checked(query.IsUnaired == true).checkboxradio('refresh');
    }

    $(document).on('pageinit', "#libraryReportPage", function () {

        var page = this;

        $('.libraryTree', page).on('itemclicked', function (event, data) {

            if (data.itemType == "libraryreport") {
                return;
            }

            if (data.itemType == "livetvservice") {
                return;
            }

            Dashboard.navigate('edititemmetadata.html?id=' + data.id);
        });

        $('#radioBasicFilters', page).on('change', function () {

            if (this.checked) {
                $('.basicFilters', page).show();
                $('.advancedFilters', page).hide();
            } else {
                $('.basicFilters', page).hide();
            }
        });

        $('#radioAdvancedFilters', page).on('change', function () {

            if (this.checked) {
                $('.advancedFilters', page).show();
                $('.basicFilters', page).hide();
            } else {
                $('.advancedFilters', page).hide();
            }
        });

        $('#selectView', page).on('change', function () {

            query.StartIndex = 0;
            query.IncludeItemTypes = this.value;

            query.SortBy = getDefaultSortOrder(this.value);
            query.SortOrder = "Ascending";

            reloadItems(page);
        });

        $('.chkVideoTypeFilter', page).on('change', function () {

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

        $('#chk3D', page).on('change', function () {

            query.StartIndex = 0;
            query.Is3D = this.checked ? true : null;

            reloadItems(page);
        });

        $('#chkHD', page).on('change', function () {

            query.StartIndex = 0;
            query.IsHD = this.checked ? true : null;

            reloadItems(page);
        });

        $('#chkSD', page).on('change', function () {

            query.StartIndex = 0;
            query.IsHD = this.checked ? false : null;

            reloadItems(page);
        });

        $('#chkSubtitle', page).on('change', function () {

            query.StartIndex = 0;
            query.HasSubtitles = this.checked ? true : null;

            reloadItems(page);
        });

        $('#chkTrailer', page).on('change', function () {

            query.StartIndex = 0;
            query.HasTrailer = this.checked ? true : null;

            reloadItems(page);
        });

        $('#chkSpecialFeature', page).on('change', function () {

            query.StartIndex = 0;
            query.HasSpecialFeature = this.checked ? true : null;

            reloadItems(page);
        });

        $('#chkIsPlaceHolder', page).on('change', function () {

            query.StartIndex = 0;
            query.IsPlaceHolder = this.checked ? true : null;

            reloadItems(page);
        });

        $('#chkThemeSong', page).on('change', function () {

            query.StartIndex = 0;
            query.HasThemeSong = this.checked ? true : null;

            reloadItems(page);
        });

        $('#chkThemeVideo', page).on('change', function () {

            query.StartIndex = 0;
            query.HasThemeVideo = this.checked ? true : null;

            reloadItems(page);
        });

        $('#chkMissingOverview', page).on('change', function () {

            query.StartIndex = 0;
            query.HasOverview = this.checked ? false : null;

            reloadItems(page);
        });

        $('#chkMissingRating', page).on('change', function () {

            query.StartIndex = 0;
            query.HasOfficialRating = this.checked ? false : null;

            reloadItems(page);
        });

        $('#chkYearMismatch', page).on('change', function () {

            query.StartIndex = 0;
            query.IsYearMismatched = this.checked ? true : null;

            reloadItems(page);
        });

        $('#chkIsUnidentified', page).on('change', function () {

            query.StartIndex = 0;
            query.IsUnidentified = this.checked ? true : null;

            reloadItems(page);
        });

        $('#chkIsLocked', page).on('change', function () {

            query.StartIndex = 0;
            query.IsLocked = this.checked ? true : null;

            reloadItems(page);
        });

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

    }).on('pagebeforeshow', "#libraryReportPage", function () {

        var page = this;

        query.SortBy = getDefaultSortOrder($('#selectView', page).val());
        query.SortOrder = "Ascending";

        reloadItems(page);

    }).on('pageshow', "#libraryReportPage", function () {

        updateFilterControls(this);
    });

})(jQuery, document, window);

