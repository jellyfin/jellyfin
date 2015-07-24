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

    function renderChannels(page, result) {

        $('.listTopPaging', page).html(LibraryBrowser.getQueryPagingHtml({
            startIndex: query.StartIndex,
            limit: query.Limit,
            totalRecordCount: result.TotalRecordCount,
            viewButton: true,
            showLimit: false
        })).trigger('create');

        updateFilterControls(this);

        var html = getChannelsHtml(result.Items);

        var elem = page.querySelector('#items');
        elem.innerHTML = html;
        ImageLoader.lazyChildren(elem);
        $(elem).trigger('create');

        $('.btnNextPage', page).on('click', function () {
            query.StartIndex += query.Limit;
            reloadItems(page);
        });

        $('.btnPreviousPage', page).on('click', function () {
            query.StartIndex -= query.Limit;
            reloadItems(page);
        });

        LibraryBrowser.saveQueryValues('movies', query);
    }
    
    function reloadItems(page) {

        showLoadingMessage(page);
        
        ApiClient.getLiveTvChannels(query).done(function (result) {

            renderChannels(page, result);

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

    $(document).on('pageinitdepends', "#liveTvChannelsPage", function () {

        var page = this;

        $('#chkFavorite', this).on('change', function () {

            query.StartIndex = 0;
            query.IsFavorite = this.checked ? true : null;

            reloadItems(page);
        });


        $('#chkLikes', this).on('change', function () {

            query.StartIndex = 0;
            query.IsLiked = this.checked ? true : null;

            reloadItems(page);
        });

        $('#chkDislikes', this).on('change', function () {

            query.StartIndex = 0;
            query.IsDisliked = this.checked ? true : null;

            reloadItems(page);
        });

        $('#selectPageSize', page).on('change', function () {
            query.Limit = parseInt(this.value);
            query.StartIndex = 0;
            reloadItems(page);
        });

    }).on('pagebeforeshowready', "#liveTvChannelsPage", function () {

        // Can't use pagebeforeshow here or the loading popup won't center correctly
        var page = this;

        if (LibraryBrowser.needsRefresh(page)) {
            query.UserId = Dashboard.getCurrentUserId();
            LibraryBrowser.loadSavedQueryValues('movies', query);
            query.Limit = query.Limit || LibraryBrowser.getDefaultPageSize();
            reloadItems(page);
            updateFilterControls(this);
        }
        
    });

})(jQuery, document);