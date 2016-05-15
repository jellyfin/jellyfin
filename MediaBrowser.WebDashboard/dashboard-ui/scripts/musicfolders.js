define(['jQuery'], function ($) {

    return function (view, params, tabContent) {

        var self = this;

        var data = {};
        function getPageData() {
            var key = getSavedQueryKey();
            var pageData = data[key];

            if (!pageData) {
                pageData = data[key] = {
                    query: {
                        SortBy: "SortName",
                        SortOrder: "Ascending",
                        Fields: "PrimaryImageAspectRatio,SortName,SyncInfo",
                        StartIndex: 0,
                        ImageTypeLimit: 1,
                        EnableImageTypes: "Primary,Backdrop,Banner,Thumb",
                        Limit: LibraryBrowser.getDefaultPageSize()
                    },
                    view: LibraryBrowser.getSavedView(key) || LibraryBrowser.getDefaultItemsView('Poster', 'Poster')
                };

                pageData.query.ParentId = LibraryMenu.getTopParentId();
                LibraryBrowser.loadSavedQueryValues(key, pageData.query);
            }
            return pageData;
        }

        function getQuery() {

            return getPageData().query;
        }

        function getSavedQueryKey() {

            return LibraryBrowser.getSavedQueryKey('folders');
        }

        function reloadItems(context) {

            Dashboard.showLoadingMsg();

            var query = getQuery();
            ApiClient.getItems(Dashboard.getCurrentUserId(), query).then(function (result) {

                // Scroll back up so they can see the results from the beginning
                window.scrollTo(0, 0);

                var html = '';
                var view = getPageData().view;
                var pagingHtml = LibraryBrowser.getQueryPagingHtml({
                    startIndex: query.StartIndex,
                    limit: query.Limit,
                    totalRecordCount: result.TotalRecordCount,
                    viewButton: false,
                    showLimit: false,
                    sortButton: false,
                    addLayoutButton: false,
                    currentLayout: view,
                    updatePageSizeSetting: false,
                    viewIcon: 'filter-list',
                    layouts: 'List,Poster,PosterCard,Timeline'
                });

                context.querySelector('.listTopPaging').innerHTML = pagingHtml;

                if (view == "Poster") {
                    html = LibraryBrowser.getPosterViewHtml({
                        items: result.Items,
                        shape: "square",
                        context: 'folders',
                        showTitle: true,
                        showParentTitle: true,
                        lazy: true,
                        centerText: true,
                        overlayPlayButton: true
                    });
                }

                var elem = context.querySelector('#items');
                elem.innerHTML = html + pagingHtml;
                ImageLoader.lazyChildren(elem);

                $('.btnNextPage', context).on('click', function () {
                    query.StartIndex += query.Limit;
                    reloadItems(context);
                });

                $('.btnPreviousPage', context).on('click', function () {
                    query.StartIndex -= query.Limit;
                    reloadItems(context);
                });

                LibraryBrowser.saveQueryValues(getSavedQueryKey(), query);
                Dashboard.hideLoadingMsg();
            });
        }
        self.renderTab = function () {

            reloadItems(tabContent);
        };
    };

});