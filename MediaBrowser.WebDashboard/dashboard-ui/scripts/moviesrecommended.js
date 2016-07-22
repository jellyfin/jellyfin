define(['libraryBrowser', 'components/categorysyncbuttons', 'scrollStyles', 'emby-itemscontainer'], function (libraryBrowser, categorysyncbuttons) {

    function getView() {

        return 'Poster';
    }

    function getResumeView() {

        return 'Thumb';
    }

    function enableScrollX() {
        return browserInfo.mobile && AppInfo.enableAppLayouts;
    }

    function getPortraitShape() {
        return enableScrollX() ? 'overflowPortrait' : 'portrait';
    }

    function getThumbShape() {
        return enableScrollX() ? 'overflowBackdrop' : 'backdrop';
    }

    function loadLatest(page, userId, parentId) {

        var options = {

            IncludeItemTypes: "Movie",
            Limit: 18,
            Fields: "PrimaryImageAspectRatio,MediaSourceCount,SyncInfo",
            ParentId: parentId,
            ImageTypeLimit: 1,
            EnableImageTypes: "Primary,Backdrop,Banner,Thumb",
            EnableTotalRecordCount: false
        };

        ApiClient.getJSON(ApiClient.getUrl('Users/' + userId + '/Items/Latest', options)).then(function (items) {

            var view = getView();
            var html = '';

            if (view == 'PosterCard') {

                html += libraryBrowser.getPosterViewHtml({
                    items: items,
                    lazy: true,
                    shape: getPortraitShape(),
                    overlayText: false,
                    showTitle: true,
                    showYear: true,
                    cardLayout: true,
                    showDetailsMenu: true

                });

            } else if (view == 'Poster') {

                html += libraryBrowser.getPosterViewHtml({
                    items: items,
                    shape: getPortraitShape(),
                    centerText: true,
                    lazy: true,
                    overlayText: false,
                    showDetailsMenu: true,
                    overlayPlayButton: true
                });
            }

            var recentlyAddedItems = page.querySelector('#recentlyAddedItems');
            recentlyAddedItems.innerHTML = html;
            ImageLoader.lazyChildren(recentlyAddedItems);
        });
    }

    function loadResume(page, userId, parentId) {

        var screenWidth = window.innerWidth;

        var options = {

            SortBy: "DatePlayed",
            SortOrder: "Descending",
            IncludeItemTypes: "Movie",
            Filters: "IsResumable",
            Limit: screenWidth >= 1920 ? 5 : (screenWidth >= 1600 ? 4 : 3),
            Recursive: true,
            Fields: "PrimaryImageAspectRatio,MediaSourceCount,SyncInfo",
            CollapseBoxSetItems: false,
            ParentId: parentId,
            ImageTypeLimit: 1,
            EnableImageTypes: "Primary,Backdrop,Banner,Thumb",
            EnableTotalRecordCount: false
        };

        ApiClient.getItems(userId, options).then(function (result) {

            if (result.Items.length) {
                page.querySelector('#resumableSection').classList.remove('hide');
            } else {
                page.querySelector('#resumableSection').classList.add('hide');
            }

            var view = getResumeView();
            var html = '';

            if (view == 'ThumbCard') {

                html += libraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    preferThumb: true,
                    shape: getThumbShape(),
                    showTitle: true,
                    showYear: true,
                    lazy: true,
                    cardLayout: true,
                    showDetailsMenu: true

                });

            } else if (view == 'Thumb') {

                html += libraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    preferThumb: true,
                    shape: getThumbShape(),
                    overlayText: true,
                    showTitle: false,
                    lazy: true,
                    showDetailsMenu: true,
                    overlayPlayButton: true
                });
            }

            var resumableItems = page.querySelector('#resumableItems');
            resumableItems.innerHTML = html;
            ImageLoader.lazyChildren(resumableItems);

        });
    }

    function getRecommendationHtml(recommendation) {

        var html = '';

        var title = '';

        switch (recommendation.RecommendationType) {

            case 'SimilarToRecentlyPlayed':
                title = Globalize.translate('RecommendationBecauseYouWatched').replace("{0}", recommendation.BaselineItemName);
                break;
            case 'SimilarToLikedItem':
                title = Globalize.translate('RecommendationBecauseYouLike').replace("{0}", recommendation.BaselineItemName);
                break;
            case 'HasDirectorFromRecentlyPlayed':
            case 'HasLikedDirector':
                title = Globalize.translate('RecommendationDirectedBy').replace("{0}", recommendation.BaselineItemName);
                break;
            case 'HasActorFromRecentlyPlayed':
            case 'HasLikedActor':
                title = Globalize.translate('RecommendationStarring').replace("{0}", recommendation.BaselineItemName);
                break;
        }

        html += '<div class="homePageSection">';
        html += '<h1 class="listHeader">' + title + '</h1>';

        if (enableScrollX()) {
            html += '<div is="emby-itemscontainer" class="hiddenScrollX">';
        } else {
            html += '<div is="emby-itemscontainer">';
        }

        var view = getView();

        if (view == 'PosterCard') {

            html += libraryBrowser.getPosterViewHtml({
                items: recommendation.Items,
                lazy: true,
                shape: getPortraitShape(),
                overlayText: false,
                showTitle: true,
                showYear: true,
                cardLayout: true,
                showDetailsMenu: true

            });

        } else if (view == 'Poster') {

            html += libraryBrowser.getPosterViewHtml({
                items: recommendation.Items,
                shape: getPortraitShape(),
                centerText: true,
                lazy: true,
                showDetailsMenu: true,
                overlayPlayButton: true
            });
        }
        html += '</div>';
        html += '</div>';

        return html;
    }

    function loadSuggestions(page, userId, parentId) {

        var screenWidth = window.innerWidth;

        var url = ApiClient.getUrl("Movies/Recommendations", {

            userId: userId,
            categoryLimit: 6,
            ItemLimit: screenWidth >= 1920 ? 8 : (screenWidth >= 1600 ? 8 : (screenWidth >= 1200 ? 6 : 5)),
            Fields: "PrimaryImageAspectRatio,MediaSourceCount,SyncInfo",
            ImageTypeLimit: 1,
            EnableImageTypes: "Primary,Backdrop,Banner,Thumb"
        });

        ApiClient.getJSON(url).then(function (recommendations) {

            if (!recommendations.length) {

                page.querySelector('.noItemsMessage').classList.remove('hide');
                page.querySelector('.recommendations').innerHTML = '';
                return;
            }

            var html = recommendations.map(getRecommendationHtml).join('');

            page.querySelector('.noItemsMessage').classList.add('hide');

            var recs = page.querySelector('.recommendations');
            recs.innerHTML = html;
            ImageLoader.lazyChildren(recs);
        });
    }

    function initSuggestedTab(page, tabContent) {

        var containers = tabContent.querySelectorAll('.itemsContainer');
        for (var i = 0, length = containers.length; i < length; i++) {
            if (enableScrollX()) {
                containers[i].classList.add('hiddenScrollX');
            } else {
                containers[i].classList.remove('hiddenScrollX');
            }
        }
    }

    function loadSuggestionsTab(view, params, tabContent) {

        var parentId = params.topParentId;

        var userId = Dashboard.getCurrentUserId();

        console.log('loadSuggestionsTab');
        loadResume(tabContent, userId, parentId);
        loadLatest(tabContent, userId, parentId);

        loadSuggestions(tabContent, userId, parentId);
    }

    return function (view, params) {

        var self = this;

        self.initTab = function () {

            var tabContent = view.querySelector('.pageTabContent[data-index=\'' + 0 + '\']');
            categorysyncbuttons.init(tabContent);
            initSuggestedTab(view, tabContent);
        };

        self.renderTab = function () {
            var tabContent = view.querySelector('.pageTabContent[data-index=\'' + 0 + '\']');
            loadSuggestionsTab(view, params, tabContent);
        };

        var mdlTabs = view.querySelector('.libraryViewNav');

        var baseUrl = 'movies.html';
        var topParentId = params.topParentId;
        if (topParentId) {
            baseUrl += '?topParentId=' + topParentId;
        }

        libraryBrowser.configurePaperLibraryTabs(view, mdlTabs, view.querySelectorAll('.pageTabContent'), [0, 3, 4, 5]);

        var tabControllers = [];
        var renderedTabs = [];

        function getTabController(page, index, callback) {

            var depends = [];

            switch (index) {

                case 0:
                    break;
                case 1:
                    depends.push('scripts/movies');
                    break;
                case 2:
                    depends.push('scripts/movietrailers');
                    break;
                case 3:
                    depends.push('scripts/moviecollections');
                    break;
                case 4:
                    depends.push('scripts/moviegenres');
                    break;
                case 5:
                    depends.push('scripts/moviestudios');
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

        mdlTabs.addEventListener('beforetabchange', function (e) {
            preLoadTab(view, parseInt(e.detail.selectedTabIndex));
        });
        mdlTabs.addEventListener('tabchange', function (e) {
            loadTab(view, parseInt(e.detail.selectedTabIndex));
        });

        view.addEventListener('viewbeforeshow', function (e) {
            if (!view.getAttribute('data-title')) {

                var parentId = params.topParentId;

                if (parentId) {

                    ApiClient.getItem(Dashboard.getCurrentUserId(), parentId).then(function (item) {

                        view.setAttribute('data-title', item.Name);
                        LibraryMenu.setTitle(item.Name);
                    });


                } else {
                    view.setAttribute('data-title', Globalize.translate('TabMovies'));
                    LibraryMenu.setTitle(Globalize.translate('TabMovies'));
                }
            }
        });

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

        view.addEventListener('viewshow', function (e) {
            Events.on(MediaController, 'playbackstop', onPlaybackStop);
        });

        view.addEventListener('viewbeforehide', function (e) {
            Events.off(MediaController, 'playbackstop', onPlaybackStop);
        });
    };

});