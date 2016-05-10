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
                        IncludeItemTypes: "Movie",
                        Recursive: true,
                        Fields: "DateCreated,SyncInfo,ItemCounts",
                        StartIndex: 0,
                        Limit: LibraryBrowser.getDefaultPageSize()
                    },
                    view: LibraryBrowser.getSavedView(key) || LibraryBrowser.getDefaultItemsView('Thumb', 'Thumb')
                };

                pageData.query.ParentId = params.topParentId;
                LibraryBrowser.loadSavedQueryValues(key, pageData.query);
            }
            return pageData;
        }

        function getQuery() {

            return getPageData().query;
        }

        function getSavedQueryKey() {

            return LibraryBrowser.getSavedQueryKey('genres');
        }

        function reloadItems(context) {

            Dashboard.showLoadingMsg();
            var query = getQuery();

            ApiClient.getGenres(Dashboard.getCurrentUserId(), query).then(function (result) {

                // Scroll back up so they can see the results from the beginning
                window.scrollTo(0, 0);

                var html = '';

                var viewStyle = getPageData().view;

                $('.listTopPaging', context).html(LibraryBrowser.getQueryPagingHtml({
                    startIndex: query.StartIndex,
                    limit: query.Limit,
                    totalRecordCount: result.TotalRecordCount,
                    viewButton: false,
                    showLimit: false,
                    updatePageSizeSetting: false,
                    addLayoutButton: true,
                    currentLayout: viewStyle
                }));

                if (viewStyle == "Thumb") {
                    html = LibraryBrowser.getPosterViewHtml({
                        items: result.Items,
                        shape: "backdrop",
                        preferThumb: true,
                        showItemCounts: true,
                        centerText: true,
                        lazy: true,
                        overlayPlayButton: true,
                        context: 'movies'
                    });
                }
                else if (viewStyle == "ThumbCard") {

                    html = LibraryBrowser.getPosterViewHtml({
                        items: result.Items,
                        shape: "backdrop",
                        preferThumb: true,
                        showItemCounts: true,
                        cardLayout: true,
                        showTitle: true,
                        lazy: true,
                        context: 'movies'
                    });
                }
                else if (viewStyle == "PosterCard") {
                    html = LibraryBrowser.getPosterViewHtml({
                        items: result.Items,
                        shape: "portrait",
                        showItemCounts: true,
                        lazy: true,
                        cardLayout: true,
                        showTitle: true,
                        context: 'movies'
                    });
                }
                else if (viewStyle == "Poster") {
                    html = LibraryBrowser.getPosterViewHtml({
                        items: result.Items,
                        shape: "portrait",
                        centerText: true,
                        showItemCounts: true,
                        lazy: true,
                        overlayPlayButton: true,
                        context: 'movies'
                    });
                }

                var elem = context.querySelector('.itemsContainer');
                elem.innerHTML = html;
                ImageLoader.lazyChildren(elem);

                $('.btnNextPage', context).on('click', function () {
                    query.StartIndex += query.Limit;
                    reloadItems(context);
                });

                $('.btnPreviousPage', context).on('click', function () {
                    query.StartIndex -= query.Limit;
                    reloadItems(context);
                });

                $('.btnChangeLayout', context).on('layoutchange', function (e, layout) {
                    getPageData().view = layout;
                    LibraryBrowser.saveViewSetting(getSavedQueryKey(), layout);
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