(function ($, document) {

    $(document).on('pagebeforeshow', "#musicRecommendedPage", function () {

        var page = this;

        var options = {

            SortBy: "DateCreated",
            SortOrder: "Descending",
            IncludeItemTypes: "MusicAlbum",
            Limit: 5,
            Recursive: true,
            Fields: "PrimaryImageAspectRatio"
        };

        ApiClient.getItems(Dashboard.getCurrentUserId(), options).done(function (result) {

            $('#recentlyAddedAlbums', page).html(LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                useAverageAspectRatio: true,
                showUnplayedIndicator: false,
                shape: "square",
                showTitle: true,
                showParentTitle: true
            })).createPosterItemHoverMenu();

        });

        options = {

            SortBy: "DateCreated",
            SortOrder: "Descending",
            IncludeItemTypes: "Audio",
            Limit: 5,
            Recursive: true,
            Fields: "PrimaryImageAspectRatio,AudioInfo"
        };

        ApiClient.getItems(Dashboard.getCurrentUserId(), options).done(function (result) {

            $('#recentlyAddedSongs', page).html(LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                useAverageAspectRatio: true,
                showUnplayedIndicator: false,
                shape: "square",
                showTitle: true,
                showParentTitle: true
            })).createPosterItemHoverMenu();

        });

        options = {

            SortBy: "DatePlayed",
            SortOrder: "Descending",
            IncludeItemTypes: "Audio",
            Limit: 5,
            Recursive: true,
            Fields: "PrimaryImageAspectRatio,AudioInfo",
            Filters: "IsPlayed"
        };

        ApiClient.getItems(Dashboard.getCurrentUserId(), options).done(function (result) {

            if (result.Items.length) {
                $('#recentlyPlayed', page).show();
            } else {
                $('#recentlyPlayed', page).hide();
            }

            $('#recentlyPlayedSongs', page).html(LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                useAverageAspectRatio: true,
                showUnplayedIndicator: false,
                shape: "square",
                showTitle: true,
                showParentTitle: true
            })).createPosterItemHoverMenu();

        });

        options = {

            SortBy: "PlayCount",
            SortOrder: "Descending",
            IncludeItemTypes: "Audio",
            Limit: 10,
            Recursive: true,
            Fields: "PrimaryImageAspectRatio,AudioInfo",
            Filters: "IsPlayed"
        };

        ApiClient.getItems(Dashboard.getCurrentUserId(), options).done(function (result) {

            if (result.Items.length) {
                $('#topPlayed', page).show();
            } else {
                $('#topPlayed', page).hide();
            }

            $('#topPlayedSongs', page).html(LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                useAverageAspectRatio: true,
                showUnplayedIndicator: false,
                shape: "square",
                showTitle: true,
                showParentTitle: true
            })).createPosterItemHoverMenu();

        });

    });


})(jQuery, document);