define(['events', 'libraryBrowser', 'imageLoader', 'listView', 'emby-itemscontainer'], function (events, libraryBrowser, imageLoader, listView) {

    return function (view, params, tabContent) {

        var self = this;

        var data = {};

        function getPageData(context) {
            var key = getSavedQueryKey(context);
            var pageData = data[key];

            if (!pageData) {
                pageData = data[key] = {
                    query: {
                        SortBy: "Album,SortName",
                        SortOrder: "Ascending",
                        IncludeItemTypes: "Audio",
                        Recursive: true,
                        Fields: "AudioInfo,ParentId",
                        Limit: 100,
                        StartIndex: 0,
                        ImageTypeLimit: 1,
                        EnableImageTypes: "Primary"
                    }
                };

                pageData.query.ParentId = params.topParentId;
                libraryBrowser.loadSavedQueryValues(key, pageData.query);
            }
            return pageData;
        }

        function getQuery(context) {

            return getPageData(context).query;
        }

        function getSavedQueryKey(context) {

            if (!context.savedQueryKey) {
                context.savedQueryKey = libraryBrowser.getSavedQueryKey('songs');
            }
            return context.savedQueryKey;
        }

        function reloadItems(page) {

            Dashboard.showLoadingMsg();

            var query = getQuery(page);

            ApiClient.getItems(Dashboard.getCurrentUserId(), query).then(function (result) {

                // Scroll back up so they can see the results from the beginning
                window.scrollTo(0, 0);

                updateFilterControls(page);

                var pagingHtml = LibraryBrowser.getQueryPagingHtml({
                    startIndex: query.StartIndex,
                    limit: query.Limit,
                    totalRecordCount: result.TotalRecordCount,
                    showLimit: false,
                    updatePageSizeSetting: false,
                    addLayoutButton: false,
                    sortButton: false,
                    filterButton: false
                });

                var html = listView.getListViewHtml({
                    items: result.Items,
                    action: 'playallfromhere',
                    smallIcon: true
                });

                var i, length;
                var elems = tabContent.querySelectorAll('.paging');
                for (i = 0, length = elems.length; i < length; i++) {
                    elems[i].innerHTML = pagingHtml;
                }

                function onNextPageClick() {
                    query.StartIndex += query.Limit;
                    reloadItems(tabContent);
                }

                function onPreviousPageClick() {
                    query.StartIndex -= query.Limit;
                    reloadItems(tabContent);
                }

                elems = tabContent.querySelectorAll('.btnNextPage');
                for (i = 0, length = elems.length; i < length; i++) {
                    elems[i].addEventListener('click', onNextPageClick);
                }

                elems = tabContent.querySelectorAll('.btnPreviousPage');
                for (i = 0, length = elems.length; i < length; i++) {
                    elems[i].addEventListener('click', onPreviousPageClick);
                }

                var itemsContainer = tabContent.querySelector('.itemsContainer');
                itemsContainer.innerHTML = html;
                imageLoader.lazyChildren(itemsContainer);

                libraryBrowser.saveQueryValues(getSavedQueryKey(page), query);

                Dashboard.hideLoadingMsg();
            });
        }

        self.showFilterMenu = function () {

            require(['components/filterdialog/filterdialog'], function (filterDialogFactory) {

                var filterDialog = new filterDialogFactory({
                    query: getQuery(tabContent),
                    mode: 'songs'
                });

                Events.on(filterDialog, 'filterchange', function () {
                    getQuery(tabContent).StartIndex = 0;
                    reloadItems(tabContent);
                });

                filterDialog.show();
            });
        }

        function updateFilterControls(tabContent) {

        }

        function initPage(tabContent) {

            tabContent.querySelector('.btnFilter').addEventListener('click', function () {
                self.showFilterMenu();
            });

            tabContent.querySelector('.btnSort').addEventListener('click', function (e) {
                libraryBrowser.showSortMenu({
                    items: [{
                        name: Globalize.translate('OptionTrackName'),
                        id: 'Name'
                    },
                        {
                            name: Globalize.translate('OptionAlbum'),
                            id: 'Album,SortName'
                        },
                        {
                            name: Globalize.translate('OptionAlbumArtist'),
                            id: 'AlbumArtist,Album,SortName'
                        },
                        {
                            name: Globalize.translate('OptionArtist'),
                            id: 'Artist,Album,SortName'
                        },
                        {
                            name: Globalize.translate('OptionDateAdded'),
                            id: 'DateCreated,SortName'
                        },
                        {
                            name: Globalize.translate('OptionDatePlayed'),
                            id: 'DatePlayed,SortName'
                        },
                        {
                            name: Globalize.translate('OptionPlayCount'),
                            id: 'PlayCount,SortName'
                        },
                        {
                            name: Globalize.translate('OptionReleaseDate'),
                            id: 'PremiereDate,AlbumArtist,Album,SortName'
                        },
                        {
                            name: Globalize.translate('OptionRuntime'),
                            id: 'Runtime,AlbumArtist,Album,SortName'
                        }],
                    callback: function () {
                        getQuery(tabContent).StartIndex = 0;
                        reloadItems(tabContent);
                    },
                    query: getQuery(tabContent),
                    button: e.target
                });
            });
        }

        self.getCurrentViewStyle = function () {
            return getPageData(tabContent).view;
        };

        initPage(tabContent);

        self.renderTab = function () {

            reloadItems(tabContent);
            updateFilterControls(tabContent);
        };

        self.destroy = function () {
        };
    };
});