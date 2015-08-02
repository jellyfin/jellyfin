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

    function loadSuggestedTab(page) {

        var tabContent = page.querySelector('.suggestedTabContent');

        if (LibraryBrowser.needsRefresh(tabContent)) {
            reload(tabContent);
        }
    }

    function loadTab(page, index) {

        switch (index) {

            case 0:
                loadSuggestedTab(page);
                break;
            default:
                break;
        }
    }

    $(document).on('pageinitdepends', "#liveTvSuggestedPage", function () {

        var page = this;

        var tabs = page.querySelector('paper-tabs');
        var pages = page.querySelector('neon-animated-pages');

        LibraryBrowser.configurePaperLibraryTabs(page, tabs, pages);

        $(tabs).on('iron-select', function () {
            var selected = this.selected;

            if (LibraryBrowser.navigateOnLibraryTabSelect()) {

                if (selected) {
                    Dashboard.navigate('livetv.html?tab=' + selected);
                } else {
                    Dashboard.navigate('livetv.html');
                }

            } else {
                page.querySelector('neon-animated-pages').selected = selected;
            }
        });

        $(pages).on('tabchange', function () {
            loadTab(page, parseInt(this.selected));
        });

    });

})(jQuery, document);