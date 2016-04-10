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
                        SortBy: "SortName",
                        SortOrder: "Ascending",
                        IncludeItemTypes: "Movie",
                        Recursive: true,
                        Fields: "PrimaryImageAspectRatio,SortName,MediaSourceCount,SyncInfo",
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
                context.savedQueryKey = LibraryBrowser.getSavedQueryKey('movies');
            }
            return context.savedQueryKey;
        }

        function reloadItems(context) {

            Dashboard.showLoadingMsg();

            var userId = Dashboard.getCurrentUserId();

            var query = getQuery(context);
            var view = getPageData(context).view;

            ApiClient.getItems(userId, query).then(function (result) {

                // Scroll back up so they can see the results from the beginning
                window.scrollTo(0, 0);

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
                    layouts: 'Banner,List,Poster,PosterCard,Thumb,ThumbCard,Timeline',
                    filterButton: true
                });

                context.querySelector('.listTopPaging').innerHTML = pagingHtml;

                updateFilterControls(context);

                if (view == "Thumb") {
                    html = LibraryBrowser.getPosterViewHtml({
                        items: result.Items,
                        shape: "backdrop",
                        preferThumb: true,
                        lazy: true,
                        showDetailsMenu: true,
                        overlayPlayButton: true
                    });
                }
                else if (view == "ThumbCard") {

                    html = LibraryBrowser.getPosterViewHtml({
                        items: result.Items,
                        shape: "backdrop",
                        preferThumb: true,
                        lazy: true,
                        showTitle: true,
                        cardLayout: true,
                        showYear: true,
                        showDetailsMenu: true
                    });
                }
                else if (view == "Banner") {

                    html = LibraryBrowser.getPosterViewHtml({
                        items: result.Items,
                        shape: "banner",
                        preferBanner: true,
                        lazy: true,
                        showDetailsMenu: true
                    });
                }
                else if (view == "List") {

                    html = LibraryBrowser.getListViewHtml({
                        items: result.Items,
                        sortBy: query.SortBy
                    });
                }
                else if (view == "Poster") {
                    html = LibraryBrowser.getPosterViewHtml({
                        items: result.Items,
                        shape: "portrait",
                        centerText: true,
                        lazy: true,
                        showDetailsMenu: true,
                        overlayPlayButton: true
                    });
                }
                else if (view == "PosterCard") {
                    html = LibraryBrowser.getPosterViewHtml({
                        items: result.Items,
                        shape: "portrait",
                        showTitle: true,
                        showYear: true,
                        lazy: true,
                        cardLayout: true,
                        showDetailsMenu: true
                    });
                }
                else if (view == "Timeline") {
                    html = LibraryBrowser.getPosterViewHtml({
                        items: result.Items,
                        shape: "portrait",
                        showTitle: true,
                        timeline: true,
                        centerText: true,
                        lazy: true,
                        showDetailsMenu: true
                    });
                }

                var elem = context.querySelector('.itemsContainer');
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

                $('.btnChangeLayout', context).on('layoutchange', function (e, layout) {

                    if (layout == 'Timeline') {
                        getQuery(context).SortBy = 'ProductionYear,PremiereDate,SortName';
                        getQuery(context).SortOrder = 'Descending';
                    }

                    getPageData(context).view = layout;
                    LibraryBrowser.saveViewSetting(getSavedQueryKey(context), layout);
                    reloadItems(context);
                });

                $('.btnFilter', context).on('click', function () {
                    showFilterMenu(context);
                });

                // On callback make sure to set StartIndex = 0
                $('.btnSort', context).on('click', function () {
                    LibraryBrowser.showSortMenu({
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
                            reloadItems(context);
                        },
                        query: query
                    });
                });

                LibraryBrowser.saveQueryValues(getSavedQueryKey(context), query);

                LibraryBrowser.setLastRefreshed(context);

                Dashboard.hideLoadingMsg();
            });
        }

        function showFilterMenu(context) {

            require(['components/filterdialog/filterdialog'], function (filterDialogFactory) {

                var filterDialog = new filterDialogFactory({
                    query: getQuery(context),
                    mode: 'movies'
                });

                Events.on(filterDialog, 'filterchange', function () {
                    reloadItems(context);
                });

                filterDialog.show();
            });
        }

        function updateFilterControls(context) {

            var query = getQuery(context);

            $('.alphabetPicker', context).alphaValue(query.NameStartsWithOrGreater);
        }

        function initPage(context) {

            $('.alphabetPicker', context).on('alphaselect', function (e, character) {

                var query = getQuery(context);
                query.NameStartsWithOrGreater = character;
                query.StartIndex = 0;

                reloadItems(context);

            }).on('alphaclear', function (e) {

                var query = getQuery(context);
                query.NameStartsWithOrGreater = '';

                reloadItems(context);
            });

            $('.itemsContainer', context).on('needsrefresh', function () {

                reloadItems(context);
            });
        }

        self.initTab = function () {

            initPage(tabContent);
        };

        self.renderTab = function () {

            reloadItems(tabContent);
            updateFilterControls(tabContent);
        };
    };

});