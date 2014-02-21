(function ($, document) {

    $(document).on('pagebeforeshow', "#tvRecommendedPage", function () {

        var screenWidth = $(window).width();

        var page = this;

        var options = {

            SortBy: "DateCreated",
            SortOrder: "Descending",
            IncludeItemTypes: "Episode",
            Limit: screenWidth >= 1920 ? 20 : (screenWidth >= 1440 ? 16 : 15),
            Recursive: true,
            Fields: "PrimaryImageAspectRatio,SeriesInfo,UserData",
            Filters: "IsUnplayed",
            ExcludeLocationTypes: "Virtual"
        };

        ApiClient.getItems(Dashboard.getCurrentUserId(), options).done(function (result) {

            $('#recentlyAddedItems', page).html(LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                useAverageAspectRatio: true,
                shape: "backdrop",
                showTitle: true,
                showParentTitle: true,
                overlayText: true
                
            })).createPosterItemHoverMenu();

        });

        options = {

            SortBy: "DatePlayed",
            SortOrder: "Descending",
            IncludeItemTypes: "Episode",
            Filters: "IsResumable",
            Limit: screenWidth >= 1920 ? 4 : (screenWidth >= 1440 ? 4 : 3),
            Recursive: true,
            Fields: "PrimaryImageAspectRatio,SeriesInfo,UserData",
            ExcludeLocationTypes: "Virtual"
        };

        ApiClient.getItems(Dashboard.getCurrentUserId(), options).done(function (result) {

            if (result.Items.length) {
                $('#resumableSection', page).show();
            } else {
                $('#resumableSection', page).hide();
            }

            $('#resumableItems', page).html(LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                useAverageAspectRatio: true,
                shape: "backdrop",
                showTitle: true,
                showParentTitle: true,
                overlayText: true
                
            })).createPosterItemHoverMenu();

        });

    });


})(jQuery, document);