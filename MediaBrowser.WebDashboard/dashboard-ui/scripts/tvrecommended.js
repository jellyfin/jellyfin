define(['libraryBrowser', 'components/categorysyncbuttons', 'scrollStyles', 'emby-itemscontainer'], function (libraryBrowser, categorysyncbuttons) {

    return function (view, params) {

        var self = this;

        function getView() {

            return 'Thumb';
        }

        function getResumeView() {

            return 'Poster';
        }

        function reload() {

            Dashboard.showLoadingMsg();

            loadResume();
            loadNextUp();
        }

        function loadNextUp() {

            var query = {

                Limit: 24,
                Fields: "PrimaryImageAspectRatio,SeriesInfo,DateCreated,SyncInfo",
                UserId: Dashboard.getCurrentUserId(),
                ImageTypeLimit: 1,
                EnableImageTypes: "Primary,Backdrop,Thumb"
            };

            query.ParentId = LibraryMenu.getTopParentId();

            ApiClient.getNextUpEpisodes(query).then(function (result) {

                if (result.Items.length) {
                    view.querySelector('.noNextUpItems').classList.add('hide');
                } else {
                    view.querySelector('.noNextUpItems').classList.remove('hide');
                }

                var viewStyle = getView();
                var html = '';

                if (viewStyle == 'ThumbCard') {

                    html += libraryBrowser.getPosterViewHtml({
                        items: result.Items,
                        shape: "backdrop",
                        showTitle: true,
                        preferThumb: true,
                        showParentTitle: true,
                        lazy: true,
                        cardLayout: true,
                        showDetailsMenu: true
                    });

                } else if (viewStyle == 'Thumb') {

                    html += libraryBrowser.getPosterViewHtml({
                        items: result.Items,
                        shape: "backdrop",
                        showTitle: true,
                        showParentTitle: true,
                        overlayText: false,
                        lazy: true,
                        preferThumb: true,
                        showDetailsMenu: true,
                        centerText: true,
                        overlayPlayButton: AppInfo.enableAppLayouts
                    });
                }

                var elem = view.querySelector('#nextUpItems');
                elem.innerHTML = html;
                ImageLoader.lazyChildren(elem);
                Dashboard.hideLoadingMsg();
            });
        }

        function enableScrollX() {
            return browserInfo.mobile && AppInfo.enableAppLayouts;
        }

        function getThumbShape() {
            return enableScrollX() ? 'overflowBackdrop' : 'backdrop';
        }

        function loadResume() {

            var parentId = LibraryMenu.getTopParentId();

            var limit = 6;

            var options = {

                SortBy: "DatePlayed",
                SortOrder: "Descending",
                IncludeItemTypes: "Episode",
                Filters: "IsResumable",
                Limit: limit,
                Recursive: true,
                Fields: "PrimaryImageAspectRatio,SeriesInfo,UserData,SyncInfo",
                ExcludeLocationTypes: "Virtual",
                ParentId: parentId,
                ImageTypeLimit: 1,
                EnableImageTypes: "Primary,Backdrop,Thumb",
                EnableTotalRecordCount: false
            };

            ApiClient.getItems(Dashboard.getCurrentUserId(), options).then(function (result) {

                if (result.Items.length) {
                    view.querySelector('#resumableSection').classList.remove('hide');
                } else {
                    view.querySelector('#resumableSection').classList.add('hide');
                }

                var viewStyle = getResumeView();
                var html = '';

                if (viewStyle == 'PosterCard') {

                    html += libraryBrowser.getPosterViewHtml({
                        items: result.Items,
                        shape: getThumbShape(),
                        showTitle: true,
                        showParentTitle: true,
                        lazy: true,
                        cardLayout: true,
                        showDetailsMenu: true,
                        preferThumb: true
                    });

                } else if (viewStyle == 'Poster') {

                    html += libraryBrowser.getPosterViewHtml({
                        items: result.Items,
                        shape: getThumbShape(),
                        showTitle: true,
                        showParentTitle: true,
                        lazy: true,
                        showDetailsMenu: true,
                        overlayPlayButton: true,
                        preferThumb: true,
                        centerText: true
                    });
                }

                var elem = view.querySelector('#resumableItems');
                elem.innerHTML = html;
                ImageLoader.lazyChildren(elem);
            });
        }

        self.initTab = function () {

            var tabContent = self.tabContent;
            if (enableScrollX()) {
                tabContent.querySelector('#resumableItems').classList.add('hiddenScrollX');
            } else {
                tabContent.querySelector('#resumableItems').classList.remove('hiddenScrollX');
            }

            categorysyncbuttons.init(tabContent);
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
                    depends.push('scripts/tvlatest');
                    break;
                case 2:
                    depends.push('scripts/tvupcoming');
                    break;
                case 3:
                    depends.push('scripts/tvshows');
                    break;
                case 4:
                    depends.push('scripts/episodes');
                    break;
                case 5:
                    depends.push('scripts/tvgenres');
                    break;
                case 6:
                    depends.push('scripts/tvstudios');
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

        function onPlaybackStop(e, state) {

            if (state.NowPlayingItem && state.NowPlayingItem.MediaType == 'Video') {

                renderedTabs = [];
                mdlTabs.dispatchEvent(new CustomEvent("tabchange", {
                    detail: {
                        selectedTabIndex: libraryBrowser.selectedTab(mdlTabs)
                    }
                }));
            }
        }

        var baseUrl = 'tv.html';
        var topParentId = params.topParentId;
        if (topParentId) {
            baseUrl += '?topParentId=' + topParentId;
        }

        if (enableScrollX()) {
            view.querySelector('#resumableItems').classList.add('hiddenScrollX');
        } else {
            view.querySelector('#resumableItems').classList.remove('hiddenScrollX');
        }
        libraryBrowser.configurePaperLibraryTabs(view, mdlTabs, view.querySelectorAll('.pageTabContent'), [0, 1, 2, 4, 5, 6]);

        mdlTabs.addEventListener('beforetabchange', function (e) {
            preLoadTab(view, parseInt(e.detail.selectedTabIndex));
        });
        mdlTabs.addEventListener('tabchange', function (e) {
            loadTab(view, parseInt(e.detail.selectedTabIndex));
        });

        function onWebSocketMessage(e, data) {

            var msg = data;

            if (msg.MessageType === "UserDataChanged") {

                if (msg.Data.UserId == Dashboard.getCurrentUserId()) {

                    renderedTabs = [];
                }
            }

        }

        view.addEventListener('viewbeforeshow', function (e) {

            if (!view.getAttribute('data-title')) {

                var parentId = params.topParentId;

                if (parentId) {

                    ApiClient.getItem(Dashboard.getCurrentUserId(), parentId).then(function (item) {

                        view.setAttribute('data-title', item.Name);
                        LibraryMenu.setTitle(item.Name);
                    });


                } else {
                    view.setAttribute('data-title', Globalize.translate('TabShows'));
                    LibraryMenu.setTitle(Globalize.translate('TabShows'));
                }
            }

            Events.on(MediaController, 'playbackstop', onPlaybackStop);
            Events.on(ApiClient, "websocketmessage", onWebSocketMessage);
        });

        view.addEventListener('viewbeforehide', function (e) {

            Events.off(MediaController, 'playbackstop', onPlaybackStop);
            Events.off(ApiClient, "websocketmessage", onWebSocketMessage);
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