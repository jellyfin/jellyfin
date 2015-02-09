(function ($, document) {

    function reload(page) {

        Dashboard.showLoadingMsg();

        ApiClient.getLiveTvRecommendedPrograms({

            userId: Dashboard.getCurrentUserId(),
            IsAiring: true,
            limit: 18

        }).done(function (result) {

            var html = LibraryBrowser.getPosterViewHtml({

                items: result.Items,
                shape: "square",
                showTitle: true,
                showParentTitle: true,
                overlayText: true,
                coverImage: true,
                lazy: true

            });

            $('.activeProgramItems', page).html(html).lazyChildren();
        });

        ApiClient.getLiveTvRecommendedPrograms({

            userId: Dashboard.getCurrentUserId(),
            IsAiring: false,
            HasAired: false,
            limit: 18

        }).done(function (result) {

            var html = LibraryBrowser.getPosterViewHtml({

                items: result.Items,
                shape: "square",
                showTitle: true,
                showParentTitle: true,
                overlayText: true,
                coverImage: true,
                lazy: true

            });

            $('.upcomingProgramItems', page).html(html).lazyChildren();
        });
    }

    $(document).on('pagebeforeshow', "#liveTvSuggestedPage", function () {

        var page = this;

        reload(page);

    });

})(jQuery, document);