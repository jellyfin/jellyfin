(function ($, document) {

    $(document).on('pagebeforeshow', "#gamesRecommendedPage", function () {

        var page = this;

        var options = {

            SortBy: "DateCreated",
            SortOrder: "Descending",
            MediaTypes: "Game",
            Limit: 5,
            Recursive: true,
            Fields: "PrimaryImageAspectRatio",
            Filters: "IsUnplayed"
        };

        ApiClient.getItems(Dashboard.getCurrentUserId(), options).done(function (result) {

            $('#recentlyAddedItems', page).html(LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                useAverageAspectRatio: true,
                showNewIndicator: false,
                transparent: true,
                borderless: true,
                imagePosition: 'center center'
            }));

        });

        options = {

            SortBy: "DatePlayed",
            SortOrder: "Descending",
            MediaTypes: "Game",
            Limit: 5,
            Recursive: true,
            Fields: "PrimaryImageAspectRatio",
            Filters: "IsPlayed"
        };

        ApiClient.getItems(Dashboard.getCurrentUserId(), options).done(function (result) {

            if (result.Items.length) {
                $('#recentlyPlayedSection', page).show();
            } else {
                $('#recentlyPlayedSection', page).hide();
            }

            $('#recentlyPlayedItems', page).html(LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                useAverageAspectRatio: true,
                transparent: true,
                borderless: true,
                imagePosition: 'center center'
            }));

        });

        options = {

            SortBy: "PlayCount",
            SortOrder: "Descending",
            MediaTypes: "Game",
            Limit: 5,
            Recursive: true,
            Fields: "PrimaryImageAspectRatio",
            Filters: "IsPlayed"
        };

        ApiClient.getItems(Dashboard.getCurrentUserId(), options).done(function (result) {

            if (result.Items.length) {
                $('#frequentlyPlayedSection', page).show();
            } else {
                $('#frequentlyPlayedSection', page).hide();
            }

            $('#frequentlyPlayedItems', page).html(LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                useAverageAspectRatio: true,
                transparent: true,
                borderless: true,
                imagePosition: 'center center'
            }));

        });

    });

})(jQuery, document);