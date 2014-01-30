(function ($, document) {

    $(document).on('pagebeforeshow', "#moviesRecommendedPage", function () {

        var screenWidth = $(window).width();

        var page = this;
        
        var options = {

            SortBy: "DateCreated",
            SortOrder: "Descending",
            IncludeItemTypes: "Movie",
            Limit: screenWidth >= 1920 ? 14 : 12,
            Recursive: true,
            Fields: "PrimaryImageAspectRatio,DateCreated,UserData",
            Filters: "IsUnplayed"
        };

        ApiClient.getItems(Dashboard.getCurrentUserId(), options).done(function (result) {

            $('#recentlyAddedItems', page).html(LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                useAverageAspectRatio: true
                
            })).createPosterItemHoverMenu();
        });


        options = {

            SortBy: "DatePlayed",
            SortOrder: "Descending",
            IncludeItemTypes: "Movie",
            Filters: "IsResumable",
            Limit: screenWidth >= 1920 ? 4 : 3,
            Recursive: true,
            Fields: "DateCreated,UserData"
        };

        ApiClient.getItems(Dashboard.getCurrentUserId(), options).done(function (result) {

            if (result.Items.length) {
                $('#resumableSection', page).show();
            } else {
                $('#resumableSection', page).hide();
            }
            
            $('#resumableItems', page).html(LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                preferBackdrop: true,
                shape: 'backdrop',
                overlayText: true,
                showTitle: true
                
            })).createPosterItemHoverMenu();

        });


        options = {

            SortBy: "DateCreated",
            SortOrder: "Descending",
            IncludeItemTypes: "Trailer",
            Limit: screenWidth >= 1920 ? 7 : 6,
            Recursive: true,
            Fields: "PrimaryImageAspectRatio,DateCreated,UserData",
            Filters: "IsUnplayed"
        };

        ApiClient.getItems(Dashboard.getCurrentUserId(), options).done(function (result) {

            if (result.Items.length) {
                $('#trailerSection', page).show();
            } else {
                $('#trailerSection', page).hide();
            }

            $('#trailerItems', page).html(LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                useAverageAspectRatio: true
                
            })).createPosterItemHoverMenu();

        });

    });


})(jQuery, document);