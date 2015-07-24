(function ($, document) {

    function loadRecommendedPrograms(page) {

        Dashboard.showLoadingMsg();

        ApiClient.getLiveTvRecommendedPrograms({

            userId: Dashboard.getCurrentUserId(),
            IsAiring: true,
            limit: 16

        }).done(function (result) {

            var html = LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                shape: "auto",
                showTitle: true,
                showParentTitle: true,
                overlayText: true,
                coverImage: true,
                lazy: true,
                overlayPlayButton: true

            });

            var elem = page.querySelector('.activeProgramItems');
            elem.innerHTML = html;
            ImageLoader.lazyChildren(elem);
            Dashboard.hideLoadingMsg();

            LibraryBrowser.setLastRefreshed(page);
        });
    }

    function reload(page) {

        loadRecommendedPrograms(page);

        ApiClient.getLiveTvRecommendedPrograms({

            userId: Dashboard.getCurrentUserId(),
            IsAiring: false,
            HasAired: false,
            limit: 8,
            IsMovie: false,
            IsSports: false

        }).done(function (result) {

            var html = LibraryBrowser.getPosterViewHtml({

                items: result.Items,
                shape: "auto",
                showTitle: true,
                showParentTitle: true,
                overlayText: true,
                coverImage: true,
                lazy: true,
                overlayMoreButton: true

            });

            var elem = page.querySelector('.upcomingProgramItems');
            elem.innerHTML = html;
            ImageLoader.lazyChildren(elem);
        });

        ApiClient.getLiveTvRecommendedPrograms({

            userId: Dashboard.getCurrentUserId(),
            IsAiring: false,
            HasAired: false,
            limit: 8,
            IsMovie: true

        }).done(function (result) {

            var html = LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                shape: "auto",
                showTitle: false,
                coverImage: true,
                overlayText: false,
                lazy: true,
                overlayMoreButton: true
            });

            var elem = page.querySelector('.upcomingTvMovieItems');
            elem.innerHTML = html;
            ImageLoader.lazyChildren(elem);
        });

        ApiClient.getLiveTvRecommendedPrograms({

            userId: Dashboard.getCurrentUserId(),
            IsAiring: false,
            HasAired: false,
            limit: 8,
            IsSports: true

        }).done(function (result) {

            var html = LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                shape: "auto",
                showTitle: false,
                coverImage: true,
                overlayText: false,
                lazy: true,
                overlayMoreButton: true
            });

            var elem = page.querySelector('.upcomingSportsItems');
            elem.innerHTML = html;
            ImageLoader.lazyChildren(elem);
        });
    }

    $(document).on('pagebeforeshowready', "#liveTvSuggestedPage", function () {

        var page = this;

        if (LibraryBrowser.needsRefresh(page)) {
            reload(page);
        }
    });

})(jQuery, document);