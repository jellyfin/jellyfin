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
                        IncludeItemTypes: "Series",
                        Recursive: true,
                        Fields: "DateCreated,SyncInfo,ItemCounts",
                        StartIndex: 0
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

                var html = '';

                var viewStyle = self.getCurrentViewStyle();

                if (viewStyle == "Thumb") {
                    html = LibraryBrowser.getPosterViewHtml({
                        items: result.Items,
                        shape: "backdrop",
                        preferThumb: true,
                        context: 'tv',
                        showItemCounts: true,
                        centerText: true,
                        lazy: true,
                        overlayPlayButton: true
                    });
                }
                else if (viewStyle == "ThumbCard") {

                    html = LibraryBrowser.getPosterViewHtml({
                        items: result.Items,
                        shape: "backdrop",
                        preferThumb: true,
                        context: 'tv',
                        showItemCounts: true,
                        cardLayout: true,
                        showTitle: true,
                        lazy: true
                    });
                }
                else if (viewStyle == "PosterCard") {
                    html = LibraryBrowser.getPosterViewHtml({
                        items: result.Items,
                        shape: "portrait",
                        context: 'tv',
                        showItemCounts: true,
                        lazy: true,
                        cardLayout: true,
                        showTitle: true
                    });
                }
                else if (viewStyle == "Poster") {
                    html = LibraryBrowser.getPosterViewHtml({
                        items: result.Items,
                        shape: "portrait",
                        context: 'tv',
                        centerText: true,
                        showItemCounts: true,
                        lazy: true,
                        overlayPlayButton: true
                    });
                }

                var elem = context.querySelector('#items');
                elem.innerHTML = html;
                ImageLoader.lazyChildren(elem);

                LibraryBrowser.saveQueryValues(getSavedQueryKey(), query);

                Dashboard.hideLoadingMsg();
            });
        }
        self.getViewStyles = function () {
            return 'Poster,PosterCard,Thumb,ThumbCard'.split(',');
        };

        self.getCurrentViewStyle = function () {
            return getPageData(tabContent).view;
        };

        self.setCurrentViewStyle = function(viewStyle) {
            getPageData(tabContent).view = viewStyle;
            LibraryBrowser.saveViewSetting(getSavedQueryKey(tabContent), viewStyle);
            reloadItems(tabContent);
        };

        self.enableViewSelection = true;

        self.renderTab = function () {

            reloadItems(tabContent);
        };

        tabContent.querySelector('.btnSelectView').addEventListener('click', function (e) {

            LibraryBrowser.showLayoutMenu(e.target, self.getCurrentViewStyle(), self.getViewStyles());
        });

        tabContent.querySelector('.btnSelectView').addEventListener('layoutchange', function (e) {

            self.setCurrentViewStyle(e.detail.viewStyle);
        });
    };
});