(function ($, document) {

    // The base query options
    var query = {

        SortBy: "SortName",
        SortOrder: "Ascending",
        IncludeItemTypes: "GameSystem",
        Recursive: true,
        Fields: "DateCreated",
        StartIndex: 0
    };

    function getSavedQueryKey() {

        return 'gamesystems' + (query.ParentId || '');
    }

    function reloadItems(page) {

        Dashboard.showLoadingMsg();

        ApiClient.getItems(Dashboard.getCurrentUserId(), query).done(function (result) {

            // Scroll back up so they can see the results from the beginning
            $(document).scrollTop(0);

            var html = '';

            updateFilterControls(page);

            html = LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                shape: "backdrop",
                context: 'games',
                showTitle: true,
                centerText: true,
                lazy: true

            });

            var pagingHtml = LibraryBrowser.getQueryPagingHtml({
                startIndex: query.StartIndex,
                limit: query.Limit,
                totalRecordCount: result.TotalRecordCount
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

            $('.selectPageSize', page).on('change', function () {
                query.Limit = parseInt(this.value);
                query.StartIndex = 0;
                reloadItems(page);
            });

            LibraryBrowser.saveQueryValues(getSavedQueryKey(), query);

            Dashboard.hideLoadingMsg();
        });
    }

    function updateFilterControls(page) {

        // Reset form values using the last used query
    }

    $(document).on('pagebeforeshow', "#gamesystemsPage", function () {

        query.ParentId = LibraryMenu.getTopParentId();

        var limit = LibraryBrowser.getDefaultPageSize();

        // If the default page size has changed, the start index will have to be reset
        if (limit != query.Limit) {
            query.Limit = limit;
            query.StartIndex = 0;
        }

        LibraryBrowser.loadSavedQueryValues(getSavedQueryKey(), query);

        reloadItems(this);

    }).on('pageshow', "#gamesystemsPage", function () {

        updateFilterControls(this);
    });

})(jQuery, document);
