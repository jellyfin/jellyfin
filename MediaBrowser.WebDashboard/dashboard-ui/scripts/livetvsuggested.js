(function ($, document, apiClient) {

    function reload(page) {

        Dashboard.showLoadingMsg();

        apiClient.getLiveTvRecommendedPrograms({

            userId: Dashboard.getCurrentUserId(),
            IsAiring: true,
            limit: 12

        }).done(function (result) {
            
            var html = LibraryBrowser.getPosterViewHtml({

                items: result.Items,
                shape: "square",
                showTitle: true,
                showParentTitle: true,
                overlayText: true,
                coverImage: true

            });

            $('.activeProgramItems', page).html(html).createPosterItemMenus();
        });

        apiClient.getLiveTvRecommendedPrograms({

            userId: Dashboard.getCurrentUserId(),
            IsAiring: false,
            HasAired: false,
            limit: 12

        }).done(function (result) {

            var html = LibraryBrowser.getPosterViewHtml({

                items: result.Items,
                shape: "square",
                showTitle: true,
                showParentTitle: true,
                overlayText: true,
                coverImage: true

            });

            $('.upcomingProgramItems', page).html(html).createPosterItemMenus();
        });
    }

    $(document).on('pagebeforeshow', "#liveTvSuggestedPage", function () {

        var page = this;

        reload(page);

    });

})(jQuery, document, ApiClient);