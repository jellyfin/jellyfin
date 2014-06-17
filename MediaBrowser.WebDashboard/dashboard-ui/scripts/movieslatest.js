(function ($, document) {

    $(document).on('pagebeforeshow', "#moviesLatestPage", function () {

        var parentId = LibraryMenu.getTopParentId();

        var screenWidth = $(window).width();

        var page = this;

        var options = {

            SortBy: "DateCreated",
            SortOrder: "Descending",
            IncludeItemTypes: "Movie",
            Limit: screenWidth >= 1920 ? 32 : (screenWidth >= 1440 ? 32 : (screenWidth >= 800 ? 28 : 18)),
            Recursive: true,
            Fields: "PrimaryImageAspectRatio",
            Filters: "IsUnplayed",
            CollapseBoxSetItems: false,
            ParentId: parentId
        };

        ApiClient.getItems(Dashboard.getCurrentUserId(), options).done(function (result) {

            $('#recentlyAddedItems', page).html(LibraryBrowser.getPosterViewHtml({
                items: result.Items

            })).createPosterItemMenus();
        });

    });


})(jQuery, document);