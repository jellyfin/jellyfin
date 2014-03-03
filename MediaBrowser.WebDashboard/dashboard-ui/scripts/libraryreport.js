(function ($, document, window) {

    // The base query options
    var query = {

        SortBy: "SeriesSortName,SortName",
        SortOrder: "Ascending",
        Recursive: true,
        Fields: "MediaStreams,DateCreated",
        StartIndex: 0,
        IncludeItemTypes: "Movie"
    };
    
    function getCodecName(stream) {

        var val = stream.Codec || '';
        val = val.toUpperCase();
        
        if (val == 'DCA') {
            return stream.Profile;
        }

        return val;
    }

    function getTableRowsHtml(items, includeParentInfo, includeSubtitles) {

        var html = '';

        for (var i = 0, length = items.length; i < length; i++) {

            var item = items[i];

            html += '<tr>';

            html += '<td>';
            
            if (item.IsUnidentified) {
                html += '<div class="libraryReportIndicator"><div class="ui-icon-alert ui-btn-icon-notext"></div></div>';
            }
            html += '</td>';

            if (includeParentInfo) {
                html += '<td>';
                if (item.SeriesName) {
                    html += '<a href="itemdetails.html?id=' + item.SeriesId + '">' + item.SeriesName + '</a>';
                }
                else if (item.Album) {
                    html += item.Album + '<br/>';
                }
                else if (item.AlbumArtist) {
                    html += item.AlbumArtist + '<br/>';
                }
                else {
                    html += '&nbsp;';
                }
                html += '</td>';
            }

            html += '<td>';
            html += '<a href="' + LibraryBrowser.getHref(item) + '">' + LibraryBrowser.getPosterViewDisplayName(item, false, true) + '</a>';
            html += '</td>';

            html += '<td>';
            if (item.DateCreated) {
                try {
                    html += parseISO8601Date(item.DateCreated, { toLocal: true }).toLocaleDateString();
                }
                catch (e) {
                    html += '&nbsp;';
                }
            }
            html += '</td>';

            html += '<td>';
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
            html += '</td>';

            html += '<td>';
            html += item.OfficialRating || '&nbsp;';
            html += '</td>';

            html += '<td>';

            if (item.RunTimeTicks) {
                html += Dashboard.getDisplayTime(item.RunTimeTicks);
            } else {
                html += '&nbsp;';
            }
            html += '</td>';

            html += '<td>';
            html += (item.MediaStreams || []).filter(function(s) {

                return s.Type != 'Subtitle';

            }).map(getCodecName).filter(function (s) {
                return s;
            }).join('<br/>');
            
            html += '</td>';

            if (includeSubtitles) {
                html += '<td>';
                html += (item.MediaStreams || []).filter(function (s) {

                    return s.Type == 'Subtitle';

                }).map(function (s) {

                    return (s.Language || 'Und') + ' - ' + s.Codec;

                }).join('<br/>');

                html += '</td>';
            }

            html += '<td>';
            if (item.SpecialFeatureCount == 1) {

                html += '1 Special<br/>';
            }
            else if (item.SpecialFeatureCount) {

                html += item.SpecialFeatureCount + ' Specials<br/>';
            }
            if (item.LocalTrailerCount == 1) {

                html += '1 Trailer<br/>';
            }
            else if (item.LocalTrailerCount) {

                html += item.LocalTrailerCount + ' Trailers<br/>';
            }
            html += '</td>';

            html += '</tr>';
        }

        return html;
    }

    function renderItems(page, result) {

        // Scroll back up so they can see the results from the beginning
        $(document).scrollTop(0);

        $('.listTopPaging', page).html(LibraryBrowser.getPagingHtml(query, result.TotalRecordCount, true)).trigger('create');

        updateFilterControls(page);

        $('.listBottomPaging', page).html(LibraryBrowser.getPagingHtml(query, result.TotalRecordCount)).trigger('create');

        var includeParentInfo = query.IncludeItemTypes == "Audio" || query.IncludeItemTypes == "MusicAlbum" || query.IncludeItemTypes == "Episode" || query.IncludeItemTypes == "Book";
        var includeSubtitles = query.IncludeItemTypes == "Movie" || query.IncludeItemTypes == "Trailer" || query.IncludeItemTypes == "Episode" || query.IncludeItemTypes == "AdultVideo" || query.IncludeItemTypes == "MusicVideo" || query.IncludeItemTypes == "Video";

        if (includeParentInfo) {

            var parentLabel = "Series";
            
            if (query.IncludeItemTypes == "Audio") {
                parentLabel = "Album";
            }
            else if (query.IncludeItemTypes == "MusicAlbum") {
                parentLabel = "Artist";
            }

            $('.thParent', page).html(parentLabel).show();

        } else {
            $('.thParent', page).hide();
        }

        if (includeSubtitles) {

            $('.thSubtitles', page).show();

        } else {
            $('.thSubtitles', page).hide();
        }

        var rowsHtml = getTableRowsHtml(result.Items, includeParentInfo, includeSubtitles);
        $('.resultBody', page).html(rowsHtml).parents('.tblLibraryReport').table("refresh").trigger('create');

        $('.btnNextPage', page).on('click', function () {
            query.StartIndex += query.Limit;
            reloadItems(page);
        });

        $('.btnPreviousPage', page).on('click', function () {
            query.StartIndex -= query.Limit;
            reloadItems(page);
        });

        $('.selectPageSize', page).on('change', function () {
            query.Limit = parseInt(this.value);
            query.StartIndex = 0;
            reloadItems(page);
        });
    }

    function reloadItems(page) {

        var url = ApiClient.getUrl("Items", query);

        $.getJSON(url).done(function (result) {

            renderItems(page, result);

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

        $('#chkMissingOverview', page).checked(query.HasOverview == false).checkboxradio('refresh');
        $('#chkYearMismatch', page).checked(query.IsYearMismatched == true).checkboxradio('refresh');
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

        $('#chkYearMismatch', page).on('change', function () {

            query.StartIndex = 0;
            query.IsYearMismatched = this.checked ? true : null;

            reloadItems(page);
        });

    }).on('pagebeforeshow', "#libraryReportPage", function () {

        var page = this;
        var limit = LibraryBrowser.getDefaultPageSize();

        // If the default page size has changed, the start index will have to be reset
        if (limit != query.Limit) {
            query.Limit = limit;
            query.StartIndex = 0;
        }

        reloadItems(page);

    }).on('pageshow', "#libraryReportPage", function () {

        updateFilterControls(this);
    });

})(jQuery, document, window);

