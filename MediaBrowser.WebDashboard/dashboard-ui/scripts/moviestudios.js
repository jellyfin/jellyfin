(function ($, document) {

    // The base query options
    var data = {};

    function getQuery() {

        var key = getSavedQueryKey();
        var pageData = data[key];

        if (!pageData) {
            pageData = data[key] = {
                query: {
                    SortBy: "SortName",
                    SortOrder: "Ascending",
                    IncludeItemTypes: "Movie",
                    Recursive: true,
                    Fields: "DateCreated,ItemCounts",
                    StartIndex: 0,
                    Limit: LibraryBrowser.getDefaultPageSize()
                }
            };

            pageData.query.ParentId = LibraryMenu.getTopParentId();
            LibraryBrowser.loadSavedQueryValues(key, pageData.query);
        }
        return pageData.query;
    }

    function getSavedQueryKey() {

        return LibraryBrowser.getSavedQueryKey('studios');
    }

    function reloadItems(page) {

        Dashboard.showLoadingMsg();

        var query = getQuery();

        ApiClient.getStudios(Dashboard.getCurrentUserId(), query).then(function (result) {

            // Scroll back up so they can see the results from the beginning
            window.scrollTo(0, 0);

            var html = '';
            var pagingHtml = LibraryBrowser.getQueryPagingHtml({
                startIndex: query.StartIndex,
                limit: query.Limit,
                totalRecordCount: result.TotalRecordCount,
                viewButton: false,
                updatePageSizeSetting: false,
                showLimit: false
            });

            page.querySelector('.listTopPaging').innerHTML = pagingHtml;

            html = LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                shape: "backdrop",
                preferThumb: true,
                context: 'movies',
                showItemCounts: true,
                centerText: true,
                lazy: true
            });

            var elem = page.querySelector('.itemsContainer');
            elem.innerHTML = html + pagingHtml;
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

    window.MoviesPage.renderStudiosTab = function (page, tabContent) {

        if (LibraryBrowser.needsRefresh(tabContent)) {
            reloadItems(tabContent);
        }
    };

})(jQuery, document);