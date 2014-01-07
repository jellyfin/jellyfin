(function ($, document, apiClient) {

    function getChannelsHtml(channels) {

        return LibraryBrowser.getPosterViewHtml({
            items: channels,
            useAverageAspectRatio: true,
            shape: "backdrop",
            centerText: true
        });
    }

    function renderChannels(page, channels) {

        $('#items', page).html(getChannelsHtml(channels)).trigger('create');
    }

    $(document).on('pagebeforeshow', "#liveTvChannelsPage", function () {

        var page = this;

        apiClient.getLiveTvChannels({
            
            userId: Dashboard.getCurrentUserId()

        }).done(function (result) {

            renderChannels(page, result.Items);
        });
    });

})(jQuery, document, ApiClient);