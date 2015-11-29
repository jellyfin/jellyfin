(function ($, document) {

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

                html += LibraryBrowser.getPosterViewHtml({
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

                html += LibraryBrowser.getPosterViewHtml({
                    items: items,
                    shape: getPortraitShape(),
                    centerText: true,
                    lazy: true,
                    overlayText: false,
                    showDetailsMenu: true
                });
            }

            var recentlyAddedItems = page.querySelector('#recentlyAddedItems');
            recentlyAddedItems.innerHTML = html;
            ImageLoader.lazyChildren(recentlyAddedItems);
            LibraryBrowser.setLastRefreshed(page);
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

                html += LibraryBrowser.getPosterViewHtml({
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

                html += LibraryBrowser.getPosterViewHtml({
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

            html += LibraryBrowser.getPosterViewHtml({
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

            html += LibraryBrowser.getPosterViewHtml({
                items: recommendation.Items,
                shape: getPortraitShape(),
                centerText: true,
                lazy: true,
                showDetailsMenu: true
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

    function loadSuggestionsTab(page, tabContent) {

        var parentId = LibraryMenu.getTopParentId();

        var userId = Dashboard.getCurrentUserId();

        if (LibraryBrowser.needsRefresh(tabContent)) {
            console.log('loadSuggestionsTab');
            loadResume(tabContent, userId, parentId);
            loadLatest(tabContent, userId, parentId);

            if (AppInfo.enableMovieHomeSuggestions) {
                loadSuggestions(tabContent, userId, parentId);
            }
        }
    }

    function loadTab(page, index) {

        var tabContent = page.querySelector('.pageTabContent[data-index=\'' + index + '\']');
        var depends = [];
        var scope = 'MoviesPage';
        var renderMethod = '';
        var initMethod = '';

        switch (index) {

            case 0:
                initMethod = 'initSuggestedTab';
                renderMethod = 'renderSuggestedTab';
                break;
            case 1:
                depends.push('scripts/movies');
                depends.push('scripts/queryfilters');
                renderMethod = 'renderMoviesTab';
                initMethod = 'initMoviesTab';
                break;
            case 2:
                depends.push('scripts/movietrailers');
                renderMethod = 'renderTrailerTab';
                initMethod = 'initTrailerTab';
                break;
            case 3:
                depends.push('scripts/moviecollections');
                renderMethod = 'renderCollectionsTab';
                initMethod = 'initCollectionsTab';
                break;
            case 4:
                depends.push('scripts/moviegenres');
                renderMethod = 'renderGenresTab';
                break;
            case 5:
                depends.push('scripts/moviestudios');
                renderMethod = 'renderStudiosTab';
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

    window.MoviesPage = window.MoviesPage || {};
    window.MoviesPage.renderSuggestedTab = loadSuggestionsTab;
    window.MoviesPage.initSuggestedTab = initSuggestedTab;

    pageIdOn('pageinit', "moviesPage", function () {

        var page = this;

        $('.recommendations', page).createCardMenus();

        var tabs = page.querySelector('paper-tabs');
        var pages = page.querySelector('neon-animated-pages');

        var baseUrl = 'movies.html';
        var topParentId = LibraryMenu.getTopParentId();
        if (topParentId) {
            baseUrl += '?topParentId=' + topParentId;
        }

        LibraryBrowser.configurePaperLibraryTabs(page, tabs, pages, baseUrl);

        pages.addEventListener('tabchange', function (e) {
            loadTab(page, parseInt(e.target.selected));
        });
    });

    pageIdOn('pagebeforeshow', "moviesPage", function () {

        var page = this;

        if (!page.getAttribute('data-title')) {

            var parentId = LibraryMenu.getTopParentId();

            if (parentId) {

                ApiClient.getItem(Dashboard.getCurrentUserId(), parentId).then(function (item) {

                    page.setAttribute('data-title', item.Name);
                    LibraryMenu.setTitle(item.Name);
                });


            } else {
                page.setAttribute('data-title', Globalize.translate('TabMovies'));
                LibraryMenu.setTitle(Globalize.translate('TabMovies'));
            }
        }

        $(MediaController).on('playbackstop', onPlaybackStop);
    });

    pageIdOn('pagebeforehide', "moviesPage", function () {

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

})(jQuery, document);