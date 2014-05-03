(function ($, document) {

    // The base query options
    var query = {

        StartIndex: 0
    };

    function reloadItems(page) {

        Dashboard.showLoadingMsg();

        query.UserId = Dashboard.getCurrentUserId();

        $.getJSON(ApiClient.getUrl("Channels", query)).done(function (result) {

            // Scroll back up so they can see the results from the beginning
            $(document).scrollTop(0);

            var html = '';

            $('.listTopPaging', page).html(LibraryBrowser.getPagingHtml(query, result.TotalRecordCount, true)).trigger('create');

            updateFilterControls(page);

            html = LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                shape: "backdrop",
                context: 'channels',
                showTitle: true,
                centerText: true,
                preferThumb: true
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

            LibraryBrowser.saveQueryValues('channels', query);

            Dashboard.hideLoadingMsg();
        });
    }

    function updateFilterControls(page) {

    }

    $(document).on('pagebeforeshow', "#channelsPage", function () {

        var limit = LibraryBrowser.getDefaultPageSize();

        // If the default page size has changed, the start index will have to be reset
        if (limit != query.Limit) {
            query.Limit = limit;
            query.StartIndex = 0;
        }

        LibraryBrowser.loadSavedQueryValues('channels', query);

        reloadItems(this);

    }).on('pageshow', "#channelsPage", function () {

        updateFilterControls(this);
    });

})(jQuery, document);