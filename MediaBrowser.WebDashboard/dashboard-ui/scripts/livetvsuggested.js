(function ($, document) {

    function loadRecommendedPrograms(page) {

        Dashboard.showLoadingMsg();

        ApiClient.getLiveTvRecommendedPrograms({

            userId: Dashboard.getCurrentUserId(),
            IsAiring: true,
            limit: 16,
            ImageTypeLimit: 1,
            EnableImageTypes: "Primary"

        }).done(function (result) {

            var html = LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                shape: "auto",
                showTitle: true,
                showParentTitle: true,
                centerText: true,
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
            IsSports: false,
            IsKids: false,
            IsSeries: true

        }).done(function (result) {

            var html = LibraryBrowser.getPosterViewHtml({

                items: result.Items,
                shape: "auto",
                showTitle: true,
                showParentTitle: true,
                centerText: true,
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
                shape: "portrait",
                showTitle: true,
                centerText: true,
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
                showTitle: true,
                centerText: true,
                coverImage: true,
                overlayText: false,
                lazy: true,
                overlayMoreButton: true
            });

            var elem = page.querySelector('.upcomingSportsItems');
            elem.innerHTML = html;
            ImageLoader.lazyChildren(elem);
        });

        ApiClient.getLiveTvRecommendedPrograms({

            userId: Dashboard.getCurrentUserId(),
            IsAiring: false,
            HasAired: false,
            limit: 8,
            IsKids: true

        }).done(function (result) {

            var html = LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                shape: "auto",
                showTitle: true,
                centerText: true,
                coverImage: true,
                overlayText: false,
                lazy: true,
                overlayMoreButton: true
            });

            var elem = page.querySelector('.upcomingKidsItems');
            elem.innerHTML = html;
            ImageLoader.lazyChildren(elem);
        });
    }

    function renderSuggestedTab(page, tabContent) {

        if (LibraryBrowser.needsRefresh(tabContent)) {
            reload(tabContent);
        }
    }

    function loadTab(page, index) {

        var tabContent = page.querySelector('.pageTabContent[data-index=\'' + index + '\']');
        var depends = [];
        var scope = 'LiveTvPage';
        var renderMethod = '';
        var initMethod = '';

        switch (index) {

            case 0:
                renderMethod = 'renderSuggestedTab';
                break;
            case 1:
                depends.push('scripts/registrationservices');
                depends.push('scripts/livetvguide');
                renderMethod = 'renderGuideTab';
                initMethod = 'initGuideTab';
                break;
            case 2:
                depends.push('scripts/livetvchannels');
                renderMethod = 'renderChannelsTab';
                initMethod = 'initChannelsTab';
                break;
            case 3:
                depends.push('scripts/livetvrecordings');
                renderMethod = 'renderRecordingsTab';
                break;
            case 4:
                depends.push('scripts/livetvtimers');
                renderMethod = 'renderTimersTab';
                break;
            case 5:
                depends.push('scripts/livetvseriestimers');
                renderMethod = 'renderSeriesTimersTab';
                break;
            default:
                break;
        }

        require(depends, function () {

            if (initMethod && !tabContent.initComplete) {

                window[scope][initMethod](page, tabContent);
                tabContent.initComplete = true;
            }

            window[scope][renderMethod](page, tabContent);

        });
    }

    $(document).on('pageinit', "#liveTvSuggestedPage", function () {

        var page = this;

        var tabs = page.querySelector('paper-tabs');
        var pages = page.querySelector('neon-animated-pages');

        LibraryBrowser.configurePaperLibraryTabs(page, tabs, pages, 'livetv.html');

        $(pages).on('tabchange', function () {
            loadTab(page, parseInt(this.selected));
        });

    });

    window.LiveTvPage = {
        renderSuggestedTab: renderSuggestedTab
    };

})(jQuery, document);