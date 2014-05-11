(function ($, document) {

    $(document).on('pagebeforeshow', "#musicRecommendedPage", function () {

        var screenWidth = $(window).width();

        var page = this;

        var parentId = LibraryMenu.getTopParentId();

        var options = {

            SortBy: "DateCreated",
            SortOrder: "Descending",
            IncludeItemTypes: "MusicAlbum",
            Limit: screenWidth >= 1920 ? 8 : (screenWidth >= 1440 ? 8 : 5),
            Recursive: true,
            Fields: "PrimaryImageAspectRatio",
            ParentId: parentId
        };

        ApiClient.getItems(Dashboard.getCurrentUserId(), options).done(function (result) {

            $('#recentlyAddedAlbums', page).html(LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                showUnplayedIndicator: false,
                shape: "square",
                showTitle: true,
                showParentTitle: true
            })).createPosterItemMenus();

        });

        options = {

            SortBy: "DateCreated",
            SortOrder: "Descending",
            IncludeItemTypes: "Audio",
            Limit: screenWidth >= 1920 ? 8 : (screenWidth >= 1440 ? 8 : 5),
            Recursive: true,
            Fields: "PrimaryImageAspectRatio,AudioInfo",
            ParentId: parentId
        };

        ApiClient.getItems(Dashboard.getCurrentUserId(), options).done(function (result) {

            $('#recentlyAddedSongs', page).html(LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                showUnplayedIndicator: false,
                shape: "square",
                showTitle: true,
                showParentTitle: true
            })).createPosterItemMenus();

        });

        options = {

            SortBy: "DatePlayed",
            SortOrder: "Descending",
            IncludeItemTypes: "Audio",
            Limit: screenWidth >= 1920 ? 8 : (screenWidth >= 1440 ? 8 : 5),
            Recursive: true,
            Fields: "PrimaryImageAspectRatio,AudioInfo",
            Filters: "IsPlayed",
            ParentId: parentId
        };

        ApiClient.getItems(Dashboard.getCurrentUserId(), options).done(function (result) {

            if (result.Items.length) {
                $('#recentlyPlayed', page).show();
            } else {
                $('#recentlyPlayed', page).hide();
            }

            $('#recentlyPlayedSongs', page).html(LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                showUnplayedIndicator: false,
                shape: "square",
                showTitle: true,
                showParentTitle: true
            })).createPosterItemMenus();

        });

        options = {

            SortBy: "PlayCount",
            SortOrder: "Descending",
            IncludeItemTypes: "Audio",
            Limit: screenWidth >= 1920 ? 14 : (screenWidth >= 1440 ? 14 : 10),
            Recursive: true,
            Fields: "PrimaryImageAspectRatio,AudioInfo",
            Filters: "IsPlayed",
            ParentId: parentId
        };

        ApiClient.getItems(Dashboard.getCurrentUserId(), options).done(function (result) {

            if (result.Items.length) {
                $('#topPlayed', page).show();
            } else {
                $('#topPlayed', page).hide();
            }

            $('#topPlayedSongs', page).html(LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                showUnplayedIndicator: false,
                shape: "square",
                showTitle: true,
                showParentTitle: true
            })).createPosterItemMenus();

        });

    });


})(jQuery, document);