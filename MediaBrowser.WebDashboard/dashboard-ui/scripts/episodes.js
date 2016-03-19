define(['jQuery'], function ($) {

    return function (view, params, tabContent) {

        var self = this;

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
                        Limit: LibraryBrowser.getDefaultPageSize()
                    },
                    view: LibraryBrowser.getSavedView(key) || LibraryBrowser.getDefaultItemsView('Poster', 'Poster')
                };

                pageData.query.ParentId = params.topParentId;
                LibraryBrowser.loadSavedQueryValues(key, pageData.query);
            }
            return pageData;
        }

        function getQuery(context) {

            return getPageData(context).query;
        }

        function getSavedQueryKey(context) {

            if (!context.savedQueryKey) {
                context.savedQueryKey = LibraryBrowser.getSavedQueryKey('episodes');
            }
            return context.savedQueryKey;
        }

        function reloadItems(page) {

            Dashboard.showLoadingMsg();

            var query = getQuery(page);
            ApiClient.getItems(Dashboard.getCurrentUserId(), query).then(function (result) {

                // Scroll back up so they can see the results from the beginning
                window.scrollTo(0, 0);

                var view = getPageData(page).view;

                var html = '';
                var pagingHtml = LibraryBrowser.getQueryPagingHtml({
                    startIndex: query.StartIndex,
                    limit: query.Limit,
                    totalRecordCount: result.TotalRecordCount,
                    showLimit: false,
                    updatePageSizeSetting: false,
                    addLayoutButton: true,
                    sortButton: true,
                    currentLayout: view,
                    layouts: 'Poster,PosterCard',
                    filterButton: true
                });

                page.querySelector('.listTopPaging').innerHTML = pagingHtml;

                updateFilterControls(page);

                if (view == "List") {

                    html = LibraryBrowser.getListViewHtml({
                        items: result.Items,
                        sortBy: query.SortBy
                    });
                }
                else if (view == "Poster") {
                    html += LibraryBrowser.getPosterViewHtml({
                        items: result.Items,
                        shape: "backdrop",
                        showTitle: true,
                        showParentTitle: true,
                        overlayText: true,
                        lazy: true,
                        showDetailsMenu: true,
                        overlayPlayButton: true
                    });
                }
                else if (view == "PosterCard") {
                    html += LibraryBrowser.getPosterViewHtml({
                        items: result.Items,
                        shape: "backdrop",
                        showTitle: true,
                        showParentTitle: true,
                        lazy: true,
                        cardLayout: true,
                        showDetailsMenu: true
                    });
                }

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

                $('.btnChangeLayout', page).on('layoutchange', function (e, layout) {
                    getPageData(page).view = layout;
                    LibraryBrowser.saveViewSetting(getSavedQueryKey(page), layout);
                    reloadItems(page);
                });

                $('.btnFilter', page).on('click', function () {
                    showFilterMenu(page);
                });

                // On callback make sure to set StartIndex = 0
                $('.btnSort', page).on('click', function () {
                    LibraryBrowser.showSortMenu({
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
                            reloadItems(page);
                        },
                        query: query
                    });
                });

                LibraryBrowser.saveQueryValues(getSavedQueryKey(page), query);

                Dashboard.hideLoadingMsg();
            });
        }

        function showFilterMenu(page) {

            require(['components/filterdialog/filterdialog'], function (filterDialogFactory) {

                var filterDialog = new filterDialogFactory({
                    query: getQuery(page),
                    mode: 'episodes'
                });

                Events.on(filterDialog, 'filterchange', function () {
                    reloadItems(page);
                });

                filterDialog.show();
            });
        }

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
        }

        initPage(tabContent);
        self.renderTab = function () {

            reloadItems(tabContent);
            updateFilterControls(tabContent);
        };
    };
});