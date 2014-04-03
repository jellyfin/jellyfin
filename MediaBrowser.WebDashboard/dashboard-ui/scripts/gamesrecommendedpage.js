(function ($, document) {

    $(document).on('pagebeforeshow', "#gamesRecommendedPage", function () {

        var page = this;

        var options = {

            SortBy: "DateCreated",
            SortOrder: "Descending",
            MediaTypes: "Game",
            Limit: 10,
            Recursive: true,
            Fields: "ItemCounts,AudioInfo,PrimaryImageAspectRatio"
        };

        ApiClient.getItems(Dashboard.getCurrentUserId(), options).done(function (result) {

            $('#recentlyAddedItems', page).html(LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                transparent: true,
                borderless: true
            }));

        });

        options = {

            SortBy: "DatePlayed",
            SortOrder: "Descending",
            MediaTypes: "Game",
            Limit: 10,
            Recursive: true,
            Filters: "IsPlayed",
            Fields: "ItemCounts,AudioInfo,PrimaryImageAspectRatio"
        };

        ApiClient.getItems(Dashboard.getCurrentUserId(), options).done(function (result) {

            if (result.Items.length) {
                $('#recentlyPlayedSection', page).show();
            } else {
                $('#recentlyPlayedSection', page).hide();
            }

            $('#recentlyPlayedItems', page).html(LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                transparent: true,
                borderless: true
            }));

        });

    });

})(jQuery, document);