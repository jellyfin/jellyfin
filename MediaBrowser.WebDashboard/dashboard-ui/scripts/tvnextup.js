(function ($, document) {

    $(document).on('pagebeforeshow', "#tvNextUpPage", function () {

        var page = this;

        var options = {

            Limit: 24,
            Fields: "PrimaryImageAspectRatio,SeriesInfo,DateCreated",
            UserId: Dashboard.getCurrentUserId(),
            ExcludeLocationTypes: "Virtual"
        };

        ApiClient.getNextUpEpisodes(options).done(function (result) {

            if (result.Items.length) {
                $('#resumableSection', page).show();
            } else {
                $('#resumableSection', page).hide();
            }

            if (result.Items.length) {

                $('#nextUpItems', page).html(LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    useAverageAspectRatio: true,
                    shape: "backdrop",
                    showTitle: true,
                    showParentTitle: true,
                    overlayText: true

                }));

            } else {

                $('#nextUpItems', page).html('<br/><p style="text-align:center;">None found. Start watching your shows!</p>');

            }

        });

    });


})(jQuery, document);