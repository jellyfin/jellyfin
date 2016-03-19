define(['jQuery'], function ($) {

    $(document).on('pagebeforeshow', "#gamesRecommendedPage", function () {

        var parentId = LibraryMenu.getTopParentId();
        var userId = Dashboard.getCurrentUserId();

        var page = this;

        var options = {

            IncludeItemTypes: "Game",
            Limit: 18,
            Fields: "PrimaryImageAspectRatio",
            ParentId: parentId,
            ImageTypeLimit: 1,
            EnableImageTypes: "Primary,Backdrop,Banner,Thumb"
        };

        ApiClient.getJSON(ApiClient.getUrl('Users/' + userId + '/Items/Latest', options)).then(function (items) {

            var recentlyAddedItems = page.querySelector('#recentlyAddedItems');
            recentlyAddedItems.innerHTML = LibraryBrowser.getPosterViewHtml({
                items: items,
                transparent: true,
                borderless: true,
                shape: 'auto',
                lazy: true

            });
            ImageLoader.lazyChildren(recentlyAddedItems);

        });

        options = {

            SortBy: "DatePlayed",
            SortOrder: "Descending",
            MediaTypes: "Game",
            Limit: 18,
            Recursive: true,
            Filters: "IsPlayed",
            Fields: "ItemCounts,AudioInfo,PrimaryImageAspectRatio",
            ParentId: parentId,
            ImageTypeLimit: 1,
            EnableImageTypes: "Primary,Backdrop,Banner,Thumb"
        };

        ApiClient.getItems(userId, options).then(function (result) {

            if (result.Items.length) {
                $('#recentlyPlayedSection', page).show();
            } else {
                $('#recentlyPlayedSection', page).hide();
            }

            var recentlyPlayedItems = page.querySelector('#recentlyPlayedItems');
            recentlyPlayedItems.innerHTML = LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                transparent: true,
                borderless: true,
                shape: 'auto',
                lazy: true

            });
            ImageLoader.lazyChildren(recentlyPlayedItems);
        });

    });

});