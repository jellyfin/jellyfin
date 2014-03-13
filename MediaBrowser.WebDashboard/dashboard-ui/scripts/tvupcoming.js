(function ($, document) {

    $(document).on('pagebeforeshow', "#tvUpcomingPage", function () {

        var page = this;

        var query = {

            Limit: 32,
            Fields: "SeriesInfo,UserData",
            UserId: Dashboard.getCurrentUserId()
        };

        $.getJSON(ApiClient.getUrl("Shows/Upcoming", query)).done(function (result) {

            var items = result.Items;

            if (!items.length) {
                $('#upcomingItems', page).html("<p>Nothing here. Please ensure <a href='metadata.html'>downloading of internet metadata</a> is enabled.</p>").trigger('create');
                return;
            }

            $('#upcomingItems', page).html(LibraryBrowser.getPosterViewHtml({
                items: items,
                showLocationTypeIndicator: false,
                shape: "backdrop",
                showTitle: true,
                showPremiereDate: true,
                showPremiereDateIndex: true,
                preferThumb: true

            })).createPosterItemHoverMenu();
        });
    });


})(jQuery, document);