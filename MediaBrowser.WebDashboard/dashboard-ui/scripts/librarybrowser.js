define(['viewManager', 'appSettings', 'appStorage', 'apphost', 'datetime', 'itemHelper', 'mediaInfo', 'scroller', 'indicators', 'dom', 'imageLoader', 'scrollStyles'], function (viewManager, appSettings, appStorage, appHost, datetime, itemHelper, mediaInfo, scroller, indicators, dom) {

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

        var pageSizeKey = 'pagesize_v4';

        var libraryBrowser = {
            getDefaultPageSize: function (key, defaultValue) {

                return 100;
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

                    if (dom.parentWithTag(elem, 'input')) {
                        return false;
                    }

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

                var progressHtml = item.IsFolder || !item.UserData ? '' : indicators.getProgressBarHtml(item);

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