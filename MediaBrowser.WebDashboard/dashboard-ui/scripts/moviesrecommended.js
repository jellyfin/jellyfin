define(['jQuery', 'libraryBrowser'], function ($, libraryBrowser) {

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

        var limit = 18;

        var options = {

            IncludeItemTypes: "Movie",
            Limit: limit,
            Fields: "PrimaryImageAspectRatio,MediaSourceCount,SyncInfo",
            ParentId: parentId,
            ImageTypeLimit: 1,
            EnableImageTypes: "Primary,Backdrop,Banner,Thumb"
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

        var screenWidth = $(window).width();

        var options = {

            SortBy: "DatePlayed",
            SortOrder: "Descending",
            IncludeItemTypes: "Movie",
            Filters: "IsResumable",
            Limit: screenWidth >= 1920 ? 6 : (screenWidth >= 1600 ? 4 : 3),
            Recursive: true,
            Fields: "PrimaryImageAspectRatio,MediaSourceCount,SyncInfo",
            CollapseBoxSetItems: false,
            ParentId: parentId,
            ImageTypeLimit: 1,
            EnableImageTypes: "Primary,Backdrop,Banner,Thumb"
        };

        ApiClient.getItems(userId, options).then(function (result) {

            if (result.Items.length) {
                $('#resumableSection', page).show();
            } else {
                $('#resumableSection', page).hide();
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
            html += '<div class="hiddenScrollX">';
        } else {
            html += '<div>';
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

        var screenWidth = $(window).width();

        var url = ApiClient.getUrl("Movies/Recommendations", {

            userId: userId,
            categoryLimit: screenWidth >= 1200 ? 4 : 3,
            ItemLimit: screenWidth >= 1920 ? 9 : (screenWidth >= 1600 ? 8 : (screenWidth >= 1200 ? 7 : 6)),
            Fields: "PrimaryImageAspectRatio,MediaSourceCount,SyncInfo",
            ImageTypeLimit: 1,
            EnableImageTypes: "Primary,Backdrop,Banner,Thumb"
        });

        ApiClient.getJSON(url).then(function (recommendations) {

            if (!recommendations.length) {

                $('.noItemsMessage', page).show();
                page.querySelector('.recommendations').innerHTML = '';
                return;
            }

            var html = recommendations.map(getRecommendationHtml).join('');

            $('.noItemsMessage', page).hide();

            var recs = page.querySelector('.recommendations');
            recs.innerHTML = html;
            ImageLoader.lazyChildren(recs);
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

    function loadSuggestionsTab(view, params, tabContent) {

        var parentId = params.topParentId;

        var userId = Dashboard.getCurrentUserId();

        console.log('loadSuggestionsTab');
        loadResume(tabContent, userId, parentId);
        loadLatest(tabContent, userId, parentId);

        if (AppInfo.enableMovieHomeSuggestions) {
            loadSuggestions(tabContent, userId, parentId);
        }
    }

    function onPlaybackStop(e, state) {

        if (state.NowPlayingItem && state.NowPlayingItem.MediaType == 'Video') {
            var page = $($.mobile.activePage)[0];
            var pageTabsContainer = page.querySelector('.pageTabsContainer');

            pageTabsContainer.dispatchEvent(new CustomEvent("tabchange", {
                detail: {
                    selectedTabIndex: libraryBrowser.selectedTab(pageTabsContainer)
                }
            }));
        }
    }

    return function (view, params) {

        var self = this;

        self.initTab = function() {
            var tabContent = view.querySelector('.pageTabContent[data-index=\'' + 0 + '\']');
            initSuggestedTab(view, tabContent);
        };

        self.renderTab = function () {
            var tabContent = view.querySelector('.pageTabContent[data-index=\'' + 0 + '\']');
            loadSuggestionsTab(view, params, tabContent);
        };

        $('.recommendations', view).createCardMenus();

        var pageTabsContainer = view.querySelector('.pageTabsContainer');

        var baseUrl = 'movies.html';
        var topParentId = params.topParentId;
        if (topParentId) {
            baseUrl += '?topParentId=' + topParentId;
        }

        libraryBrowser.configurePaperLibraryTabs(view, view.querySelector('paper-tabs'), pageTabsContainer, baseUrl);

        var tabControllers = [];
        var renderedTabs = [];

        function loadTab(page, index) {

            var tabContent = page.querySelector('.pageTabContent[data-index=\'' + index + '\']');
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

                if (index == 0) {
                    self.tabContent = tabContent;
                }
                var controller = tabControllers[index];
                if (!controller) {
                    controller = index ? new controllerFactory(view, params, tabContent) : self;
                    tabControllers[index] = controller;

                    if (controller.initTab) {
                        controller.initTab();
                    }
                }

                if (renderedTabs.indexOf(index) == -1) {
                    renderedTabs.push(index);
                    controller.renderTab();
                }
            });
        }

        pageTabsContainer.addEventListener('tabchange', function (e) {
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

        view.addEventListener('viewshow', function (e) {
            Events.on(MediaController, 'playbackstop', onPlaybackStop);
        });

        view.addEventListener('viewbeforehide', function (e) {
            Events.off(MediaController, 'playbackstop', onPlaybackStop);
        });
    };

});