(function ($, document) {

    $(document).on('pagebeforeshow', "#gamesRecommendedPage", function () {

        var parentId = LibraryMenu.getTopParentId();
        var userId = Dashboard.getCurrentUserId();

        var page = this;

        var options = {

            IncludeItemTypes: "Game",
            Limit: 8,
            Fields: "PrimaryImageAspectRatio",
            ParentId: parentId
        };

        ApiClient.getJSON(ApiClient.getUrl('Users/' + userId + '/Items/Latest', options)).done(function (items) {

            $('#recentlyAddedItems', page).html(LibraryBrowser.getPosterViewHtml({
                items: items,
                transparent: true,
                borderless: true,
                shape: 'auto',
                lazy: true

            })).trigger('create').createPosterItemMenus();

        });

        options = {

            SortBy: "DatePlayed",
            SortOrder: "Descending",
            MediaTypes: "Game",
            Limit: 8,
            Recursive: true,
            Filters: "IsPlayed",
            Fields: "ItemCounts,AudioInfo,PrimaryImageAspectRatio",
            ParentId: parentId
        };

        ApiClient.getItems(userId, options).done(function (result) {

            if (result.Items.length) {
                $('#recentlyPlayedSection', page).show();
            } else {
                $('#recentlyPlayedSection', page).hide();
            }

            $('#recentlyPlayedItems', page).html(LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                transparent: true,
                borderless: true,
                shape: 'auto',
                lazy: true

            })).trigger('create').createPosterItemMenus();

        });

    });

})(jQuery, document);