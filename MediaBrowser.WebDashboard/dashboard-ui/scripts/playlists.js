define(['listView', 'cardBuilder', 'libraryBrowser', 'emby-itemscontainer'], function (listView, cardBuilder, libraryBrowser) {

    return function (view, params) {

        var data = {};
        function getPageData(context) {
            var key = getSavedQueryKey(context);
            var pageData = data[key];

            if (!pageData) {
                pageData = data[key] = {
                    query: {
                        SortBy: "SortName",
                        SortOrder: "Ascending",
                        IncludeItemTypes: "Playlist",
                        Recursive: true,
                        Fields: "PrimaryImageAspectRatio,SortName,CumulativeRunTimeTicks,CanDelete",
                        StartIndex: 0,
                        Limit: LibraryBrowser.getDefaultPageSize()
                    },
                    view: LibraryBrowser.getSavedView(key) || 'PosterCard'
                };

                pageData.query.ParentId = LibraryMenu.getTopParentId();
                LibraryBrowser.loadSavedQueryValues(key, pageData.query);
            }
            return pageData;
        }

        function getQuery(context) {

            return getPageData(context).query;
        }

        function getSavedQueryKey(context) {

            if (!context.savedQueryKey) {
                context.savedQueryKey = libraryBrowser.getSavedQueryKey();
            }
            return context.savedQueryKey;
        }

        function showLoadingMessage() {

            Dashboard.showLoadingMsg();
        }

        function hideLoadingMessage() {
            Dashboard.hideLoadingMsg();
        }

        function onViewStyleChange() {

            var viewStyle = getPageData(view).view;

            var itemsContainer = view.querySelector('.itemsContainer');

            if (viewStyle == "List") {

                itemsContainer.classList.add('vertical-list');
                itemsContainer.classList.remove('vertical-wrap');
            }
            else {

                itemsContainer.classList.remove('vertical-list');
                itemsContainer.classList.add('vertical-wrap');
            }
            itemsContainer.innerHTML = '';
        }

        function reloadItems() {

            showLoadingMessage();

            var query = getQuery(view);
            var promise1 = ApiClient.getItems(Dashboard.getCurrentUserId(), query);
            var promise2 = Dashboard.getCurrentUser();

            Promise.all([promise1, promise2]).then(function (responses) {

                var result = responses[0];
                var user = responses[1];

                // Scroll back up so they can see the results from the beginning
                window.scrollTo(0, 0);

                var html = '';
                var viewStyle = getPageData(view).view;

                view.querySelector('.listTopPaging').innerHTML = LibraryBrowser.getQueryPagingHtml({
                    startIndex: query.StartIndex,
                    limit: query.Limit,
                    totalRecordCount: result.TotalRecordCount,
                    viewButton: false,
                    showLimit: false,
                    updatePageSizeSetting: false,
                    addLayoutButton: true,
                    layouts: 'List,Poster,PosterCard,Thumb,ThumbCard',
                    currentLayout: viewStyle

                });

                if (result.TotalRecordCount) {

                    if (viewStyle == "List") {

                        html = listView.getListViewHtml({
                            items: result.Items,
                            sortBy: query.SortBy
                        });
                    }
                    else if (viewStyle == "PosterCard") {
                        html = cardBuilder.getCardsHtml({
                            items: result.Items,
                            shape: "square",
                            showTitle: true,
                            lazy: true,
                            coverImage: true,
                            showItemCounts: true,
                            cardLayout: true
                        });
                    }
                    else if (viewStyle == "Thumb") {
                        html = cardBuilder.getCardsHtml({
                            items: result.Items,
                            shape: "backdrop",
                            showTitle: true,
                            centerText: true,
                            lazy: true,
                            preferThumb: true,
                            overlayPlayButton: true
                        });
                    }
                    else if (viewStyle == "ThumbCard") {
                        html = cardBuilder.getCardsHtml({
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
                        html = cardBuilder.getCardsHtml({
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

                    view.querySelector('.noItemsMessage').classList.add('hide');

                } else {

                    view.querySelector('.noItemsMessage').classList.remove('hide');
                }

                var elem = view.querySelector('.itemsContainer');
                elem.innerHTML = html;
                ImageLoader.lazyChildren(elem);

                var btnNextPage = view.querySelector('.btnNextPage');
                if (btnNextPage) {
                    btnNextPage.addEventListener('click', function () {
                        query.StartIndex += query.Limit;
                        reloadItems();
                    });
                }

                var btnPreviousPage = view.querySelector('.btnPreviousPage');
                if (btnPreviousPage) {
                    btnPreviousPage.addEventListener('click', function () {
                        query.StartIndex -= query.Limit;
                        reloadItems();
                    });
                }

                var btnChangeLayout = view.querySelector('.btnChangeLayout');
                if (btnChangeLayout) {
                    btnChangeLayout.addEventListener('layoutchange', function (e) {
                        var layout = e.detail.viewStyle;
                        getPageData(view).view = layout;
                        LibraryBrowser.saveViewSetting(getSavedQueryKey(view), layout);
                        onViewStyleChange();
                        reloadItems();
                    });
                }

                LibraryBrowser.saveQueryValues(getSavedQueryKey(view), query);

                hideLoadingMessage();
            });
        }

        view.addEventListener('viewbeforeshow', function() {
            reloadItems();
        });

        onViewStyleChange();

    };
});