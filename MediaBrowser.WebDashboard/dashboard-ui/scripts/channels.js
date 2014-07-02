(function ($, document) {

    // The base query options
    var query = {

        StartIndex: 0
    };

    function reloadItems(page) {

        Dashboard.showLoadingMsg();

        query.UserId = Dashboard.getCurrentUserId();

        ApiClient.getJSON(ApiClient.getUrl("Channels", query)).done(function (result) {

            // Scroll back up so they can see the results from the beginning
            $(document).scrollTop(0);

            var html = '';

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

            LibraryBrowser.saveQueryValues('channels', query);

            Dashboard.hideLoadingMsg();
        });
    }

    function updateFilterControls(page) {

    }

    $(document).on('pagebeforeshow', "#channelsPage", function () {

        LibraryBrowser.loadSavedQueryValues('channels', query);

        reloadItems(this);

    }).on('pageshow', "#channelsPage", function () {

        updateFilterControls(this);
    });

})(jQuery, document);