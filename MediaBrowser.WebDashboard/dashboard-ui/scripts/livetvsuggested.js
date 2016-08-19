define(['libraryBrowser', 'cardBuilder', 'scrollStyles', 'emby-itemscontainer', 'emby-tabs', 'emby-button'], function (libraryBrowser, cardBuilder) {

    function enableScrollX() {
        return browserInfo.mobile && AppInfo.enableAppLayouts;
    }

    function getPortraitShape() {
        return enableScrollX() ? 'overflowPortrait' : 'portrait';
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
            EnableImageTypes: "Primary",
            EnableTotalRecordCount: false

        }).then(function (result) {

            renderItems(page, result.Items, 'activeProgramItems', 'play');
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
            IsSeries: true,
            EnableTotalRecordCount: false

        }).then(function (result) {

            renderItems(page, result.Items, 'upcomingProgramItems');
        });

        ApiClient.getLiveTvRecommendedPrograms({

            userId: Dashboard.getCurrentUserId(),
            IsAiring: false,
            HasAired: false,
            limit: getLimit(),
            IsMovie: true,
            EnableTotalRecordCount: false

        }).then(function (result) {

            renderItems(page, result.Items, 'upcomingTvMovieItems', null, getPortraitShape());
        });

        ApiClient.getLiveTvRecommendedPrograms({

            userId: Dashboard.getCurrentUserId(),
            IsAiring: false,
            HasAired: false,
            limit: getLimit(),
            IsSports: true,
            EnableTotalRecordCount: false

        }).then(function (result) {

            renderItems(page, result.Items, 'upcomingSportsItems');
        });

        ApiClient.getLiveTvRecommendedPrograms({

            userId: Dashboard.getCurrentUserId(),
            IsAiring: false,
            HasAired: false,
            limit: getLimit(),
            IsKids: true,
            EnableTotalRecordCount: false

        }).then(function (result) {

            renderItems(page, result.Items, 'upcomingKidsItems');
        });
    }

    function renderItems(page, items, sectionClass, overlayButton, shape) {

        var html = cardBuilder.getCardsHtml({
            items: items,
            shape: shape || (enableScrollX() ? 'autooverflow' : 'auto'),
            showTitle: true,
            centerText: true,
            coverImage: true,
            overlayText: false,
            lazy: true,
            overlayMoreButton: overlayButton != 'play',
            overlayPlayButton: overlayButton == 'play',
            allowBottomPadding: !enableScrollX()
        });

        var elem = page.querySelector('.' + sectionClass);

        elem.innerHTML = html;
        ImageLoader.lazyChildren(elem);
    }

    return function (view, params) {

        var self = this;

        self.initTab = function () {

            var tabContent = view.querySelector('.pageTabContent[data-index=\'' + 0 + '\']');

            var containers = tabContent.querySelectorAll('.itemsContainer');

            for (var i = 0, length = containers.length; i < length; i++) {
                if (enableScrollX()) {
                    containers[i].classList.add('hiddenScrollX');
                    containers[i].classList.remove('vertical-wrap');
                } else {
                    containers[i].classList.remove('hiddenScrollX');
                    containers[i].classList.add('vertical-wrap');
                }
            }
        };

        self.renderTab = function () {
            var tabContent = view.querySelector('.pageTabContent[data-index=\'' + 0 + '\']');
            reload(tabContent);
        };

        var tabControllers = [];
        var renderedTabs = [];

        function loadTab(page, index) {

            var tabContent = page.querySelector('.pageTabContent[data-index=\'' + index + '\']');
            var depends = [];

            switch (index) {

                case 0:
                    break;
                case 1:
                    document.body.classList.add('autoScrollY');
                    depends.push('scripts/livetvguide');
                    break;
                case 2:
                    document.body.classList.remove('autoScrollY');
                    depends.push('scripts/livetvchannels');
                    break;
                case 3:
                    document.body.classList.remove('autoScrollY');
                    depends.push('scripts/livetvrecordings');
                    break;
                case 4:
                    document.body.classList.remove('autoScrollY');
                    depends.push('scripts/livetvseriestimers');
                    break;
                default:
                    break;
            }

            require(depends, function (controllerFactory) {

                if (index == 0) {
                    self.tabContent = tabContent;
                }
                var controller = tabControllers[index];
                if (!controller) {
                    controller = index ? new controllerFactory(view, params, tabContent) : self;
                    tabControllers[index] = controller;

                    if (controller.initTab) {
                        controller.initTab();
                    }
                }

                if (renderedTabs.indexOf(index) == -1) {
                    renderedTabs.push(index);
                    controller.renderTab();
                }
            });
        }

        var viewTabs = view.querySelector('.libraryViewNav');

        libraryBrowser.configurePaperLibraryTabs(view, viewTabs, view.querySelectorAll('.pageTabContent'), [0, 2, 3, 4]);

        viewTabs.addEventListener('tabchange', function (e) {
            loadTab(view, parseInt(e.detail.selectedTabIndex));
        });

        view.addEventListener('viewbeforehide', function (e) {

            document.body.classList.remove('autoScrollY');
        });

        if (AppInfo.enableHeadRoom) {
            require(["headroom-window"], function (headroom) {
                headroom.add(viewTabs);
                self.headroom = headroom;
            });
        }

        view.addEventListener('viewdestroy', function (e) {

            if (self.headroom) {
                self.headroom.remove(viewTabs);
            }
            tabControllers.forEach(function (t) {
                if (t.destroy) {
                    t.destroy();
                }
            });
        });
    };
});