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

            if (items.length) {
                $('.noItemsMessage', page).hide();
            } else {
                $('.noItemsMessage', page).show();
            }

            $('#upcomingItems', page).html(LibraryBrowser.getPosterViewHtml({
                items: items,
                showLocationTypeIndicator: false,
                shape: "backdrop",
                showTitle: true,
                showPremiereDate: true,
                showPremiereDateIndex: true,
                preferThumb: true,
                context: 'home-upcoming',
                lazy: true

            })).trigger('create').createPosterItemMenus();
        });
    });


})(jQuery, document);