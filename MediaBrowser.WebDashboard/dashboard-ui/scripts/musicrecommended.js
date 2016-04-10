define(['jQuery'], function ($) {

    function itemsPerRow() {

        var screenWidth = $(window).width();

        return screenWidth >= 1920 ? 9 : (screenWidth >= 1200 ? 12 : (screenWidth >= 1000 ? 10 : 8));
    }

    function enableScrollX() {
        return browserInfo.mobile && AppInfo.enableAppLayouts;
    }

    function getSquareShape() {
        return enableScrollX() ? 'overflowSquare' : 'square';
    }

    function loadLatest(page, parentId) {

        Dashboard.showLoadingMsg();

        var userId = Dashboard.getCurrentUserId();

        var options = {
            IncludeItemTypes: "Audio",
            Limit: itemsPerRow(),
            Fields: "PrimaryImageAspectRatio,SyncInfo",
            ParentId: parentId,
            ImageTypeLimit: 1,
            EnableImageTypes: "Primary,Backdrop,Banner,Thumb"
        };

        ApiClient.getJSON(ApiClient.getUrl('Users/' + userId + '/Items/Latest', options)).then(function (items) {

            var elem = page.querySelector('#recentlyAddedSongs');
            elem.innerHTML = LibraryBrowser.getPosterViewHtml({
                items: items,
                showUnplayedIndicator: false,
                showLatestItemsPopup: false,
                shape: getSquareShape(),
                showTitle: true,
                showParentTitle: true,
                lazy: true,
                centerText: true,
                overlayPlayButton: true

            });
            ImageLoader.lazyChildren(elem);

            Dashboard.hideLoadingMsg();

            LibraryBrowser.setLastRefreshed(page);
        });
    }

    function loadRecentlyPlayed(page, parentId) {

        var options = {

            SortBy: "DatePlayed",
            SortOrder: "Descending",
            IncludeItemTypes: "Audio",
            Limit: itemsPerRow(),
            Recursive: true,
            Fields: "PrimaryImageAspectRatio,AudioInfo,SyncInfo",
            Filters: "IsPlayed",
            ParentId: parentId,
            ImageTypeLimit: 1,
            EnableImageTypes: "Primary,Backdrop,Banner,Thumb"
        };

        ApiClient.getItems(Dashboard.getCurrentUserId(), options).then(function (result) {

            var elem;

            if (result.Items.length) {
                elem = $('#recentlyPlayed', page).show()[0];
            } else {
                elem = $('#recentlyPlayed', page).hide()[0];
            }

            var itemsContainer = elem.querySelector('.itemsContainer');
            itemsContainer.innerHTML = LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                showUnplayedIndicator: false,
                shape: getSquareShape(),
                showTitle: true,
                showParentTitle: true,
                defaultAction: 'instantmix',
                lazy: true,
                centerText: true,
                overlayMoreButton: true

            });
            ImageLoader.lazyChildren(itemsContainer);

        });

    }

    function loadFrequentlyPlayed(page, parentId) {

        var options = {

            SortBy: "PlayCount",
            SortOrder: "Descending",
            IncludeItemTypes: "Audio",
            Limit: itemsPerRow(),
            Recursive: true,
            Fields: "PrimaryImageAspectRatio,AudioInfo,SyncInfo",
            Filters: "IsPlayed",
            ParentId: parentId,
            ImageTypeLimit: 1,
            EnableImageTypes: "Primary,Backdrop,Banner,Thumb"
        };

        ApiClient.getItems(Dashboard.getCurrentUserId(), options).then(function (result) {

            var elem;

            if (result.Items.length) {
                elem = $('#topPlayed', page).show()[0];
            } else {
                elem = $('#topPlayed', page).hide()[0];
            }

            var itemsContainer = elem.querySelector('.itemsContainer');
            itemsContainer.innerHTML = LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                showUnplayedIndicator: false,
                shape: getSquareShape(),
                showTitle: true,
                showParentTitle: true,
                defaultAction: 'instantmix',
                lazy: true,
                centerText: true,
                overlayMoreButton: true

            });
            ImageLoader.lazyChildren(itemsContainer);

        });

    }

    function loadPlaylists(page, parentId) {

        var options = {

            SortBy: "SortName",
            SortOrder: "Ascending",
            IncludeItemTypes: "Playlist",
            Recursive: true,
            Fields: "PrimaryImageAspectRatio,SortName,CumulativeRunTimeTicks,CanDelete,SyncInfo",
            StartIndex: 0,
            Limit: itemsPerRow()
        };

        ApiClient.getItems(Dashboard.getCurrentUserId(), options).then(function (result) {

            var elem;

            if (result.Items.length) {
                elem = $('#playlists', page).show()[0];
            } else {
                elem = $('#playlists', page).hide()[0];
            }

            var itemsContainer = elem.querySelector('.itemsContainer');
            itemsContainer.innerHTML = LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                shape: getSquareShape(),
                showTitle: true,
                lazy: true,
                coverImage: true,
                showItemCounts: true,
                centerText: true,
                overlayPlayButton: true

            });
            ImageLoader.lazyChildren(itemsContainer);

        });
    }

    function initSuggestedTab(page, tabContent) {

        var containers = tabContent.querySelectorAll('.itemsContainer');
        if (enableScrollX()) {
            $(containers).addClass('hiddenScrollX');
        } else {
            $(containers).removeClass('hiddenScrollX');
        }

        $(containers).createCardMenus();
    }

    function loadSuggestionsTab(page, tabContent) {

        var parentId = LibraryMenu.getTopParentId();

        if (LibraryBrowser.needsRefresh(tabContent)) {
            console.log('loadSuggestionsTab');
            loadLatest(tabContent, parentId);
            loadPlaylists(tabContent, parentId);
            loadRecentlyPlayed(tabContent, parentId);
            loadFrequentlyPlayed(tabContent, parentId);

            require(['components/favoriteitems'], function (favoriteItems) {

                favoriteItems.render(tabContent, Dashboard.getCurrentUserId(), parentId, ['favoriteArtists', 'favoriteAlbums', 'favoriteSongs']);

            });
        }
    }

    function loadTab(page, index) {

        var tabContent = page.querySelector('.pageTabContent[data-index=\'' + index + '\']');
        var depends = [];
        var scope = 'MusicPage';
        var renderMethod = '';
        var initMethod = '';

        switch (index) {

            case 0:
                initMethod = 'initSuggestedTab';
                renderMethod = 'renderSuggestedTab';
                break;
            case 1:
                depends.push('scripts/musicalbums');
                renderMethod = 'renderAlbumsTab';
                initMethod = 'initAlbumsTab';
                break;
            case 2:
                depends.push('scripts/musicalbumartists');
                renderMethod = 'renderAlbumArtistsTab';
                initMethod = 'initAlbumArtistsTab';
                break;
            case 3:
                depends.push('scripts/musicartists');
                renderMethod = 'renderArtistsTab';
                initMethod = 'initArtistsTab';
                break;
            case 4:
                depends.push('scripts/songs');
                renderMethod = 'renderSongsTab';
                depends.push('paper-icon-item');
                depends.push('paper-item-body');
                break;
            case 5:
                depends.push('scripts/musicgenres');
                renderMethod = 'renderGenresTab';
                break;
            case 6:
                depends.push('scripts/musicfolders');
                renderMethod = 'renderFoldersTab';
                initMethod = 'initFoldersTab';
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

    window.MusicPage = window.MusicPage || {};
    window.MusicPage.renderSuggestedTab = loadSuggestionsTab;
    window.MusicPage.initSuggestedTab = initSuggestedTab;

    pageIdOn('pageinit', "musicRecommendedPage", function () {

        var page = this;

        $('.recommendations', page).createCardMenus();

        var tabs = page.querySelector('paper-tabs');
        var pageTabsContainer = page.querySelector('.pageTabsContainer');

        var baseUrl = 'music.html';
        var topParentId = LibraryMenu.getTopParentId();
        if (topParentId) {
            baseUrl += '?topParentId=' + topParentId;
        }

        LibraryBrowser.configurePaperLibraryTabs(page, tabs, pageTabsContainer, baseUrl);

        pageTabsContainer.addEventListener('tabchange', function (e) {
            loadTab(page, parseInt(e.detail.selectedTabIndex));
        });

    });

    pageIdOn('pagebeforeshow', "musicRecommendedPage", function () {

        var page = this;

        if (!page.getAttribute('data-title')) {

            var parentId = LibraryMenu.getTopParentId();

            if (parentId) {

                ApiClient.getItem(Dashboard.getCurrentUserId(), parentId).then(function (item) {

                    page.setAttribute('data-title', item.Name);
                    LibraryMenu.setTitle(item.Name);
                });


            } else {
                page.setAttribute('data-title', Globalize.translate('TabMusic'));
                LibraryMenu.setTitle(Globalize.translate('TabMusic'));
            }
        }

    });


});