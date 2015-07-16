(function ($, document) {

    var view = LibraryBrowser.getDefaultItemsView('Poster', 'Poster');

    var data = {};

    function getQuery() {

        var key = getSavedQueryKey();
        var pageData = data[key];

        if (!pageData) {
            pageData = data[key] = {
                query: {
                    SortBy: "SortName",
                    SortOrder: "Ascending",
                    Recursive: true,
                    Fields: "PrimaryImageAspectRatio,SortName,SyncInfo",
                    ImageTypeLimit: 1,
                    EnableImageTypes: "Primary,Backdrop,Banner,Thumb",
                    StartIndex: 0,
                    Limit: LibraryBrowser.getDefaultPageSize()
                }
            };

            var type = getParameterByName('type');
            if (type) {
                pageData.query.IncludeItemTypes = type;
            }

            var filters = getParameterByName('filters');
            if (type) {
                pageData.query.Filters = filters;
            }

            pageData.query.ParentId = getParameterByName('parentid') || LibraryMenu.getTopParentId();
            LibraryBrowser.loadSavedQueryValues(key, pageData.query);
        }
        return pageData.query;
    }

    function getSavedQueryKey() {

        return getWindowUrl();
    }

    function onListItemClick(e) {

        var page = $(this).parents('.page')[0];
        var info = LibraryBrowser.getListItemInfo(this);

        if (info.mediaType == 'Photo') {
            var query = getQuery();

            require(['scripts/photos'], function () {
                Photos.startSlideshow(page, query, info.id);
            });
            return false;
        }
    }

    function reloadItems(page) {

        Dashboard.showLoadingMsg();

        var query = getQuery();

        ApiClient.getItems(Dashboard.getCurrentUserId(), query).done(function (result) {

            // Scroll back up so they can see the results from the beginning
            window.scrollTo(0, 0);

            var html = '';
            var pagingHtml = LibraryBrowser.getQueryPagingHtml({
                startIndex: query.StartIndex,
                limit: query.Limit,
                totalRecordCount: result.TotalRecordCount,
                showLimit: false
            });

            page.querySelector('.listTopPaging').innerHTML = pagingHtml;

            var posterOptions = {
                items: result.Items,
                shape: "auto",
                centerText: true,
                lazy: true,
                overlayText: true
            };

            if (query.IncludeItemTypes == "MusicAlbum") {
                posterOptions.overlayText = false;
                posterOptions.showParentTitle = true;
                posterOptions.overlayPlayButton = true;
            }
            else if (query.IncludeItemTypes == "MusicArtist") {
                posterOptions.overlayText = false;
                posterOptions.overlayPlayButton = true;
            }
            else if (query.IncludeItemTypes == "Episode") {
                posterOptions.overlayText = false;
                posterOptions.showParentTitle = true;
                posterOptions.overlayPlayButton = true;
                posterOptions.centerText = false;
            }

            // Poster
            html = LibraryBrowser.getPosterViewHtml(posterOptions);

            var elem = page.querySelector('#items');
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

            LibraryBrowser.setLastRefreshed(page);
            Dashboard.hideLoadingMsg();
        });
    }

    $(document).on('pageinitdepends', "#secondaryItemsPage", function () {

        var page = this;

        $(page).on('click', '.mediaItem', onListItemClick);

    }).on('pagebeforeshowready', "#secondaryItemsPage", function () {

        var page = this;

        if (getParameterByName('parentid')) {
            ApiClient.getItem(Dashboard.getCurrentUserId(), getParameterByName('parentid')).done(function (parent) {
                LibraryMenu.setTitle(parent.Name);
            });
        }

        if (LibraryBrowser.needsRefresh(page)) {
            reloadItems(page);
        }
    });

})(jQuery, document);