define(['connectionManager', 'cardBuilder', 'registrationServices', 'appSettings', 'dom', 'apphost', 'layoutManager', 'imageLoader', 'globalize', 'itemShortcuts', 'itemHelper', 'appRouter', 'emby-button', 'paper-icon-button-light', 'emby-itemscontainer', 'emby-scroller', 'emby-linkbutton', 'css!./homesections'], function (connectionManager, cardBuilder, registrationServices, appSettings, dom, appHost, layoutManager, imageLoader, globalize, itemShortcuts, itemHelper, appRouter) {
    'use strict';

    function getDefaultSection(index) {

        switch (index) {

            case 0:
                return 'smalllibrarytiles';
            case 1:
                return 'resume';
            case 2:
                return 'resumeaudio';
            case 3:
                return 'livetv';
            case 4:
                return 'nextup';
            case 5:
                return 'latestmedia';
            case 6:
                return 'none';
            default:
                return '';
        }
    }

    function getAllSectionsToShow(userSettings, sectionCount) {

        var sections = [];

        for (var i = 0, length = sectionCount; i < length; i++) {

            var section = userSettings.get('homesection' + i) || getDefaultSection(i);

            if (section === 'folders') {
                section = getDefaultSection(0);
            }

            sections.push(section);
        }

        return sections;
    }

    function loadSections(elem, apiClient, user, userSettings) {

        return getUserViews(apiClient, user.Id).then(function (userViews) {

            var i, length;
            var sectionCount = 7;

            var html = '';
            for (i = 0, length = sectionCount; i < length; i++) {

                html += '<div class="verticalSection section' + i + '"></div>';
            }

            elem.innerHTML = html;
            elem.classList.add('homeSectionsContainer');

            var promises = [];
            var sections = getAllSectionsToShow(userSettings, sectionCount);

            for (i = 0, length = sections.length; i < length; i++) {

                promises.push(loadSection(elem, apiClient, user, userSettings, userViews, sections, i));
            }

            return Promise.all(promises).then(function () {

                html = '';

                var style = 'margin-top:4em;';

                if (layoutManager.tv) {
                    style += 'padding: 0 7.5%;';
                }

                html += '<div class="verticalSection padded-left padded-right customizeSection hide" style="' + style + '">';
                html += '<a href="' + appRouter.getRouteUrl('settings') + '" is="emby-linkbutton" class="raised block"><span>' + globalize.translate('sharedcomponents#HeaderCustomizeHomeScreen') + '</span></a>';
                html += '</div>';

                elem.insertAdjacentHTML('beforeend', html);

                return resume(elem, {
                    refresh: true,
                    returnPromise: false
                });
            });
        });
    }

    function destroySections(elem) {

        var elems = elem.querySelectorAll('.itemsContainer');
        var i, length;

        for (i = 0, length = elems.length; i < length; i++) {

            elems[i].fetchData = null;
            elems[i].parentContainer = null;
            elems[i].getItemsHtml = null;
        }

        elem.innerHTML = '';
    }

    function pause(elem) {

        var elems = elem.querySelectorAll('.itemsContainer');
        var i, length;
        for (i = 0, length = elems.length; i < length; i++) {

            elems[i].pause();
        }
    }

    function resume(elem, options) {

        var elems = elem.querySelectorAll('.itemsContainer');
        var i, length;
        var promises = [];

        for (i = 0, length = elems.length; i < length; i++) {
            promises.push(elems[i].resume(options));
        }

        var promise = Promise.all(promises).then(function () {
            elem.querySelector('.customizeSection').classList.remove('hide');
        });

        if (!options || options.returnPromise !== false) {
            return promise;
        }
    }

    function loadSection(page, apiClient, user, userSettings, userViews, allSections, index) {

        var section = allSections[index];
        var userId = user.Id;

        var elem = page.querySelector('.section' + index);

        if (section === 'latestmedia') {
            loadRecentlyAdded(elem, apiClient, user, userViews);
        }
        else if (section === 'librarytiles' || section === 'smalllibrarytiles' || section === 'smalllibrarytiles-automobile' || section === 'librarytiles-automobile') {
            return loadLibraryTiles(elem, apiClient, user, userSettings, 'smallBackdrop', userViews, allSections);
        }
        else if (section === 'librarybuttons') {
            return loadlibraryButtons(elem, apiClient, user, userSettings, userViews, allSections);
        }
        else if (section === 'resume') {
            loadResumeVideo(elem, apiClient, userId);
        }
        else if (section === 'resumeaudio') {
            loadResumeAudio(elem, apiClient, userId);
        }
        else if (section === 'activerecordings') {
            loadLatestLiveTvRecordings(elem, true, apiClient, userId);
        }
        else if (section === 'nextup') {
            loadNextUp(elem, apiClient, userId);
        }
        else if (section === 'onnow' || section === 'livetv') {
            return loadOnNow(elem, apiClient, user);
        }
        else {

            elem.innerHTML = '';

            return Promise.resolve();
        }
        return Promise.resolve();
    }

    function getUserViews(apiClient, userId) {

        return apiClient.getUserViews({}, userId || apiClient.getCurrentUserId()).then(function (result) {

            return result.Items;
        });
    }

    function enableScrollX() {
        return true;
    }

    function getSquareShape() {
        return enableScrollX() ? 'overflowSquare' : 'square';
    }

    function getThumbShape() {
        return enableScrollX() ? 'overflowBackdrop' : 'backdrop';
    }

    function getPortraitShape() {
        return enableScrollX() ? 'autooverflow' : 'auto';
    }

    function getLibraryButtonsHtml(items) {

        var html = "";

        html += '<div class="verticalSection verticalSection-extrabottompadding">';
        html += '<div class="sectionTitleContainer sectionTitleContainer-cards">';
        html += '<h2 class="sectionTitle sectionTitle-cards padded-left">' + globalize.translate('sharedcomponents#HeaderMyMedia') + '</h2>';

        if (!layoutManager.tv) {
            html += '<button type="button" is="paper-icon-button-light" class="sectionTitleIconButton btnHomeScreenSettings"><i class="md-icon">&#xE5D3;</i></button>';
        }

        html += '</div>';

        html += '<div is="emby-itemscontainer" class="itemsContainer padded-left padded-right vertical-wrap focuscontainer-x" data-multiselect="false">';

        // "My Library" backgrounds
        for (var i = 0, length = items.length; i < length; i++) {

            var item = items[i];

            var icon;

            switch (item.CollectionType) {
                case "movies":
                    icon = "local_movies";
                    break;
                case "music":
                    icon = "library_music";
                    break;
                case "photos":
                    icon = "photo";
                    break;
                case "livetv":
                    icon = "live_tv";
                    break;
                case "tvshows":
                    icon = "live_tv";
                    break;
                case "games":
                    icon = "folder";
                    break;
                case "trailers":
                    icon = "local_movies";
                    break;
                case "homevideos":
                    icon = "video_library";
                    break;
                case "musicvideos":
                    icon = "video_library";
                    break;
                case "books":
                    icon = "folder";
                    break;
                case "channels":
                    icon = "folder";
                    break;
                case "playlists":
                    icon = "folder";
                    break;
                default:
                    icon = "folder";
                    break;
            }

            html += '<a is="emby-linkbutton" href="' + appRouter.getRouteUrl(item) + '" class="raised homeLibraryButton"><i class="md-icon">' + icon + '</i><span>' + item.Name + '</span></a>';
        }

        html += '</div>';
        html += '</div>';

        return html;
    }

    function loadlibraryButtons(elem, apiClient, user, userSettings, userViews) {

        return Promise.all([getAppInfo(apiClient), getDownloadsSectionHtml(apiClient, user, userSettings)]).then(function (responses) {

            var infoHtml = responses[0];
            var downloadsHtml = responses[1];

            elem.classList.remove('verticalSection');

            var html = getLibraryButtonsHtml(userViews);

            elem.innerHTML = html + downloadsHtml + infoHtml;

            bindHomeScreenSettingsIcon(elem, apiClient, user.Id, userSettings);

            if (infoHtml) {
                bindAppInfoEvents(elem);
            }
            imageLoader.lazyChildren(elem);
        });
    }

    function bindAppInfoEvents(elem) {

        elem.querySelector('.appInfoSection').addEventListener('click', function (e) {

            if (dom.parentWithClass(e.target, 'card')) {
                registrationServices.showPremiereInfo();
            }
        });
    }

    /**
     * Returns a random integer between min (inclusive) and max (inclusive)
     * Using Math.round() will give you a non-uniform distribution!
     */
    function getRandomInt(min, max) {
        return Math.floor(Math.random() * (max - min + 1)) + min;
    }

    function getAppInfo(apiClient) {

        var frequency = 172800000;

        var cacheKey = 'lastappinfopresent5';
        var lastDatePresented = parseInt(appSettings.get(cacheKey) || '0');

        // Don't show the first time, right after installation
        if (!lastDatePresented) {
            appSettings.set(cacheKey, new Date().getTime());
            return Promise.resolve('');
        }

        if ((new Date().getTime() - lastDatePresented) < frequency) {
            return Promise.resolve('');
        }

        return registrationServices.validateFeature('dvr', {

            showDialog: false,
            viewOnly: true

        }).then(function () {

            appSettings.set(cacheKey, new Date().getTime());
            return '';

        }, function () {

            appSettings.set(cacheKey, new Date().getTime());

            var infos = [getPremiereInfo];

            if (appHost.supports('otherapppromotions')) {
                infos.push(getTheaterInfo);
            }

            return infos[getRandomInt(0, infos.length - 1)]();
        });
    }

    function getCard(img, shape) {

        shape = shape || 'backdropCard';
        var html = '<div class="card scalableCard ' + shape + ' ' + shape + '-scalable"><div class="cardBox"><div class="cardScalable"><div class="cardPadder cardPadder-backdrop"></div>';

        html += '<div class="cardContent">';

        html += '<div class="cardImage lazy" data-src="' + img + '"></div>';

        html += '</div>';

        html += '</div></div></div>';

        return html;
    }

    function getTheaterInfo() {

        var html = '';
        html += '<div class="verticalSection appInfoSection">';
        html += '<div class="sectionTitleContainer sectionTitleContainer-cards">';
        html += '<h2 class="sectionTitle sectionTitle-cards padded-left">Discover Emby Theater</h2>';
        html += '<button is="paper-icon-button-light" class="sectionTitleButton" onclick="this.parentNode.parentNode.remove();" class="autoSize"><i class="md-icon">close</i></button>';
        html += '</div>';

        var nameText = 'Emby Theater';
        html += '<div class="padded-left padded-right">';
        html += '<p class="sectionTitle-cards">A beautiful app for your TV and large screen tablet. ' + nameText + ' runs on Windows, Xbox One, Raspberry Pi, Samsung Smart TVs, Sony PS4, Web Browsers, and more.</p>';
        html += '<div class="itemsContainer vertical-wrap" is="emby-itemscontainer">';
        html += getCard('https://raw.githubusercontent.com/MediaBrowser/Emby.Resources/master/apps/theater1.png');
        html += getCard('https://raw.githubusercontent.com/MediaBrowser/Emby.Resources/master/apps/theater2.png');
        html += getCard('https://raw.githubusercontent.com/MediaBrowser/Emby.Resources/master/apps/theater3.png');
        html += '</div>';
        html += '</div>';
        html += '</div>';
        return html;
    }

    function getPremiereInfo() {

        var html = '';
        html += '<div class="verticalSection appInfoSection">';
        html += '<div class="sectionTitleContainer sectionTitleContainer-cards">';
        html += '<h2 class="sectionTitle sectionTitle-cards padded-left">Discover Emby Premiere</h2>';
        html += '<button is="paper-icon-button-light" class="sectionTitleButton" onclick="this.parentNode.parentNode.remove();" class="autoSize"><i class="md-icon">close</i></button>';
        html += '</div>';

        html += '<div class="padded-left padded-right">';
        html += '<p class="sectionTitle-cards">Enjoy Emby DVR, get free access to Emby apps, and more.</p>';
        html += '<div class="itemsContainer vertical-wrap" is="emby-itemscontainer">';
        html += getCard('https://raw.githubusercontent.com/MediaBrowser/Emby.Resources/master/apps/theater1.png');
        html += getCard('https://raw.githubusercontent.com/MediaBrowser/Emby.Resources/master/apps/theater2.png');
        html += getCard('https://raw.githubusercontent.com/MediaBrowser/Emby.Resources/master/apps/theater3.png');
        html += '</div>';
        html += '</div>';
        html += '</div>';
        return html;
    }

    function getFetchLatestItemsFn(serverId, parentId, collectionType) {

        return function () {

            var apiClient = connectionManager.getApiClient(serverId);

            var limit = 16;

            if (enableScrollX()) {

                if (collectionType === 'music') {
                    limit = 30;
                }
            }
            else {

                if (collectionType === 'tvshows') {
                    limit = 5;
                } else if (collectionType === 'music') {
                    limit = 9;
                } else {
                    limit = 8;
                }
            }

            var options = {

                Limit: limit,
                Fields: "PrimaryImageAspectRatio,BasicSyncInfo",
                ImageTypeLimit: 1,
                EnableImageTypes: "Primary,Backdrop,Thumb",
                ParentId: parentId
            };

            return apiClient.getLatestItems(options);
        };
    }

    function getLatestItemsHtmlFn(itemType, viewType) {

        return function (items) {

            var shape = itemType === 'Channel' || viewType === 'movies' ?
                getPortraitShape() :
                viewType === 'music' ?
                    getSquareShape() :
                    getThumbShape();

            var cardLayout = false;

            return cardBuilder.getCardsHtml({
                items: items,
                shape: shape,
                preferThumb: viewType !== 'movies' && itemType !== 'Channel' && viewType !== 'music' ? 'auto' : null,
                showUnplayedIndicator: false,
                showChildCountIndicator: true,
                context: 'home',
                overlayText: false,
                centerText: !cardLayout,
                overlayPlayButton: viewType !== 'photos',
                allowBottomPadding: !enableScrollX() && !cardLayout,
                cardLayout: cardLayout,
                showTitle: viewType !== 'photos',
                showYear: viewType === 'movies' || viewType === 'tvshows' || !viewType,
                showParentTitle: viewType === 'music' || viewType === 'tvshows' || !viewType || (cardLayout && (viewType === 'tvshows')),
                lines: 2
            });
        };
    }

    function renderLatestSection(elem, apiClient, user, parent) {

        var html = '';
        html += '<div class="sectionTitleContainer sectionTitleContainer-cards padded-left">';
        if (!layoutManager.tv) {

            html += '<a is="emby-linkbutton" href="' + appRouter.getRouteUrl(parent, {

                section: 'latest'

            }) + '" class="more button-flat button-flat-mini sectionTitleTextButton">';
            html += '<h2 class="sectionTitle sectionTitle-cards">';
            html += globalize.translate('sharedcomponents#LatestFromLibrary', parent.Name);
            html += '</h2>';
            html += '<i class="md-icon">&#xE5CC;</i>';
            html += '</a>';

        } else {
            html += '<h2 class="sectionTitle sectionTitle-cards">' + globalize.translate('sharedcomponents#LatestFromLibrary', parent.Name) + '</h2>';
        }
        html += '</div>';

        if (enableScrollX()) {
            html += '<div is="emby-scroller" data-mousewheel="false" data-centerfocus="true" class="padded-top-focusscale padded-bottom-focusscale"><div is="emby-itemscontainer" class="itemsContainer scrollSlider focuscontainer-x padded-left padded-right">';
        } else {
            html += '<div is="emby-itemscontainer" class="itemsContainer padded-left padded-right vertical-wrap focuscontainer-x">';
        }

        if (enableScrollX()) {
            html += '</div>';
        }
        html += '</div>';

        elem.innerHTML = html;

        var itemsContainer = elem.querySelector('.itemsContainer');
        itemsContainer.fetchData = getFetchLatestItemsFn(apiClient.serverId(), parent.Id, parent.CollectionType);
        itemsContainer.getItemsHtml = getLatestItemsHtmlFn(parent.Type, parent.CollectionType);
        itemsContainer.parentContainer = elem;

    }

    function loadRecentlyAdded(elem, apiClient, user, userViews) {

        elem.classList.remove('verticalSection');

        var excludeViewTypes = ['playlists', 'livetv', 'boxsets', 'channels'];

        for (var i = 0, length = userViews.length; i < length; i++) {

            var item = userViews[i];

            if (user.Configuration.LatestItemsExcludes.indexOf(item.Id) !== -1) {
                continue;
            }

            if (excludeViewTypes.indexOf(item.CollectionType || []) !== -1) {
                continue;
            }

            var frag = document.createElement('div');
            frag.classList.add('verticalSection');
            frag.classList.add('hide');
            elem.appendChild(frag);

            renderLatestSection(frag, apiClient, user, item);
        }
    }

    function getRequirePromise(deps) {

        return new Promise(function (resolve, reject) {

            require(deps, resolve);
        });
    }

    function showHomeScreenSettings(elem, options) {
        return getRequirePromise(['homescreenSettingsDialog']).then(function (homescreenSettingsDialog) {

            return homescreenSettingsDialog.show(options).then(function () {

                dom.parentWithClass(elem, 'homeSectionsContainer').dispatchEvent(new CustomEvent('settingschange', {
                    cancelable: false
                }));
            });
        });
    }

    function bindHomeScreenSettingsIcon(elem, apiClient, userId, userSettings) {

        var btnHomeScreenSettings = elem.querySelector('.btnHomeScreenSettings');
        if (!btnHomeScreenSettings) {
            return;
        }

        btnHomeScreenSettings.addEventListener('click', function () {
            showHomeScreenSettings(elem, {
                serverId: apiClient.serverId(),
                userId: userId,
                userSettings: userSettings

            });
        });
    }

    function getDownloadsSectionHtml(apiClient, user, userSettings) {

        if (!appHost.supports('sync') || !user.Policy.EnableContentDownloading) {
            return Promise.resolve('');
        }

        var promise = apiClient.getLatestOfflineItems ? apiClient.getLatestOfflineItems({

            Limit: 20,
            Filters: 'IsNotFolder'

        }) : Promise.resolve([]);

        return promise.then(function (items) {

            var html = '';

            html += '<div class="verticalSection">';

            html += '<div class="sectionTitleContainer sectionTitleContainer-cards padded-left">';

            if (!layoutManager.tv) {

                html += '<a is="emby-linkbutton" href="' + appRouter.getRouteUrl('downloads') + '" class="more button-flat button-flat-mini sectionTitleTextButton">';
                html += '<h2 class="sectionTitle sectionTitle-cards">';
                html += globalize.translate('sharedcomponents#HeaderMyDownloads');
                html += '</h2>';
                html += '<i class="md-icon">&#xE5CC;</i>';
                html += '</a>';

                html += '<a is="emby-linkbutton" href="' + appRouter.getRouteUrl('managedownloads') + '" class="sectionTitleIconButton"><i class="md-icon">&#xE8B8;</i></a>';

            } else {
                html += '<h2 class="sectionTitle sectionTitle-cards">' + globalize.translate('sharedcomponents#HeaderMyDownloads') + '</h2>';
            }
            html += '</div>';

            html += '<div is="emby-scroller" data-mousewheel="false" data-centerfocus="true" class="padded-top-focusscale padded-bottom-focusscale"><div is="emby-itemscontainer" class="scrollSlider focuscontainer-x padded-left padded-right">';

            var cardLayout = false;

            html += cardBuilder.getCardsHtml({
                items: items,
                preferThumb: 'auto',
                shape: 'autooverflow',
                overlayText: false,
                showTitle: true,
                showParentTitle: true,
                lazy: true,
                showDetailsMenu: true,
                overlayPlayButton: true,
                context: 'home',
                centerText: !cardLayout,
                allowBottomPadding: false,
                cardLayout: cardLayout,
                showYear: true,
                lines: 2
            });

            html += '</div>';
            html += '</div>';

            return html;
        });
    }

    function loadLibraryTiles(elem, apiClient, user, userSettings, shape, userViews, allSections) {

        elem.classList.remove('verticalSection');

        var html = '';

        var scrollX = !layoutManager.desktop;

        if (userViews.length) {

            html += '<div class="verticalSection">';

            html += '<div class="sectionTitleContainer sectionTitleContainer-cards">';
            html += '<h2 class="sectionTitle sectionTitle-cards padded-left">' + globalize.translate('sharedcomponents#HeaderMyMedia') + '</h2>';

            if (!layoutManager.tv) {
                html += '<button type="button" is="paper-icon-button-light" class="sectionTitleIconButton btnHomeScreenSettings"><i class="md-icon">&#xE5D3;</i></button>';
            }

            html += '</div>';

            if (scrollX) {
                html += '<div is="emby-scroller" class="padded-top-focusscale padded-bottom-focusscale" data-mousewheel="false" data-centerfocus="true"><div is="emby-itemscontainer" class="scrollSlider focuscontainer-x padded-left padded-right">';
            } else {
                html += '<div is="emby-itemscontainer" class="itemsContainer padded-left padded-right vertical-wrap focuscontainer-x">';
            }

            html += cardBuilder.getCardsHtml({
                items: userViews,
                shape: scrollX ? 'overflowSmallBackdrop' : shape,
                showTitle: true,
                centerText: true,
                overlayText: false,
                lazy: true,
                transition: false,
                allowBottomPadding: !scrollX
            });

            if (scrollX) {
                html += '</div>';
            }
            html += '</div>';
            html += '</div>';
        }

        return Promise.all([getAppInfo(apiClient), getDownloadsSectionHtml(apiClient, user, userSettings)]).then(function (responses) {

            var infoHtml = responses[0];
            var downloadsHtml = responses[1];

            elem.innerHTML = html + downloadsHtml + infoHtml;

            bindHomeScreenSettingsIcon(elem, apiClient, user.Id, userSettings);

            if (infoHtml) {
                bindAppInfoEvents(elem);
            }

            imageLoader.lazyChildren(elem);
        });
    }

    function getContinueWatchingFetchFn(serverId) {

        return function () {

            var apiClient = connectionManager.getApiClient(serverId);

            var screenWidth = dom.getWindowSize().innerWidth;

            var limit;

            if (enableScrollX()) {

                limit = 12;

            } else {

                limit = screenWidth >= 1920 ? 8 : (screenWidth >= 1600 ? 8 : (screenWidth >= 1200 ? 9 : 6));
                limit = Math.min(limit, 5);
            }

            var options = {

                Limit: limit,
                Recursive: true,
                Fields: "PrimaryImageAspectRatio,BasicSyncInfo",
                ImageTypeLimit: 1,
                EnableImageTypes: "Primary,Backdrop,Thumb",
                EnableTotalRecordCount: false,
                MediaTypes: 'Video'
            };

            return apiClient.getResumableItems(apiClient.getCurrentUserId(), options);
        };
    }

    function getContinueWatchingItemsHtml(items) {

        var cardLayout = false;

        return cardBuilder.getCardsHtml({
            items: items,
            preferThumb: true,
            shape: getThumbShape(),
            overlayText: false,
            showTitle: true,
            showParentTitle: true,
            lazy: true,
            showDetailsMenu: true,
            overlayPlayButton: true,
            context: 'home',
            centerText: !cardLayout,
            allowBottomPadding: false,
            cardLayout: cardLayout,
            showYear: true,
            lines: 2
        });
    }

    function loadResumeVideo(elem, apiClient, userId) {

        var html = '';
        html += '<h2 class="sectionTitle sectionTitle-cards padded-left">' + globalize.translate('sharedcomponents#HeaderContinueWatching') + '</h2>';

        if (enableScrollX()) {
            html += '<div is="emby-scroller" data-mousewheel="false" data-centerfocus="true" class="padded-top-focusscale padded-bottom-focusscale"><div is="emby-itemscontainer" class="itemsContainer scrollSlider focuscontainer-x padded-left padded-right" data-monitor="videoplayback,markplayed">';
        } else {
            html += '<div is="emby-itemscontainer" class="itemsContainer padded-left padded-right vertical-wrap focuscontainer-x" data-monitor="videoplayback,markplayed">';
        }

        if (enableScrollX()) {
            html += '</div>';
        }
        html += '</div>';

        elem.classList.add('hide');
        elem.innerHTML = html;

        var itemsContainer = elem.querySelector('.itemsContainer');
        itemsContainer.fetchData = getContinueWatchingFetchFn(apiClient.serverId());
        itemsContainer.getItemsHtml = getContinueWatchingItemsHtml;
        itemsContainer.parentContainer = elem;
    }

    function getContinueListeningFetchFn(serverId) {

        return function () {

            var apiClient = connectionManager.getApiClient(serverId);

            var screenWidth = dom.getWindowSize().innerWidth;

            var limit;

            if (enableScrollX()) {

                limit = 12;

            } else {

                limit = screenWidth >= 1920 ? 8 : (screenWidth >= 1600 ? 8 : (screenWidth >= 1200 ? 9 : 6));
                limit = Math.min(limit, 5);
            }

            var options = {

                Limit: limit,
                Recursive: true,
                Fields: "PrimaryImageAspectRatio,BasicSyncInfo",
                ImageTypeLimit: 1,
                EnableImageTypes: "Primary,Backdrop,Thumb",
                EnableTotalRecordCount: false,
                MediaTypes: 'Audio'
            };

            return apiClient.getResumableItems(apiClient.getCurrentUserId(), options);
        };
    }

    function getContinueListeningItemsHtml(items) {

        var cardLayout = false;

        return cardBuilder.getCardsHtml({
            items: items,
            preferThumb: true,
            shape: getThumbShape(),
            overlayText: false,
            showTitle: true,
            showParentTitle: true,
            lazy: true,
            showDetailsMenu: true,
            overlayPlayButton: true,
            context: 'home',
            centerText: !cardLayout,
            allowBottomPadding: false,
            cardLayout: cardLayout,
            showYear: true,
            lines: 2
        });
    }

    function loadResumeAudio(elem, apiClient, userId) {

        var html = '';
        html += '<h2 class="sectionTitle sectionTitle-cards padded-left">' + globalize.translate('sharedcomponents#HeaderContinueWatching') + '</h2>';

        if (enableScrollX()) {
            html += '<div is="emby-scroller" data-mousewheel="false" data-centerfocus="true" class="padded-top-focusscale padded-bottom-focusscale"><div is="emby-itemscontainer" class="itemsContainer scrollSlider focuscontainer-x padded-left padded-right" data-monitor="audioplayback,markplayed">';
        } else {
            html += '<div is="emby-itemscontainer" class="itemsContainer padded-left padded-right vertical-wrap focuscontainer-x" data-monitor="audioplayback,markplayed">';
        }

        if (enableScrollX()) {
            html += '</div>';
        }
        html += '</div>';

        elem.classList.add('hide');
        elem.innerHTML = html;

        var itemsContainer = elem.querySelector('.itemsContainer');
        itemsContainer.fetchData = getContinueListeningFetchFn(apiClient.serverId());
        itemsContainer.getItemsHtml = getContinueListeningItemsHtml;
        itemsContainer.parentContainer = elem;
    }

    function bindUnlockClick(elem) {

        var btnUnlock = elem.querySelector('.btnUnlock');
        if (btnUnlock) {
            btnUnlock.addEventListener('click', function (e) {

                registrationServices.validateFeature('livetv', {

                    viewOnly: true

                }).then(function () {

                    dom.parentWithClass(elem, 'homeSectionsContainer').dispatchEvent(new CustomEvent('settingschange', {
                        cancelable: false
                    }));
                });
            });
        }
    }

    function getOnNowFetchFn(serverId) {

        return function () {

            var apiClient = connectionManager.getApiClient(serverId);

            return apiClient.getLiveTvRecommendedPrograms({

                userId: apiClient.getCurrentUserId(),
                IsAiring: true,
                limit: 24,
                ImageTypeLimit: 1,
                EnableImageTypes: "Primary,Thumb,Backdrop",
                EnableTotalRecordCount: false,
                Fields: "ChannelInfo,PrimaryImageAspectRatio"

            });
        };
    }

    function getOnNowItemsHtml(items) {

        var cardLayout = false;

        return cardBuilder.getCardsHtml({
            items: items,
            preferThumb: 'auto',
            inheritThumb: false,
            shape: (enableScrollX() ? 'autooverflow' : 'auto'),
            showParentTitleOrTitle: true,
            showTitle: true,
            centerText: true,
            coverImage: true,
            overlayText: false,
            allowBottomPadding: !enableScrollX(),
            showAirTime: true,
            showChannelName: false,
            showAirDateTime: false,
            showAirEndTime: true,
            defaultShape: getThumbShape(),
            lines: 3,
            overlayPlayButton: true
        });
    }

    function loadOnNow(elem, apiClient, user) {

        if (!user.Policy.EnableLiveTvAccess) {
            return Promise.resolve();
        }

        var promises = [];

        promises.push(registrationServices.validateFeature('livetv',
            {
                viewOnly: true,
                showDialog: false
            }).then(function () {
                return true;
            }, function () {
                return false;
            }));

        var userId = user.Id;

        promises.push(apiClient.getLiveTvRecommendedPrograms({

            userId: apiClient.getCurrentUserId(),
            IsAiring: true,
            limit: 1,
            ImageTypeLimit: 1,
            EnableImageTypes: "Primary,Thumb,Backdrop",
            EnableTotalRecordCount: false,
            Fields: "ChannelInfo,PrimaryImageAspectRatio"

        }));

        return Promise.all(promises).then(function (responses) {

            var registered = responses[0];
            var result = responses[1];
            var html = '';

            if (result.Items.length && registered) {

                elem.classList.remove('padded-left');
                elem.classList.remove('padded-right');
                elem.classList.remove('padded-bottom');
                elem.classList.remove('verticalSection');

                html += '<div class="verticalSection">';
                html += '<div class="sectionTitleContainer sectionTitleContainer-cards padded-left">';

                html += '<h2 class="sectionTitle sectionTitle-cards">' + globalize.translate('sharedcomponents#LiveTV') + '</h2>';

                html += '</div>';

                if (enableScrollX()) {
                    html += '<div is="emby-scroller" class="padded-top-focusscale padded-bottom-focusscale" data-mousewheel="false" data-centerfocus="true" data-scrollbuttons="false">';
                    html += '<div class="scrollSlider padded-left padded-right padded-top padded-bottom focuscontainer-x">';
                }
                else {
                    html += '<div class="padded-left padded-right padded-top focuscontainer-x">';
                }

                html += '<a style="margin-left:.8em;margin-right:0;" is="emby-linkbutton" href="' + appRouter.getRouteUrl('livetv', {

                    serverId: apiClient.serverId()

                }) + '" class="raised"><span>' + globalize.translate('sharedcomponents#Programs') + '</span></a>';

                html += '<a style="margin-left:.5em;margin-right:0;" is="emby-linkbutton" href="' + appRouter.getRouteUrl('livetv', {

                    serverId: apiClient.serverId(),
                    section: 'guide'

                }) + '" class="raised"><span>' + globalize.translate('sharedcomponents#Guide') + '</span></a>';

                html += '<a style="margin-left:.5em;margin-right:0;" is="emby-linkbutton" href="' + appRouter.getRouteUrl('recordedtv', {

                    serverId: apiClient.serverId()

                }) + '" class="raised"><span>' + globalize.translate('sharedcomponents#Recordings') + '</span></a>';

                html += '<a style="margin-left:.5em;margin-right:0;" is="emby-linkbutton" href="' + appRouter.getRouteUrl('livetv', {

                    serverId: apiClient.serverId(),
                    section: 'dvrschedule'

                }) + '" class="raised"><span>' + globalize.translate('sharedcomponents#Schedule') + '</span></a>';

                html += '</div>';

                if (enableScrollX()) {
                    html += '</div>';
                }

                html += '</div>';
                html += '</div>';

                html += '<div class="verticalSection">';
                html += '<div class="sectionTitleContainer sectionTitleContainer-cards padded-left">';

                if (!layoutManager.tv) {

                    html += '<a is="emby-linkbutton" href="' + appRouter.getRouteUrl('livetv', {

                        serverId: apiClient.serverId(),
                        section: 'onnow'

                    }) + '" class="more button-flat button-flat-mini sectionTitleTextButton">';
                    html += '<h2 class="sectionTitle sectionTitle-cards">';
                    html += globalize.translate('sharedcomponents#HeaderOnNow');
                    html += '</h2>';
                    html += '<i class="md-icon">&#xE5CC;</i>';
                    html += '</a>';

                } else {
                    html += '<h2 class="sectionTitle sectionTitle-cards">' + globalize.translate('sharedcomponents#HeaderOnNow') + '</h2>';
                }
                html += '</div>';

                if (enableScrollX()) {
                    html += '<div is="emby-scroller" data-mousewheel="false" data-centerfocus="true" class="padded-top-focusscale padded-bottom-focusscale"><div is="emby-itemscontainer" class="itemsContainer scrollSlider focuscontainer-x padded-left padded-right" data-refreshinterval="300000">';
                } else {
                    html += '<div is="emby-itemscontainer" class="itemsContainer padded-left padded-right vertical-wrap focuscontainer-x" data-refreshinterval="300000">';
                }

                if (enableScrollX()) {
                    html += '</div>';
                }

                html += '</div>';
                html += '</div>';

                elem.innerHTML = html;

                var itemsContainer = elem.querySelector('.itemsContainer');
                itemsContainer.parentContainer = elem;
                itemsContainer.fetchData = getOnNowFetchFn(apiClient.serverId());
                itemsContainer.getItemsHtml = getOnNowItemsHtml;

            } else if (result.Items.length && !registered) {

                elem.classList.add('padded-left');
                elem.classList.add('padded-right');
                elem.classList.add('padded-bottom');

                html += '<h2 class="sectionTitle">' + globalize.translate('sharedcomponents#LiveTvRequiresUnlock') + '</h2>';
                html += '<button is="emby-button" type="button" class="raised button-submit block btnUnlock">';
                html += '<span>' + globalize.translate('sharedcomponents#HeaderBecomeProjectSupporter') + '</span>';
                html += '</button>';

                elem.innerHTML = html;
            }

            bindUnlockClick(elem);
        });
    }

    function getNextUpFetchFn(serverId) {

        return function () {

            var apiClient = connectionManager.getApiClient(serverId);

            return apiClient.getNextUpEpisodes({

                Limit: enableScrollX() ? 24 : 15,
                Fields: "PrimaryImageAspectRatio,SeriesInfo,DateCreated,BasicSyncInfo",
                UserId: apiClient.getCurrentUserId(),
                ImageTypeLimit: 1,
                EnableImageTypes: "Primary,Backdrop,Banner,Thumb",
                EnableTotalRecordCount: false
            });
        };
    }

    function getNextUpItemsHtml(items) {

        var cardLayout = false;

        return cardBuilder.getCardsHtml({
            items: items,
            preferThumb: true,
            shape: getThumbShape(),
            overlayText: false,
            showTitle: true,
            showParentTitle: true,
            lazy: true,
            overlayPlayButton: true,
            context: 'home',
            centerText: !cardLayout,
            allowBottomPadding: !enableScrollX(),
            cardLayout: cardLayout
        });
    }

    function loadNextUp(elem, apiClient, userId) {

        var html = '';
        html += '<div class="sectionTitleContainer sectionTitleContainer-cards padded-left">';
        if (!layoutManager.tv) {

            html += '<a is="emby-linkbutton" href="' + appRouter.getRouteUrl('nextup', {

                serverId: apiClient.serverId()

            }) + '" class="button-flat button-flat-mini sectionTitleTextButton">';
            html += '<h2 class="sectionTitle sectionTitle-cards">';
            html += globalize.translate('sharedcomponents#HeaderNextUp');
            html += '</h2>';
            html += '<i class="md-icon">&#xE5CC;</i>';
            html += '</a>';

        } else {
            html += '<h2 class="sectionTitle sectionTitle-cards">' + globalize.translate('sharedcomponents#HeaderNextUp') + '</h2>';
        }
        html += '</div>';

        if (enableScrollX()) {
            html += '<div is="emby-scroller" data-mousewheel="false" data-centerfocus="true" class="padded-top-focusscale padded-bottom-focusscale"><div is="emby-itemscontainer" class="itemsContainer scrollSlider focuscontainer-x padded-left padded-right" data-monitor="videoplayback,markplayed">';
        } else {
            html += '<div is="emby-itemscontainer" class="itemsContainer padded-left padded-right vertical-wrap focuscontainer-x" data-monitor="videoplayback,markplayed">';
        }

        if (enableScrollX()) {
            html += '</div>';
        }

        html += '</div>';

        elem.classList.add('hide');
        elem.innerHTML = html;

        var itemsContainer = elem.querySelector('.itemsContainer');
        itemsContainer.fetchData = getNextUpFetchFn(apiClient.serverId());
        itemsContainer.getItemsHtml = getNextUpItemsHtml;
        itemsContainer.parentContainer = elem;
    }

    function getLatestRecordingsFetchFn(serverId, activeRecordingsOnly) {

        return function () {

            var apiClient = connectionManager.getApiClient(serverId);

            return apiClient.getLiveTvRecordings({

                userId: apiClient.getCurrentUserId(),
                Limit: enableScrollX() ? 12 : 5,
                Fields: "PrimaryImageAspectRatio,BasicSyncInfo",
                EnableTotalRecordCount: false,
                IsLibraryItem: activeRecordingsOnly ? null : false,
                IsInProgress: activeRecordingsOnly ? true : null

            });
        };
    }

    function getLatestRecordingItemsHtml(activeRecordingsOnly) {

        return function (items) {
            var cardLayout = false;

            return cardBuilder.getCardsHtml({
                items: items,
                shape: enableScrollX() ? 'autooverflow' : 'auto',
                showTitle: true,
                showParentTitle: true,
                coverImage: true,
                lazy: true,
                showDetailsMenu: true,
                centerText: true,
                overlayText: false,
                showYear: true,
                lines: 2,
                overlayPlayButton: !activeRecordingsOnly,
                allowBottomPadding: !enableScrollX(),
                preferThumb: true,
                cardLayout: false,
                overlayMoreButton: activeRecordingsOnly,
                action: activeRecordingsOnly ? 'none' : null,
                centerPlayButton: activeRecordingsOnly
            });
        };
    }

    function loadLatestLiveTvRecordings(elem, activeRecordingsOnly, apiClient, userId) {

        var title = activeRecordingsOnly ?
            globalize.translate('sharedcomponents#HeaderActiveRecordings') :
            globalize.translate('sharedcomponents#HeaderLatestRecordings');

        var html = '';

        html += '<div class="sectionTitleContainer sectionTitleContainer-cards">';
        html += '<h2 class="sectionTitle sectionTitle-cards padded-left">' + title + '</h2>';
        if (!layoutManager.tv) {
            //html += '<a href="livetv.html?tab=3" class="clearLink" style="margin-left:2em;"><button is="emby-button" type="button" class="raised more mini"><span>' + globalize.translate('sharedcomponents#More') + '</span></button></a>';
            //html += '<button data-href="" type="button" is="emby-button" class="raised raised-mini sectionTitleButton btnMore">';
            //html += '<span>' + globalize.translate('sharedcomponents#More') + '</span>';
            //html += '</button>';
        }
        html += '</div>';

        if (enableScrollX()) {
            html += '<div is="emby-scroller" data-mousewheel="false" data-centerfocus="true" class="padded-top-focusscale padded-bottom-focusscale"><div is="emby-itemscontainer" class="itemsContainer scrollSlider focuscontainer-x padded-left padded-right">';
        } else {
            html += '<div is="emby-itemscontainer" class="itemsContainer padded-left padded-right vertical-wrap focuscontainer-x">';
        }

        if (enableScrollX()) {
            html += '</div>';
        }

        html += '</div>';

        elem.classList.add('hide');
        elem.innerHTML = html;

        var itemsContainer = elem.querySelector('.itemsContainer');
        itemsContainer.fetchData = getLatestRecordingsFetchFn(apiClient.serverId(), activeRecordingsOnly);
        itemsContainer.getItemsHtml = getLatestRecordingItemsHtml(activeRecordingsOnly);
        itemsContainer.parentContainer = elem;
    }

    return {
        loadLibraryTiles: loadLibraryTiles,
        getDefaultSection: getDefaultSection,
        loadSections: loadSections,
        destroySections: destroySections,
        pause: pause,
        resume: resume
    };
});