(function ($, document) {

    $(document).on('pagebeforeshow', "#tvUpcomingPage", function () {

        var page = this;

        var query = {

            SortBy: "PremiereDate,AirTime,SortName",
            SortOrder: "Ascending",
            IncludeItemTypes: "Episode",
            Limit: 30,
            Recursive: true,
            Fields: "SeriesInfo,UserData"
        };

        var missedItemsQuery = $.extend({

            IsUnaired: false

        }, query);
        
        var yesterday = new Date();

        yesterday.setDate(yesterday.getDate() - 1);
        yesterday.setHours(0, 0, 0, 0);
        
        missedItemsQuery.MinPremiereDate = yesterday.toISOString();

        var unairedQuery = $.extend({

            IsUnaired: true

        }, query);

        var promise1 = ApiClient.getItems(Dashboard.getCurrentUserId(), missedItemsQuery);
        var promise2 = ApiClient.getItems(Dashboard.getCurrentUserId(), unairedQuery);

        $.when(promise1, promise2).done(function (response1, response2) {

            var missedItems = response1[0].Items;
            var unairedItems = response2[0].Items;

            for (var i = 0, length = unairedItems.length; i < length; i++) {
                missedItems.push(unairedItems[i]);
            }

            if (!missedItems.length) {
                $('#upcomingItems', page).html("<p>Nothing here. Please ensure <a href='metadata.html'>downloading of internet metadata</a> is enabled.</p>").trigger('create');
                return;
            }

            $('#upcomingItems', page).html(LibraryBrowser.getPosterViewHtml({
                items: missedItems,
                showLocationTypeIndicator: false,
                shape: "backdrop",
                showTitle: true,
                showPremiereDate: true,
                showPremiereDateIndex: true,
                preferThumb: true
            }));
        });
    });


})(jQuery, document);