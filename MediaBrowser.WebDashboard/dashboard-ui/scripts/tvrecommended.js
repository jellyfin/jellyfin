(function ($, document) {

    function loadResume(page) {

        var screenWidth = $(window).width();

        var options = {

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
    }

    function loadNextUp(page) {

        var options = {

            Limit: 24,
            Fields: "PrimaryImageAspectRatio,SeriesInfo,DateCreated",
            UserId: Dashboard.getCurrentUserId(),
            ExcludeLocationTypes: "Virtual"
        };

        ApiClient.getNextUpEpisodes(options).done(function (result) {

            if (result.Items.length) {
                $('#resumableSection', page).show();
            } else {
                $('#resumableSection', page).hide();
            }

            if (result.Items.length) {

                $('#nextUpItems', page).html(LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    useAverageAspectRatio: true,
                    shape: "backdrop",
                    showTitle: true,
                    showParentTitle: true,
                    overlayText: true

                })).createPosterItemHoverMenu();

            } else {

                $('#nextUpItems', page).html('<br/><p>None found. Start watching your shows!</p>');

            }

        });
    }

    $(document).on('pagebeforeshow', "#tvRecommendedPage", function () {

        var page = this;

        loadResume(page);
        loadNextUp(page);
    });


})(jQuery, document);