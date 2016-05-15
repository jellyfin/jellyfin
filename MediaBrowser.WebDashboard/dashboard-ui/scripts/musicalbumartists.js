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
                        Recursive: true,
                        Fields: "PrimaryImageAspectRatio,SortName,DateCreated,SyncInfo,ItemCounts",
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
                context.savedQueryKey = LibraryBrowser.getSavedQueryKey('albumartists');
            }
            return context.savedQueryKey;
        }

        function reloadItems(page) {

            Dashboard.showLoadingMsg();

            var query = getQuery(page);

            ApiClient.getAlbumArtists(Dashboard.getCurrentUserId(), query).then(function (result) {

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
                    currentLayout: view,
                    filterButton: true
                });

                page.querySelector('.listTopPaging').innerHTML = pagingHtml;

                updateFilterControls(page);

                if (view == "List") {

                    html = LibraryBrowser.getListViewHtml({
                        items: result.Items,
                        context: 'music',
                        sortBy: query.SortBy
                    });
                }
                else if (view == "Poster") {
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
                else if (view == "PosterCard") {

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

                $('.btnChangeLayout', page).on('layoutchange', function (e, layout) {
                    getPageData(page).view = layout;
                    LibraryBrowser.saveViewSetting(getSavedQueryKey(page), layout);
                    reloadItems(page);
                });

                $('.btnFilter', page).on('click', function () {
                    showFilterMenu(page);
                });

                LibraryBrowser.saveQueryValues(getSavedQueryKey(page), query);
                Dashboard.hideLoadingMsg();
            });
        }

        function showFilterMenu(page) {

            require(['components/filterdialog/filterdialog'], function (filterDialogFactory) {

                var filterDialog = new filterDialogFactory({
                    query: getQuery(page),
                    mode: 'albumartists'
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

        self.renderTab = function () {

            reloadItems(tabContent);
        };
    };

});