define(['jQuery'], function ($) {


    return function (view, params, tabContent) {

        var self = this;

        var defaultSortBy = "Album,SortName";

        var data = {};
        function getPageData(context) {
            var key = getSavedQueryKey(context);
            var pageData = data[key];

            if (!pageData) {
                pageData = data[key] = {
                    query: {
                        SortBy: defaultSortBy,
                        SortOrder: "Ascending",
                        IncludeItemTypes: "Audio",
                        Recursive: true,
                        Fields: "AudioInfo,ParentId,SyncInfo",
                        Limit: 100,
                        StartIndex: 0,
                        ImageTypeLimit: 1,
                        EnableImageTypes: "Primary,Backdrop,Banner,Thumb"
                    }
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
                context.savedQueryKey = LibraryBrowser.getSavedQueryKey('songs');
            }
            return context.savedQueryKey;
        }

        function reloadItems(context) {

            Dashboard.showLoadingMsg();

            var query = getQuery(context);
            ApiClient.getItems(Dashboard.getCurrentUserId(), query).then(function (result) {

                // Scroll back up so they can see the results from the beginning
                window.scrollTo(0, 0);

                var html = '';
                var pagingHtml = LibraryBrowser.getQueryPagingHtml({
                    startIndex: query.StartIndex,
                    limit: query.Limit,
                    totalRecordCount: result.TotalRecordCount,
                    showLimit: false,
                    sortButton: true,
                    updatePageSizeSetting: false,
                    filterButton: true
                });

                context.querySelector('.listTopPaging').innerHTML = pagingHtml;

                html += LibraryBrowser.getListViewHtml({
                    items: result.Items,
                    showIndex: true,
                    defaultAction: 'play',
                    smallIcon: true
                });

                var elem = context.querySelector('#items');
                elem.innerHTML = html + pagingHtml;
                ImageLoader.lazyChildren(elem);

                $('.btnNextPage', context).on('click', function () {
                    query.StartIndex += query.Limit;
                    reloadItems(context);
                });

                $('.btnPreviousPage', context).on('click', function () {
                    query.StartIndex -= query.Limit;
                    reloadItems(context);
                });

                $('.btnFilter', context).on('click', function () {
                    showFilterMenu(context);
                });

                // On callback make sure to set StartIndex = 0
                $('.btnSort', context).on('click', function () {
                    LibraryBrowser.showSortMenu({
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
                            reloadItems(context);
                        },
                        query: query
                    });
                });

                LibraryBrowser.saveQueryValues(getSavedQueryKey(context), query);

                Dashboard.hideLoadingMsg();
            });
        }

        function showFilterMenu(context) {

            require(['components/filterdialog/filterdialog'], function (filterDialogFactory) {

                var filterDialog = new filterDialogFactory({
                    query: getQuery(context),
                    mode: 'songs'
                });

                Events.on(filterDialog, 'filterchange', function () {
                    reloadItems(context);
                });

                filterDialog.show();
            });
        }
        self.renderTab = function () {

            reloadItems(tabContent);
        };
    };

});