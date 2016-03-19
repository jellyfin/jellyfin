define(['jQuery'], function ($) {

    var data = {};

    function getPageData(context) {
        var key = getSavedQueryKey(context);
        var pageData = data[key];

        if (!pageData) {
            pageData = data[key] = {
                query: {
                    StartIndex: 0,
                    EnableFavoriteSorting: true,
                    Limit: LibraryBrowser.getDefaultPageSize()
                }
            };

            LibraryBrowser.loadSavedQueryValues(key, pageData.query);
        }
        return pageData;
    }

    function getQuery(context) {

        return getPageData(context).query;
    }

    function getSavedQueryKey(context) {

        if (!context.savedQueryKey) {
            context.savedQueryKey = LibraryBrowser.getSavedQueryKey('channels');
        }
        return context.savedQueryKey;
    }

    function getChannelsHtml(channels) {

        return LibraryBrowser.getListViewHtml({
            items: channels,
            smallIcon: true
        });
    }

    function renderChannels(page, result) {

        var query = getQuery(page);

        $('.listTopPaging', page).html(LibraryBrowser.getQueryPagingHtml({
            startIndex: query.StartIndex,
            limit: query.Limit,
            totalRecordCount: result.TotalRecordCount,
            showLimit: false,
            updatePageSizeSetting: false,
            filterButton: true
        }));

        var html = getChannelsHtml(result.Items);

        var elem = page.querySelector('#items');
        elem.innerHTML = html;
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

        LibraryBrowser.saveQueryValues(getSavedQueryKey(page), query);
    }

    function showFilterMenu(page) {

        require(['components/filterdialog/filterdialog'], function (filterDialogFactory) {

            var filterDialog = new filterDialogFactory({
                query: getQuery(page),
                mode: 'livetvchannels'
            });

            Events.on(filterDialog, 'filterchange', function () {
                reloadItems(page);
            });

            filterDialog.show();
        });
    }

    function reloadItems(page) {

        Dashboard.showLoadingMsg();

        var query = getQuery(page);

        query.UserId = Dashboard.getCurrentUserId();

        ApiClient.getLiveTvChannels(query).then(function (result) {

            renderChannels(page, result);

            Dashboard.hideLoadingMsg();

            LibraryBrowser.setLastRefreshed(page);
        });
    }

    window.LiveTvPage.renderChannelsTab = function (page, tabContent) {

        if (LibraryBrowser.needsRefresh(tabContent)) {
            reloadItems(tabContent);
        }
    };

});