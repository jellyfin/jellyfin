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

    function getQuery() {

        return getPageData().query;
    }

    function getSavedQueryKey() {

        return LibraryBrowser.getSavedQueryKey('albumartists');
    }

    function reloadItems(page, viewPanel) {

        Dashboard.showLoadingMsg();

        var query = getQuery();

        ApiClient.getAlbumArtists(Dashboard.getCurrentUserId(), query).then(function (result) {

            // Scroll back up so they can see the results from the beginning
            window.scrollTo(0, 0);

            var view = getPageData().view;

            var html = '';
            var pagingHtml = LibraryBrowser.getQueryPagingHtml({
                startIndex: query.StartIndex,
                limit: query.Limit,
                totalRecordCount: result.TotalRecordCount,
                showLimit: false,
                viewPanelClass: 'albumArtistsViewPanel',
                updatePageSizeSetting: false,
                addLayoutButton: true,
                currentLayout: view,
                viewButton: true,
                viewIcon: 'filter-list'
            });

            page.querySelector('.listTopPaging').innerHTML = pagingHtml;

            updateFilterControls(page, viewPanel);

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
                reloadItems(page, viewPanel);
            });

            $('.btnPreviousPage', page).on('click', function () {
                query.StartIndex -= query.Limit;
                reloadItems(page, viewPanel);
            });

            $('.btnChangeLayout', page).on('layoutchange', function (e, layout) {
                getPageData().view = layout;
                LibraryBrowser.saveViewSetting(getSavedQueryKey(), layout);
                reloadItems(page, viewPanel);
            });

            LibraryBrowser.saveQueryValues(getSavedQueryKey(), query);
            LibraryBrowser.setLastRefreshed(page);
            Dashboard.hideLoadingMsg();
        });
    }

    function updateFilterControls(tabContent, viewPanel) {

        var query = getQuery();

        $('.chkStandardFilter', viewPanel).each(function () {

            var filters = "," + (query.Filters || "");
            var filterName = this.getAttribute('data-filter');

            this.checked = filters.indexOf(',' + filterName) != -1;

        });

        $('.alphabetPicker', tabContent).alphaValue(query.NameStartsWithOrGreater);
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

            query.NameStartsWithOrGreater = character;
            query.StartIndex = 0;

            reloadItems(tabContent, viewPanel);

        }).on('alphaclear', function (e) {

            var query = getQuery();

            query.NameStartsWithOrGreater = '';

            reloadItems(tabContent, viewPanel);
        });
    }

    window.MusicPage.initAlbumArtistsTab = function (page, tabContent) {

        var viewPanel = page.querySelector('.albumArtistsViewPanel');
        initPage(tabContent, viewPanel);
    };

    window.MusicPage.renderAlbumArtistsTab = function (page, tabContent) {

        if (LibraryBrowser.needsRefresh(tabContent)) {
            var viewPanel = page.querySelector('.albumArtistsViewPanel');
            reloadItems(tabContent, viewPanel);
        }
    };

})(jQuery, document);