define(['libraryBrowser'], function (libraryBrowser) {

    var defaultFirstSection = 'smalllibrarytiles';

    function getDefaultSection(index) {

        if (AppInfo.isNativeApp && browserInfo.safari) {

            switch (index) {

                case 0:
                    return defaultFirstSection;
                case 1:
                    return 'resume';
                case 2:
                    return 'nextup';
                case 3:
                    return 'latestmovies';
                case 4:
                    return 'latestepisodes';
                case 5:
                    return 'latesttvrecordings';
                default:
                    return '';
            }
        }

        switch (index) {

            case 0:
                return defaultFirstSection;
            case 1:
                return 'resume';
            case 2:
                return 'latestmedia';
            case 3:
                return 'latesttvrecordings';
            default:
                return '';
        }

    }

    function loadSection(page, user, displayPreferences, index) {

        var userId = user.Id;

        var section = displayPreferences.CustomPrefs['home' + index] || getDefaultSection(index);

        if (section == 'folders') {
            section = defaultFirstSection;
        }

        var showLibraryTileNames = displayPreferences.CustomPrefs.enableLibraryTileNames != '0';

        var elem = page.querySelector('.section' + index);

        if (section == 'latestmedia') {
            return Sections.loadRecentlyAdded(elem, user);
        }
        else if (section == 'latestmovies') {
            return Sections.loadLatestMovies(elem, user);
        }
        else if (section == 'latestepisodes') {
            return Sections.loadLatestEpisodes(elem, user);
        }
        else if (section == 'librarytiles') {
            return Sections.loadLibraryTiles(elem, user, 'backdrop', index, false, showLibraryTileNames);
        }
        else if (section == 'smalllibrarytiles') {
            return Sections.loadLibraryTiles(elem, user, 'smallBackdrop', index, false, showLibraryTileNames);
        }
        else if (section == 'smalllibrarytiles-automobile') {
            return Sections.loadLibraryTiles(elem, user, 'smallBackdrop', index, true, showLibraryTileNames);
        }
        else if (section == 'librarytiles-automobile') {
            return Sections.loadLibraryTiles(elem, user, 'backdrop', index, true, showLibraryTileNames);
        }
        else if (section == 'librarybuttons') {
            return Sections.loadlibraryButtons(elem, userId, index);
        }
        else if (section == 'resume') {
            return Sections.loadResume(elem, userId);
        }
        else if (section == 'nextup') {
            return Sections.loadNextUp(elem, userId);
        }
        else if (section == 'latesttvrecordings') {
            return Sections.loadLatestLiveTvRecordings(elem, userId);
        }
        else if (section == 'latestchannelmedia') {
            return Sections.loadLatestChannelMedia(elem, userId);

        } else {

            elem.innerHTML = '';

            return new Promise(function (resolve, reject) {

                resolve();
            });
        }
    }

    function loadSections(page, user, displayPreferences) {

        var i, length;
        var sectionCount = 6;

        var elem = page.querySelector('.sections');

        if (!elem.innerHTML.length) {
            var html = '';
            for (i = 0, length = sectionCount; i < length; i++) {

                html += '<div class="homePageSection section' + i + '"></div>';
            }

            elem.innerHTML = html;
        }

        var promises = [];

        for (i = 0, length = sectionCount; i < length; i++) {

            promises.push(loadSection(page, user, displayPreferences, i));
        }

        return Promise.all(promises);
    }

    var homePageDismissValue = '14';
    var homePageTourKey = 'homePageTour';

    function displayPreferencesKey() {
        if (AppInfo.isNativeApp) {
            return 'Emby Mobile';
        }

        return 'webclient';
    }

    function dismissWelcome(page, userId) {

        getDisplayPreferences('home', userId).then(function (result) {

            result.CustomPrefs[homePageTourKey] = homePageDismissValue;
            ApiClient.updateDisplayPreferences('home', result, userId, displayPreferencesKey());
        });
    }

    function showWelcomeIfNeeded(page, displayPreferences) {

        if (displayPreferences.CustomPrefs[homePageTourKey] == homePageDismissValue) {
            page.querySelector('.welcomeMessage').classList.add('hide');
        } else {

            Dashboard.hideLoadingMsg();

            var elem = page.querySelector('.welcomeMessage');
            elem.classList.remove('hide');

            if (displayPreferences.CustomPrefs[homePageTourKey]) {

                elem.querySelector('.tourHeader').innerHTML = Globalize.translate('HeaderWelcomeBack');
                elem.querySelector('.tourButtonText').innerHTML = Globalize.translate('ButtonTakeTheTourToSeeWhatsNew');

            } else {

                elem.querySelector('.tourHeader').innerHTML = Globalize.translate('HeaderWelcomeToProjectWebClient');
                elem.querySelector('.tourButtonText').innerHTML = Globalize.translate('ButtonTakeTheTour');
            }
        }
    }

    function takeTour(page, userId) {

        require(['slideshow'], function () {

            var slides = [
                    { imageUrl: 'css/images/tour/web/tourcontent.jpg', title: Globalize.translate('WebClientTourContent') },
                    { imageUrl: 'css/images/tour/web/tourmovies.jpg', title: Globalize.translate('WebClientTourMovies') },
                    { imageUrl: 'css/images/tour/web/tourmouseover.jpg', title: Globalize.translate('WebClientTourMouseOver') },
                    { imageUrl: 'css/images/tour/web/tourtaphold.jpg', title: Globalize.translate('WebClientTourTapHold') },
                    { imageUrl: 'css/images/tour/web/tourmysync.png', title: Globalize.translate('WebClientTourMySync') },
                    { imageUrl: 'css/images/tour/web/toureditor.png', title: Globalize.translate('WebClientTourMetadataManager') },
                    { imageUrl: 'css/images/tour/web/tourplaylist.png', title: Globalize.translate('WebClientTourPlaylists') },
                    { imageUrl: 'css/images/tour/web/tourcollections.jpg', title: Globalize.translate('WebClientTourCollections') },
                    { imageUrl: 'css/images/tour/web/tourusersettings1.png', title: Globalize.translate('WebClientTourUserPreferences1') },
                    { imageUrl: 'css/images/tour/web/tourusersettings2.png', title: Globalize.translate('WebClientTourUserPreferences2') },
                    { imageUrl: 'css/images/tour/web/tourusersettings3.png', title: Globalize.translate('WebClientTourUserPreferences3') },
                    { imageUrl: 'css/images/tour/web/tourusersettings4.png', title: Globalize.translate('WebClientTourUserPreferences4') },
                    { imageUrl: 'css/images/tour/web/tourmobile1.jpg', title: Globalize.translate('WebClientTourMobile1') },
                    { imageUrl: 'css/images/tour/web/tourmobile2.png', title: Globalize.translate('WebClientTourMobile2') },
                    { imageUrl: 'css/images/tour/enjoy.jpg', title: Globalize.translate('MessageEnjoyYourStay') }
            ];

            require(['slideshow'], function (slideshow) {

                var newSlideShow = new slideshow({
                    slides: slides,
                    interactive: true,
                    loop: false
                });

                newSlideShow.show();

                dismissWelcome(page, userId);
                page.querySelector('.welcomeMessage').classList.add('hide');
            });
        });
    }

    function loadHomeTab(page, tabContent) {

        if (window.ApiClient) {
            var userId = Dashboard.getCurrentUserId();
            Dashboard.showLoadingMsg();

            getDisplayPreferences('home', userId).then(function (result) {

                Dashboard.getCurrentUser().then(function (user) {

                    loadSections(tabContent, user, result).then(function () {

                        if (!AppInfo.isNativeApp) {
                            showWelcomeIfNeeded(page, result);
                        }
                        Dashboard.hideLoadingMsg();
                    });

                });
            });
        }
    }

    function getDisplayPreferences(key, userId) {

        return ApiClient.getDisplayPreferences(key, userId, displayPreferencesKey());
    }

    return function (view, params) {

        var self = this;

        self.renderTab = function () {
            var tabContent = view.querySelector('.pageTabContent[data-index=\'' + 0 + '\']');
            loadHomeTab(view, tabContent);
        };

        var mdlTabs = view.querySelector('.libraryViewNav');

        libraryBrowser.configurePaperLibraryTabs(view, mdlTabs, view.querySelectorAll('.pageTabContent'), [0, 1, 2, 3]);

        var tabControllers = [];
        var renderedTabs = [];

        function getTabController(page, index, callback) {

            var depends = [];

            switch (index) {

                case 0:
                    depends.push('scripts/sections');
                    break;
                case 1:
                    depends.push('scripts/homenextup');
                    break;
                case 2:
                    depends.push('scripts/homefavorites');
                    break;
                case 3:
                    depends.push('scripts/homeupcoming');
                    break;
                default:
                    return;
            }

            require(depends, function (controllerFactory) {
                var tabContent;
                if (index == 0) {
                    tabContent = view.querySelector('.pageTabContent[data-index=\'' + index + '\']');
                    self.tabContent = tabContent;
                }
                var controller = tabControllers[index];
                if (!controller) {
                    tabContent = view.querySelector('.pageTabContent[data-index=\'' + index + '\']');
                    controller = index ? new controllerFactory(view, params, tabContent) : self;
                    tabControllers[index] = controller;

                    if (controller.initTab) {
                        controller.initTab();
                    }
                }

                callback(controller);
            });
        }

        function preLoadTab(page, index) {

            getTabController(page, index, function (controller) {
                if (renderedTabs.indexOf(index) == -1) {
                    if (controller.preRender) {
                        controller.preRender();
                    }
                }
            });
        }

        function loadTab(page, index) {

            getTabController(page, index, function (controller) {
                if (renderedTabs.indexOf(index) == -1) {
                    renderedTabs.push(index);
                    controller.renderTab();
                }
            });
        }

        mdlTabs.addEventListener('beforetabchange', function (e) {
            preLoadTab(view, parseInt(e.detail.selectedTabIndex));
        });

        mdlTabs.addEventListener('tabchange', function (e) {
            loadTab(view, parseInt(e.detail.selectedTabIndex));
        });

        view.querySelector('.btnTakeTour').addEventListener('click', function () {
            takeTour(view, Dashboard.getCurrentUserId());
        });

        if (AppInfo.enableHomeTabs) {
            view.classList.remove('noSecondaryNavPage');
            view.querySelector('.libraryViewNav').classList.remove('hide');
        } else {
            view.classList.add('noSecondaryNavPage');
            view.querySelector('.libraryViewNav').classList.add('hide');
        }

        function onPlaybackStop(e, state) {

            if (state.NowPlayingItem && state.NowPlayingItem.MediaType == 'Video') {

                mdlTabs.dispatchEvent(new CustomEvent("tabchange", {
                    detail: {
                        selectedTabIndex: libraryBrowser.selectedTab(mdlTabs)
                    }
                }));
            }
        }

        function onWebSocketMessage(e, data) {

            var msg = data;

            if (msg.MessageType === "UserDataChanged") {

                if (msg.Data.UserId == Dashboard.getCurrentUserId()) {

                    renderedTabs = [];
                }
            }

        }

        view.addEventListener('viewshow', function (e) {
            Events.on(MediaController, 'playbackstop', onPlaybackStop);
            Events.on(ApiClient, "websocketmessage", onWebSocketMessage);
        });

        view.addEventListener('viewbeforehide', function (e) {
            Events.off(MediaController, 'playbackstop', onPlaybackStop);
            Events.off(ApiClient, "websocketmessage", onWebSocketMessage);
        });
    };
});