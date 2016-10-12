define(['libraryBrowser', 'cardBuilder'], function (libraryBrowser, cardBuilder) {

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
                        IncludeItemTypes: "Audio,MusicAlbum",
                        Recursive: true,
                        Fields: "DateCreated,ItemCounts",
                        StartIndex: 0
                    },
                    view: libraryBrowser.getSavedView(key) || 'PosterCard'
                };

                pageData.query.ParentId = params.topParentId;
                libraryBrowser.loadSavedQueryValues(key, pageData.query);
            }
            return pageData;
        }

        function getQuery() {

            return getPageData().query;
        }

        function getSavedQueryKey() {

            return libraryBrowser.getSavedQueryKey('genres');
        }

        function getPromise() {

            Dashboard.showLoadingMsg();
            var query = getQuery();

            return ApiClient.getGenres(Dashboard.getCurrentUserId(), query);
        }

        function reloadItems(context, promise) {

            var query = getQuery();

            promise.then(function (result) {

                var html = '';

                var viewStyle = self.getCurrentViewStyle();

                if (viewStyle == "Thumb") {
                    html = cardBuilder.getCardsHtml({
                        items: result.Items,
                        shape: "backdrop",
                        preferThumb: true,
                        context: 'music',
                        showItemCounts: true,
                        centerText: true,
                        lazy: true,
                        overlayMoreButton: true,
                        showTitle: true
                    });
                }
                else if (viewStyle == "ThumbCard") {

                    html = cardBuilder.getCardsHtml({
                        items: result.Items,
                        shape: "backdrop",
                        preferThumb: true,
                        context: 'music',
                        showItemCounts: true,
                        cardLayout: true,
                        showTitle: true,
                        lazy: true,
                        vibrant: true
                    });
                }
                else if (viewStyle == "PosterCard") {
                    html = cardBuilder.getCardsHtml({
                        items: result.Items,
                        shape: "auto",
                        context: 'music',
                        showItemCounts: true,
                        lazy: true,
                        cardLayout: true,
                        showTitle: true,
                        vibrant: true
                    });
                }
                else if (viewStyle == "Poster") {
                    html = cardBuilder.getCardsHtml({
                        items: result.Items,
                        shape: "auto",
                        context: 'music',
                        centerText: true,
                        showItemCounts: true,
                        lazy: true,
                        overlayMoreButton: true,
                        showTitle: true
                    });
                }

                var elem = context.querySelector('#items');
                elem.innerHTML = html;
                ImageLoader.lazyChildren(elem);

                libraryBrowser.saveQueryValues(getSavedQueryKey(), query);

                Dashboard.hideLoadingMsg();
            });
        }
        self.getViewStyles = function () {
            return 'Poster,PosterCard,Thumb,ThumbCard'.split(',');
        };

        self.getCurrentViewStyle = function () {
            return getPageData(tabContent).view;
        };

        self.setCurrentViewStyle = function (viewStyle) {
            getPageData(tabContent).view = viewStyle;
            libraryBrowser.saveViewSetting(getSavedQueryKey(tabContent), viewStyle);
            fullyReload();
        };

        self.enableViewSelection = true;
        var promise;

        self.preRender = function () {
            promise = getPromise();
        };

        self.renderTab = function () {

            reloadItems(tabContent, promise);
        };

        function fullyReload() {
            self.preRender();
            self.renderTab();
        }

        var btnSelectView = tabContent.querySelector('.btnSelectView');
        btnSelectView.addEventListener('click', function (e) {

            libraryBrowser.showLayoutMenu(e.target, self.getCurrentViewStyle(), self.getViewStyles());
        });

        btnSelectView.addEventListener('layoutchange', function (e) {

            self.setCurrentViewStyle(e.detail.viewStyle);
        });
    };
});