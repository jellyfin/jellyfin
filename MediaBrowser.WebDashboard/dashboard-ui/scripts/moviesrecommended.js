define(['libraryBrowser', 'components/categorysyncbuttons', 'cardBuilder', 'scrollStyles', 'emby-itemscontainer'], function (libraryBrowser, categorysyncbuttons, cardBuilder) {

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
            Fields: "PrimaryImageAspectRatio,MediaSourceCount,BasicSyncInfo",
            ParentId: parentId,
            ImageTypeLimit: 1,
            EnableImageTypes: "Primary,Backdrop,Banner,Thumb",
            EnableTotalRecordCount: false
        };

        ApiClient.getJSON(ApiClient.getUrl('Users/' + userId + '/Items/Latest', options)).then(function (items) {

            var container = page.querySelector('#recentlyAddedItems');
            cardBuilder.buildCards(items, {
                itemsContainer: container,
                shape: getPortraitShape(),
                scalable: true,
                overlayPlayButton: true
            });
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
            Fields: "PrimaryImageAspectRatio,MediaSourceCount,BasicSyncInfo",
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

            var container = page.querySelector('#resumableItems');
            cardBuilder.buildCards(result.Items, {
                itemsContainer: container,
                preferThumb: true,
                shape: getThumbShape(),
                scalable: true,
                overlayPlayButton: true
            });

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
            html += '<div is="emby-itemscontainer" class="itemsContainer hiddenScrollX">';
        } else {
            html += '<div is="emby-itemscontainer" class="itemsContainer vertical-wrap">';
        }

        html += cardBuilder.getCardsHtml(recommendation.Items, {
            shape: getPortraitShape(),
            scalable: true,
            overlayPlayButton: true
        });

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
            Fields: "PrimaryImageAspectRatio,MediaSourceCount,BasicSyncInfo",
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
                containers[i].classList.remove('vertical-wrap');
            } else {
                containers[i].classList.remove('hiddenScrollX');
                containers[i].classList.add('vertical-wrap');
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