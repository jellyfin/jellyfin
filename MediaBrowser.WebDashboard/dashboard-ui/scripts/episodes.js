(function ($, document) {

    var view = "Poster";

    // The base query options
    var query = {

        SortBy: "SeriesSortName,SortName",
        SortOrder: "Ascending",
        IncludeItemTypes: "Episode",
        Recursive: true,
        Fields: "DateCreated,SeriesInfo",
        StartIndex: 0
    };

    LibraryBrowser.loadSavedQueryValues('episodes', query);

    function reloadItems(page) {

        Dashboard.showLoadingMsg();

        ApiClient.getItems(Dashboard.getCurrentUserId(), query).done(function (result) {

            // Scroll back up so they can see the results from the beginning
            $(document).scrollTop(0);

            var html = '';

            $('.listTopPaging', page).html(LibraryBrowser.getPagingHtml(query, result.TotalRecordCount, true)).trigger('create');

            if (view == "Poster") {
                html += LibraryBrowser.getPosterDetailViewHtml({
                    items: result.Items,
                    context: "tv",
                    shape: "backdrop"
                });
                $('.itemsContainer', page).removeClass('timelineItemsContainer');
            }

            html += LibraryBrowser.getPagingHtml(query, result.TotalRecordCount);

            $('#items', page).html(html).trigger('create');

            $('.selectPage', page).on('change', function () {
                query.StartIndex = (parseInt(this.value) - 1) * query.Limit;
                reloadItems(page);
            });

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

            LibraryBrowser.saveQueryValues('episodes', query);

            Dashboard.hideLoadingMsg();
        });
    }

    function formatDigit(i) {
        return i < 10 ? "0" + i : i;
    }

    function getDateFormat(date) {

        // yyyyMMddHHmmss
        var d = date;

        return "" + d.getFullYear() + formatDigit(d.getMonth() + 1) + formatDigit(d.getDate()) + formatDigit(d.getHours()) + formatDigit(d.getMinutes()) + formatDigit(d.getSeconds());
    }

    $(document).on('pageinit', "#episodesPage", function () {

        var page = this;

        $('.radioSortBy', this).on('click', function () {
            query.StartIndex = 0;
            query.SortBy = this.getAttribute('data-sortby');
            reloadItems(page);
        });

        $('.radioSortOrder', this).on('click', function () {
            query.StartIndex = 0;
            query.SortOrder = this.getAttribute('data-sortorder');
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

        $('#chkSpecialFeature', this).on('change', function () {

            query.ParentIndexNumber = this.checked ? 0 : null;

            reloadItems(page);
        });

        $('#chkMissingEpisode', this).on('change', function () {

            var futureChecked = $('#chkFutureEpisode', page).checked();
            
            query.LocationTypes = this.checked || futureChecked ? "virtual" : null;
            query.HasPremiereDate = this.checked || futureChecked ? true : null;
            query.MaxPremiereDate = this.checked ? getDateFormat(new Date()) : null;

            reloadItems(page);
        });

        $('#chkFutureEpisode', this).on('change', function () {

            var missingChecked = $('#chkMissingEpisode', page).checked();

            query.LocationTypes = this.checked || missingChecked ? "virtual" : null;
            query.HasPremiereDate = this.checked || missingChecked ? true : null;
            query.MinPremiereDate = this.checked ? getDateFormat(new Date()) : null;

            reloadItems(page);
        });

        $('.alphabetPicker', this).on('alphaselect', function (e, character) {

            query.NameStartsWithOrGreater = character;
            query.StartIndex = 0;

            reloadItems(page);

        }).on('alphaclear', function (e) {

            query.NameStartsWithOrGreater = '';

            reloadItems(page);
        });

    }).on('pagebeforeshow', "#episodesPage", function () {

        var limit = LibraryBrowser.getDefaultPageSize();

        // If the default page size has changed, the start index will have to be reset
        if (limit != query.Limit) {
            query.Limit = limit;
            query.StartIndex = 0;
        }

        reloadItems(this);

    }).on('pageshow', "#episodesPage", function () {

        // Reset form values using the last used query
        $('.radioSortBy', this).each(function () {

            this.checked = query.SortBy == this.getAttribute('data-sortby');

        }).checkboxradio('refresh');

        $('.radioSortOrder', this).each(function () {

            this.checked = query.SortOrder == this.getAttribute('data-sortorder');

        }).checkboxradio('refresh');

        $('.chkStandardFilter', this).each(function () {

            var filters = "," + (query.Filters || "");
            var filterName = this.getAttribute('data-filter');

            this.checked = filters.indexOf(',' + filterName) != -1;

        }).checkboxradio('refresh');

        $('.chkVideoTypeFilter', this).each(function () {

            var filters = "," + (query.VideoTypes || "");
            var filterName = this.getAttribute('data-filter');

            this.checked = filters.indexOf(',' + filterName) != -1;

        }).checkboxradio('refresh');

        $('#chk3D', this).checked(query.Is3D == true).checkboxradio('refresh');

        $('#chkSubtitle', this).checked(query.HasSubtitles == true).checkboxradio('refresh');
        $('#chkTrailer', this).checked(query.HasTrailer == true).checkboxradio('refresh');
        $('#chkThemeSong', this).checked(query.HasThemeSong == true).checkboxradio('refresh');
        $('#chkThemeVideo', this).checked(query.HasThemeVideo == true).checkboxradio('refresh');
        $('#chkSpecialFeature', this).checked(query.ParentIndexNumber == 0).checkboxradio('refresh');
        $('#chkMissingEpisode', this).checked(query.LocationTypes == "virtual").checkboxradio('refresh');

        $('.alphabetPicker', this).alphaValue(query.NameStartsWithOrGreater);

    });

})(jQuery, document);