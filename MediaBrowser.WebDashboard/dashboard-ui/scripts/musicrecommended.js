(function ($, document) {

    function loadLatest(page, parentId) {

        var userId = Dashboard.getCurrentUserId();

        var options = {
            IncludeItemTypes: "Audio",
            Limit: 9,
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
                lazy: true,
                cardLayout: true

            })).lazyChildren();

        });
    }

    function loadRecentlyPlayed(page, parentId) {

        var options = {

            SortBy: "DatePlayed",
            SortOrder: "Descending",
            IncludeItemTypes: "Audio",
            Limit: 9,
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
                lazy: true,
                cardLayout: true

            })).lazyChildren();

        });

    }

    function loadFrequentlyPlayed(page, parentId) {

        var options = {

            SortBy: "PlayCount",
            SortOrder: "Descending",
            IncludeItemTypes: "Audio",
            Limit: 9,
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
                lazy: true,
                cardLayout: true

            })).lazyChildren();

        });

    }

    function loadPlaylists(page, parentId) {

        var options = {

            SortBy: "SortName",
            SortOrder: "Ascending",
            IncludeItemTypes: "Playlist",
            Recursive: true,
            ParentId: parentId,
            Fields: "PrimaryImageAspectRatio,SortName,CumulativeRunTimeTicks,CanDelete,SyncInfo",
            StartIndex: 0,
            Limit: 9
        };

        ApiClient.getItems(Dashboard.getCurrentUserId(), options).done(function (result) {

            var elem;

            if (result.Items.length) {
                elem = $('#playlists', page).show();
            } else {
                elem = $('#playlists', page).hide();
            }

            $('.itemsContainer', elem).html(LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                shape: "square",
                showTitle: true,
                lazy: true,
                coverImage: true,
                showItemCounts: true,
                cardLayout: true

            })).lazyChildren();

        });

    }

    $(document).on('pagebeforeshow', "#musicRecommendedPage", function () {

        var parentId = LibraryMenu.getTopParentId();

        var page = this;

        loadLatest(page, parentId);
        loadPlaylists(page, parentId);
        loadRecentlyPlayed(page, parentId);
        loadFrequentlyPlayed(page, parentId);
    });


})(jQuery, document);