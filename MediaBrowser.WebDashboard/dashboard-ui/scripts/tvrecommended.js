define(['libraryBrowser', 'scripts/alphapicker'], function (libraryBrowser) {

    function getView() {

        return 'Thumb';
    }

    function getResumeView() {

        return 'Poster';
    }

    function reload(page) {

        Dashboard.showLoadingMsg();

        loadResume(page);
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
                page.querySelector('.noNextUpItems').classList.add('hide');
            } else {
                page.querySelector('.noNextUpItems').classList.remove('hide');
            }

            var view = getView();
            var html = '';

            if (view == 'ThumbCard') {

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

            } else if (view == 'Thumb') {

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

            var elem = page.querySelector('#nextUpItems');
            elem.innerHTML = html;
            ImageLoader.lazyChildren(elem);
            Dashboard.hideLoadingMsg();

            libraryBrowser.setLastRefreshed(page);
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
                page.querySelector('#resumableSection').classList.remove('hide');
            } else {
                page.querySelector('#resumableSection').classList.add('hide');
            }

            var view = getResumeView();
            var html = '';

            if (view == 'PosterCard') {

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

            } else if (view == 'Poster') {

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
        libraryBrowser.createCardMenus(tabContent.querySelector('#resumableItems'));
    }

    function loadSuggestionsTab(page, tabContent) {

        if (libraryBrowser.needsRefresh(tabContent)) {
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

    function onPlaybackStop(e, state) {

        if (state.NowPlayingItem && state.NowPlayingItem.MediaType == 'Video') {
            var page = $.mobile.activePage;
            var pages = page.querySelector('neon-animated-pages');

            pages.dispatchEvent(new CustomEvent("tabchange", {}));
        }
    }

    return function (view, params) {

        var tabs = view.querySelector('paper-tabs');
        var pages = view.querySelector('neon-animated-pages');

        var baseUrl = 'tv.html';
        var topParentId = LibraryMenu.getTopParentId();
        if (topParentId) {
            baseUrl += '?topParentId=' + topParentId;
        }

        if (enableScrollX()) {
            view.querySelector('#resumableItems').classList.add('hiddenScrollX');
        } else {
            view.querySelector('#resumableItems').classList.remove('hiddenScrollX');
        }
        libraryBrowser.createCardMenus(view.querySelector('#resumableItems'));

        libraryBrowser.configurePaperLibraryTabs(view, tabs, pages, baseUrl);

        pages.addEventListener('tabchange', function (e) {
            loadTab(view, parseInt(this.selected));
        });

        view.addEventListener('viewbeforeshow', function (e) {

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

            Events.on(MediaController, 'playbackstop', onPlaybackStop);
        });

        view.addEventListener('viewbeforehide', function (e) {

            var page = this;
            Events.off(MediaController, 'playbackstop', onPlaybackStop);
        });
    };
});