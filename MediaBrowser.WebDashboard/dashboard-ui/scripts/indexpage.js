(function ($, document) {

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
            return Sections.loadLibraryTiles(elem, user, 'homePageSmallBackdrop', index, false, showLibraryTileNames);
        }
        else if (section == 'smalllibrarytiles-automobile') {
            return Sections.loadLibraryTiles(elem, user, 'homePageSmallBackdrop', index, true, showLibraryTileNames);
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

            var deferred = DeferredBuilder.Deferred();
            deferred.resolve();
            return deferred.promise();
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

    function dismissWelcome(page, userId) {

        getDisplayPreferences('home', userId).then(function (result) {

            result.CustomPrefs[homePageTourKey] = homePageDismissValue;
            ApiClient.updateDisplayPreferences('home', result, userId, AppSettings.displayPreferencesKey());
        });
    }

    function showWelcomeIfNeeded(page, displayPreferences) {

        if (displayPreferences.CustomPrefs[homePageTourKey] == homePageDismissValue) {
            $('.welcomeMessage', page).hide();
        } else {

            Dashboard.hideLoadingMsg();

            var elem = $('.welcomeMessage', page).show();

            if (displayPreferences.CustomPrefs[homePageTourKey]) {

                $('.tourHeader', elem).html(Globalize.translate('HeaderWelcomeBack'));
                $('.tourButtonText', elem).html(Globalize.translate('ButtonTakeTheTourToSeeWhatsNew'));

            } else {

                $('.tourHeader', elem).html(Globalize.translate('HeaderWelcomeToProjectWebClient'));
                $('.tourButtonText', elem).html(Globalize.translate('ButtonTakeTheTour'));
            }
        }
    }

    function takeTour(page, userId) {

        require(['swipebox'], function () {

            $.swipebox([
                    { href: 'css/images/tour/web/tourcontent.jpg', title: Globalize.translate('WebClientTourContent') },
                    { href: 'css/images/tour/web/tourmovies.jpg', title: Globalize.translate('WebClientTourMovies') },
                    { href: 'css/images/tour/web/tourmouseover.jpg', title: Globalize.translate('WebClientTourMouseOver') },
                    { href: 'css/images/tour/web/tourtaphold.jpg', title: Globalize.translate('WebClientTourTapHold') },
                    { href: 'css/images/tour/web/tourmysync.png', title: Globalize.translate('WebClientTourMySync') },
                    { href: 'css/images/tour/web/toureditor.png', title: Globalize.translate('WebClientTourMetadataManager') },
                    { href: 'css/images/tour/web/tourplaylist.png', title: Globalize.translate('WebClientTourPlaylists') },
                    { href: 'css/images/tour/web/tourcollections.jpg', title: Globalize.translate('WebClientTourCollections') },
                    { href: 'css/images/tour/web/tourusersettings1.png', title: Globalize.translate('WebClientTourUserPreferences1') },
                    { href: 'css/images/tour/web/tourusersettings2.png', title: Globalize.translate('WebClientTourUserPreferences2') },
                    { href: 'css/images/tour/web/tourusersettings3.png', title: Globalize.translate('WebClientTourUserPreferences3') },
                    { href: 'css/images/tour/web/tourusersettings4.png', title: Globalize.translate('WebClientTourUserPreferences4') },
                    { href: 'css/images/tour/web/tourmobile1.jpg', title: Globalize.translate('WebClientTourMobile1') },
                    { href: 'css/images/tour/web/tourmobile2.png', title: Globalize.translate('WebClientTourMobile2') },
                    { href: 'css/images/tour/enjoy.jpg', title: Globalize.translate('MessageEnjoyYourStay') }
            ], {
                afterClose: function () {
                    dismissWelcome(page, userId);
                    $('.welcomeMessage', page).hide();
                },
                hideBarsDelay: 30000
            });
        });
    }

    function loadHomeTab(page, tabContent) {

        if (LibraryBrowser.needsRefresh(tabContent)) {
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

                            LibraryBrowser.setLastRefreshed(tabContent);
                        });

                    });
                });
            }
        }
    }

    function loadTab(page, index) {

        var tabContent = page.querySelector('.pageTabContent[data-index=\'' + index + '\']');
        var depends = [];
        var scope = 'HomePage';
        var method = '';

        switch (index) {

            case 0:
                depends.push('scripts/sections');
                method = 'renderHomeTab';
                break;
            case 1:
                depends.push('scripts/homenextup');
                method = 'renderNextUp';
                break;
            case 2:
                depends.push('scripts/favorites');
                method = 'renderFavorites';
                break;
            case 3:
                depends.push('scripts/homeupcoming');
                method = 'renderUpcoming';
                break;
            default:
                break;
        }

        require(depends, function () {

            window[scope][method](page, tabContent);

        });
    }

    pageIdOn('pageinit', "indexPage", function () {

        var page = this;

        var tabs = page.querySelector('paper-tabs');
        var pages = page.querySelector('neon-animated-pages');

        LibraryBrowser.configurePaperLibraryTabs(page, tabs, pages, 'index.html');

        pages.addEventListener('tabchange', function (e) {
            loadTab(page, parseInt(e.target.selected));
        });

        page.querySelector('.btnTakeTour').addEventListener('click', function () {
            takeTour(page, Dashboard.getCurrentUserId());
        });

        if (AppInfo.enableHomeTabs) {
            page.classList.remove('noSecondaryNavPage');
            page.querySelector('.libraryViewNav').classList.remove('hide');
        } else {
            page.classList.add('noSecondaryNavPage');
            page.querySelector('.libraryViewNav').classList.add('hide');
        }
    });

    pageIdOn('pageshow', "indexPage", function () {

        var page = this;
        $(MediaController).on('playbackstop', onPlaybackStop);
    });

    pageIdOn('pagebeforehide', "indexPage", function () {

        var page = this;
        $(MediaController).off('playbackstop', onPlaybackStop);
    });

    function onPlaybackStop(e, state) {

        if (state.NowPlayingItem && state.NowPlayingItem.MediaType == 'Video') {
            var page = $($.mobile.activePage)[0];
            var pages = page.querySelector('neon-animated-pages');

            pages.dispatchEvent(new CustomEvent("tabchange", {}));
        }
    }

    function getDisplayPreferences(key, userId) {

        return ApiClient.getDisplayPreferences(key, userId, AppSettings.displayPreferencesKey());
    }

    window.HomePage = {
        renderHomeTab: loadHomeTab
    };

})(jQuery, document);
