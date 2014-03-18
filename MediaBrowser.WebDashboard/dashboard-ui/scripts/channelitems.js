(function ($, document) {

    // The base query options
    var query = {

        StartIndex: 0
    };

    function getSavedQueryId() {
        return 'channels-' + getParameterByName('id');
    }

    function reloadItems(page) {

        Dashboard.showLoadingMsg();

        var channelId = getParameterByName('id');

        query.UserId = Dashboard.getCurrentUserId();

        $.getJSON(ApiClient.getUrl("Channels/" + channelId + "/Items", query)).done(function (result) {

            // Scroll back up so they can see the results from the beginning
            $(document).scrollTop(0);

            var html = '';

            $('.listTopPaging', page).html(LibraryBrowser.getPagingHtml(query, result.TotalRecordCount, true)).trigger('create');

            updateFilterControls(page);

            html = LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                shape: "portrait",
                context: 'channels',
                useAverageAspectRatio: true,
                showTitle: true,
                centerText: true
            });

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

            LibraryBrowser.saveQueryValues(getSavedQueryId(), query);

            Dashboard.hideLoadingMsg();
        });
    }

    function updateFilterControls(page) {

    }

    $(document).on('pagebeforeshow', "#channelItemsPage", function () {

        var limit = LibraryBrowser.getDefaultPageSize();

        // If the default page size has changed, the start index will have to be reset
        if (limit != query.Limit) {
            query.Limit = limit;
            query.StartIndex = 0;
        }

        LibraryBrowser.loadSavedQueryValues(getSavedQueryId(), query);

        reloadItems(this);

    }).on('pageshow', "#channelItemsPage", function () {

        updateFilterControls(this);
    });

})(jQuery, document);