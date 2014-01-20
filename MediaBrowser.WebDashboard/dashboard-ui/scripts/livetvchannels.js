(function ($, document, apiClient) {

    var query = {

        StartIndex: 0
    };

    function getChannelsHtml(channels) {

        return LibraryBrowser.getPosterViewHtml({
            items: channels,
            useAverageAspectRatio: true,
            shape: "smallBackdrop",
            centerText: true
        });
    }

    function renderChannels(page, result) {

        $('.listTopPaging', page).html(LibraryBrowser.getPagingHtml(query, result.TotalRecordCount, true)).trigger('create');

        var html = getChannelsHtml(result.Items);
        
        html += LibraryBrowser.getPagingHtml(query, result.TotalRecordCount);

        $('#items', page).html(html).trigger('create');

        $('.selectPage', page).on('change', function () {
            query.StartIndex = (parseInt(this.value) - 1) * query.Limit;
            reloadItems(page);
        });

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
        apiClient.getLiveTvChannels(query).done(function (result) {

            renderChannels(page, result);
        });
    }

    $(document).on('pagebeforeshow', "#liveTvChannelsPage", function () {

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
    });

})(jQuery, document, ApiClient);