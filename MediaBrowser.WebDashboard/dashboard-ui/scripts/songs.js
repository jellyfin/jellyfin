(function ($, document) {

    var defaultSortBy = "Album,SortName";

    var data = {};
    function getPageData() {
        var key = getSavedQueryKey();
        var pageData = data[key];

        if (!pageData) {
            pageData = data[key] = {
                query: {
                    SortBy: defaultSortBy,
                    SortOrder: "Ascending",
                    IncludeItemTypes: "Audio",
                    Recursive: true,
                    Fields: "AudioInfo,ParentId,SyncInfo",
                    Limit: 100,
                    StartIndex: 0,
                    ImageTypeLimit: 1,
                    EnableImageTypes: "Primary,Backdrop,Banner,Thumb"
                }
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

        return LibraryBrowser.getSavedQueryKey('songs');
    }

    function reloadItems(page, viewPanel) {

        Dashboard.showLoadingMsg();

        var query = getQuery();
        ApiClient.getItems(Dashboard.getCurrentUserId(), query).then(function (result) {

            // Scroll back up so they can see the results from the beginning
            window.scrollTo(0, 0);

            var html = '';
            var pagingHtml = LibraryBrowser.getQueryPagingHtml({
                startIndex: query.StartIndex,
                limit: query.Limit,
                totalRecordCount: result.TotalRecordCount,
                viewButton: true,
                showLimit: false,
                sortButton: true,
                viewPanelClass: 'songsViewPanel',
                updatePageSizeSetting: false,
                viewIcon: 'filter-list'
            });

            page.querySelector('.listTopPaging').innerHTML = pagingHtml;

            html += LibraryBrowser.getListViewHtml({
                items: result.Items,
                showIndex: true,
                defaultAction: 'play',
                smallIcon: true
            });

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

            // On callback make sure to set StartIndex = 0
            $('.btnSort', page).on('click', function () {
                LibraryBrowser.showSortMenu({
                    items: [{
                        name: Globalize.translate('OptionTrackName'),
                        id: 'Name'
                    },
                    {
                        name: Globalize.translate('OptionAlbum'),
                        id: 'Album,SortName'
                    },
                    {
                        name: Globalize.translate('OptionAlbumArtist'),
                        id: 'AlbumArtist,Album,SortName'
                    },
                    {
                        name: Globalize.translate('OptionArtist'),
                        id: 'Artist,Album,SortName'
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
                        name: Globalize.translate('OptionPlayCount'),
                        id: 'PlayCount,SortName'
                    },
                    {
                        name: Globalize.translate('OptionReleaseDate'),
                        id: 'PremiereDate,AlbumArtist,Album,SortName'
                    },
                    {
                        name: Globalize.translate('OptionRuntime'),
                        id: 'Runtime,AlbumArtist,Album,SortName'
                    }],
                    callback: function () {
                        reloadItems(page, viewPanel);
                    },
                    query: query
                });
            });

            LibraryBrowser.saveQueryValues(getSavedQueryKey(), query);

            Dashboard.hideLoadingMsg();
        });
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
    }

    window.MusicPage.initSongsTab = function (page, tabContent) {

        var viewPanel = page.querySelector('.songsViewPanel');
        initPage(tabContent, viewPanel);
    };

    window.MusicPage.renderSongsTab = function (page, tabContent) {

        if (LibraryBrowser.needsRefresh(tabContent)) {
            var viewPanel = page.querySelector('.songsViewPanel');
            reloadItems(tabContent, viewPanel);
        }
    };

})(jQuery, document);