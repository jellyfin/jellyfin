(function ($, document) {

    var data = {};

    function getPageData() {
        var key = getSavedQueryKey();
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

    function getQuery() {

        return getPageData().query;
    }

    function getSavedQueryKey() {

        return LibraryBrowser.getSavedQueryKey('channels');
    }

    function getChannelsHtml(channels) {

        return LibraryBrowser.getListViewHtml({
            items: channels,
            smallIcon: true
        });
    }

    function renderChannels(page, viewPanel, result) {

        var query = getQuery();

        $('.listTopPaging', page).html(LibraryBrowser.getQueryPagingHtml({
            startIndex: query.StartIndex,
            limit: query.Limit,
            totalRecordCount: result.TotalRecordCount,
            viewButton: true,
            showLimit: false,
            viewPanelClass: 'channelViewPanel',
            updatePageSizeSetting: false,
            viewIcon: 'filter-list'

        }));

        updateFilterControls(viewPanel);

        var html = getChannelsHtml(result.Items);

        var elem = page.querySelector('#items');
        elem.innerHTML = html;
        ImageLoader.lazyChildren(elem);

        $('.btnNextPage', page).on('click', function () {
            query.StartIndex += query.Limit;
            reloadItems(page, viewPanel);
        });

        $('.btnPreviousPage', page).on('click', function () {
            query.StartIndex -= query.Limit;
            reloadItems(page, viewPanel);
        });

        LibraryBrowser.saveQueryValues(getSavedQueryKey(), query);
    }

    function reloadItems(page, viewPanel) {

        Dashboard.showLoadingMsg();

        var query = getQuery();

        query.UserId = Dashboard.getCurrentUserId();

        ApiClient.getLiveTvChannels(query).then(function (result) {

            renderChannels(page, viewPanel, result);

            Dashboard.hideLoadingMsg();

            LibraryBrowser.setLastRefreshed(page);
        });
    }

    function updateFilterControls(page) {

        var query = getQuery();
        $('.chkFavorite', page).checked(query.IsFavorite == true);
        $('.chkLikes', page).checked(query.IsLiked == true);
        $('.chkDislikes', page).checked(query.IsDisliked == true);
    }

    window.LiveTvPage.initChannelsTab = function (page, tabContent) {

        var viewPanel = page.querySelector('.channelViewPanel');

        $('.chkFavorite', viewPanel).on('change', function () {

            var query = getQuery();
            query.StartIndex = 0;
            query.IsFavorite = this.checked ? true : null;

            reloadItems(tabContent, viewPanel);
        });


        $('.chkLikes', viewPanel).on('change', function () {

            var query = getQuery();
            query.StartIndex = 0;
            query.IsLiked = this.checked ? true : null;

            reloadItems(tabContent, viewPanel);
        });

        $('.chkDislikes', viewPanel).on('change', function () {

            var query = getQuery();
            query.StartIndex = 0;
            query.IsDisliked = this.checked ? true : null;

            reloadItems(tabContent, viewPanel);
        });
    };

    window.LiveTvPage.renderChannelsTab = function (page, tabContent) {

        var viewPanel = page.querySelector('.channelViewPanel');

        if (LibraryBrowser.needsRefresh(tabContent)) {
            reloadItems(tabContent, viewPanel);
            updateFilterControls(viewPanel);
        }
    };

})(jQuery, document);