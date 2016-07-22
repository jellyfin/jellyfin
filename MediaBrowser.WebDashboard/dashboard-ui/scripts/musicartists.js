define(['events', 'libraryBrowser', 'imageLoader', 'alphaPicker', 'listView', 'emby-itemscontainer'], function (events, libraryBrowser, imageLoader, alphaPicker, listView) {

    return function (view, params, tabContent) {

        var self = this;

        var data = {};

        function getPageData(context) {
            var key = getSavedQueryKey(context);
            var pageData = data[key];

            if (!pageData) {
                pageData = data[key] = {
                    query: {
                        SortBy: "SortName",
                        SortOrder: "Ascending",
                        Recursive: true,
                        Fields: "PrimaryImageAspectRatio,SortName,DateCreated,SyncInfo,ItemCounts",
                        StartIndex: 0,
                        ImageTypeLimit: 1,
                        EnableImageTypes: "Primary,Backdrop,Banner,Thumb",
                        Limit: LibraryBrowser.getDefaultPageSize()
                    },
                    view: libraryBrowser.getSavedView(key) || libraryBrowser.getDefaultItemsView('PosterCard', 'PosterCard')
                };

                pageData.query.ParentId = params.topParentId;
                libraryBrowser.loadSavedQueryValues(key, pageData.query);
            }
            return pageData;
        }

        function getQuery(context) {

            return getPageData(context).query;
        }

        function getSavedQueryKey(context) {

            if (!context.savedQueryKey) {
                context.savedQueryKey = LibraryBrowser.getSavedQueryKey(self.mode);
            }
            return context.savedQueryKey;
        }

        function reloadItems(page) {

            Dashboard.showLoadingMsg();

            var query = getQuery(page);

            var promise = self.mode == 'albumartists' ?
                ApiClient.getAlbumArtists(Dashboard.getCurrentUserId(), query) :
                ApiClient.getArtists(Dashboard.getCurrentUserId(), query);

            promise.then(function (result) {

                // Scroll back up so they can see the results from the beginning
                window.scrollTo(0, 0);

                updateFilterControls(page);

                var pagingHtml = LibraryBrowser.getQueryPagingHtml({
                    startIndex: query.StartIndex,
                    limit: query.Limit,
                    totalRecordCount: result.TotalRecordCount,
                    showLimit: false,
                    updatePageSizeSetting: false,
                    addLayoutButton: false,
                    sortButton: false,
                    filterButton: false
                });

                var html;
                var viewStyle = self.getCurrentViewStyle();

                if (viewStyle == "List") {

                    html = listView.getListViewHtml({
                        items: result.Items,
                        sortBy: query.SortBy
                    });
                }
                else if (viewStyle == "PosterCard") {

                    html = LibraryBrowser.getPosterViewHtml({
                        items: result.Items,
                        shape: "square",
                        context: 'music',
                        showTitle: true,
                        coverImage: true,
                        lazy: true,
                        cardLayout: true,
                        showSongCount: true
                    });
                }
                else {

                    // Poster
                    html = LibraryBrowser.getPosterViewHtml({
                        items: result.Items,
                        shape: "square",
                        context: 'music',
                        showTitle: true,
                        coverImage: true,
                        lazy: true,
                        centerText: true,
                        overlayPlayButton: true
                    });
                }

                var i, length;
                var elems = tabContent.querySelectorAll('.paging');
                for (i = 0, length = elems.length; i < length; i++) {
                    elems[i].innerHTML = pagingHtml;
                }

                function onNextPageClick() {
                    query.StartIndex += query.Limit;
                    reloadItems(tabContent);
                }

                function onPreviousPageClick() {
                    query.StartIndex -= query.Limit;
                    reloadItems(tabContent);
                }

                elems = tabContent.querySelectorAll('.btnNextPage');
                for (i = 0, length = elems.length; i < length; i++) {
                    elems[i].addEventListener('click', onNextPageClick);
                }

                elems = tabContent.querySelectorAll('.btnPreviousPage');
                for (i = 0, length = elems.length; i < length; i++) {
                    elems[i].addEventListener('click', onPreviousPageClick);
                }

                var itemsContainer = tabContent.querySelector('.itemsContainer');
                itemsContainer.innerHTML = html;
                imageLoader.lazyChildren(itemsContainer);

                libraryBrowser.saveQueryValues(getSavedQueryKey(page), query);

                Dashboard.hideLoadingMsg();
            });
        }

        self.showFilterMenu = function () {

            require(['components/filterdialog/filterdialog'], function (filterDialogFactory) {

                var filterDialog = new filterDialogFactory({
                    query: getQuery(tabContent),
                    mode: self.mode
                });

                Events.on(filterDialog, 'filterchange', function () {
                    getQuery(tabContent).StartIndex = 0;
                    reloadItems(tabContent);
                });

                filterDialog.show();
            });
        }

        function updateFilterControls(tabContent) {

            var query = getQuery(tabContent);
            self.alphaPicker.value(query.NameStartsWithOrGreater);
        }

        function initPage(tabContent) {

            var alphaPickerElement = tabContent.querySelector('.alphaPicker');
            alphaPickerElement.addEventListener('alphavaluechanged', function (e) {
                var newValue = e.detail.value;
                var query = getQuery(tabContent);
                query.NameStartsWithOrGreater = newValue;
                query.StartIndex = 0;
                reloadItems(tabContent);
            });

            self.alphaPicker = new alphaPicker({
                element: alphaPickerElement,
                valueChangeEvent: 'click'
            });

            tabContent.querySelector('.btnFilter').addEventListener('click', function () {
                self.showFilterMenu();
            });

            var btnSelectView = tabContent.querySelector('.btnSelectView');
            btnSelectView.addEventListener('click', function (e) {

                libraryBrowser.showLayoutMenu(e.target, self.getCurrentViewStyle(), 'List,Poster,PosterCard'.split(','));
            });

            btnSelectView.addEventListener('layoutchange', function (e) {

                var viewStyle = e.detail.viewStyle;

                getPageData(tabContent).view = viewStyle;
                libraryBrowser.saveViewSetting(getSavedQueryKey(tabContent), viewStyle);
                getQuery(tabContent).StartIndex = 0;
                reloadItems(tabContent);
            });
        }

        self.getCurrentViewStyle = function () {
            return getPageData(tabContent).view;
        };

        initPage(tabContent);

        self.renderTab = function () {

            reloadItems(tabContent);
            updateFilterControls(tabContent);
        };

        self.destroy = function () {
        };
    };
});