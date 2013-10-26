(function ($, document) {

    $(document).on('pagebeforeshow', "#tvUpcomingPage", function () {

        var page = this;

        var now = new Date();

        var options = {

            SortBy: "PremiereDate,AirTime",
            SortOrder: "Ascending",
            IncludeItemTypes: "Episode",
            Limit: 30,
            Recursive: true,
            Fields: "PrimaryImageAspectRatio,SeriesInfo,UserData",
            IsUnaired: true
        };

        ApiClient.getItems(Dashboard.getCurrentUserId(), options).done(function (result) {

            if (!result.Items.length) {
                $('#upcomingItems', page).html("<p>Nothing here. Please ensure <a href='metadata.html'>downloading of internet metadata</a> is enabled.</p>").trigger('create');
                return;
            }
            $('#upcomingItems', page).html(LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                useAverageAspectRatio: true,
                showLocationTypeIndicator: false,
                showNewIndicator: false,
                shape: "backdrop",
                showTitle: true,
                showParentTitle: true,
                showPremiereDate: true,
                showPremiereDateIndex: true,
                preferThumb: true
            }));

        });
    });


})(jQuery, document);