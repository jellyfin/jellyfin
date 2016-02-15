(function ($, document) {

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
                    Fields: "PrimaryImageAspectRatio,SortName,MediaSourceCount,SyncInfo",
                    ImageTypeLimit: 1,
                    EnableImageTypes: "Primary,Backdrop,Banner,Thumb",
                    StartIndex: 0,
                    Limit: LibraryBrowser.getDefaultPageSize()
                },
                view: LibraryBrowser.getSavedView(key) || LibraryBrowser.getDefaultItemsView('Poster', 'Poster')
            };

            pageData.query.ParentId = LibraryMenu.getTopParentId();
            LibraryBrowser.loadSavedQueryValues(key, pageData.query);
        }
        return pageData;
    }

    function getQuery() {

        return getPageData().query;
    }

    function getSavedQueryKey() {

        return LibraryBrowser.getSavedQueryKey('movies');
    }

    function reloadItems(page, viewPanel) {

        Dashboard.showLoadingMsg();

        var userId = Dashboard.getCurrentUserId();

        var query = getQuery();
        var view = getPageData().view;

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

            page.querySelector('.listTopPaging').innerHTML = pagingHtml;

            updateFilterControls(page);

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

                if (layout == 'Timeline') {
                    getQuery().SortBy = 'ProductionYear,PremiereDate,SortName';
                    getQuery().SortOrder = 'Descending';
                }

                getPageData().view = layout;
                LibraryBrowser.saveViewSetting(getSavedQueryKey(), layout);
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
                        reloadItems(page);
                    },
                    query: query
                });
            });

            LibraryBrowser.saveQueryValues(getSavedQueryKey(), query);

            LibraryBrowser.setLastRefreshed(page);

            Dashboard.hideLoadingMsg();
        });
    }

    function showFilterMenu(page) {

        require(['components/filterdialog/filterdialog'], function (filterDialogFactory) {

            var filterDialog = new filterDialogFactory({
                query: getQuery(),
                mode: 'movies'
            });

            Events.on(filterDialog, 'filterchange', function () {
                reloadItems(page);
            });

            filterDialog.show();
        });
    }

    function updateFilterControls(tabContent) {

        var query = getQuery();

        $('.alphabetPicker', tabContent).alphaValue(query.NameStartsWithOrGreater);
    }

    function initPage(tabContent) {

        $('.alphabetPicker', tabContent).on('alphaselect', function (e, character) {

            var query = getQuery();
            query.NameStartsWithOrGreater = character;
            query.StartIndex = 0;

            reloadItems(tabContent);

        }).on('alphaclear', function (e) {

            var query = getQuery();
            query.NameStartsWithOrGreater = '';

            reloadItems(tabContent);
        });

        $('.itemsContainer', tabContent).on('needsrefresh', function () {

            reloadItems(tabContent);
        });
    }

    window.MoviesPage.initMoviesTab = function (page, tabContent) {

        initPage(tabContent);
    };

    window.MoviesPage.renderMoviesTab = function (page, tabContent) {

        if (LibraryBrowser.needsRefresh(tabContent)) {
            reloadItems(tabContent);
            updateFilterControls(tabContent);
        }
    };

})(jQuery, document);