(function ($, document) {

    $(document).on('pagebeforeshow', "#musicRecommendedPage", function () {

        var userId = Dashboard.getCurrentUserId();

        var page = this;

        var parentId = LibraryMenu.getTopParentId();

        var options = {
            IncludeItemTypes: "Audio",
            Limit: 20,
            Fields: "PrimaryImageAspectRatio,SyncInfo",
            ParentId: parentId,
            ImageTypeLimit: 1,
            EnableImageTypes: "Primary,Backdrop,Banner,Thumb"
        };

        ApiClient.getJSON(ApiClient.getUrl('Users/' + userId + '/Items/Latest', options)).done(function (items) {

            $('#recentlyAddedSongs', page).html(LibraryBrowser.getPosterViewHtml({
                items: items,
                showUnplayedIndicator: false,
                showChildCountIndicator: true,
                shape: "square",
                showTitle: true,
                showParentTitle: true,
                lazy: true
            })).lazyChildren();

        });

        options = {

            SortBy: "DatePlayed",
            SortOrder: "Descending",
            IncludeItemTypes: "Audio",
            Limit: 10,
            Recursive: true,
            Fields: "PrimaryImageAspectRatio,AudioInfo,SyncInfo",
            Filters: "IsPlayed",
            ParentId: parentId,
            ImageTypeLimit: 1,
            EnableImageTypes: "Primary,Backdrop,Banner,Thumb"
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
                showParentTitle: true,
                defaultAction: 'play',
                lazy: true

            })).lazyChildren();

        });

        options = {

            SortBy: "PlayCount",
            SortOrder: "Descending",
            IncludeItemTypes: "Audio",
            Limit: 20,
            Recursive: true,
            Fields: "PrimaryImageAspectRatio,AudioInfo,SyncInfo",
            Filters: "IsPlayed",
            ParentId: parentId,
            ImageTypeLimit: 1,
            EnableImageTypes: "Primary,Backdrop,Banner,Thumb"
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
                showParentTitle: true,
                defaultAction: 'play',
                lazy: true

            })).lazyChildren();

        });

    });


})(jQuery, document);