define(['jQuery'], function ($) {

    var view = 'Poster';

    var data = {};
    function getQuery(tab) {

        var key = getSavedQueryKey(tab);
        var pageData = data[key];

        if (!pageData) {
            pageData = data[key] = {
                query: {
                    SortBy: "SortName",
                    SortOrder: "Ascending",
                    Fields: "PrimaryImageAspectRatio,SortName,SyncInfo",
                    ImageTypeLimit: 1,
                    EnableImageTypes: "Primary",
                    StartIndex: 0,
                    Limit: LibraryBrowser.getDefaultPageSize()
                }
            };
            setQueryPerTab(tab, pageData.query);
            pageData.query.ParentId = LibraryMenu.getTopParentId();
            LibraryBrowser.loadSavedQueryValues(key, pageData.query);
        }
        return pageData.query;
    }

    function getSavedQueryKey(tab) {

        return LibraryBrowser.getSavedQueryKey('tab=' + tab);
    }

    function reloadItems(page, tabIndex) {

        Dashboard.showLoadingMsg();

        var query = getQuery(tabIndex);

        ApiClient.getItems(Dashboard.getCurrentUserId(), query).then(function (result) {

            // Scroll back up so they can see the results from the beginning
            window.scrollTo(0, 0);

            var html = '';
            var pagingHtml = LibraryBrowser.getQueryPagingHtml({
                startIndex: query.StartIndex,
                limit: query.Limit,
                totalRecordCount: result.TotalRecordCount,
                viewButton: false,
                showLimit: false
            });

            page.querySelector('.listTopPaging').innerHTML = pagingHtml;

            if (view == "Poster") {
                // Poster
                html = LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    shape: "square",
                    context: getParameterByName('context') || 'photos',
                    overlayText: true,
                    lazy: true,
                    coverImage: true,
                    showTitle: tabIndex == 0,
                    centerText: true
                });
            }

            var elem = page.querySelector('.itemsContainer');
            elem.innerHTML = html + pagingHtml;
            ImageLoader.lazyChildren(elem);

            $('.btnNextPage', page).on('click', function () {
                query.StartIndex += query.Limit;
                reloadItems(page, tabIndex);
            });

            $('.btnPreviousPage', page).on('click', function () {
                query.StartIndex -= query.Limit;
                reloadItems(page, tabIndex);
            });

            LibraryBrowser.saveQueryValues(getSavedQueryKey(tabIndex), query);

            Dashboard.hideLoadingMsg();
        });
    }

    function setQueryPerTab(tab, query) {

        if (tab == 1) {
            query.Recursive = true;
            query.MediaTypes = 'Photo';
        }
        else if (tab == 2) {
            query.Recursive = true;
            query.MediaTypes = 'Video';
        }
        else if (tab == 0) {
            query.Recursive = false;
            query.MediaTypes = null;
        }

        query.ParentId = getParameterByName('parentId') || LibraryMenu.getTopParentId();
    }

    function startSlideshow(page, itemQuery, startItemId) {

        var userId = Dashboard.getCurrentUserId();

        var localQuery = $.extend({}, itemQuery);
        localQuery.StartIndex = 0;
        localQuery.Limit = null;
        localQuery.MediaTypes = "Photo";
        localQuery.Recursive = true;
        localQuery.Filters = "IsNotFolder";

        ApiClient.getItems(userId, localQuery).then(function (result) {

            showSlideshow(page, result.Items, startItemId);
        });
    }

    function showSlideshow(page, items, startItemId) {

        var index = items.map(function (i) {
            return i.Id;

        }).indexOf(startItemId);

        if (index == -1) {
            index = 0;
        }

        require(['slideshow'], function (slideshow) {

            var newSlideShow = new slideshow({
                showTitle: false,
                cover: false,
                items: items,
                startIndex: index,
                interval: 7000,
                interactive: true
            });

            newSlideShow.show();
        });
    }

    function onListItemClick(e) {

        var page = $(this).parents('.page')[0];
        var info = LibraryBrowser.getListItemInfo(this);

        if (info.mediaType == 'Photo') {
            var query = getQuery(LibraryBrowser.selectedTab(page.querySelector('.pageTabsContainer')));

            Photos.startSlideshow(page, query, info.id);
            return false;
        }
    }

    function loadTab(page, index) {

        switch (index) {

            case 0:
                {
                    reloadItems(page.querySelector('.albumTabContent'), 0);
                }
                break;
            case 1:
                {
                    reloadItems(page.querySelector('.photoTabContent'), 1);
                }
                break;
            case 2:
                {
                    reloadItems(page.querySelector('.videoTabContent'), 2);
                }
                break;
            default:
                break;
        }
    }

    pageIdOn('pageinit', "photosPage", function () {

        var page = this;

        var tabs = page.querySelector('paper-tabs');

        var baseUrl = 'photos.html';
        var topParentId = LibraryMenu.getTopParentId();
        if (topParentId) {
            baseUrl += '?topParentId=' + topParentId;
        }

        LibraryBrowser.configurePaperLibraryTabs(page, tabs, page.querySelector('.pageTabsContainer'), baseUrl);

        page.querySelector('.pageTabsContainer').addEventListener('tabchange', function (e) {
            loadTab(page, parseInt(e.detail.selectedTabIndex));
        });

        $(page).on('click', '.mediaItem', onListItemClick);

    });

    window.Photos = {
        startSlideshow: startSlideshow
    };

});