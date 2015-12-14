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

    function getQuery() {

        return getPageData().query;
    }

    function getSavedQueryKey() {

        return LibraryBrowser.getSavedQueryKey('albums');
    }

    function reloadItems(page, viewPanel) {

        Dashboard.showLoadingMsg();

        var query = getQuery();

        ApiClient.getItems(Dashboard.getCurrentUserId(), query).then(function (result) {

            // Scroll back up so they can see the results from the beginning
            window.scrollTo(0, 0);

            var html = '';
            var view = getPageData().view;
            var pagingHtml = LibraryBrowser.getQueryPagingHtml({
                startIndex: query.StartIndex,
                limit: query.Limit,
                totalRecordCount: result.TotalRecordCount,
                viewButton: true,
                showLimit: false,
                sortButton: true,
                viewPanelClass: 'albumsViewPanel',
                addLayoutButton: true,
                currentLayout: view,
                updatePageSizeSetting: false,
                viewIcon: 'filter-list',
                layouts: 'List,Poster,PosterCard,Timeline'
            });

            page.querySelector('.listTopPaging').innerHTML = pagingHtml;

            updateFilterControls(page, viewPanel);

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
                reloadItems(page, viewPanel);
            });

            $('.btnPreviousPage', page).on('click', function () {
                query.StartIndex -= query.Limit;
                reloadItems(page, viewPanel);
            });

            $('.btnChangeLayout', page).on('layoutchange', function (e, layout) {

                if (layout == 'Timeline') {
                    getQuery().SortBy = 'ProductionYear,PremiereDate,SortName';
                    getQuery().SortOrder = 'Descending';
                }

                getPageData().view = layout;
                LibraryBrowser.saveViewSetting(getSavedQueryKey(), layout);
                reloadItems(page, viewPanel);
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
                        reloadItems(page, viewPanel);
                    },
                    query: query
                });
            });

            LibraryBrowser.saveQueryValues(getSavedQueryKey(), query);
            LibraryBrowser.setLastRefreshed(page);
            Dashboard.hideLoadingMsg();
        });
    }

    function updateFilterControls(page, viewPanel) {

        var query = getQuery();

        $('.alphabetPicker', page).alphaValue(query.NameStartsWith);
    }

    function reloadFiltersIfNeeded(page, viewPanel) {

        if (!getPageData().filtersLoaded) {

            getPageData().filtersLoaded = true;

            var query = getQuery();
            QueryFilters.loadFilters(viewPanel, Dashboard.getCurrentUserId(), query, function () {

                reloadItems(page, viewPanel);
            });
        }
    }

    function initPage(tabContent, viewPanel) {

        $(viewPanel).on('panelopen', function () {

            reloadFiltersIfNeeded(tabContent, viewPanel);
        });

        $('.chkStandardFilter', viewPanel).on('change', function () {

            var query = getQuery();
            var filterName = this.getAttribute('data-filter');
            var filters = query.Filters || "";

            filters = (',' + filters).replace(',' + filterName, '').substring(1);

            if (this.checked) {
                filters = filters ? (filters + ',' + filterName) : filterName;
            }

            query.StartIndex = 0;
            query.Filters = filters;

            reloadItems(tabContent, viewPanel);
        });

        $('.alphabetPicker', tabContent).on('alphaselect', function (e, character) {

            var query = getQuery();

            if (query.SortBy.indexOf('AlbumArtist') == -1) {
                query.NameStartsWithOrGreater = character;
                query.AlbumArtistStartsWithOrGreater = '';
            } else {
                query.AlbumArtistStartsWithOrGreater = character;
                query.NameStartsWithOrGreater = '';
            }

            query.StartIndex = 0;

            reloadItems(tabContent, viewPanel);

        }).on('alphaclear', function (e) {

            var query = getQuery();

            query.NameStartsWithOrGreater = '';
            query.AlbumArtistStartsWithOrGreater = '';

            reloadItems(tabContent, viewPanel);
        });
    }

    window.MusicPage.initAlbumsTab = function (page, tabContent) {

        var viewPanel = page.querySelector('.albumsViewPanel');
        initPage(tabContent, viewPanel);
    };

    window.MusicPage.renderAlbumsTab = function (page, tabContent) {

        if (LibraryBrowser.needsRefresh(tabContent)) {
            var viewPanel = page.querySelector('.albumsViewPanel');
            reloadItems(tabContent, viewPanel);
        }
    };

})(jQuery, document);