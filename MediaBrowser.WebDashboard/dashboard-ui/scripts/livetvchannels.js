(function ($, document, apiClient) {

    var query = {

        StartIndex: 0
    };

    function getChannelsHtml(channels) {

        return LibraryBrowser.getPosterViewHtml({
            items: channels,
            shape: "smallBackdrop",
            centerText: true
        });
    }

    function showLoadingMessage(page) {

        $('.popupLoading', page).popup('open');
    }

    function hideLoadingMessage(page) {
        $('.popupLoading', page).popup('close');
    }

    function renderChannels(page, result) {

        $('.listTopPaging', page).html(LibraryBrowser.getPagingHtml(query, result.TotalRecordCount, true)).trigger('create');

        updateFilterControls(this);

        var html = getChannelsHtml(result.Items);
        
        html += LibraryBrowser.getPagingHtml(query, result.TotalRecordCount);

        $('#items', page).html(html).trigger('create').createPosterItemMenus();

        $('.btnNextPage', page).on('click', function () {
            query.StartIndex += query.Limit;
            reloadItems(page);
        });

        $('.btnPreviousPage', page).on('click', function () {
            query.StartIndex -= query.Limit;
            reloadItems(page);
        });

        $('.selectPageSize', page).on('change', function () {
            query.Limit = parseInt(this.value);
            query.StartIndex = 0;
            reloadItems(page);
        });

        LibraryBrowser.saveQueryValues('movies', query);
    }
    
    function reloadItems(page) {

        showLoadingMessage(page);
        
        apiClient.getLiveTvChannels(query).done(function (result) {

            renderChannels(page, result);

            hideLoadingMessage(page);
        });
    }

    function updateFilterControls(page) {

        $('#chkFavorite', page).checked(query.IsFavorite == true).checkboxradio('refresh');
        $('#chkLikes', page).checked(query.IsLiked == true).checkboxradio('refresh');
        $('#chkDislikes', page).checked(query.IsDisliked == true).checkboxradio('refresh');
    }

    $(document).on('pageinit', "#liveTvChannelsPage", function () {

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

    }).on('pageshow', "#liveTvChannelsPage", function () {

        // Can't use pagebeforeshow here or the loading popup won't center correctly
        var page = this;

        var limit = LibraryBrowser.getDefaultPageSize();

        // If the default page size has changed, the start index will have to be reset
        if (limit != query.Limit) {
            query.Limit = limit;
            query.StartIndex = 0;
        }

        query.UserId = Dashboard.getCurrentUserId();

        LibraryBrowser.loadSavedQueryValues('movies', query);

        reloadItems(page);
        
        updateFilterControls(this);
        
    });

})(jQuery, document, ApiClient);