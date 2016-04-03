define(['jQuery'], function ($) {

    var data = {};
    function getPageData() {
        var key = getSavedQueryKey();
        var pageData = data[key];

        if (!pageData) {
            pageData = data[key] = {
                query: {
                    SortBy: "SortName",
                    SortOrder: "Ascending",
                    IncludeItemTypes: "BoxSet",
                    Recursive: true,
                    Fields: "PrimaryImageAspectRatio,SortName,SyncInfo,CanDelete",
                    ImageTypeLimit: 1,
                    EnableImageTypes: "Primary,Backdrop,Banner,Thumb",
                    StartIndex: 0,
                    Limit: LibraryBrowser.getDefaultPageSize()
                },
                view: LibraryBrowser.getSavedView(key) || LibraryBrowser.getDefaultItemsView('Poster', 'Poster')
            };

            LibraryBrowser.loadSavedQueryValues(key, pageData.query);
        }
        return pageData;
    }

    function getQuery() {

        return getPageData().query;
    }

    function getSavedQueryKey() {

        return LibraryBrowser.getSavedQueryKey('collections');
    }

    function reloadItems(page) {

        Dashboard.showLoadingMsg();

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

            $('.listTopPaging', page).html(LibraryBrowser.getQueryPagingHtml({
                startIndex: query.StartIndex,
                limit: query.Limit,
                totalRecordCount: result.TotalRecordCount,
                viewButton: false,
                sortButton: true,
                showLimit: false,
                updatePageSizeSetting: false,
                addLayoutButton: true,
                currentLayout: view

            }));

            if (result.TotalRecordCount) {

                if (view == "List") {

                    html = LibraryBrowser.getListViewHtml({
                        items: result.Items,
                        sortBy: query.SortBy
                    });
                }
                else if (view == "Poster") {
                    html = LibraryBrowser.getPosterViewHtml({
                        items: result.Items,
                        shape: "auto",
                        showTitle: true,
                        centerText: true,
                        lazy: true,
                        overlayPlayButton: true
                    });
                }
                else if (view == "PosterCard") {
                    html = LibraryBrowser.getPosterViewHtml({
                        items: result.Items,
                        shape: "auto",
                        showTitle: true,
                        cardLayout: true,
                        lazy: true,
                        showItemCounts: true
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

                $('.noItemsMessage', page).hide();

            } else {

                $('.noItemsMessage', page).show();
            }

            var elem = page.querySelector('.itemsContainer');
            elem.innerHTML = html;
            ImageLoader.lazyChildren(elem);

            $('.btnNextPage', page).on('click', function () {
                query.StartIndex += query.Limit;
                reloadItems(page);
            });

            $('.btnPreviousPage', page).on('click', function () {
                query.StartIndex -= query.Limit;
                reloadItems(page);
            });

            $('.btnChangeLayout', page).on('layoutchange', function (e, layout) {
                getPageData().view = layout;
                LibraryBrowser.saveViewSetting(getSavedQueryKey(), layout);
                reloadItems(page);
            });

            // On callback make sure to set StartIndex = 0
            $('.btnSort', page).on('click', function () {
                LibraryBrowser.showSortMenu({
                    items: [{
                        name: Globalize.translate('OptionNameSort'),
                        id: 'SortName'
                    },
                    {
                        name: Globalize.translate('OptionImdbRating'),
                        id: 'CommunityRating,SortName'
                    },
                    {
                        name: Globalize.translate('OptionDateAdded'),
                        id: 'DateCreated,SortName'
                    },
                    {
                        name: Globalize.translate('OptionParentalRating'),
                        id: 'OfficialRating,SortName'
                    },
                    {
                        name: Globalize.translate('OptionReleaseDate'),
                        id: 'PremiereDate,SortName'
                    }],
                    callback: function () {
                        reloadItems(page);
                    },
                    query: query
                });
            });

            LibraryBrowser.saveQueryValues(getSavedQueryKey(), query);

            Dashboard.hideLoadingMsg();
        });
    }

    function initPage(tabContent) {

        // The button is created dynamically
        $('.btnNewCollection', tabContent).on('click', function () {

            require(['collectioneditor'], function (collectioneditor) {

                new collectioneditor().show();

            });
        });
    }

    pageIdOn('pageinit', 'boxsetsPage', function () {

        var page = this;

        var content = page;

        initPage(content);

    });
    pageIdOn('pagebeforeshow', 'boxsetsPage', function () {

        var page = this;

        var content = page;

        reloadItems(content);

    });

    return function (view, params, tabContent) {

        var self = this;

        self.initTab = function () {

            initPage(tabContent);
        };

        self.renderTab = function () {

            reloadItems(tabContent);
        };
    };

});