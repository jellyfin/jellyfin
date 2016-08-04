define(['libraryBrowser', 'cardBuilder', 'scrollStyles', 'emby-itemscontainer'], function (libraryBrowser, cardBuilder) {

    function itemsPerRow() {

        var screenWidth = window.innerWidth;

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
            EnableImageTypes: "Primary,Backdrop,Banner,Thumb",
            EnableTotalRecordCount: false
        };

        ApiClient.getJSON(ApiClient.getUrl('Users/' + userId + '/Items/Latest', options)).then(function (items) {

            var elem = page.querySelector('#recentlyAddedSongs');
            elem.innerHTML = cardBuilder.getCardsHtml({
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
        });
    }

    function loadRecentlyPlayed(page, parentId) {

        var options = {

            SortBy: "DatePlayed",
            SortOrder: "Descending",
            IncludeItemTypes: "Audio",
            Limit: itemsPerRow(),
            Recursive: true,
            Fields: "PrimaryImageAspectRatio,AudioInfo",
            Filters: "IsPlayed",
            ParentId: parentId,
            ImageTypeLimit: 1,
            EnableImageTypes: "Primary,Backdrop,Banner,Thumb",
            EnableTotalRecordCount: false
        };

        ApiClient.getItems(Dashboard.getCurrentUserId(), options).then(function (result) {

            var elem = page.querySelector('#recentlyPlayed');

            if (result.Items.length) {
                elem.classList.remove('hide');
            } else {
                elem.classList.add('hide');
            }

            var itemsContainer = elem.querySelector('.itemsContainer');
            itemsContainer.innerHTML = cardBuilder.getCardsHtml({
                items: result.Items,
                showUnplayedIndicator: false,
                shape: getSquareShape(),
                showTitle: true,
                showParentTitle: true,
                action: 'instantmix',
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
            Fields: "PrimaryImageAspectRatio,AudioInfo",
            Filters: "IsPlayed",
            ParentId: parentId,
            ImageTypeLimit: 1,
            EnableImageTypes: "Primary,Backdrop,Banner,Thumb",
            EnableTotalRecordCount: false
        };

        ApiClient.getItems(Dashboard.getCurrentUserId(), options).then(function (result) {

            var elem = page.querySelector('#topPlayed');

            if (result.Items.length) {
                elem.classList.remove('hide');
            } else {
                elem.classList.add('hide');
            }

            var itemsContainer = elem.querySelector('.itemsContainer');
            itemsContainer.innerHTML = cardBuilder.getCardsHtml({
                items: result.Items,
                showUnplayedIndicator: false,
                shape: getSquareShape(),
                showTitle: true,
                showParentTitle: true,
                action: 'instantmix',
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
            Fields: "PrimaryImageAspectRatio,SortName,CumulativeRunTimeTicks,CanDelete",
            StartIndex: 0,
            Limit: itemsPerRow(),
            EnableTotalRecordCount: false
        };

        ApiClient.getItems(Dashboard.getCurrentUserId(), options).then(function (result) {

            var elem = page.querySelector('#playlists');

            if (result.Items.length) {
                elem.classList.remove('hide');
            } else {
                elem.classList.add('hide');
            }

            var itemsContainer = elem.querySelector('.itemsContainer');
            itemsContainer.innerHTML = cardBuilder.getCardsHtml({
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

    function loadSuggestionsTab(page, tabContent) {

        var parentId = LibraryMenu.getTopParentId();

        console.log('loadSuggestionsTab');
        loadLatest(tabContent, parentId);
        loadPlaylists(tabContent, parentId);
        loadRecentlyPlayed(tabContent, parentId);
        loadFrequentlyPlayed(tabContent, parentId);

        require(['components/favoriteitems'], function (favoriteItems) {

            favoriteItems.render(tabContent, Dashboard.getCurrentUserId(), parentId, ['favoriteArtists', 'favoriteAlbums', 'favoriteSongs']);

        });
    }

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

    return function (view, params) {

        var self = this;

        function reload() {

            Dashboard.showLoadingMsg();

            var tabContent = view.querySelector('.pageTabContent[data-index=\'' + 0 + '\']');
            loadSuggestionsTab(view, tabContent);
        }

        function enableScrollX() {
            return browserInfo.mobile && AppInfo.enableAppLayouts;
        }

        function getThumbShape() {
            return enableScrollX() ? 'overflowBackdrop' : 'backdrop';
        }

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
            reload();
        };

        var tabControllers = [];
        var renderedTabs = [];

        function getTabController(page, index, callback) {

            var depends = [];

            switch (index) {

                case 0:
                    break;
                case 1:
                    depends.push('scripts/musicalbums');
                    break;
                case 2:
                    depends.push('scripts/musicartists');
                    break;
                case 3:
                    depends.push('scripts/musicartists');
                    break;
                case 4:
                    depends.push('scripts/songs');
                    break;
                case 5:
                    depends.push('scripts/musicgenres');
                    break;
                case 6:
                    depends.push('scripts/musicfolders');
                    break;
                default:
                    break;
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

                    if (index == 2) {
                        controller.mode = 'albumartists';
                    } else if (index == 3) {
                        controller.mode = 'artists';
                    }

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

        var mdlTabs = view.querySelector('.libraryViewNav');

        var baseUrl = 'music.html';
        var topParentId = params.topParentId;
        if (topParentId) {
            baseUrl += '?topParentId=' + topParentId;
        }

        libraryBrowser.configurePaperLibraryTabs(view, mdlTabs, view.querySelectorAll('.pageTabContent'), [0, 4, 5, 6]);

        mdlTabs.addEventListener('beforetabchange', function (e) {
            preLoadTab(view, parseInt(e.detail.selectedTabIndex));
        });
        mdlTabs.addEventListener('tabchange', function (e) {
            loadTab(view, parseInt(e.detail.selectedTabIndex));
        });

        view.addEventListener('viewdestroy', function (e) {

            tabControllers.forEach(function (t) {
                if (t.destroy) {
                    t.destroy();
                }
            });
        });
    };

});