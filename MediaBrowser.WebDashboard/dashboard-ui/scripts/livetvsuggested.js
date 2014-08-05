(function ($, document, apiClient) {

    function reload(page) {

        Dashboard.showLoadingMsg();

        apiClient.getLiveTvRecommendedPrograms({

            userId: Dashboard.getCurrentUserId(),
            IsAiring: true,
            limit: 10

        }).done(function (result) {

            var html = LibraryBrowser.getPosterViewHtml({

                items: result.Items,
                shape: "homePageSquare",
                showTitle: true,
                showParentTitle: true,
                overlayText: true,
                coverImage: true

            });

            $('.activeProgramItems', page).html(html).createCardMenus();
        });

        apiClient.getLiveTvRecommendedPrograms({

            userId: Dashboard.getCurrentUserId(),
            IsAiring: false,
            HasAired: false,
            limit: 10

        }).done(function (result) {

            var html = LibraryBrowser.getPosterViewHtml({

                items: result.Items,
                shape: "homePageSquare",
                showTitle: true,
                showParentTitle: true,
                overlayText: true,
                coverImage: true

            });

            $('.upcomingProgramItems', page).html(html).createCardMenus();
        });
    }

    $(document).on('pagebeforeshow', "#liveTvSuggestedPage", function () {

        var page = this;

        reload(page);

    });

})(jQuery, document, ApiClient);