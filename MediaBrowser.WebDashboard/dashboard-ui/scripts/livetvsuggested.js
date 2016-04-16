define(['jQuery', 'scrollStyles'], function ($) {

    function enableScrollX() {
        return browserInfo.mobile && AppInfo.enableAppLayouts;
    }

    function getPortraitShape() {
        return enableScrollX() ? 'overflowPortrait' : 'portrait';
    }

    function getThumbShape() {
        return enableScrollX() ? 'overflowBackdrop' : 'backdrop';
    }

    function getSquareShape() {
        return enableScrollX() ? 'overflowSquare' : 'square';
    }

    function getLimit() {

        return enableScrollX() ? 12 : 8;
    }

    function loadRecommendedPrograms(page) {

        Dashboard.showLoadingMsg();

        ApiClient.getLiveTvRecommendedPrograms({

            userId: Dashboard.getCurrentUserId(),
            IsAiring: true,
            limit: getLimit() * 2,
            ImageTypeLimit: 1,
            EnableImageTypes: "Primary"

        }).then(function (result) {

            renderItems(page, result.Items, 'activeProgramItems', 'play');
            LibraryBrowser.setLastRefreshed(page);
            Dashboard.hideLoadingMsg();
        });
    }

    function reload(page) {

        loadRecommendedPrograms(page);

        ApiClient.getLiveTvRecommendedPrograms({

            userId: Dashboard.getCurrentUserId(),
            IsAiring: false,
            HasAired: false,
            limit: getLimit(),
            IsMovie: false,
            IsSports: false,
            IsKids: false,
            IsSeries: true

        }).then(function (result) {

            renderItems(page, result.Items, 'upcomingProgramItems');
        });

        ApiClient.getLiveTvRecommendedPrograms({

            userId: Dashboard.getCurrentUserId(),
            IsAiring: false,
            HasAired: false,
            limit: getLimit(),
            IsMovie: true

        }).then(function (result) {

            renderItems(page, result.Items, 'upcomingTvMovieItems', null, getPortraitShape());
        });

        ApiClient.getLiveTvRecommendedPrograms({

            userId: Dashboard.getCurrentUserId(),
            IsAiring: false,
            HasAired: false,
            limit: getLimit(),
            IsSports: true

        }).then(function (result) {

            renderItems(page, result.Items, 'upcomingSportsItems');
        });

        ApiClient.getLiveTvRecommendedPrograms({

            userId: Dashboard.getCurrentUserId(),
            IsAiring: false,
            HasAired: false,
            limit: getLimit(),
            IsKids: true

        }).then(function (result) {

            renderItems(page, result.Items, 'upcomingKidsItems');
        });
    }

    function renderItems(page, items, sectionClass, overlayButton, shape) {

        var html = LibraryBrowser.getPosterViewHtml({
            items: items,
            shape: shape || (enableScrollX() ? getSquareShape() : 'auto'),
            showTitle: true,
            centerText: true,
            coverImage: true,
            overlayText: false,
            lazy: true,
            overlayMoreButton: overlayButton != 'play',
            overlayPlayButton: overlayButton == 'play'
        });

        var elem = page.querySelector('.' + sectionClass);

        elem.innerHTML = html;
        ImageLoader.lazyChildren(elem);
    }

    function initSuggestedTab(page, tabContent) {

        if (enableScrollX()) {
            $('.itemsContainer', tabContent).addClass('hiddenScrollX').createCardMenus();
        } else {
            $('.itemsContainer', tabContent).removeClass('hiddenScrollX').createCardMenus();
        }
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
                initMethod = 'initSuggestedTab';
                break;
            case 1:
                depends.push('registrationservices');
                depends.push('scripts/livetvguide');
                renderMethod = 'renderGuideTab';
                initMethod = 'initGuideTab';
                break;
            case 2:
                depends.push('scripts/livetvchannels');
                depends.push('paper-icon-item');
                depends.push('paper-item-body');
                renderMethod = 'renderChannelsTab';
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

    pageIdOn('pageinit', "liveTvSuggestedPage", function () {

        var page = this;

        var tabs = page.querySelector('paper-tabs');
        var pageTabsContainer = page.querySelector('.pageTabsContainer');

        LibraryBrowser.configurePaperLibraryTabs(page, tabs, pageTabsContainer, 'livetv.html');

        pageTabsContainer.addEventListener('tabchange', function (e) {
            loadTab(page, parseInt(e.detail.selectedTabIndex));
        });

    });

    window.LiveTvPage = {
        renderSuggestedTab: renderSuggestedTab,
        initSuggestedTab: initSuggestedTab
    };

});