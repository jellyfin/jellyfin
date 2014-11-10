(function ($, document) {

    var defaultSortBy = "Album,SortName";

    // The base query options
    var query = {

        SortBy: defaultSortBy,
        SortOrder: "Ascending",
        IncludeItemTypes: "Audio",
        Recursive: true,
        Fields: "AudioInfo,ParentId",
        Limit: 200,
        StartIndex: 0
    };

    function getSavedQueryKey() {

        return 'songs' + (query.ParentId || '');
    }

    function updateFilterControls(page) {

        // Reset form values using the last used query
        $('.radioSortBy', page).each(function () {

            this.checked = (query.SortBy || '').toLowerCase() == this.getAttribute('data-sortby').toLowerCase();

        }).checkboxradio('refresh');

        $('.radioSortOrder', this).each(function () {

            this.checked = (query.SortOrder || '').toLowerCase() == this.getAttribute('data-sortorder').toLowerCase();

        }).checkboxradio('refresh');
        $('#selectPageSize', page).val(query.Limit).selectmenu('refresh');
    }

    function reloadItems(page) {

        Dashboard.showLoadingMsg();

        ApiClient.getItems(Dashboard.getCurrentUserId(), query).done(function (result) {

            // Scroll back up so they can see the results from the beginning
            $(document).scrollTop(0);

            var html = '';

            var pagingHtml = LibraryBrowser.getQueryPagingHtml({
                startIndex: query.StartIndex,
                limit: query.Limit,
                totalRecordCount: result.TotalRecordCount,
                viewButton: true,
                showLimit: false
            });
            $('.listTopPaging', page).html(pagingHtml).trigger('create');

            updateFilterControls(page);

            html += LibraryBrowser.getListViewHtml({
                items: result.Items,
                smallIcon: true,
                showIndex: true,
                defaultAction: 'play'
            });

            html += pagingHtml;

            $('#items', page).html(html).trigger('create').createCardMenus();

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

            LibraryBrowser.saveQueryValues(getSavedQueryKey(), query);

            Dashboard.hideLoadingMsg();
        });
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

    $(document).on('pageinit', "#songsPage", function () {

        var page = this;

        $('.viewPanel', page).on('panelopen', function () {

            reloadFiltersIfNeeded(page);
        });

        $('.radioSortBy', this).on('click', function () {
            query.SortBy = this.getAttribute('data-sortby');
            query.StartIndex = 0;
            reloadItems(page);
        });

        $('.radioSortOrder', this).on('click', function () {
            query.SortOrder = this.getAttribute('data-sortorder');
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

        $('#selectPageSize', page).on('change', function () {
            query.Limit = parseInt(this.value);
            query.StartIndex = 0;
            reloadItems(page);
        });

    }).on('pagebeforeshow', "#songsPage", function () {

        var page = this;

        query.ParentId = LibraryMenu.getTopParentId();

        LibraryBrowser.loadSavedQueryValues(getSavedQueryKey(), query);
        QueryFilters.onPageShow(page, query);

        reloadItems(page);

    }).on('pageshow', "#songsPage", function () {

        updateFilterControls(this);

    });

})(jQuery, document);