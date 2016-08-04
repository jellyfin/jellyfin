define(['events', 'libraryBrowser', 'imageLoader', 'alphaPicker', 'listView', 'cardBuilder', 'emby-itemscontainer'], function (events, libraryBrowser, imageLoader, alphaPicker, listView, cardBuilder) {

    return function (view, params, tabContent) {

        var self = this;
        var pageSize = libraryBrowser.getDefaultPageSize();

        var data = {};

        function getPageData(context) {
            var key = getSavedQueryKey(context);
            var pageData = data[key];

            if (!pageData) {
                pageData = data[key] = {
                    query: {
                        SortBy: "SortName",
                        SortOrder: "Ascending",
                        IncludeItemTypes: "Movie",
                        Recursive: true,
                        Fields: "PrimaryImageAspectRatio,MediaSourceCount,SortName,BasicSyncInfo",
                        ImageTypeLimit: 1,
                        EnableImageTypes: "Primary,Backdrop,Banner,Thumb",
                        StartIndex: 0,
                        Limit: pageSize
                    },
                    view: libraryBrowser.getSavedView(key) || 'Poster'
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
                context.savedQueryKey = libraryBrowser.getSavedQueryKey('movies');
            }
            return context.savedQueryKey;
        }

        function onViewStyleChange() {

            var viewStyle = self.getCurrentViewStyle();

            var itemsContainer = tabContent.querySelector('.itemsContainer');

            if (viewStyle == "List") {

                itemsContainer.classList.add('vertical-list');
                itemsContainer.classList.remove('vertical-wrap');
            }
            else {

                itemsContainer.classList.remove('vertical-list');
                itemsContainer.classList.add('vertical-wrap');
            }
            itemsContainer.innerHTML = '';
        }

        function reloadItems(page) {

            Dashboard.showLoadingMsg();

            var query = getQuery(page);

            ApiClient.getItems(Dashboard.getCurrentUserId(), query).then(function (result) {

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

                if (viewStyle == "Thumb") {

                    html = cardBuilder.getCardsHtml({
                        items: result.Items,
                        shape: "backdrop",
                        preferThumb: true,
                        context: 'movies',
                        lazy: true,
                        overlayPlayButton: true
                    });
                }
                else if (viewStyle == "ThumbCard") {

                    html = cardBuilder.getCardsHtml({
                        items: result.Items,
                        shape: "backdrop",
                        preferThumb: true,
                        context: 'movies',
                        lazy: true,
                        cardLayout: true,
                        showTitle: true,
                        showYear: true
                    });
                }
                else if (viewStyle == "Banner") {

                    html = cardBuilder.getCardsHtml({
                        items: result.Items,
                        shape: "banner",
                        preferBanner: true,
                        context: 'movies',
                        lazy: true
                    });
                }
                else if (viewStyle == "List") {

                    html = listView.getListViewHtml({
                        items: result.Items,
                        context: 'movies',
                        sortBy: query.SortBy
                    });
                }
                else if (viewStyle == "PosterCard") {

                    html = cardBuilder.getCardsHtml({
                        items: result.Items,
                        shape: "portrait",
                        context: 'movies',
                        showTitle: true,
                        showYear: true,
                        lazy: true,
                        cardLayout: true
                    });
                }
                else {

                    // Poster
                    html = cardBuilder.getCardsHtml({
                        items: result.Items,
                        shape: "portrait",
                        context: 'movies',
                        centerText: true,
                        lazy: true,
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
                    mode: 'movies'
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

            tabContent.querySelector('.btnSort').addEventListener('click', function (e) {
                libraryBrowser.showSortMenu({
                    items: [{
                        name: Globalize.translate('OptionNameSort'),
                        id: 'SortName'
                    },
                        {
                            name: Globalize.translate('OptionBudget'),
                            id: 'Budget,SortName'
                        },
                        {
                            name: Globalize.translate('OptionImdbRating'),
                            id: 'CommunityRating,SortName'
                        },
                        {
                            name: Globalize.translate('OptionCriticRating'),
                            id: 'CriticRating,SortName'
                        },
                        {
                            name: Globalize.translate('OptionDateAdded'),
                            id: 'DateCreated,SortName'
                        },
                        {
                            name: Globalize.translate('OptionDatePlayed'),
                            id: 'DatePlayed,SortName'
                        },
                        {
                            name: Globalize.translate('OptionMetascore'),
                            id: 'Metascore,SortName'
                        },
                        {
                            name: Globalize.translate('OptionParentalRating'),
                            id: 'OfficialRating,SortName'
                        },
                        {
                            name: Globalize.translate('OptionPlayCount'),
                            id: 'PlayCount,SortName'
                        },
                        {
                            name: Globalize.translate('OptionReleaseDate'),
                            id: 'PremiereDate,SortName'
                        },
                        {
                            name: Globalize.translate('OptionRevenue'),
                            id: 'Revenue,SortName'
                        },
                        {
                            name: Globalize.translate('OptionRuntime'),
                            id: 'Runtime,SortName'
                        },
                        {
                            name: Globalize.translate('OptionVideoBitrate'),
                            id: 'VideoBitRate,SortName'
                        }],
                    callback: function () {
                        getQuery(tabContent).StartIndex = 0;
                        reloadItems(tabContent);
                    },
                    query: getQuery(tabContent),
                    button: e.target
                });
            });

            var btnSelectView = tabContent.querySelector('.btnSelectView');
            btnSelectView.addEventListener('click', function (e) {

                libraryBrowser.showLayoutMenu(e.target, self.getCurrentViewStyle(), 'Banner,List,Poster,PosterCard,Thumb,ThumbCard'.split(','));
            });

            btnSelectView.addEventListener('layoutchange', function (e) {

                var viewStyle = e.detail.viewStyle;

                getPageData(tabContent).view = viewStyle;
                libraryBrowser.saveViewSetting(getSavedQueryKey(tabContent), viewStyle);
                getQuery(tabContent).StartIndex = 0;
                onViewStyleChange();
                reloadItems(tabContent);
            });
        }

        self.getCurrentViewStyle = function () {
            return getPageData(tabContent).view;
        };

        initPage(tabContent);
        onViewStyleChange();

        self.renderTab = function () {

            reloadItems(tabContent);
            updateFilterControls(tabContent);
        };

        self.destroy = function () {
        };
    };
});