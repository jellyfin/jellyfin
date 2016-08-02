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
                        IncludeItemTypes: "Movie",
                        Recursive: true,
                        Fields: "DateCreated,ItemCounts,PrimaryImageAspectRatio",
                        StartIndex: 0
                    },
                    view: libraryBrowser.getSavedView(key) || 'Thumb'
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
                var elem = context.querySelector('#items');

                if (viewStyle == "Thumb") {
                    cardBuilder.buildCards(result.Items, {
                        itemsContainer: elem,
                        shape: "backdrop",
                        preferThumb: true,
                        showTitle: true,
                        scalable: true,
                        showItemCounts: true,
                        centerText: true,
                        overlayMoreButton: true
                    });
                }
                else if (viewStyle == "ThumbCard") {

                    cardBuilder.buildCards(result.Items, {
                        itemsContainer: elem,
                        shape: "backdrop",
                        preferThumb: true,
                        showTitle: true,
                        scalable: true,
                        showItemCounts: true,
                        centerText: true,
                        cardLayout: true
                    });
                }
                else if (viewStyle == "PosterCard") {
                    cardBuilder.buildCards(result.Items, {
                        itemsContainer: elem,
                        shape: "auto",
                        showTitle: true,
                        scalable: true,
                        showItemCounts: true,
                        centerText: true,
                        cardLayout: true
                    });
                }
                else if (viewStyle == "Poster") {
                    cardBuilder.buildCards(result.Items, {
                        itemsContainer: elem,
                        shape: "auto",
                        showTitle: true,
                        scalable: true,
                        showItemCounts: true,
                        centerText: true,
                        overlayMoreButton: true
                    });
                }

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