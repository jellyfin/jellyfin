define(['viewManager', 'appSettings', 'appStorage', 'apphost', 'datetime', 'itemHelper', 'mediaInfo', 'scroller', 'indicators', 'dom', 'scrollStyles'], function (viewManager, appSettings, appStorage, appHost, datetime, itemHelper, mediaInfo, scroller, indicators, dom) {

    function fadeInRight(elem) {

        var pct = browserInfo.mobile ? '3.5%' : '0.5%';

        var keyframes = [
          { opacity: '0', transform: 'translate3d(' + pct + ', 0, 0)', offset: 0 },
          { opacity: '1', transform: 'none', offset: 1 }];

        elem.animate(keyframes, {
            duration: 160,
            iterations: 1,
            easing: 'ease-out'
        });
    }

    function animateSelectionBar(button) {

        var elem = button.querySelector('.pageTabButtonSelectionBar');

        if (!elem) {
            return;
        }

        var keyframes = [
          { transform: 'translate3d(-100%, 0, 0)', offset: 0 },
          { transform: 'none', offset: 1 }];

        if (!elem.animate) {
            return;
        }
        elem.animate(keyframes, {
            duration: 120,
            iterations: 1,
            easing: 'ease-out'
        });
    }

    var libraryBrowser = (function (window, document, screen) {

        // Regular Expressions for parsing tags and attributes
        var SURROGATE_PAIR_REGEXP = /[\uD800-\uDBFF][\uDC00-\uDFFF]/g,
          // Match everything outside of normal chars and " (quote character)
          NON_ALPHANUMERIC_REGEXP = /([^\#-~| |!])/g;

        /**
         * Escapes all potentially dangerous characters, so that the
         * resulting string can be safely inserted into attribute or
         * element text.
         * @param value
         * @returns {string} escaped text
         */
        function htmlEncode(value) {
            return value.
              replace(/&/g, '&amp;').
              replace(SURROGATE_PAIR_REGEXP, function (value) {
                  var hi = value.charCodeAt(0);
                  var low = value.charCodeAt(1);
                  return '&#' + (((hi - 0xD800) * 0x400) + (low - 0xDC00) + 0x10000) + ';';
              }).
              replace(NON_ALPHANUMERIC_REGEXP, function (value) {
                  return '&#' + value.charCodeAt(0) + ';';
              }).
              replace(/</g, '&lt;').
              replace(/>/g, '&gt;');
        }

        var pageSizeKey = 'pagesize_v4';

        function getDesiredAspect(shape) {

            if (shape) {
                shape = shape.toLowerCase();
                if (shape.indexOf('portrait') != -1) {
                    return (2 / 3);
                }
                if (shape.indexOf('backdrop') != -1) {
                    return (16 / 9);
                }
                if (shape.indexOf('square') != -1) {
                    return 1;
                }
            }
            return null;
        }

        var libraryBrowser = {
            getDefaultPageSize: function (key, defaultValue) {

                return 100;
            },

            getDefaultItemsView: function (view, mobileView) {

                return browserInfo.mobile ? mobileView : view;

            },

            getSavedQueryKey: function (modifier) {

                return window.location.href.split('#')[0] + (modifier || '');
            },

            loadSavedQueryValues: function (key, query) {

                var values = appStorage.getItem(key + '_' + Dashboard.getCurrentUserId());

                if (values) {

                    values = JSON.parse(values);

                    return Object.assign(query, values);
                }

                return query;
            },

            saveQueryValues: function (key, query) {

                var values = {};

                if (query.SortBy) {
                    values.SortBy = query.SortBy;
                }
                if (query.SortOrder) {
                    values.SortOrder = query.SortOrder;
                }

                try {
                    appStorage.setItem(key + '_' + Dashboard.getCurrentUserId(), JSON.stringify(values));
                } catch (e) {

                }
            },

            saveViewSetting: function (key, value) {

                try {
                    appStorage.setItem(key + '_' + Dashboard.getCurrentUserId() + '_view', value);
                } catch (e) {

                }
            },

            getSavedView: function (key) {

                var val = appStorage.getItem(key + '_' + Dashboard.getCurrentUserId() + '_view');

                return val;
            },

            getSavedViewSetting: function (key) {

                return new Promise(function (resolve, reject) {

                    var val = LibraryBrowser.getSavedView(key);
                    resolve(val);
                });
            },

            allowSwipe: function (target) {

                function allowSwipeOn(elem) {

                    if (elem.classList) {
                        return !elem.classList.contains('hiddenScrollX') && !elem.classList.contains('smoothScrollX') && !elem.classList.contains('libraryViewNav');
                    }

                    return true;
                }

                var parent = target;
                while (parent != null) {
                    if (!allowSwipeOn(parent)) {
                        return false;
                    }
                    parent = parent.parentNode;
                }

                return true;
            },

            selectedTab: function (tabs, selected) {

                if (selected == null) {

                    return tabs.selectedTabIndex || 0;
                }

                var current = LibraryBrowser.selectedTab(tabs);
                tabs.selectedTabIndex = selected;
                if (current == selected) {
                    tabs.dispatchEvent(new CustomEvent("beforetabchange", {
                        detail: {
                            selectedTabIndex: selected
                        }
                    }));
                    tabs.dispatchEvent(new CustomEvent("tabchange", {
                        detail: {
                            selectedTabIndex: selected
                        }
                    }));
                } else {
                    var tabButtons = tabs.querySelectorAll('.pageTabButton');
                    tabButtons[selected].click();
                }
            },

            configureSwipeTabs: function (ownerpage, tabs) {

                var pageCount = ownerpage.querySelectorAll('.pageTabContent').length;

                require(['hammer'], function (Hammer) {

                    var hammertime = new Hammer(ownerpage);
                    hammertime.get('swipe').set({ direction: Hammer.DIRECTION_HORIZONTAL });

                    hammertime.on('swipeleft', function (e) {
                        if (LibraryBrowser.allowSwipe(e.target)) {
                            var selected = parseInt(LibraryBrowser.selectedTab(tabs) || '0');
                            if (selected < (pageCount - 1)) {
                                LibraryBrowser.selectedTab(tabs, selected + 1);
                            }
                        }
                    });

                    hammertime.on('swiperight', function (e) {
                        if (LibraryBrowser.allowSwipe(e.target)) {
                            var selected = parseInt(LibraryBrowser.selectedTab(tabs) || '0');
                            if (selected > 0) {
                                LibraryBrowser.selectedTab(tabs, selected - 1);
                            }
                        }
                    });
                });
            },

            configurePaperLibraryTabs: function (ownerpage, tabs, panels, animateTabs) {

                if (!browserInfo.safari) {
                    LibraryBrowser.configureSwipeTabs(ownerpage, tabs);
                }

                if (!browserInfo.safari || !AppInfo.isNativeApp) {
                    var buttons = tabs.querySelectorAll('.pageTabButton');
                    for (var i = 0, length = buttons.length; i < length; i++) {
                        var div = document.createElement('div');
                        div.classList.add('pageTabButtonSelectionBar');
                        buttons[i].appendChild(div);
                    }
                }

                tabs.addEventListener('click', function (e) {

                    var current = tabs.querySelector('.is-active');
                    var link = dom.parentWithClass(e.target, 'pageTabButton');

                    if (link && link != current) {

                        if (current) {
                            current.classList.remove('is-active');
                            panels[parseInt(current.getAttribute('data-index'))].classList.remove('is-active');
                        }

                        link.classList.add('is-active');
                        animateSelectionBar(link);
                        var index = parseInt(link.getAttribute('data-index'));
                        var newPanel = panels[index];

                        tabs.dispatchEvent(new CustomEvent("beforetabchange", {
                            detail: {
                                selectedTabIndex: index
                            }
                        }));

                        // If toCenter is called syncronously within the click event, it sometimes ends up canceling it
                        setTimeout(function () {

                            if (animateTabs && animateTabs.indexOf(index) != -1 && /*browserInfo.animate &&*/ newPanel.animate) {
                                fadeInRight(newPanel);
                            }

                            tabs.selectedTabIndex = index;

                            tabs.dispatchEvent(new CustomEvent("tabchange", {
                                detail: {
                                    selectedTabIndex: index
                                }
                            }));

                            newPanel.classList.add('is-active');
                        }, 120);

                        if (tabs.scroller) {
                            tabs.scroller.toCenter(link, false);
                        }
                    }
                });

                ownerpage.addEventListener('viewbeforeshow', LibraryBrowser.onTabbedpagebeforeshow);

                var contentScrollSlider = tabs.querySelector('.contentScrollSlider');
                if (contentScrollSlider) {
                    tabs.scroller = new scroller(tabs, {
                        horizontal: 1,
                        itemNav: 0,
                        mouseDragging: 1,
                        touchDragging: 1,
                        slidee: tabs.querySelector('.contentScrollSlider'),
                        smart: true,
                        releaseSwing: true,
                        scrollBy: 200,
                        speed: 120,
                        elasticBounds: 1,
                        dragHandle: 1,
                        dynamicHandle: 1,
                        clickBar: 1,
                        //centerOffset: window.innerWidth * .05,
                        hiddenScroll: true,
                        requireAnimation: true
                    });
                    tabs.scroller.init();
                } else {
                    tabs.classList.add('hiddenScrollX');
                }
            },

            onTabbedpagebeforeshow: function (e) {

                var page = e.target;
                var delay = 0;
                var isFirstLoad = false;

                if (!page.getAttribute('data-firstload')) {
                    delay = 300;
                    isFirstLoad = true;
                    page.setAttribute('data-firstload', '1');
                }

                if (delay) {
                    setTimeout(function () {

                        LibraryBrowser.onTabbedpagebeforeshowInternal(page, e, isFirstLoad);
                    }, delay);
                } else {
                    LibraryBrowser.onTabbedpagebeforeshowInternal(page, e, isFirstLoad);
                }
            },

            onTabbedpagebeforeshowInternal: function (page, e, isFirstLoad) {

                var pageTabsContainer = page.querySelector('.libraryViewNav');

                if (isFirstLoad) {

                    console.log('selected tab is null, checking query string');

                    var selected = page.firstTabIndex != null ? page.firstTabIndex : parseInt(getParameterByName('tab') || '0');

                    console.log('selected tab will be ' + selected);

                    LibraryBrowser.selectedTab(pageTabsContainer, selected);

                } else {

                    // Go back to the first tab
                    if (!e.detail.isRestored) {
                        LibraryBrowser.selectedTab(pageTabsContainer, 0);
                        return;
                    }
                    pageTabsContainer.dispatchEvent(new CustomEvent("beforetabchange", {
                        detail: {
                            selectedTabIndex: LibraryBrowser.selectedTab(pageTabsContainer)
                        }
                    }));
                    pageTabsContainer.dispatchEvent(new CustomEvent("tabchange", {
                        detail: {
                            selectedTabIndex: LibraryBrowser.selectedTab(pageTabsContainer)
                        }
                    }));
                }
            },

            showTab: function (url, index) {

                var afterNavigate = function () {

                    document.removeEventListener('pagebeforeshow', afterNavigate);

                    if (window.location.href.toLowerCase().indexOf(url.toLowerCase()) != -1) {

                        this.firstTabIndex = index;
                    }
                };

                if (window.location.href.toLowerCase().indexOf(url.toLowerCase()) != -1) {

                    afterNavigate.call(viewManager.currentView());
                } else {

                    pageClassOn('pagebeforeshow', 'page', afterNavigate);
                    Dashboard.navigate(url);
                }
            },

            getDateParamValue: function (date) {

                function formatDigit(i) {
                    return i < 10 ? "0" + i : i;
                }

                var d = date;

                return "" + d.getFullYear() + formatDigit(d.getMonth() + 1) + formatDigit(d.getDate()) + formatDigit(d.getHours()) + formatDigit(d.getMinutes()) + formatDigit(d.getSeconds());
            },

            playAllFromHere: function (fn, index) {

                fn(index, 100, "MediaSources,Chapters").then(function (result) {

                    MediaController.play({
                        items: result.Items
                    });
                });
            },

            queueAllFromHere: function (query, index) {

                fn(index, 100, "MediaSources,Chapters").then(function (result) {

                    MediaController.queue({
                        items: result.Items
                    });
                });
            },

            getItemCountsHtml: function (options, item) {

                var counts = [];

                var childText;

                if (item.Type == 'Playlist') {

                    childText = '';

                    if (item.CumulativeRunTimeTicks) {

                        var minutes = item.CumulativeRunTimeTicks / 600000000;

                        minutes = minutes || 1;

                        childText += Globalize.translate('ValueMinutes', Math.round(minutes));

                    } else {
                        childText += Globalize.translate('ValueMinutes', 0);
                    }

                    counts.push(childText);

                }
                else if (options.context == "movies") {

                    if (item.MovieCount) {

                        childText = item.MovieCount == 1 ?
                        Globalize.translate('ValueOneMovie') :
                        Globalize.translate('ValueMovieCount', item.MovieCount);

                        counts.push(childText);
                    }
                    if (item.TrailerCount) {

                        childText = item.TrailerCount == 1 ?
                        Globalize.translate('ValueOneTrailer') :
                        Globalize.translate('ValueTrailerCount', item.TrailerCount);

                        counts.push(childText);
                    }

                } else if (options.context == "tv") {

                    if (item.SeriesCount) {

                        childText = item.SeriesCount == 1 ?
                        Globalize.translate('ValueOneSeries') :
                        Globalize.translate('ValueSeriesCount', item.SeriesCount);

                        counts.push(childText);
                    }
                    if (item.EpisodeCount) {

                        childText = item.EpisodeCount == 1 ?
                        Globalize.translate('ValueOneEpisode') :
                        Globalize.translate('ValueEpisodeCount', item.EpisodeCount);

                        counts.push(childText);
                    }

                } else if (options.context == "games") {

                    if (item.GameCount) {

                        childText = item.GameCount == 1 ?
                        Globalize.translate('ValueOneGame') :
                        Globalize.translate('ValueGameCount', item.GameCount);

                        counts.push(childText);
                    }
                } else if (options.context == "music") {

                    if (item.AlbumCount) {

                        childText = item.AlbumCount == 1 ?
                        Globalize.translate('ValueOneAlbum') :
                        Globalize.translate('ValueAlbumCount', item.AlbumCount);

                        counts.push(childText);
                    }
                    if (item.SongCount) {

                        childText = item.SongCount == 1 ?
                        Globalize.translate('ValueOneSong') :
                        Globalize.translate('ValueSongCount', item.SongCount);

                        counts.push(childText);
                    }
                    if (item.MusicVideoCount) {

                        childText = item.MusicVideoCount == 1 ?
                        Globalize.translate('ValueOneMusicVideo') :
                        Globalize.translate('ValueMusicVideoCount', item.MusicVideoCount);

                        counts.push(childText);
                    }
                }

                return counts.join(' • ');
            },

            getArtistLinksHtml: function (artists, cssClass) {

                var html = [];

                for (var i = 0, length = artists.length; i < length; i++) {

                    var artist = artists[i];

                    var css = cssClass ? (' class="' + cssClass + '"') : '';
                    html.push('<a' + css + ' href="itemdetails.html?id=' + artist.Id + '">' + artist.Name + '</a>');

                }

                html = html.join(' / ');

                return html;
            },

            playInExternalPlayer: function (id) {

                Dashboard.loadExternalPlayer().then(function () {
                    ExternalPlayer.showMenu(id);
                });
            },

            editImages: function (itemId) {

                return new Promise(function (resolve, reject) {

                    require(['components/imageeditor/imageeditor'], function (ImageEditor) {

                        ImageEditor.show(itemId).then(resolve, reject);
                    });
                });
            },

            getHref: function (item, context, topParentId) {

                var href = LibraryBrowser.getHrefInternal(item, context);

                if (context == 'tv') {
                    if (!topParentId) {
                        topParentId = LibraryMenu.getTopParentId();
                    }

                    if (topParentId) {
                        href += href.indexOf('?') == -1 ? "?topParentId=" : "&topParentId=";
                        href += topParentId;
                    }
                }

                return href;
            },

            getHrefInternal: function (item, context) {

                if (!item) {
                    throw new Error('item cannot be null');
                }

                if (item.url) {
                    return item.url;
                }

                // Handle search hints
                var id = item.Id || item.ItemId;

                if (item.CollectionType == 'livetv') {
                    return 'livetv.html';
                }

                if (item.CollectionType == 'channels') {

                    return 'channels.html';
                }

                if (context != 'folders') {
                    if (item.CollectionType == 'movies') {
                        return 'movies.html?topParentId=' + item.Id;
                    }

                    if (item.CollectionType == 'boxsets') {
                        return 'itemlist.html?topParentId=' + item.Id + '&parentId=' + item.Id;
                    }

                    if (item.CollectionType == 'tvshows') {
                        return 'tv.html?topParentId=' + item.Id;
                    }

                    if (item.CollectionType == 'music') {
                        return 'music.html?topParentId=' + item.Id;
                    }

                    if (item.CollectionType == 'games') {
                        return id ? "itemlist.html?parentId=" + id : "#";
                        //return 'gamesrecommended.html?topParentId=' + item.Id;
                    }
                    if (item.CollectionType == 'playlists') {
                        return 'playlists.html?topParentId=' + item.Id;
                    }
                    if (item.CollectionType == 'photos') {
                        return 'photos.html?topParentId=' + item.Id;
                    }
                }
                else if (item.IsFolder) {
                    return id ? "itemlist.html?parentId=" + id : "#";
                }

                if (item.Type == 'CollectionFolder') {
                    return 'itemlist.html?topParentId=' + item.Id + '&parentId=' + item.Id;
                }

                if (item.Type == "PhotoAlbum") {
                    return "itemlist.html?context=photos&parentId=" + id;
                }
                if (item.Type == "Playlist") {
                    return "itemdetails.html?id=" + id;
                }
                if (item.Type == "TvChannel") {
                    return "itemdetails.html?id=" + id;
                }
                if (item.Type == "Channel") {
                    return "channelitems.html?id=" + id;
                }
                if ((item.IsFolder && item.SourceType == 'Channel') || item.Type == 'ChannelFolderItem') {
                    return "channelitems.html?id=" + item.ChannelId + '&folderId=' + item.Id;
                }
                if (item.Type == "Program") {
                    return "itemdetails.html?id=" + id;
                }

                if (item.Type == "BoxSet") {
                    return "itemdetails.html?id=" + id;
                }
                if (item.Type == "MusicAlbum") {
                    return "itemdetails.html?id=" + id;
                }
                if (item.Type == "GameSystem") {
                    return "itemdetails.html?id=" + id;
                }
                if (item.Type == "Genre") {
                    return "itemdetails.html?id=" + id;
                }
                if (item.Type == "MusicGenre") {
                    return "itemdetails.html?id=" + id;
                }
                if (item.Type == "GameGenre") {
                    return "itemdetails.html?id=" + id;
                }
                if (item.Type == "Studio") {
                    return "itemdetails.html?id=" + id;
                }
                if (item.Type == "Person") {
                    return "itemdetails.html?id=" + id;
                }
                if (item.Type == "Recording") {
                    return "itemdetails.html?id=" + id;
                }

                if (item.Type == "MusicArtist") {
                    return "itemdetails.html?id=" + id;
                }

                var contextSuffix = context ? ('&context=' + context) : '';

                if (item.Type == "Series" || item.Type == "Season" || item.Type == "Episode") {
                    return "itemdetails.html?id=" + id + contextSuffix;
                }

                if (item.IsFolder) {
                    return id ? "itemlist.html?parentId=" + id : "#";
                }

                return "itemdetails.html?id=" + id;
            },

            getImageUrl: function (item, type, index, options) {

                options = options || {};
                options.type = type;
                options.index = index;

                if (type == 'Backdrop') {
                    options.tag = item.BackdropImageTags[index];
                } else if (type == 'Screenshot') {
                    options.tag = item.ScreenshotImageTags[index];
                } else if (type == 'Primary') {
                    options.tag = item.PrimaryImageTag || item.ImageTags[type];
                } else {
                    options.tag = item.ImageTags[type];
                }

                // For search hints
                return ApiClient.getScaledImageUrl(item.Id || item.ItemId, options);

            },

            getUserDataCssClass: function (key) {

                if (!key) return '';

                return 'libraryItemUserData' + key.replace(new RegExp(' ', 'g'), '');
            },

            getItemDataAttributesList: function (item, options, index) {

                var atts = [];

                atts.push({
                    name: 'serverid',
                    value: item.ServerId || options.serverId
                });

                atts.push({
                    name: 'id',
                    value: item.Id
                });

                if (options.context) {
                    atts.push({
                        name: 'context',
                        value: options.context || ''
                    });
                }

                if (item.IsFolder) {
                    atts.push({
                        name: 'isfolder',
                        value: item.IsFolder
                    });
                }

                atts.push({
                    name: 'type',
                    value: item.Type
                });

                if (item.MediaType) {
                    atts.push({
                        name: 'mediatype',
                        value: item.MediaType || ''
                    });
                }

                if (item.UserData && item.UserData.PlaybackPositionTicks) {
                    atts.push({
                        name: 'positionticks',
                        value: (item.UserData.PlaybackPositionTicks || 0)
                    });
                }

                atts.push({
                    name: 'index',
                    value: index
                });

                if (item.ChannelId) {
                    atts.push({
                        name: 'channelid',
                        value: item.ChannelId
                    });
                }

                if (item.SeriesId) {
                    atts.push({
                        name: 'seriesid',
                        value: item.SeriesId
                    });
                }

                if (options.collectionId) {
                    atts.push({
                        name: 'collectionid',
                        value: options.collectionId
                    });
                }

                return atts;
            },

            getItemDataAttributes: function (item, options, index) {

                var atts = LibraryBrowser.getItemDataAttributesList(item, options, index).map(function (i) {
                    return 'data-' + i.name + '="' + i.value + '"';
                });

                var html = atts.join(' ');

                if (html) {
                    html = ' ' + html;
                }

                return html;
            },

            shapes: ['square', 'portrait', 'banner', 'smallBackdrop', 'backdrop', 'overflowBackdrop', 'overflowPortrait', 'overflowSquare'],

            getPostersPerRow: function (screenWidth) {

                function getValue(shape) {

                    switch (shape) {

                        case 'portrait':
                            if (screenWidth >= 2200) return 10;
                            if (screenWidth >= 2100) return 9;
                            if (screenWidth >= 1600) return 8;
                            if (screenWidth >= 1400) return 7;
                            if (screenWidth >= 1200) return 6;
                            if (screenWidth >= 800) return 5;
                            if (screenWidth >= 640) return 4;
                            return 3;
                        case 'square':
                            if (screenWidth >= 2100) return 9;
                            if (screenWidth >= 1800) return 8;
                            if (screenWidth >= 1400) return 7;
                            if (screenWidth >= 1200) return 6;
                            if (screenWidth >= 900) return 5;
                            if (screenWidth >= 700) return 4;
                            if (screenWidth >= 500) return 3;
                            return 2;
                        case 'banner':
                            if (screenWidth >= 2200) return 4;
                            if (screenWidth >= 1200) return 3;
                            if (screenWidth >= 800) return 2;
                            return 1;
                        case 'backdrop':
                            if (screenWidth >= 2500) return 6;
                            if (screenWidth >= 2100) return 5;
                            if (screenWidth >= 1200) return 4;
                            if (screenWidth >= 770) return 3;
                            if (screenWidth >= 420) return 2;
                            return 1;
                        case 'smallBackdrop':
                            if (screenWidth >= 1440) return 8;
                            if (screenWidth >= 1100) return 6;
                            if (screenWidth >= 800) return 5;
                            if (screenWidth >= 600) return 4;
                            if (screenWidth >= 540) return 3;
                            if (screenWidth >= 420) return 2;
                            return 1;
                        case 'overflowPortrait':
                            if (screenWidth >= 1000) return 100 / 23;
                            if (screenWidth >= 640) return 100 / 36;
                            return 2.5;
                        case 'overflowSquare':
                            if (screenWidth >= 1000) return 100 / 22;
                            if (screenWidth >= 640) return 100 / 30;
                            return 100 / 42;
                        case 'overflowBackdrop':
                            if (screenWidth >= 1000) return 100 / 40;
                            if (screenWidth >= 640) return 100 / 60;
                            return 100 / 84;
                        default:
                            return 4;
                    }
                }

                var info = {};

                for (var i = 0, length = LibraryBrowser.shapes.length; i < length; i++) {
                    var currentShape = LibraryBrowser.shapes[i];
                    info[currentShape] = getValue(currentShape);
                }
                return info;
            },

            posterSizes: [],

            getPosterViewInfo: function () {

                var screenWidth = window.innerWidth;

                if (!browserInfo.mobile) {
                    var roundScreenTo = 100;
                    screenWidth = Math.ceil(screenWidth / roundScreenTo) * roundScreenTo;
                }

                var cachedResults = LibraryBrowser.posterSizes;

                for (var i = 0, length = cachedResults.length; i < length; i++) {

                    if (cachedResults[i].screenWidth == screenWidth) {
                        return cachedResults[i];
                    }
                }

                var result = LibraryBrowser.getPosterViewInfoInternal(screenWidth);
                result.screenWidth = screenWidth;

                cachedResults.push(result);

                return result;
            },

            getPosterViewInfoInternal: function (screenWidth) {

                var imagesPerRow = LibraryBrowser.getPostersPerRow(screenWidth);

                var result = {};
                result.screenWidth = screenWidth;

                for (var i = 0, length = LibraryBrowser.shapes.length; i < length; i++) {
                    var currentShape = LibraryBrowser.shapes[i];

                    var shapeWidth = screenWidth / imagesPerRow[currentShape];

                    result[currentShape + 'Width'] = Math.round(shapeWidth);
                }

                return result;
            },

            setPosterViewData: function (options) {

                var items = options.items;

                options.shape = options.shape || "portrait";

                var primaryImageAspectRatio = LibraryBrowser.getAveragePrimaryImageAspectRatio(items);
                var isThumbAspectRatio = primaryImageAspectRatio && Math.abs(primaryImageAspectRatio - 1.777777778) < .3;
                var isSquareAspectRatio = primaryImageAspectRatio && Math.abs(primaryImageAspectRatio - 1) < .33 ||
                    primaryImageAspectRatio && Math.abs(primaryImageAspectRatio - 1.3333334) < .01;

                if (options.shape == 'auto' || options.shape == 'autohome' || options.shape == 'autooverflow') {

                    if (isThumbAspectRatio) {
                        options.shape = options.shape == 'autooverflow' ? 'overflowBackdrop' : 'backdrop';
                    } else if (isSquareAspectRatio) {
                        options.coverImage = true;
                        options.shape = options.shape == 'autooverflow' ? 'overflowSquare' : 'square';
                    } else if (primaryImageAspectRatio && primaryImageAspectRatio > 1.9) {
                        options.shape = 'banner';
                        options.coverImage = true;
                    } else if (primaryImageAspectRatio && Math.abs(primaryImageAspectRatio - 0.6666667) < .2) {
                        options.shape = options.shape == 'autooverflow' ? 'overflowPortrait' : 'portrait';
                    } else {
                        options.shape = options.defaultShape || (options.shape == 'autooverflow' ? 'overflowSquare' : 'square');
                    }
                }

                var posterInfo = LibraryBrowser.getPosterViewInfo();

                var thumbWidth = posterInfo.backdropWidth;
                var posterWidth = posterInfo.portraitWidth;
                var squareSize = posterInfo.squareWidth;
                var bannerWidth = posterInfo.bannerWidth;

                if (isThumbAspectRatio) {
                    posterWidth = thumbWidth;
                }
                else if (isSquareAspectRatio) {
                    posterWidth = squareSize;
                }

                if (options.shape == 'overflowBackdrop') {
                    thumbWidth = posterInfo.overflowBackdropWidth;
                }
                else if (options.shape == 'overflowPortrait') {
                    posterWidth = posterInfo.overflowPortraitWidth;
                }
                else if (options.shape == 'overflowSquare') {
                    squareSize = posterInfo.overflowSquareWidth;
                }
                else if (options.shape == 'smallBackdrop') {
                    thumbWidth = posterInfo.smallBackdropWidth;
                }
                else if (options.shape == 'detailPagePortrait') {
                    posterWidth = 200;
                }
                else if (options.shape == 'detailPageSquare') {
                    posterWidth = 240;
                    squareSize = 240;
                }
                else if (options.shape == 'detailPage169') {
                    posterWidth = 320;
                    thumbWidth = 320;
                }

                options.uiAspect = getDesiredAspect(options.shape);
                options.primaryImageAspectRatio = primaryImageAspectRatio;
                options.posterWidth = posterWidth;
                options.thumbWidth = thumbWidth;
                options.bannerWidth = bannerWidth;
                options.squareSize = squareSize;
            },

            getPosterViewHtml: function (options) {

                LibraryBrowser.setPosterViewData(options);

                var items = options.items;
                var currentIndexValue;

                options.shape = options.shape || "portrait";

                var html = "";

                var primaryImageAspectRatio;
                var thumbWidth = options.thumbWidth;
                var posterWidth = options.posterWidth;
                var squareSize = options.squareSize;
                var bannerWidth = options.bannerWidth;

                var dateText;
                var uiAspect = options.uiAspect;

                for (var i = 0, length = items.length; i < length; i++) {

                    var item = items[i];

                    dateText = null;

                    primaryImageAspectRatio = LibraryBrowser.getAveragePrimaryImageAspectRatio([item]);

                    if (options.showStartDateIndex) {

                        if (item.StartDate) {
                            try {

                                dateText = LibraryBrowser.getFutureDateText(datetime.parseISO8601Date(item.StartDate, true), true);

                            } catch (err) {
                            }
                        }

                        var newIndexValue = dateText || Globalize.translate('HeaderUnknownDate');

                        if (newIndexValue != currentIndexValue) {

                            html += '<h1 class="timelineHeader" style="text-align:center;">' + newIndexValue + '</h1>';
                            currentIndexValue = newIndexValue;
                        }
                    } else if (options.timeline) {
                        var year = item.ProductionYear || Globalize.translate('HeaderUnknownYear');

                        if (year != currentIndexValue) {

                            html += '<h1 class="timelineHeader">' + year + '</h1>';
                            currentIndexValue = year;
                        }
                    }

                    html += LibraryBrowser.getPosterViewItemHtml(item, i, options, primaryImageAspectRatio, thumbWidth, posterWidth, squareSize, bannerWidth, uiAspect);
                }

                return html;
            },

            getPosterViewItemHtml: function (item, index, options, primaryImageAspectRatio, thumbWidth, posterWidth, squareSize, bannerWidth, uiAspect) {

                var html = '';
                var imgUrl = null;
                var icon;
                var width = null;
                var height = null;

                var forceName = false;

                var enableImageEnhancers = options.enableImageEnhancers !== false;

                var cssClass = "card";

                if (options.fullWidthOnMobile) {
                    cssClass += " fullWidthCardOnMobile";
                }

                var showTitle = options.showTitle == 'auto' ? true : options.showTitle;

                // Force the title for these
                if (item.Type == 'PhotoAlbum' || item.Type == 'Folder') {
                    showTitle = true;
                }
                var coverImage = options.coverImage;
                var imageItem = item.Type == 'Timer' ? (item.ProgramInfo || item) : item;

                if (options.autoThumb && imageItem.ImageTags && imageItem.ImageTags.Primary && imageItem.PrimaryImageAspectRatio && imageItem.PrimaryImageAspectRatio >= 1.34) {

                    width = posterWidth;
                    height = primaryImageAspectRatio ? Math.round(posterWidth / primaryImageAspectRatio) : null;

                    imgUrl = ApiClient.getScaledImageUrl(imageItem.Id, {
                        type: "Primary",
                        maxHeight: height,
                        maxWidth: width,
                        tag: imageItem.ImageTags.Primary,
                        enableImageEnhancers: enableImageEnhancers
                    });

                    if (primaryImageAspectRatio) {
                        if (uiAspect) {
                            if (Math.abs(primaryImageAspectRatio - uiAspect) <= .2) {
                                coverImage = true;
                            }
                        }
                    }

                } else if (options.autoThumb && imageItem.ImageTags && imageItem.ImageTags.Thumb) {

                    imgUrl = ApiClient.getScaledImageUrl(imageItem.Id, {
                        type: "Thumb",
                        maxWidth: thumbWidth,
                        tag: imageItem.ImageTags.Thumb,
                        enableImageEnhancers: enableImageEnhancers
                    });

                } else if (options.preferBackdrop && imageItem.BackdropImageTags && imageItem.BackdropImageTags.length) {

                    imgUrl = ApiClient.getScaledImageUrl(imageItem.Id, {
                        type: "Backdrop",
                        maxWidth: thumbWidth,
                        tag: imageItem.BackdropImageTags[0],
                        enableImageEnhancers: enableImageEnhancers
                    });

                } else if (options.preferThumb && imageItem.ImageTags && imageItem.ImageTags.Thumb) {

                    imgUrl = ApiClient.getScaledImageUrl(imageItem.Id, {
                        type: "Thumb",
                        maxWidth: thumbWidth,
                        tag: imageItem.ImageTags.Thumb,
                        enableImageEnhancers: enableImageEnhancers
                    });

                } else if (options.preferBanner && imageItem.ImageTags && imageItem.ImageTags.Banner) {

                    imgUrl = ApiClient.getScaledImageUrl(imageItem.Id, {
                        type: "Banner",
                        maxWidth: bannerWidth,
                        tag: imageItem.ImageTags.Banner,
                        enableImageEnhancers: enableImageEnhancers
                    });

                } else if (options.preferThumb && imageItem.SeriesThumbImageTag && options.inheritThumb !== false) {

                    imgUrl = ApiClient.getScaledImageUrl(imageItem.SeriesId, {
                        type: "Thumb",
                        maxWidth: thumbWidth,
                        tag: imageItem.SeriesThumbImageTag,
                        enableImageEnhancers: enableImageEnhancers
                    });

                } else if (options.preferThumb && imageItem.ParentThumbItemId && options.inheritThumb !== false) {

                    imgUrl = ApiClient.getScaledImageUrl(imageItem.ParentThumbItemId, {
                        type: "Thumb",
                        maxWidth: thumbWidth,
                        enableImageEnhancers: enableImageEnhancers,
                        tag: imageItem.ParentThumbImageTag
                    });

                } else if (options.preferThumb && imageItem.BackdropImageTags && imageItem.BackdropImageTags.length) {

                    imgUrl = ApiClient.getScaledImageUrl(imageItem.Id, {
                        type: "Backdrop",
                        maxWidth: thumbWidth,
                        tag: imageItem.BackdropImageTags[0],
                        enableImageEnhancers: enableImageEnhancers
                    });

                    forceName = true;

                } else if (imageItem.ImageTags && imageItem.ImageTags.Primary) {

                    width = posterWidth;
                    height = primaryImageAspectRatio ? Math.round(posterWidth / primaryImageAspectRatio) : null;

                    imgUrl = ApiClient.getScaledImageUrl(imageItem.Id, {
                        type: "Primary",
                        maxHeight: height,
                        maxWidth: width,
                        tag: imageItem.ImageTags.Primary,
                        enableImageEnhancers: enableImageEnhancers
                    });

                    if (primaryImageAspectRatio) {
                        if (uiAspect) {
                            if (Math.abs(primaryImageAspectRatio - uiAspect) <= .2) {
                                coverImage = true;
                            }
                        }
                    }
                }
                else if (imageItem.ParentPrimaryImageTag) {

                    imgUrl = ApiClient.getScaledImageUrl(imageItem.ParentPrimaryImageItemId, {
                        type: "Primary",
                        maxWidth: posterWidth,
                        tag: item.ParentPrimaryImageTag,
                        enableImageEnhancers: enableImageEnhancers
                    });
                }
                else if (imageItem.AlbumId && imageItem.AlbumPrimaryImageTag) {

                    height = squareSize;
                    width = primaryImageAspectRatio ? Math.round(height * primaryImageAspectRatio) : null;

                    imgUrl = ApiClient.getScaledImageUrl(imageItem.AlbumId, {
                        type: "Primary",
                        maxHeight: height,
                        maxWidth: width,
                        tag: imageItem.AlbumPrimaryImageTag,
                        enableImageEnhancers: enableImageEnhancers
                    });

                    if (primaryImageAspectRatio) {
                        if (uiAspect) {
                            if (Math.abs(primaryImageAspectRatio - uiAspect) <= .2) {
                                coverImage = true;
                            }
                        }
                    }
                }
                else if (imageItem.Type == 'Season' && imageItem.ImageTags && imageItem.ImageTags.Thumb) {

                    imgUrl = ApiClient.getScaledImageUrl(imageItem.Id, {
                        type: "Thumb",
                        maxWidth: thumbWidth,
                        tag: imageItem.ImageTags.Thumb,
                        enableImageEnhancers: enableImageEnhancers
                    });

                }
                else if (imageItem.BackdropImageTags && imageItem.BackdropImageTags.length) {

                    imgUrl = ApiClient.getScaledImageUrl(imageItem.Id, {
                        type: "Backdrop",
                        maxWidth: thumbWidth,
                        tag: imageItem.BackdropImageTags[0],
                        enableImageEnhancers: enableImageEnhancers
                    });

                } else if (imageItem.ImageTags && imageItem.ImageTags.Thumb) {

                    imgUrl = ApiClient.getScaledImageUrl(imageItem.Id, {
                        type: "Thumb",
                        maxWidth: thumbWidth,
                        tag: imageItem.ImageTags.Thumb,
                        enableImageEnhancers: enableImageEnhancers
                    });

                } else if (imageItem.ParentThumbItemId) {

                    imgUrl = ApiClient.getScaledImageUrl(imageItem.ParentThumbItemId, {
                        type: "Thumb",
                        maxWidth: thumbWidth,
                        enableImageEnhancers: enableImageEnhancers,
                        tag: imageItem.ParentThumbImageTag
                    });

                } else if (item.MediaType == "Audio" || item.Type == "MusicAlbum" || item.Type == "MusicArtist") {

                    if (item.Name && showTitle) {
                        icon = 'library_music';
                    }
                    cssClass += " defaultBackground";

                } else if (item.Type == "Recording" || item.Type == "Program" || item.Type == "TvChannel") {

                    if (item.Name && showTitle) {
                        icon = 'folder_open';
                    }

                    cssClass += " defaultBackground";
                } else if (item.MediaType == "Video" || item.Type == "Season" || item.Type == "Series") {

                    if (item.Name && showTitle) {
                        icon = 'videocam';
                    }
                    cssClass += " defaultBackground";
                } else if (item.Type == "Person") {

                    if (item.Name && showTitle) {
                        icon = 'person';
                    }
                    cssClass += " defaultBackground";
                } else {
                    if (item.Name && showTitle) {
                        icon = 'folder_open';
                    }
                    cssClass += " defaultBackground";
                }

                icon = item.icon || icon;
                cssClass += ' ' + options.shape + 'Card';

                var mediaSourceCount = item.MediaSourceCount || 1;

                var href = options.linkItem === false ? '#' : LibraryBrowser.getHref(item, options.context);

                if (options.showChildCountIndicator && item.ChildCount && options.showLatestItemsPopup !== false) {
                    cssClass += ' groupedCard';
                }

                if ((showTitle || options.showItemCounts) && !options.overlayText) {
                    cssClass += ' bottomPaddedCard';
                }

                var dataAttributes = LibraryBrowser.getItemDataAttributes(item, options, index);

                var defaultAction = options.defaultAction;
                if (defaultAction == 'play' || defaultAction == 'playallfromhere') {
                    if (item.PlayAccess != 'Full') {
                        defaultAction = null;
                    }
                }
                var defaultActionAttribute = defaultAction ? (' data-action="' + defaultAction + '"') : '';

                // card
                html += '<div' + dataAttributes + ' class="' + cssClass + '">';

                var style = "";

                if (imgUrl && !options.lazy) {
                    style += 'background-image:url(\'' + imgUrl + '\');';
                }

                var imageCssClass = 'cardImage';

                if (icon) {
                    imageCssClass += " iconCardImage";
                }

                if (coverImage) {
                    imageCssClass += " coveredCardImage";
                    if (item.MediaType == 'Photo' || item.Type == 'PhotoAlbum' || item.Type == 'Folder' || item.Type == 'Program' || item.Type == 'Recording') {
                        imageCssClass += " noScale";
                    }
                }
                if (options.centerImage) {
                    imageCssClass += " centeredCardImage";
                }

                var dataSrc = "";

                if (options.lazy && imgUrl) {
                    imageCssClass += " lazy";
                    dataSrc = ' data-src="' + imgUrl + '"';
                }

                var cardboxCssClass = 'cardBox';

                if (options.cardLayout) {
                    cardboxCssClass += ' visualCardBox';
                }
                html += '<div class="' + cardboxCssClass + '">';
                html += '<div class="cardScalable">';

                html += '<div class="cardPadder"></div>';

                var anchorCssClass = "cardContent";

                anchorCssClass += ' mediaItem';

                if (options.defaultAction) {
                    anchorCssClass += ' itemAction';
                }

                var transition = options.transition === false || !AppInfo.enableSectionTransitions ? '' : ' data-transition="slide"';
                var onclick = item.onclick ? ' onclick="' + item.onclick + '"' : '';
                html += '<a' + onclick + transition + ' class="' + anchorCssClass + '" href="' + href + '"' + defaultActionAttribute + '>';
                html += '<div class="' + imageCssClass + '" style="' + style + '"' + dataSrc + '>';
                if (icon) {
                    html += '<i class="md-icon">' + icon + '</i>';
                }
                html += '</div>';

                if (item.LocationType == "Virtual" || item.LocationType == "Offline") {
                    if (options.showLocationTypeIndicator !== false) {
                        html += LibraryBrowser.getOfflineIndicatorHtml(item);
                    }
                } else if (options.showUnplayedIndicator !== false) {
                    html += indicators.getPlayedIndicatorHtml(item);
                } else if (options.showChildCountIndicator) {
                    html += LibraryBrowser.getGroupCountIndicator(item);
                }

                if (item.SeriesTimerId) {
                    html += '<i class="md-icon seriesTimerIndicator">fiber_smart_record</i>';
                }

                html += LibraryBrowser.getSyncIndicator(item);

                if (mediaSourceCount > 1) {
                    html += '<div class="mediaSourceIndicator">' + mediaSourceCount + '</div>';
                }

                var progressHtml = options.showProgress === false || item.IsFolder ? '' : LibraryBrowser.getItemProgressBarHtml((item.Type == 'Recording' ? item : item.UserData || {}));

                var footerOverlayed = false;

                if (options.overlayText || (forceName && !showTitle)) {

                    var footerCssClass = progressHtml ? 'cardFooter fullCardFooter' : 'cardFooter';

                    html += LibraryBrowser.getCardFooterText(item, options, showTitle, imgUrl, forceName, footerCssClass, progressHtml, false);
                    footerOverlayed = true;
                }
                else if (progressHtml) {
                    html += '<div class="cardFooter fullCardFooter lightCardFooter">';
                    html += "<div class='cardProgress cardText'>";
                    html += progressHtml;
                    html += "</div>";
                    //cardFooter
                    html += "</div>";

                    progressHtml = '';
                }

                // cardContent
                html += '</a>';

                if (options.overlayPlayButton && !item.IsPlaceHolder && (item.LocationType != 'Virtual' || !item.MediaType || item.Type == 'Program') && item.Type != 'Person' && item.PlayAccess == 'Full') {
                    html += '<div class="cardOverlayButtonContainer"><button is="paper-icon-button-light" class="cardOverlayPlayButton itemAction autoSize" data-action="playmenu" onclick="return false;"><i class="md-icon">play_arrow</i></button></div>';
                }
                if (options.overlayMoreButton) {
                    html += '<div class="cardOverlayButtonContainer"><button is="paper-icon-button-light" class="cardOverlayMoreButton itemAction autoSize" data-action="menu" onclick="return false;"><i class="md-icon">' + AppInfo.moreIcon.replace('-', '_') + '</i></button></div>';
                }

                // cardScalable
                html += '</div>';

                if (!options.overlayText && !footerOverlayed) {
                    html += LibraryBrowser.getCardFooterText(item, options, showTitle, imgUrl, forceName, 'cardFooter outerCardFooter', progressHtml, true);
                }

                // cardBox
                html += '</div>';

                // card
                html += "</div>";

                return html;
            },

            getTextActionButton: function (item, text) {

                if (!text) {
                    text = itemHelper.getDisplayName(item);
                }

                var html = '<button data-id="' + item.Id + '" data-type="' + item.Type + '" data-mediatype="' + item.MediaType + '" data-channelid="' + item.ChannelId + '" data-isfolder="' + item.IsFolder + '" type="button" class="itemAction textActionButton" data-action="link">';
                html += text;
                html += '</button>';

                return html;
            },

            getCardFooterText: function (item, options, showTitle, imgUrl, forceName, footerClass, progressHtml, isOuterFooter) {

                var html = '';

                if (options.cardLayout) {
                    html += '<div class="cardButtonContainer">';
                    html += '<button is="paper-icon-button-light" class="itemAction btnCardOptions autoSize" data-action="menu"><i class="md-icon">' + AppInfo.moreIcon.replace('-', '_') + '</i></button>';
                    html += "</div>";
                }

                var name = options.showTitle == 'auto' && !item.IsFolder && item.MediaType == 'Photo' ? '' : itemHelper.getDisplayName(item);

                if (!imgUrl && !showTitle) {
                    html += "<div class='cardDefaultText'>";
                    html += htmlEncode(name);
                    html += "</div>";
                }

                var cssClass = options.centerText ? "cardText cardTextCentered" : "cardText";

                var lines = [];

                var parentTitleUnderneath = item.Type == 'MusicAlbum' || item.Type == 'Audio' || item.Type == 'MusicVideo';
                if (options.showParentTitle && !parentTitleUnderneath) {

                    if (isOuterFooter && item.Type == 'Episode' && item.SeriesName && item.SeriesId) {

                        lines.push(LibraryBrowser.getTextActionButton({
                            Id: item.SeriesId,
                            Name: item.SeriesName,
                            Type: 'Series',
                            IsFolder: true
                        }));
                    }
                    else {

                        lines.push(item.EpisodeTitle ? item.Name : (item.SeriesName || item.Album || item.AlbumArtist || item.GameSystem || ""));
                    }
                }

                if (showTitle || forceName) {

                    lines.push(htmlEncode(name));
                }

                if (options.showParentTitle && parentTitleUnderneath) {

                    if (isOuterFooter && item.AlbumArtists && item.AlbumArtists.length) {
                        item.AlbumArtists[0].Type = 'MusicArtist';
                        item.AlbumArtists[0].IsFolder = true;
                        lines.push(LibraryBrowser.getTextActionButton(item.AlbumArtists[0]));
                    } else {
                        lines.push(item.EpisodeTitle ? item.Name : (item.SeriesName || item.Album || item.AlbumArtist || item.GameSystem || ""));
                    }
                }

                if (options.showItemCounts) {

                    var itemCountHtml = LibraryBrowser.getItemCountsHtml(options, item);

                    lines.push(itemCountHtml);
                }

                if (options.textLines) {
                    var additionalLines = options.textLines(item);
                    for (var i = 0, length = additionalLines.length; i < length; i++) {
                        lines.push(additionalLines[i]);
                    }
                }

                if (options.showSongCount) {

                    var songLine = '';

                    if (item.SongCount) {
                        songLine = item.SongCount == 1 ?
                        Globalize.translate('ValueOneSong') :
                        Globalize.translate('ValueSongCount', item.SongCount);
                    }

                    lines.push(songLine);
                }

                if (options.showPremiereDate) {

                    if (item.PremiereDate) {
                        try {

                            lines.push(LibraryBrowser.getPremiereDateText(item));

                        } catch (err) {
                            lines.push('');

                        }
                    } else {
                        lines.push('');
                    }
                }

                if (options.showYear) {

                    lines.push(item.ProductionYear || '');
                }

                if (options.showChannelName) {

                    lines.push(item.ChannelName || '');
                }

                if (options.showAirTime) {

                    var airTimeText;
                    if (item.StartDate) {

                        try {
                            var date = datetime.parseISO8601Date(item.StartDate);

                            airTimeText = date.toLocaleDateString();

                            airTimeText += ', ' + datetime.getDisplayTime(date);

                            if (item.EndDate) {
                                date = datetime.parseISO8601Date(item.EndDate);
                                airTimeText += ' - ' + datetime.getDisplayTime(date);
                            }
                        }
                        catch (e) {
                            console.log("Error parsing date: " + item.PremiereDate);
                        }
                    }

                    lines.push(airTimeText || '');
                }

                if (item.Type == 'TvChannel') {

                    if (item.CurrentProgram) {
                        lines.push(itemHelper.getDisplayName(item.CurrentProgram));
                    } else {
                        lines.push('');
                    }
                }

                if (options.showSeriesYear) {

                    if (item.Status == "Continuing") {

                        lines.push(Globalize.translate('ValueSeriesYearToPresent', item.ProductionYear || ''));

                    } else {
                        lines.push(item.ProductionYear || '');
                    }

                }

                if (options.showProgramAirInfo) {

                    var date = datetime.parseISO8601Date(item.StartDate, true);

                    var text = item.StartDate ?
                        date.toLocaleString() :
                        '';

                    lines.push(text || '&nbsp;');

                    lines.push(item.ChannelName || '&nbsp;');
                }

                html += LibraryBrowser.getCardTextLines(lines, cssClass, !options.overlayText);

                if (options.overlayText) {

                    if (progressHtml) {
                        html += "<div class='cardText cardProgress'>";
                        html += progressHtml;
                        html += "</div>";
                    }
                }

                if (html) {
                    html = '<div class="' + footerClass + '">' + html;

                    //cardFooter
                    html += "</div>";
                }

                return html;
            },

            getListItemInfo: function (elem) {

                var elemWithAttributes = elem;

                while (!elemWithAttributes.getAttribute('data-id')) {
                    elemWithAttributes = elemWithAttributes.parentNode;
                }

                var itemId = elemWithAttributes.getAttribute('data-id');
                var index = elemWithAttributes.getAttribute('data-index');
                var mediaType = elemWithAttributes.getAttribute('data-mediatype');

                return {
                    id: itemId,
                    index: index,
                    mediaType: mediaType,
                    context: elemWithAttributes.getAttribute('data-context')
                };
            },

            getCardTextLines: function (lines, cssClass, forceLines) {

                var html = '';

                var valid = 0;
                var i, length;

                for (i = 0, length = lines.length; i < length; i++) {

                    var text = lines[i];

                    if (text) {
                        html += "<div class='" + cssClass + "'>";
                        html += text;
                        html += "</div>";
                        valid++;
                    }
                }

                if (forceLines) {
                    while (valid < length) {
                        html += "<div class='" + cssClass + "'>&nbsp;</div>";
                        valid++;
                    }
                }

                return html;
            },

            getFutureDateText: function (date) {

                var weekday = [];
                weekday[0] = Globalize.translate('OptionSunday');
                weekday[1] = Globalize.translate('OptionMonday');
                weekday[2] = Globalize.translate('OptionTuesday');
                weekday[3] = Globalize.translate('OptionWednesday');
                weekday[4] = Globalize.translate('OptionThursday');
                weekday[5] = Globalize.translate('OptionFriday');
                weekday[6] = Globalize.translate('OptionSaturday');

                var day = weekday[date.getDay()];
                date = date.toLocaleDateString();

                if (date.toLowerCase().indexOf(day.toLowerCase()) == -1) {
                    return day + " " + date;
                }

                return date;
            },

            getPremiereDateText: function (item, date) {

                var studio = (item.Studios || [])[0];

                if (!date) {

                    var text = '';

                    if (item.AirTime) {
                        text += item.AirTime;
                    }

                    if (studio) {

                        if (text) {
                            text += " on " + studio;
                        } else {
                            text += studio;
                        }
                    }

                    return text;
                }

                var day = LibraryBrowser.getFutureDateText(date);

                if (item.AirTime) {
                    day += " at " + item.AirTime;
                }

                if (studio) {
                    day += " on " + studio;
                }

                return day;
            },

            getOfflineIndicatorHtml: function (item) {

                if (item.LocationType == "Offline") {
                    return '<div class="posterRibbon offlinePosterRibbon">' + Globalize.translate('HeaderOffline') + '</div>';
                }

                if (item.Type == 'Episode') {
                    try {

                        var date = datetime.parseISO8601Date(item.PremiereDate, true);

                        if (item.PremiereDate && (new Date().getTime() < date.getTime())) {
                            return '<div class="posterRibbon unairedPosterRibbon">' + Globalize.translate('HeaderUnaired') + '</div>';
                        }
                    } catch (err) {

                    }

                    return '<div class="posterRibbon missingPosterRibbon">' + Globalize.translate('HeaderMissing') + '</div>';
                }

                return '';
            },

            getGroupCountIndicator: function (item) {

                if (item.ChildCount) {
                    return '<div class="playedIndicator">' + item.ChildCount + '</div>';
                }

                return '';
            },

            getSyncIndicator: function (item) {

                if (item.SyncStatus == 'Synced') {

                    return '<div class="syncIndicator"><i class="md-icon">sync</i></div>';
                }

                var syncPercent = item.SyncPercent;
                if (syncPercent) {
                    return '<div class="syncIndicator"><i class="md-icon">sync</i></div>';
                }

                if (item.SyncStatus == 'Queued' || item.SyncStatus == 'Converting' || item.SyncStatus == 'ReadyToTransfer' || item.SyncStatus == 'Transferring') {

                    return '<div class="syncIndicator"><i class="md-icon">sync</i></div>';
                }

                return '';
            },

            getAveragePrimaryImageAspectRatio: function (items) {

                var values = [];

                for (var i = 0, length = items.length; i < length; i++) {

                    var ratio = items[i].PrimaryImageAspectRatio || 0;

                    if (!ratio) {
                        continue;
                    }

                    values[values.length] = ratio;
                }

                if (!values.length) {
                    return null;
                }

                // Use the median
                values.sort(function (a, b) { return a - b; });

                var half = Math.floor(values.length / 2);

                var result;

                if (values.length % 2)
                    result = values[half];
                else
                    result = (values[half - 1] + values[half]) / 2.0;

                // If really close to 2:3 (poster image), just return 2:3
                if (Math.abs(0.66666666667 - result) <= .15) {
                    return 0.66666666667;
                }

                // If really close to 16:9 (episode image), just return 16:9
                if (Math.abs(1.777777778 - result) <= .2) {
                    return 1.777777778;
                }

                // If really close to 1 (square image), just return 1
                if (Math.abs(1 - result) <= .15) {
                    return 1;
                }

                // If really close to 4:3 (poster image), just return 2:3
                if (Math.abs(1.33333333333 - result) <= .15) {
                    return 1.33333333333;
                }

                return result;
            },

            metroColors: ["#6FBD45", "#4BB3DD", "#4164A5", "#E12026", "#800080", "#E1B222", "#008040", "#0094FF", "#FF00C7", "#FF870F", "#7F0037"],

            getRandomMetroColor: function () {

                var index = Math.floor(Math.random() * (LibraryBrowser.metroColors.length - 1));

                return LibraryBrowser.metroColors[index];
            },

            getMetroColor: function (str) {

                if (str) {
                    var character = String(str.substr(0, 1).charCodeAt());
                    var sum = 0;
                    for (var i = 0; i < character.length; i++) {
                        sum += parseInt(character.charAt(i));
                    }
                    var index = String(sum).substr(-1);

                    return LibraryBrowser.metroColors[index];
                } else {
                    return LibraryBrowser.getRandomMetroColor();
                }

            },

            renderName: function (item, nameElem, linkToElement, context) {

                var name = itemHelper.getDisplayName(item, {
                    includeParentInfo: false
                });

                Dashboard.setPageTitle(name);

                if (linkToElement) {
                    nameElem.innerHTML = '<a class="detailPageParentLink" href="' + LibraryBrowser.getHref(item, context) + '">' + name + '</a>';
                } else {
                    nameElem.innerHTML = name;
                }
            },

            renderParentName: function (item, parentNameElem, context) {

                var html = [];

                var contextParam = context ? ('&context=' + context) : '';

                if (item.AlbumArtists) {
                    html.push(LibraryBrowser.getArtistLinksHtml(item.AlbumArtists, "detailPageParentLink"));
                } else if (item.ArtistItems && item.ArtistItems.length && item.Type == "MusicVideo") {
                    html.push(LibraryBrowser.getArtistLinksHtml(item.ArtistItems, "detailPageParentLink"));
                } else if (item.SeriesName && item.Type == "Episode") {

                    html.push('<a class="detailPageParentLink" href="itemdetails.html?id=' + item.SeriesId + contextParam + '">' + item.SeriesName + '</a>');
                }

                if (item.SeriesName && item.Type == "Season") {

                    html.push('<a class="detailPageParentLink" href="itemdetails.html?id=' + item.SeriesId + contextParam + '">' + item.SeriesName + '</a>');

                } else if (item.ParentIndexNumber != null && item.Type == "Episode") {

                    html.push('<a class="detailPageParentLink" href="itemdetails.html?id=' + item.SeasonId + contextParam + '">' + item.SeasonName + '</a>');

                } else if (item.Album && item.Type == "Audio" && (item.AlbumId || item.ParentId)) {
                    html.push('<a class="detailPageParentLink" href="itemdetails.html?id=' + (item.AlbumId || item.ParentId) + contextParam + '">' + item.Album + '</a>');

                } else if (item.Album && item.Type == "MusicVideo" && item.AlbumId) {
                    html.push('<a class="detailPageParentLink" href="itemdetails.html?id=' + item.AlbumId + contextParam + '">' + item.Album + '</a>');

                } else if (item.Album) {
                    html.push(item.Album);
                } else if (item.Type == 'Program' && item.EpisodeTitle) {
                    html.push(item.Name);
                }

                if (html.length) {
                    parentNameElem.classList.remove('hide');
                    parentNameElem.innerHTML = html.join(' - ');
                } else {
                    parentNameElem.classList.add('hide');
                }
            },

            renderLinks: function (linksElem, item) {

                var links = [];

                if (item.HomePageUrl) {
                    links.push('<a class="textlink" href="' + item.HomePageUrl + '" target="_blank">' + Globalize.translate('ButtonWebsite') + '</a>');
                }

                if (item.ExternalUrls) {

                    for (var i = 0, length = item.ExternalUrls.length; i < length; i++) {

                        var url = item.ExternalUrls[i];

                        links.push('<a class="textlink" href="' + url.Url + '" target="_blank">' + url.Name + '</a>');
                    }
                }

                if (links.length) {

                    var html = links.join('&nbsp;&nbsp;/&nbsp;&nbsp;');

                    html = Globalize.translate('ValueLinks', html);

                    linksElem.innerHTML = html;
                    linksElem.classList.remove('hide');

                } else {
                    linksElem.classList.add('hide');
                }
            },

            showLayoutMenu: function (button, currentLayout, views) {

                var dispatchEvent = true;

                if (!views) {

                    dispatchEvent = false;
                    // Add banner and list once all screens support them
                    views = button.getAttribute('data-layouts');

                    views = views ? views.split(',') : ['List', 'Poster', 'PosterCard', 'Thumb', 'ThumbCard'];
                }

                var menuItems = views.map(function (v) {
                    return {
                        name: Globalize.translate('Option' + v),
                        id: v,
                        selected: currentLayout == v
                    };
                });

                require(['actionsheet'], function (actionsheet) {

                    actionsheet.show({
                        items: menuItems,
                        positionTo: button,
                        callback: function (id) {

                            button.dispatchEvent(new CustomEvent('layoutchange', {
                                detail: {
                                    viewStyle: id
                                },
                                bubbles: true,
                                cancelable: false
                            }));

                            if (!dispatchEvent) {
                                if (window.$) {
                                    $(button).trigger('layoutchange', [id]);
                                }
                            }
                        }
                    });

                });

            },

            getQueryPagingHtml: function (options) {

                var startIndex = options.startIndex;
                var limit = options.limit;
                var totalRecordCount = options.totalRecordCount;

                if (limit && options.updatePageSizeSetting !== false) {
                    try {
                        appStorage.setItem(options.pageSizeKey || pageSizeKey, limit);
                    } catch (e) {

                    }
                }

                var html = '';

                var recordsEnd = Math.min(startIndex + limit, totalRecordCount);

                // 20 is the minimum page size
                var showControls = totalRecordCount > 20 || limit < totalRecordCount;

                html += '<div class="listPaging">';

                if (showControls) {
                    html += '<span style="vertical-align:middle;">';

                    var startAtDisplay = totalRecordCount ? startIndex + 1 : 0;
                    html += startAtDisplay + '-' + recordsEnd + ' of ' + totalRecordCount;

                    html += '</span>';
                }

                if (showControls || options.viewButton || options.filterButton || options.sortButton || options.addLayoutButton) {

                    html += '<div style="display:inline-block;">';

                    if (showControls) {

                        html += '<button is="paper-icon-button-light" class="btnPreviousPage autoSize" ' + (startIndex ? '' : 'disabled') + '><i class="md-icon">&#xE5C4;</i></button>';
                        html += '<button is="paper-icon-button-light" class="btnNextPage autoSize" ' + (startIndex + limit >= totalRecordCount ? 'disabled' : '') + '><i class="md-icon">arrow_forward</i></button>';
                    }

                    if (options.addLayoutButton) {

                        html += '<button is="paper-icon-button-light" title="' + Globalize.translate('ButtonSelectView') + '" class="btnChangeLayout autoSize" data-layouts="' + (options.layouts || '') + '" onclick="LibraryBrowser.showLayoutMenu(this, \'' + (options.currentLayout || '') + '\');"><i class="md-icon">view_comfy</i></button>';
                    }

                    if (options.sortButton) {

                        html += '<button is="paper-icon-button-light" class="btnSort autoSize" title="' + Globalize.translate('ButtonSort') + '"><i class="md-icon">sort_by_alpha</i></button>';
                    }

                    if (options.filterButton) {

                        html += '<button is="paper-icon-button-light" class="btnFilter autoSize" title="' + Globalize.translate('ButtonFilter') + '"><i class="md-icon">filter_list</i></button>';
                    }

                    html += '</div>';

                    if (showControls && options.showLimit) {

                        var id = "selectPageSize";

                        var pageSizes = options.pageSizes || [20, 50, 100, 200, 300, 400, 500];

                        var optionsHtml = pageSizes.map(function (val) {

                            if (limit == val) {

                                return '<option value="' + val + '" selected="selected">' + val + '</option>';

                            } else {
                                return '<option value="' + val + '">' + val + '</option>';
                            }
                        }).join('');

                        // Add styles to defeat jquery mobile
                        html += '<div class="pageSizeContainer"><label class="labelPageSize" for="' + id + '">' + Globalize.translate('LabelLimit') + '</label><select style="width:auto;" class="selectPageSize" id="' + id + '" data-inline="true" data-mini="true">' + optionsHtml + '</select></div>';
                    }
                }

                html += '</div>';

                return html;
            },

            showSortMenu: function (options) {

                require(['dialogHelper', 'emby-radio'], function (dialogHelper) {

                    var dlg = dialogHelper.createDialog({
                        removeOnClose: true,
                        modal: false,
                        entryAnimationDuration: 160,
                        exitAnimationDuration: 200
                    });

                    dlg.classList.add('ui-body-a');
                    dlg.classList.add('background-theme-a');
                    dlg.classList.add('formDialog');

                    var html = '';

                    html += '<div style="margin:0;padding:1.25em 1.5em 1.5em;">';

                    html += '<h2 style="margin:0 0 .5em;">';
                    html += Globalize.translate('HeaderSortBy');
                    html += '</h2>';

                    var i, length;
                    var isChecked;

                    html += '<div>';
                    for (i = 0, length = options.items.length; i < length; i++) {

                        var option = options.items[i];

                        var radioValue = option.id.replace(',', '_');
                        isChecked = (options.query.SortBy || '').replace(',', '_') == radioValue ? ' checked' : '';
                        html += '<label class="block"><input type="radio" is="emby-radio" name="SortBy" data-id="' + option.id + '" value="' + radioValue + '" class="menuSortBy" ' + isChecked + ' /><span>' + option.name + '</span></label>';
                    }
                    html += '</div>';

                    html += '<h2 style="margin: 1em 0 .5em;">';
                    html += Globalize.translate('HeaderSortOrder');
                    html += '</h2>';
                    html += '<div>';
                    isChecked = options.query.SortOrder == 'Ascending' ? ' checked' : '';
                    html += '<label class="block"><input type="radio" is="emby-radio" name="SortOrder" value="Ascending" class="menuSortOrder" ' + isChecked + ' /><span>' + Globalize.translate('OptionAscending') + '</span></label>';
                    isChecked = options.query.SortOrder == 'Descending' ? ' checked' : '';
                    html += '<label class="block"><input type="radio" is="emby-radio" name="SortOrder" value="Descending" class="menuSortOrder" ' + isChecked + ' /><span>' + Globalize.translate('OptionDescending') + '</span></label>';
                    html += '</div>';
                    html += '</div>';

                    dlg.innerHTML = html;
                    document.body.appendChild(dlg);

                    // Seeing an issue in Firefox and IE where it's initially visible in the bottom right, then moves to the center
                    var delay = browserInfo.animate ? 0 : 100;
                    setTimeout(function () {
                        dialogHelper.open(dlg);
                    }, delay);

                    function onSortByChange() {
                        var newValue = this.value;
                        if (this.checked) {
                            var changed = options.query.SortBy != newValue;

                            options.query.SortBy = newValue.replace('_', ',');
                            options.query.StartIndex = 0;

                            if (options.callback && changed) {
                                options.callback();
                            }
                        }
                    }

                    var sortBys = dlg.querySelectorAll('.menuSortBy');
                    for (i = 0, length = sortBys.length; i < length; i++) {
                        sortBys[i].addEventListener('change', onSortByChange);
                    }

                    function onSortOrderChange() {
                        var newValue = this.value;
                        if (this.checked) {
                            var changed = options.query.SortOrder != newValue;

                            options.query.SortOrder = newValue;
                            options.query.StartIndex = 0;

                            if (options.callback && changed) {
                                options.callback();
                            }
                        }
                    }

                    var sortOrders = dlg.querySelectorAll('.menuSortOrder');
                    for (i = 0, length = sortOrders.length; i < length; i++) {
                        sortOrders[i].addEventListener('change', onSortOrderChange);
                    }
                });
            },

            getItemProgressBarHtml: function (item) {


                if (item.Type == "Recording" && item.CompletionPercentage) {

                    return '<progress class="itemProgressBar recordingProgressBar" min="0" max="100" value="' + item.CompletionPercentage + '"></progress>';
                }

                var pct = item.PlayedPercentage;

                if (pct && pct < 100) {

                    return '<progress class="itemProgressBar" min="0" max="100" value="' + pct + '"></progress>';
                }

                return null;
            },

            renderDetailImage: function (elem, item, editable, preferThumb) {

                var imageTags = item.ImageTags || {};

                if (item.PrimaryImageTag) {
                    imageTags.Primary = item.PrimaryImageTag;
                }

                var html = '';

                var url;
                var shape = 'portrait';

                var imageHeight = 360;
                var detectRatio = false;

                if (preferThumb && imageTags.Thumb) {

                    url = ApiClient.getScaledImageUrl(item.Id, {
                        type: "Thumb",
                        maxHeight: imageHeight,
                        tag: item.ImageTags.Thumb
                    });
                    shape = 'thumb';
                }
                else if (imageTags.Primary) {

                    url = ApiClient.getScaledImageUrl(item.Id, {
                        type: "Primary",
                        maxHeight: imageHeight,
                        tag: item.ImageTags.Primary
                    });
                    detectRatio = true;
                }
                else if (item.BackdropImageTags && item.BackdropImageTags.length) {

                    url = ApiClient.getScaledImageUrl(item.Id, {
                        type: "Backdrop",
                        maxHeight: imageHeight,
                        tag: item.BackdropImageTags[0]
                    });
                    shape = 'thumb';
                }
                else if (imageTags.Thumb) {

                    url = ApiClient.getScaledImageUrl(item.Id, {
                        type: "Thumb",
                        maxHeight: imageHeight,
                        tag: item.ImageTags.Thumb
                    });
                    shape = 'thumb';
                }
                else if (imageTags.Disc) {

                    url = ApiClient.getScaledImageUrl(item.Id, {
                        type: "Disc",
                        maxHeight: imageHeight,
                        tag: item.ImageTags.Disc
                    });
                    shape = 'square';
                }
                else if (item.AlbumId && item.AlbumPrimaryImageTag) {

                    url = ApiClient.getScaledImageUrl(item.AlbumId, {
                        type: "Primary",
                        maxHeight: imageHeight,
                        tag: item.AlbumPrimaryImageTag
                    });
                    shape = 'square';
                }
                else if (item.MediaType == "Audio" || item.Type == "MusicAlbum" || item.Type == "MusicGenre") {
                    url = "css/images/items/detail/audio.png";
                    shape = 'square';
                }
                else if (item.MediaType == "Game" || item.Type == "GameGenre") {
                    url = "css/images/items/detail/game.png";
                    shape = 'square';
                }
                else if (item.Type == "Person") {
                    url = "css/images/items/detail/person.png";
                    shape = 'square';
                }
                else if (item.Type == "Genre" || item.Type == "Studio") {
                    url = "css/images/items/detail/video.png";
                    shape = 'square';
                }
                else if (item.Type == "TvChannel") {
                    url = "css/images/items/detail/tv.png";
                    shape = 'square';
                }
                else {
                    url = "css/images/items/detail/video.png";
                    shape = 'square';
                }

                html += '<div style="position:relative;">';

                if (editable) {
                    html += "<a class='itemDetailGalleryLink' href='#'>";
                }

                if (detectRatio && item.PrimaryImageAspectRatio) {

                    if (item.PrimaryImageAspectRatio >= 1.48) {
                        shape = 'thumb';
                    } else if (item.PrimaryImageAspectRatio >= .85 && item.PrimaryImageAspectRatio <= 1.34) {
                        shape = 'square';
                    }
                }

                html += "<img class='itemDetailImage lazy' src='css/images/empty.png' />";

                if (editable) {
                    html += "</a>";
                }

                var progressHtml = item.IsFolder || !item.UserData ? '' : LibraryBrowser.getItemProgressBarHtml((item.Type == 'Recording' ? item : item.UserData));

                html += '<div class="detailImageProgressContainer">';
                if (progressHtml) {
                    html += progressHtml;
                }
                html += "</div>";

                html += "</div>";

                elem.innerHTML = html;

                if (shape == 'thumb') {
                    elem.classList.add('thumbDetailImageContainer');
                    elem.classList.remove('portraitDetailImageContainer');
                    elem.classList.remove('squareDetailImageContainer');
                }
                else if (shape == 'square') {
                    elem.classList.remove('thumbDetailImageContainer');
                    elem.classList.remove('portraitDetailImageContainer');
                    elem.classList.add('squareDetailImageContainer');
                } else {
                    elem.classList.remove('thumbDetailImageContainer');
                    elem.classList.add('portraitDetailImageContainer');
                    elem.classList.remove('squareDetailImageContainer');
                }

                var img = elem.querySelector('img');
                img.onload = function () {
                    if (img.src.indexOf('empty.png') == -1) {
                        img.classList.add('loaded');
                    }
                };
                ImageLoader.lazyImage(img, url);
            },

            refreshDetailImageUserData: function (elem, item) {

                var progressHtml = item.IsFolder || !item.UserData ? '' : LibraryBrowser.getItemProgressBarHtml((item.Type == 'Recording' ? item : item.UserData));

                var detailImageProgressContainer = elem.querySelector('.detailImageProgressContainer');

                detailImageProgressContainer.innerHTML = progressHtml || '';
            },

            renderOverview: function (elems, item) {

                for (var i = 0, length = elems.length; i < length; i++) {
                    var elem = elems[i];
                    var overview = item.Overview || '';

                    if (overview) {
                        elem.innerHTML = overview;

                        elem.classList.remove('empty');

                        var anchors = elem.querySelectorAll('a');
                        for (var j = 0, length2 = anchors.length; j < length2; j++) {
                            anchors[j].setAttribute("target", "_blank");
                        }

                    } else {
                        elem.innerHTML = '';

                        elem.classList.add('empty');
                    }
                }
            },

            renderStudios: function (elem, item, isStatic) {

                if (item.Studios && item.Studios.length && item.Type != "Series") {

                    var html = '';

                    for (var i = 0, length = item.Studios.length; i < length; i++) {

                        if (i > 0) {
                            html += '&nbsp;&nbsp;/&nbsp;&nbsp;';
                        }

                        if (isStatic) {
                            html += item.Studios[i].Name;
                        } else {
                            html += '<a class="textlink" href="itemdetails.html?id=' + item.Studios[i].Id + '">' + item.Studios[i].Name + '</a>';
                        }
                    }

                    var translationKey = item.Studios.length > 1 ? "ValueStudios" : "ValueStudio";

                    html = Globalize.translate(translationKey, html);

                    elem.innerHTML = html;
                    elem.classList.remove('hide');

                } else {
                    elem.classList.add('hide');
                }
            },

            renderGenres: function (elem, item, limit, isStatic) {

                var html = '';

                var genres = item.Genres || [];

                for (var i = 0, length = genres.length; i < length; i++) {

                    if (limit && i >= limit) {
                        break;
                    }

                    if (i > 0) {
                        html += '<span>&nbsp;&nbsp;/&nbsp;&nbsp;</span>';
                    }

                    var param = item.Type == "Audio" || item.Type == "MusicArtist" || item.Type == "MusicAlbum" ? "musicgenre" : "genre";

                    if (item.MediaType == "Game") {
                        param = "gamegenre";
                    }

                    if (isStatic) {
                        html += genres[i];
                    } else {
                        html += '<a class="textlink" href="itemdetails.html?' + param + '=' + ApiClient.encodeName(genres[i]) + '">' + genres[i] + '</a>';
                    }
                }

                elem.innerHTML = html;
            },

            renderPremiereDate: function (elem, item) {
                if (item.PremiereDate) {
                    try {

                        var date = datetime.parseISO8601Date(item.PremiereDate, true);

                        var translationKey = new Date().getTime() > date.getTime() ? "ValuePremiered" : "ValuePremieres";

                        elem.show().html(Globalize.translate(translationKey, date.toLocaleDateString()));

                    } catch (err) {
                        elem.hide();
                    }
                } else {
                    elem.hide();
                }
            },

            renderAwardSummary: function (elem, item) {
                if (item.AwardSummary) {
                    elem.classList.remove('hide');
                    elem.innerHTML = Globalize.translate('ValueAwards', item.AwardSummary);
                } else {
                    elem.classList.add('hide');
                }
            },

            renderDetailPageBackdrop: function (page, item) {

                var screenWidth = screen.availWidth;

                var imgUrl;
                var hasbackdrop = false;

                var itemBackdropElement = page.querySelector('#itemBackdrop');

                if (item.BackdropImageTags && item.BackdropImageTags.length) {

                    imgUrl = ApiClient.getScaledImageUrl(item.Id, {
                        type: "Backdrop",
                        index: 0,
                        maxWidth: screenWidth,
                        tag: item.BackdropImageTags[0]
                    });

                    itemBackdropElement.classList.remove('noBackdrop');
                    ImageLoader.lazyImage(itemBackdropElement, imgUrl, false);
                    hasbackdrop = true;
                }
                else if (item.ParentBackdropItemId && item.ParentBackdropImageTags && item.ParentBackdropImageTags.length) {

                    imgUrl = ApiClient.getScaledImageUrl(item.ParentBackdropItemId, {
                        type: 'Backdrop',
                        index: 0,
                        tag: item.ParentBackdropImageTags[0],
                        maxWidth: screenWidth
                    });

                    itemBackdropElement.classList.remove('noBackdrop');
                    ImageLoader.lazyImage(itemBackdropElement, imgUrl, false);
                    hasbackdrop = true;
                }
                else {

                    itemBackdropElement.classList.add('noBackdrop');
                    itemBackdropElement.style.backgroundImage = '';
                }

                return hasbackdrop;
            }
        };

        return libraryBrowser;

    })(window, document, screen);

    window.LibraryBrowser = libraryBrowser;

    return libraryBrowser;
});