(function ($, document) {

    var view = LibraryBrowser.getDefaultItemsView('Poster', 'List');

    // The base query options
    var query = {

        SortBy: "SortName",
        SortOrder: "Ascending",
        IncludeItemTypes: "Playlist",
        Recursive: true,
        Fields: "PrimaryImageAspectRatio,SortName,CumulativeRunTimeTicks",
        StartIndex: 0
    };

    function getSavedQueryKey() {

        return 'playlists' + (query.ParentId || '');
    }

    function showLoadingMessage(page) {

        $('.popupLoading', page).popup('open');
    }

    function hideLoadingMessage(page) {
        $('.popupLoading', page).popup('close');
    }

    function reloadItems(page) {

        showLoadingMessage(page);

        var promise1 = ApiClient.getItems(Dashboard.getCurrentUserId(), query);
        var promise2 = Dashboard.getCurrentUser();

        $.when(promise1, promise2).done(function (response1, response2) {

            var result = response1[0];
            var user = response2[0];

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

            if (result.TotalRecordCount) {

                if (view == "List") {

                    html = LibraryBrowser.getListViewHtml({
                        items: result.Items,
                        context: 'playlists',
                        sortBy: query.SortBy
                    });
                }
                else if (view == "Poster") {
                    html = LibraryBrowser.getPosterViewHtml({
                        items: result.Items,
                        shape: "square",
                        context: 'playlists',
                        showTitle: true,
                        lazy: true,
                        coverImage: true,
                        showItemCounts: true
                    });
                }

                html += pagingHtml;
                $('.noItemsMessage', page).hide();

            } else {

                $('.noItemsMessage', page).show();
            }

            $('.itemsContainer', page).html(html).trigger('create').createCardMenus();

            $('.btnNextPage', page).on('click', function () {
                query.StartIndex += query.Limit;
                reloadItems(page);
            });

            $('.btnPreviousPage', page).on('click', function () {
                query.StartIndex -= query.Limit;
                reloadItems(page);
            });

            LibraryBrowser.saveQueryValues(getSavedQueryKey(), query);

            hideLoadingMessage(page);
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

        $('.chkStandardFilter', page).each(function () {

            var filters = "," + (query.Filters || "");
            var filterName = this.getAttribute('data-filter');

            this.checked = filters.indexOf(',' + filterName) != -1;

        }).checkboxradio('refresh');

        $('#selectView', page).val(view).selectmenu('refresh');

        $('#selectPageSize', page).val(query.Limit).selectmenu('refresh');
    }

    $(document).on('pageinit', "#playlistsPage", function () {

        var page = this;

        $('.radioSortBy', this).on('click', function () {
            query.SortBy = this.getAttribute('data-sortby');
            reloadItems(page);
        });

        $('.radioSortOrder', this).on('click', function () {
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

        $('#selectView', this).on('change', function () {

            view = this.value;

            reloadItems(page);

            LibraryBrowser.saveViewSetting(getSavedQueryKey(), view);
        });

        $('#selectPageSize', page).on('change', function () {
            query.Limit = parseInt(this.value);
            query.StartIndex = 0;
            reloadItems(page);
        });

    }).on('pagebeforeshow', "#playlistsPage", function () {

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

        LibraryBrowser.getSavedViewSetting(viewkey).done(function (val) {

            if (val) {
                $('#selectView', page).val(val).selectmenu('refresh').trigger('change');
            } else {
                reloadItems(page);
            }
        });

    }).on('pageshow', "#playlistsPage", function () {

        updateFilterControls(this);

    });

})(jQuery, document);