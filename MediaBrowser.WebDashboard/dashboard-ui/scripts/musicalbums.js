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
                        IncludeItemTypes: "MusicAlbum",
                        Recursive: true,
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

        function getQuery(context) {

            return getPageData(context).query;
        }

        function getSavedQueryKey(context) {

            if (!context.savedQueryKey) {
                context.savedQueryKey = LibraryBrowser.getSavedQueryKey('albums');
            }
            return context.savedQueryKey;
        }

        function reloadItems(page) {

            Dashboard.showLoadingMsg();

            var query = getQuery(page);

            ApiClient.getItems(Dashboard.getCurrentUserId(), query).then(function (result) {

                // Scroll back up so they can see the results from the beginning
                window.scrollTo(0, 0);

                var html = '';
                var view = getPageData(page).view;
                var pagingHtml = LibraryBrowser.getQueryPagingHtml({
                    startIndex: query.StartIndex,
                    limit: query.Limit,
                    totalRecordCount: result.TotalRecordCount,
                    showLimit: false,
                    sortButton: true,
                    addLayoutButton: true,
                    currentLayout: view,
                    updatePageSizeSetting: false,
                    layouts: 'List,Poster,PosterCard,Timeline',
                    filterButton: true
                });

                page.querySelector('.listTopPaging').innerHTML = pagingHtml;

                updateFilterControls(page);

                if (view == "Poster") {
                    html = LibraryBrowser.getPosterViewHtml({
                        items: result.Items,
                        shape: "square",
                        context: 'music',
                        showTitle: true,
                        showParentTitle: true,
                        lazy: true,
                        centerText: true,
                        overlayPlayButton: true
                    });
                }
                else if (view == "PosterCard") {

                    html = LibraryBrowser.getPosterViewHtml({
                        items: result.Items,
                        shape: "square",
                        context: 'music',
                        showTitle: true,
                        coverImage: true,
                        showParentTitle: true,
                        lazy: true,
                        cardLayout: true
                    });
                }
                else if (view == "List") {

                    html = LibraryBrowser.getListViewHtml({
                        items: result.Items,
                        context: 'music',
                        sortBy: query.SortBy
                    });
                }
                else if (view == "Timeline") {
                    html = LibraryBrowser.getPosterViewHtml({
                        items: result.Items,
                        shape: "square",
                        context: 'music',
                        showTitle: true,
                        showParentTitle: true,
                        timeline: true,
                        lazy: true
                    });
                }

                var elem = page.querySelector('#items');
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

                $('.btnFilter', page).on('click', function () {
                    showFilterMenu(page);
                });

                $('.btnChangeLayout', page).on('layoutchange', function (e, layout) {

                    if (layout == 'Timeline') {
                        getQuery(page).SortBy = 'ProductionYear,PremiereDate,SortName';
                        getQuery(page).SortOrder = 'Descending';
                    }

                    getPageData(page).view = layout;
                    LibraryBrowser.saveViewSetting(getSavedQueryKey(page), layout);
                    reloadItems(page);
                });

                // On callback make sure to set StartIndex = 0
                $('.btnSort', page).on('click', function () {
                    LibraryBrowser.showSortMenu({
                        items: [{
                            name: Globalize.translate('OptionNameSort'),
                            id: 'SortName'
                        },
                        {
                            name: Globalize.translate('OptionAlbumArtist'),
                            id: 'AlbumArtist,SortName'
                        },
                        {
                            name: Globalize.translate('OptionCommunityRating'),
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
                            name: Globalize.translate('OptionReleaseDate'),
                            id: 'ProductionYear,PremiereDate,SortName'
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
                    mode: 'albums'
                });

                Events.on(filterDialog, 'filterchange', function () {
                    reloadItems(page);
                });

                filterDialog.show();
            });
        }

        function updateFilterControls(page) {

            var query = getQuery(page);

            $('.alphabetPicker', page).alphaValue(query.NameStartsWithOrGreater);
        }

        $('.alphabetPicker', tabContent).on('alphaselect', function (e, character) {

            var query = getQuery(tabContent);

            if (query.SortBy.indexOf('AlbumArtist') == -1) {
                query.NameStartsWithOrGreater = character;
                query.AlbumArtistStartsWithOrGreater = '';
            } else {
                query.AlbumArtistStartsWithOrGreater = character;
                query.NameStartsWithOrGreater = '';
            }

            query.StartIndex = 0;

            reloadItems(tabContent);

        }).on('alphaclear', function (e) {

            var query = getQuery(tabContent);

            query.NameStartsWithOrGreater = '';
            query.AlbumArtistStartsWithOrGreater = '';

            reloadItems(tabContent);
        });

        self.renderTab = function () {

            reloadItems(tabContent);
        };
    };
});