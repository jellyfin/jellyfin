define(['ironCardList', 'scrollThreshold', 'events', 'libraryBrowser', 'jQuery'], function (ironCardList, scrollThreshold, events, libraryBrowser, $) {

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
                        SortBy: "SeriesSortName,SortName",
                        SortOrder: "Ascending",
                        IncludeItemTypes: "Episode",
                        Recursive: true,
                        Fields: "PrimaryImageAspectRatio,MediaSourceCount,UserData,SyncInfo",
                        IsMissing: false,
                        IsVirtualUnaired: false,
                        ImageTypeLimit: 1,
                        EnableImageTypes: "Primary,Backdrop,Banner,Thumb",
                        StartIndex: 0,
                        Limit: pageSize
                    },
                    view: libraryBrowser.getSavedView(key) || libraryBrowser.getDefaultItemsView('Poster', 'Poster')
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
                context.savedQueryKey = libraryBrowser.getSavedQueryKey('episodes');
            }
            return context.savedQueryKey;
        }

        function setCardOptions(result) {

            var cardOptions;

            var view = self.getCurrentViewStyle();

            if (view == "List") {

                html = libraryBrowser.getListViewHtml({
                    items: result.Items,
                    sortBy: query.SortBy
                });
            }
            else if (view == "PosterCard") {
                cardOptions = {
                    items: result.Items,
                    shape: "backdrop",
                    showTitle: true,
                    showParentTitle: true,
                    lazy: true,
                    cardLayout: true,
                    showDetailsMenu: true
                };
            }
            else {
                // poster
                cardOptions = {
                    items: result.Items,
                    shape: "backdrop",
                    showTitle: true,
                    showParentTitle: true,
                    overlayText: true,
                    lazy: true,
                    showDetailsMenu: true,
                    overlayPlayButton: true
                };
            }

            self.cardOptions = cardOptions;
        }

        function reloadItems(page) {

            self.isLoading = true;
            Dashboard.showLoadingMsg();

            var query = getQuery(page);
            var startIndex = query.StartIndex;
            var reloadList = !self.cardOptions || startIndex == 0;

            ApiClient.getItems(Dashboard.getCurrentUserId(), query).then(function (result) {

                updateFilterControls(page);

                var pushItems = true;
                if (reloadList) {
                    setCardOptions(result);
                    pushItems = false;
                }
                libraryBrowser.setPosterViewData(self.cardOptions);
                libraryBrowser.setPosterViewDataOnItems(self.cardOptions, result.Items);

                var ironList = page.querySelector('#ironList');
                if (pushItems) {
                    for (var i = 0, length = result.Items.length; i < length; i++) {
                        ironList.push('items', result.Items[i]);
                    }
                } else {
                    ironList.items = result.Items;
                }

                // Hack: notifyResize needs to be done after the items have been rendered
                setTimeout(function () {
                    ironList.notifyResize();
                    self.scrollThreshold.resetSize();
                }, 300);

                libraryBrowser.saveQueryValues(getSavedQueryKey(page), query);

                Dashboard.hideLoadingMsg();
                self.hasMoreItems = result.TotalRecordCount > (startIndex + result.Items.length);
                self.isLoading = false;
            });
        }

        self.showFilterMenu = function () {
            require(['components/filterdialog/filterdialog'], function (filterDialogFactory) {

                var filterDialog = new filterDialogFactory({
                    query: getQuery(tabContent),
                    mode: 'episodes'
                });

                Events.on(filterDialog, 'filterchange', function () {
                    reloadItems(tabContent);
                });

                filterDialog.show();
            });
        };

        function updateFilterControls(tabContent) {

            var query = getQuery(tabContent);

            $('.alphabetPicker', tabContent).alphaValue(query.NameStartsWithOrGreater);
        }

        function initPage(tabContent) {

            $('.alphabetPicker', tabContent).on('alphaselect', function (e, character) {

                var query = getQuery(tabContent);
                query.NameStartsWithOrGreater = character;
                query.StartIndex = 0;

                reloadItems(tabContent);

            }).on('alphaclear', function (e) {

                var query = getQuery(tabContent);
                query.NameStartsWithOrGreater = '';

                reloadItems(tabContent);
            });

            $('.itemsContainer', tabContent).on('needsrefresh', function () {

                reloadItems(tabContent);
            });

            tabContent.querySelector('.btnFilter').addEventListener('click', function () {
                self.showFilterMenu();
            });

            tabContent.querySelector('.btnSort').addEventListener('click', function () {
                libraryBrowser.showSortMenu({
                    items: [{
                        name: Globalize.translate('OptionNameSort'),
                        id: 'SeriesSortName,SortName'
                    },
                    {
                        name: Globalize.translate('OptionTvdbRating'),
                        id: 'CommunityRating,SeriesSortName,SortName'
                    },
                    {
                        name: Globalize.translate('OptionDateAdded'),
                        id: 'DateCreated,SeriesSortName,SortName'
                    },
                    {
                        name: Globalize.translate('OptionPremiereDate'),
                        id: 'PremiereDate,SeriesSortName,SortName'
                    },
                    {
                        name: Globalize.translate('OptionDatePlayed'),
                        id: 'DatePlayed,SeriesSortName,SortName'
                    },
                    {
                        name: Globalize.translate('OptionParentalRating'),
                        id: 'OfficialRating,SeriesSortName,SortName'
                    },
                    {
                        name: Globalize.translate('OptionPlayCount'),
                        id: 'PlayCount,SeriesSortName,SortName'
                    },
                    {
                        name: Globalize.translate('OptionRuntime'),
                        id: 'Runtime,SeriesSortName,SortName'
                    },
                    {
                        name: Globalize.translate('OptionVideoBitrate'),
                        id: 'VideoBitRate,SeriesSortName,SortName'
                    }],
                    callback: function () {
                        reloadItems(tabContent);
                    },
                    query: getQuery(tabContent)
                });
            });

            tabContent.querySelector('.btnSelectView').addEventListener('click', function (e) {

                libraryBrowser.showLayoutMenu(e.target, self.getCurrentViewStyle(), 'List,Poster,PosterCard'.split(','));
            });

            tabContent.querySelector('.btnSelectView').addEventListener('layoutchange', function (e) {

                var viewStyle = e.detail.viewStyle;
                getPageData(tabContent).view = viewStyle;
                libraryBrowser.saveViewSetting(getSavedQueryKey(tabContent), viewStyle);
                reloadItems(tabContent);
            });
        }

        self.getCurrentViewStyle = function () {
            return getPageData(tabContent).view;
        };

        initPage(tabContent);
        function createList() {

            if (self.listCreated) {
                return Promise.resolve();
            }

            return ironCardList.getTemplate('episodesTab').then(function (html) {

                tabContent.querySelector('.itemsContainer').innerHTML = html;
                self.listCreated = true;
            });
        }

        function loadMoreItems() {

            if (!self.isLoading && self.hasMoreItems) {

                getQuery(tabContent).StartIndex += pageSize;
                reloadItems(tabContent);
            }
        }

        self.scrollThreshold = new scrollThreshold(tabContent, false);
        events.on(self.scrollThreshold, 'lower-threshold', loadMoreItems);

        self.renderTab = function () {

            createList().then(function () {
                reloadItems(tabContent);
                updateFilterControls(tabContent);
            });
        };

        self.destroy = function () {
            events.off(self.scrollThreshold, 'lower-threshold', loadMoreItems);
            if (self.scrollThreshold) {
                self.scrollThreshold.destroy();
            }
        };
    };
});