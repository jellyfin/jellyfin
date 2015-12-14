
(function ($, document) {

    var view = LibraryBrowser.getDefaultItemsView('Poster', 'List');

    // The base query options
    var query = {

        SortBy: "SortName",
        SortOrder: "Ascending",
        MediaTypes: "Game",
        Recursive: true,
        Fields: "Genres,Studios,PrimaryImageAspectRatio,SortName",
        StartIndex: 0,
        ImageTypeLimit: 1,
        EnableImageTypes: "Primary,Backdrop,Banner,Thumb"
    };

    function getSavedQueryKey() {

        return LibraryBrowser.getSavedQueryKey();
    }

    function reloadItems(page) {

        Dashboard.showLoadingMsg();

        ApiClient.getItems(Dashboard.getCurrentUserId(), query).then(function (result) {

            // Scroll back up so they can see the results from the beginning
            window.scrollTo(0, 0);

            var html = '';

            $('.listTopPaging', page).html(LibraryBrowser.getQueryPagingHtml({
                startIndex: query.StartIndex,
                limit: query.Limit,
                totalRecordCount: result.TotalRecordCount,
                viewButton: true,
                showLimit: false
            }));

            updateFilterControls(page);
            var trigger = false;

            if (view == "List") {

                html = LibraryBrowser.getListViewHtml({
                    items: result.Items,
                    context: 'games',
                    sortBy: query.SortBy
                });
            }
            else if (view == "Poster") {
                html = LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    shape: "auto",
                    context: 'games',
                    showTitle: true,
                    showParentTitle: true,
                    centerText: true
                });
            }
            else if (view == "PosterCard") {
                html = LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    shape: "auto",
                    context: 'games',
                    showTitle: true,
                    showParentTitle: true,
                    cardLayout: true
                });
            }

            var elem = page.querySelector('#items');
            elem.innerHTML = html;
            ImageLoader.lazyChildren(elem);

            $('.btnNextPage', page).on('click', function () {
                query.StartIndex += query.Limit;
                reloadItems(page);
            });

            $('.btnPreviousPage', page).on('click', function () {
                query.StartIndex -= query.Limit;
                reloadItems(page);
            });

            LibraryBrowser.saveQueryValues(getSavedQueryKey(), query);

            Dashboard.hideLoadingMsg();
        });
    }

    function updateFilterControls(page) {

        // Reset form values using the last used query
        $('.radioSortBy', page).each(function () {

            this.checked = (query.SortBy || '').toLowerCase() == this.getAttribute('data-sortby').toLowerCase();

        }).checkboxradio('refresh');

        $('.radioSortOrder', page).each(function () {

            this.checked = (query.SortOrder || '').toLowerCase() == this.getAttribute('data-sortorder').toLowerCase();

        }).checkboxradio('refresh');

        $('.radioPlayers', page).each(function () {

            var val = this.getAttribute('data-value');

            if (val == "all") {

                this.checked = query.MinPlayers == null;
            } else {
                this.checked = query.MinPlayers == val;
            }

        }).checkboxradio('refresh');

        $('.chkStandardFilter', page).each(function () {

            var filters = "," + (query.Filters || "");
            var filterName = this.getAttribute('data-filter');

            this.checked = filters.indexOf(',' + filterName) != -1;

        }).checkboxradio('refresh');

        $('#selectView', page).val(view);

        $('.alphabetPicker', page).alphaValue(query.NameStartsWith);
        $('#selectPageSize', page).val(query.Limit);
    }

    var filtersLoaded;
    function reloadFiltersIfNeeded(page) {

        if (!filtersLoaded) {

            filtersLoaded = true;

            QueryFilters.loadFilters(page, Dashboard.getCurrentUserId(), query, function () {

                reloadItems(page);
            });
        }
    }

    $(document).on('pageinit', "#gamesPage", function () {

        var page = this;

        $('.viewPanel', page).on('panelopen', function () {

            reloadFiltersIfNeeded(page);
        });

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

        $('.radioPlayers', this).on('click', function () {

            query.StartIndex = 0;

            var val = this.getAttribute('data-value');

            query.MinPlayers = val == "all" ? null : val;

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

        $('.alphabetPicker', this).on('alphaselect', function (e, character) {

            query.NameStartsWithOrGreater = character;
            query.StartIndex = 0;

            reloadItems(page);

        }).on('alphaclear', function (e) {

            query.NameStartsWithOrGreater = '';

            reloadItems(page);
        });

        $('#selectView', this).on('change', function () {

            view = this.value;

            if (view == "Timeline") {

                query.SortBy = "PremiereDate";
                query.SortOrder = "Descending";
                query.StartIndex = 0;
                $('#radioPremiereDate', page)[0].click();

            } else {
                reloadItems(page);
            }

            LibraryBrowser.saveViewSetting(getSavedQueryKey(), view);
        });

        $('#selectPageSize', page).on('change', function () {
            query.Limit = parseInt(this.value);
            query.StartIndex = 0;
            reloadItems(page);
        });

    }).on('pagebeforeshow', "#gamesPage", function () {

        var page = this;
        query.ParentId = LibraryMenu.getTopParentId();

        var limit = LibraryBrowser.getDefaultPageSize();

        // If the default page size has changed, the start index will have to be reset
        if (limit != query.Limit) {
            query.Limit = limit;
            query.StartIndex = 0;
        }

        var viewkey = getSavedQueryKey();

        LibraryBrowser.loadSavedQueryValues(viewkey, query);
        QueryFilters.onPageShow(page, query);

        LibraryBrowser.getSavedViewSetting(viewkey).then(function (val) {

            if (val) {
                $('#selectView', page).val(val).trigger('change');
            } else {
                reloadItems(page);
            }
        });

        updateFilterControls(this);
    });

})(jQuery, document);