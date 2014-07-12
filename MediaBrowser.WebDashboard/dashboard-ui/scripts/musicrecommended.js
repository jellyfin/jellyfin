(function ($, document) {

    $(document).on('pagebeforeshow', "#musicRecommendedPage", function () {

        var screenWidth = $(window).width();
        var userId = Dashboard.getCurrentUserId();

        var page = this;

        var parentId = LibraryMenu.getTopParentId();

        var options = {
            IncludeItemTypes: "Audio",
            Limit: screenWidth >= 1920 ? 15 : (screenWidth >= 1200 ? 10 : 12),
            Fields: "PrimaryImageAspectRatio",
            ParentId: parentId
        };

        ApiClient.getJSON(ApiClient.getUrl('Users/' + userId + '/Items/Latest', options)).done(function (items) {

            $('#recentlyAddedSongs', page).html(LibraryBrowser.getPosterViewHtml({
                items: items,
                showUnplayedIndicator: false,
                showChildCountIndicator: true,
                shape: "square",
                showTitle: true,
                showParentTitle: true
            })).createPosterItemMenus();

        });

        options = {

            SortBy: "DatePlayed",
            SortOrder: "Descending",
            IncludeItemTypes: "Audio",
            Limit: screenWidth >= 1920 ? 5 : (screenWidth >= 1200 ? 5 : 6),
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
            Limit: screenWidth >= 1920 ? 10 : (screenWidth >= 1200 ? 10 : 12),
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