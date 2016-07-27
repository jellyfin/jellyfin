define(['listView'], function (listView) {

    var data = {};
    function getPageData() {
        var key = getSavedQueryKey();
        var pageData = data[key];

        if (!pageData) {
            pageData = data[key] = {
                query: {
                    SortBy: "SortName",
                    SortOrder: "Ascending",
                    IncludeItemTypes: "Playlist",
                    Recursive: true,
                    Fields: "PrimaryImageAspectRatio,SortName,CumulativeRunTimeTicks,CanDelete,SyncInfo",
                    StartIndex: 0,
                    Limit: LibraryBrowser.getDefaultPageSize()
                },
                view: LibraryBrowser.getSavedView(key) || LibraryBrowser.getDefaultItemsView('PosterCard', 'PosterCard')
            };

            pageData.query.ParentId = LibraryMenu.getTopParentId();
            LibraryBrowser.loadSavedQueryValues(key, pageData.query);
        }
        return pageData;
    }

    function getQuery() {

        return getPageData().query;
    }

    function getSavedQueryKey() {

        return LibraryBrowser.getSavedQueryKey();
    }

    function showLoadingMessage(page) {

        Dashboard.showLoadingMsg();
    }

    function hideLoadingMessage(page) {
        Dashboard.hideLoadingMsg();
    }

    function reloadItems(page) {

        showLoadingMessage(page);

        var query = getQuery();
        var promise1 = ApiClient.getItems(Dashboard.getCurrentUserId(), query);
        var promise2 = Dashboard.getCurrentUser();

        Promise.all([promise1, promise2]).then(function (responses) {

            var result = responses[0];
            var user = responses[1];

            // Scroll back up so they can see the results from the beginning
            window.scrollTo(0, 0);

            var html = '';
            var view = getPageData().view;

            page.querySelector('.listTopPaging').innerHTML = LibraryBrowser.getQueryPagingHtml({
                startIndex: query.StartIndex,
                limit: query.Limit,
                totalRecordCount: result.TotalRecordCount,
                viewButton: false,
                showLimit: false,
                updatePageSizeSetting: false,
                addLayoutButton: true,
                layouts: 'List,Poster,PosterCard,Thumb,ThumbCard',
                currentLayout: view

            });

            if (result.TotalRecordCount) {

                if (view == "List") {

                    html = listView.getListViewHtml({
                        items: result.Items,
                        sortBy: query.SortBy
                    });
                }
                else if (view == "PosterCard") {
                    html = LibraryBrowser.getPosterViewHtml({
                        items: result.Items,
                        shape: "square",
                        showTitle: true,
                        lazy: true,
                        coverImage: true,
                        showItemCounts: true,
                        cardLayout: true
                    });
                }
                else if (view == "Thumb") {
                    html = LibraryBrowser.getPosterViewHtml({
                        items: result.Items,
                        shape: "backdrop",
                        showTitle: true,
                        centerText: true,
                        lazy: true,
                        preferThumb: true,
                        overlayPlayButton: true
                    });
                }
                else if (view == "ThumbCard") {
                    html = LibraryBrowser.getPosterViewHtml({
                        items: result.Items,
                        shape: "backdrop",
                        showTitle: true,
                        lazy: true,
                        preferThumb: true,
                        cardLayout: true,
                        showItemCounts: true
                    });
                }
                else {
                    // Poster
                    html = LibraryBrowser.getPosterViewHtml({
                        items: result.Items,
                        shape: "square",
                        showTitle: true,
                        lazy: true,
                        coverImage: true,
                        showItemCounts: true,
                        centerText: true,
                        overlayPlayButton: true
                    });
                }

                page.querySelector('.noItemsMessage').classList.add('hide');

            } else {

                page.querySelector('.noItemsMessage').classList.remove('hide');
            }

            var elem = page.querySelector('.itemsContainer');
            elem.innerHTML = html;
            ImageLoader.lazyChildren(elem);

            var btnNextPage = page.querySelector('.btnNextPage');
            if (btnNextPage) {
                btnNextPage.addEventListener('click', function () {
                    query.StartIndex += query.Limit;
                    reloadItems(page);
                });
            }

            var btnPreviousPage = page.querySelector('.btnPreviousPage');
            if (btnPreviousPage) {
                btnPreviousPage.addEventListener('click', function () {
                    query.StartIndex -= query.Limit;
                    reloadItems(page);
                });
            }

            var btnChangeLayout = page.querySelector('.btnChangeLayout');
            if (btnChangeLayout) {
                btnChangeLayout.addEventListener('layoutchange', function (e) {
                    var layout = e.detail.viewStyle;
                    getPageData().view = layout;
                    LibraryBrowser.saveViewSetting(getSavedQueryKey(), layout);
                    reloadItems(page);
                });
            }

            LibraryBrowser.saveQueryValues(getSavedQueryKey(), query);

            hideLoadingMessage(page);
        });
    }

    pageIdOn('pagebeforeshow', "playlistsPage", function () {

        var page = this;
        reloadItems(page);
    });

});