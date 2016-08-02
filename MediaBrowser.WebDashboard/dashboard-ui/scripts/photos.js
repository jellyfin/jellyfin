define(['jQuery', 'cardBuilder', 'emby-itemscontainer'], function ($, cardBuilder) {

    var view = 'Poster';

    var data = {};
    function getQuery() {

        var key = getSavedQueryKey();
        var pageData = data[key];

        if (!pageData) {
            pageData = data[key] = {
                query: {
                    SortBy: "IsFolder,SortName",
                    SortOrder: "Ascending",
                    Fields: "PrimaryImageAspectRatio,SortName",
                    ImageTypeLimit: 1,
                    EnableImageTypes: "Primary",
                    StartIndex: 0,
                    Limit: LibraryBrowser.getDefaultPageSize()
                }
            };

            pageData.query.Recursive = false;
            pageData.query.MediaTypes = null;
            pageData.query.ParentId = getParameterByName('parentId') || LibraryMenu.getTopParentId();

            LibraryBrowser.loadSavedQueryValues(key, pageData.query);
        }
        return pageData.query;
    }

    function getSavedQueryKey() {

        return LibraryBrowser.getSavedQueryKey('v1');
    }

    function reloadItems(page) {

        Dashboard.showLoadingMsg();

        var query = getQuery();
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
                html = cardBuilder.getCardsHtml({
                    items: result.Items,
                    shape: "square",
                    context: getParameterByName('context') || 'photos',
                    overlayText: true,
                    lazy: true,
                    coverImage: true,
                    showTitle: false,
                    centerText: true
                });
            }

            var elem = page.querySelector('.itemsContainer');
            elem.innerHTML = html + pagingHtml;
            ImageLoader.lazyChildren(elem);

            $('.btnNextPage', page).on('click', function () {
                query.StartIndex += query.Limit;
                reloadItems(page);
            });

            $('.btnPreviousPage', page).on('click', function () {
                query.StartIndex -= query.Limit;
                reloadItems(page);
            });

            LibraryBrowser.saveQueryValues(getSavedQueryKey(), query);

            Dashboard.hideLoadingMsg();
        });
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
            var query = getQuery();

            Photos.startSlideshow(page, query, info.id);
            return false;
        }
    }

    pageIdOn('pageinit', "photosPage", function () {

        var page = this;

        reloadItems(page, 0);

        $(page).on('click', '.mediaItem', onListItemClick);

    });

    window.Photos = {
        startSlideshow: startSlideshow
    };

});