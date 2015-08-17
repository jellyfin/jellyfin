(function ($, document) {

    var query = {

        StartIndex: 0,
        EnableFavoriteSorting: true
    };

    function getChannelsHtml(channels) {

        return LibraryBrowser.getListViewHtml({
            items: channels,
            smallIcon: true
        });
    }

    function showLoadingMessage(page) {

        Dashboard.showLoadingMsg();
    }

    function hideLoadingMessage(page) {
        Dashboard.hideLoadingMsg();
    }

    function renderChannels(page, viewPanel, result) {

        $('.listTopPaging', page).html(LibraryBrowser.getQueryPagingHtml({
            startIndex: query.StartIndex,
            limit: query.Limit,
            totalRecordCount: result.TotalRecordCount,
            viewButton: true,
            showLimit: false,
            viewPanelClass: 'channelViewPanel'

        })).trigger('create');

        updateFilterControls(viewPanel);

        var html = getChannelsHtml(result.Items);

        var elem = page.querySelector('#items');
        elem.innerHTML = html;
        ImageLoader.lazyChildren(elem);
        $(elem).trigger('create');

        $('.btnNextPage', page).on('click', function () {
            query.StartIndex += query.Limit;
            reloadItems(page, viewPanel);
        });

        $('.btnPreviousPage', page).on('click', function () {
            query.StartIndex -= query.Limit;
            reloadItems(page, viewPanel);
        });

        LibraryBrowser.saveQueryValues('movies', query);
    }
    
    function reloadItems(page, viewPanel) {

        showLoadingMessage(page);
        
        ApiClient.getLiveTvChannels(query).done(function (result) {

            renderChannels(page, viewPanel, result);

            hideLoadingMessage(page);

            LibraryBrowser.setLastRefreshed(page);
        });
    }

    function updateFilterControls(page) {

        $('#chkFavorite', page).checked(query.IsFavorite == true).checkboxradio('refresh');
        $('#chkLikes', page).checked(query.IsLiked == true).checkboxradio('refresh');
        $('#chkDislikes', page).checked(query.IsDisliked == true).checkboxradio('refresh');
        $('#selectPageSize', page).val(query.Limit).selectmenu('refresh');
    }

    $(document).on('pageinitdepends', "#liveTvSuggestedPage", function () {

        var page = this.querySelector('.channelsTabContent');
        var viewPanel = this.querySelector('.channelViewPanel');

        $('#chkFavorite', viewPanel).on('change', function () {

            query.StartIndex = 0;
            query.IsFavorite = this.checked ? true : null;

            reloadItems(page, viewPanel);
        });


        $('#chkLikes', viewPanel).on('change', function () {

            query.StartIndex = 0;
            query.IsLiked = this.checked ? true : null;

            reloadItems(page, viewPanel);
        });

        $('#chkDislikes', viewPanel).on('change', function () {

            query.StartIndex = 0;
            query.IsDisliked = this.checked ? true : null;

            reloadItems(page, viewPanel);
        });

        $('#selectPageSize', viewPanel).on('change', function () {
            query.Limit = parseInt(this.value);
            query.StartIndex = 0;
            reloadItems(page, viewPanel);
        });

    });

    $(document).on('pageinitdepends', "#liveTvSuggestedPage", function () {

        var page = this;

        $(page.querySelector('neon-animated-pages')).on('tabchange', function () {

            if (parseInt(this.selected) == 2) {
                var tabContent = page.querySelector('.channelsTabContent');
                var viewPanel = page.querySelector('.channelViewPanel');

                if (LibraryBrowser.needsRefresh(tabContent)) {
                    query.UserId = Dashboard.getCurrentUserId();
                    LibraryBrowser.loadSavedQueryValues('movies', query);
                    query.Limit = query.Limit || LibraryBrowser.getDefaultPageSize();
                    reloadItems(tabContent, viewPanel);
                    updateFilterControls(viewPanel);
                }
            }
        });

    });

})(jQuery, document);