(function ($, document) {

    $(document).on('pagebeforeshow', "#tvRecommendedPage", function () {

        var page = this;

        var options = {

            SortBy: "DateCreated",
            SortOrder: "Descending",
            IncludeItemTypes: "Episode",
            Limit: 15,
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
            Limit: 3,
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