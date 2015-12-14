(function ($, document) {

    function getView() {

        return 'Thumb';
    }

    function getResumeView() {

        return 'Poster';
    }

    function reload(page) {

        Dashboard.showLoadingMsg();

        if (LibraryMenu.getTopParentId()) {

            $('.scopedContent', page).show();

            loadResume(page);

        } else {
            $('.scopedContent', page).hide();
        }

        loadNextUp(page);
    }

    function loadNextUp(page) {

        var limit = AppInfo.hasLowImageBandwidth ?
         16 :
         24;

        var query = {

            Limit: limit,
            Fields: "PrimaryImageAspectRatio,SeriesInfo,DateCreated,SyncInfo",
            UserId: Dashboard.getCurrentUserId(),
            ImageTypeLimit: 1,
            EnableImageTypes: "Primary,Backdrop,Banner,Thumb"
        };

        query.ParentId = LibraryMenu.getTopParentId();

        ApiClient.getNextUpEpisodes(query).then(function (result) {

            if (result.Items.length) {
                $('.noNextUpItems', page).hide();
            } else {
                $('.noNextUpItems', page).show();
            }

            var view = getView();
            var html = '';

            if (view == 'ThumbCard') {

                html += LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    shape: "backdrop",
                    showTitle: true,
                    preferThumb: true,
                    showParentTitle: true,
                    lazy: true,
                    cardLayout: true,
                    showDetailsMenu: true
                });

            } else if (view == 'Thumb') {

                html += LibraryBrowser.getPosterViewHtml({
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

            var elem = page.querySelector('#nextUpItems');
            elem.innerHTML = html;
            ImageLoader.lazyChildren(elem);
            Dashboard.hideLoadingMsg();

            LibraryBrowser.setLastRefreshed(page);
        });
    }

    function enableScrollX() {
        return browserInfo.mobile && AppInfo.enableAppLayouts;
    }

    function getThumbShape() {
        return enableScrollX() ? 'overflowBackdrop' : 'backdrop';
    }

    function loadResume(page) {

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
            EnableImageTypes: "Primary,Backdrop,Banner,Thumb"
        };

        ApiClient.getItems(Dashboard.getCurrentUserId(), options).then(function (result) {

            if (result.Items.length) {
                $('#resumableSection', page).show();
            } else {
                $('#resumableSection', page).hide();
            }

            var view = getResumeView();
            var html = '';

            if (view == 'PosterCard') {

                html += LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    shape: getThumbShape(),
                    showTitle: true,
                    showParentTitle: true,
                    lazy: true,
                    cardLayout: true,
                    showDetailsMenu: true,
                    preferThumb: true
                });

            } else if (view == 'Poster') {

                html += LibraryBrowser.getPosterViewHtml({
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

            var elem = page.querySelector('#resumableItems');
            elem.innerHTML = html;
            ImageLoader.lazyChildren(elem);
        });
    }

    function initSuggestedTab(page, tabContent) {

        if (enableScrollX()) {
            tabContent.querySelector('#resumableItems').classList.add('hiddenScrollX');
        } else {
            tabContent.querySelector('#resumableItems').classList.remove('hiddenScrollX');
        }
        $(tabContent.querySelector('#resumableItems')).createCardMenus();
    }

    function loadSuggestionsTab(page, tabContent) {

        if (LibraryBrowser.needsRefresh(tabContent)) {
            reload(tabContent);
        }
    }

    function loadTab(page, index) {

        var tabContent = page.querySelector('.pageTabContent[data-index=\'' + index + '\']');
        var depends = [];
        var scope = 'TvPage';
        var renderMethod = '';
        var initMethod = '';

        switch (index) {

            case 0:
                initMethod = 'initSuggestedTab';
                renderMethod = 'renderSuggestedTab';
                break;
            case 1:
                depends.push('scripts/tvlatest');
                renderMethod = 'renderLatestTab';
                break;
            case 2:
                depends.push('scripts/tvupcoming');
                renderMethod = 'renderUpcomingTab';
                break;
            case 3:
                depends.push('scripts/tvshows');
                depends.push('scripts/queryfilters');
                renderMethod = 'renderSeriesTab';
                initMethod = 'initSeriesTab';
                break;
            case 4:
                depends.push('scripts/episodes');
                renderMethod = 'renderEpisodesTab';
                initMethod = 'initEpisodesTab';
                break;
            case 5:
                depends.push('scripts/tvgenres');
                renderMethod = 'renderGenresTab';
                break;
            case 6:
                depends.push('scripts/tvstudios');
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

    window.TvPage = window.TvPage || {};
    window.TvPage.renderSuggestedTab = loadSuggestionsTab;
    window.TvPage.initSuggestedTab = initSuggestedTab;

    pageIdOn('pageinit', "tvRecommendedPage", function () {

        var page = this;

        $('.recommendations', page).createCardMenus();

        var tabs = page.querySelector('paper-tabs');
        var pages = page.querySelector('neon-animated-pages');

        var baseUrl = 'tv.html';
        var topParentId = LibraryMenu.getTopParentId();
        if (topParentId) {
            baseUrl += '?topParentId=' + topParentId;
        }

        if (enableScrollX()) {
            page.querySelector('#resumableItems').classList.add('hiddenScrollX');
        } else {
            page.querySelector('#resumableItems').classList.remove('hiddenScrollX');
        }
        $(page.querySelector('#resumableItems')).createCardMenus();

        LibraryBrowser.configurePaperLibraryTabs(page, tabs, pages, baseUrl);

        pages.addEventListener('tabchange', function (e) {
            loadTab(page, parseInt(this.selected));
        });
    });

    pageIdOn('pagebeforeshow', "tvRecommendedPage", function () {

        var page = this;

        if (!page.getAttribute('data-title')) {

            var parentId = LibraryMenu.getTopParentId();

            if (parentId) {

                ApiClient.getItem(Dashboard.getCurrentUserId(), parentId).then(function (item) {

                    page.setAttribute('data-title', item.Name);
                    LibraryMenu.setTitle(item.Name);
                });


            } else {
                page.setAttribute('data-title', Globalize.translate('TabShows'));
                LibraryMenu.setTitle(Globalize.translate('TabShows'));
            }
        }

        $(MediaController).on('playbackstop', onPlaybackStop);
    });

    pageIdOn('pagebeforehide', "tvRecommendedPage", function () {

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