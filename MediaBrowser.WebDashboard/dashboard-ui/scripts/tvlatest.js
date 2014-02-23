(function ($, document) {

    $(document).on('pagebeforeshow', "#tvNextUpPage", function () {

        var screenWidth = $(window).width();

        var page = this;

        var options = {

            SortBy: "DateCreated",
            SortOrder: "Descending",
            IncludeItemTypes: "Episode",
            Limit: screenWidth >= 1920 ? 24 : (screenWidth >= 1440 ? 16 : 15),
            Recursive: true,
            Fields: "PrimaryImageAspectRatio,SeriesInfo,UserData",
            Filters: "IsUnplayed",
            ExcludeLocationTypes: "Virtual"
        };

        ApiClient.getItems(Dashboard.getCurrentUserId(), options).done(function (result) {

            $('#latestEpisodes', page).html(LibraryBrowser.getPosterViewHtml({
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