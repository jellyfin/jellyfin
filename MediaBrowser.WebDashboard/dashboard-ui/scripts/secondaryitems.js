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

            pageData.query.ParentId = LibraryMenu.getTopParentId();
            LibraryBrowser.loadSavedQueryValues(key, pageData.query);
        }
        return pageData.query;
    }

    function getSavedQueryKey() {

        return getWindowUrl();
    }

    function reloadItems(page) {

        Dashboard.showLoadingMsg();

        var query = getQuery();

        ApiClient.getItems(Dashboard.getCurrentUserId(), query).done(function (result) {

            // Scroll back up so they can see the results from the beginning
            window.scrollTo(0, 0);

            var html = '';
            var trigger = false;
            var pagingHtml = LibraryBrowser.getQueryPagingHtml({
                startIndex: query.StartIndex,
                limit: query.Limit,
                totalRecordCount: result.TotalRecordCount,
                showLimit: false
            });

            page.querySelector('.listTopPaging').innerHTML = pagingHtml;

            if (view == "Thumb") {

                html = LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    shape: "backdrop",
                    preferThumb: true,
                    lazy: true,
                    overlayText: true
                });

            }
            else if (view == "ThumbCard") {

                html = LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    shape: "backdrop",
                    preferThumb: true,
                    lazy: true,
                    cardLayout: true,
                    showTitle: true,
                    showSeriesYear: true
                });
            }
            else if (view == "Banner") {

                html = LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    shape: "banner",
                    preferBanner: true,
                    lazy: true
                });
            }
            else if (view == "List") {

                html = LibraryBrowser.getListViewHtml({
                    items: result.Items,
                    sortBy: query.SortBy
                });
                trigger = true;
            }
            else {

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
            }

            var elem = page.querySelector('#items');
            elem.innerHTML = html + pagingHtml;
            ImageLoader.lazyChildren(elem);

            if (trigger) {
                Events.trigger(elem, 'create');
            }

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

    $(document).on('pagebeforeshowready', "#secondaryItemsPage", function () {

        var page = this;

        if (LibraryBrowser.needsRefresh(page)) {
            reloadItems(page);
        }
    });

})(jQuery, document);