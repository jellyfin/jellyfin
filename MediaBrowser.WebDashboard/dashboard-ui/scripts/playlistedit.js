(function ($, document) {

    var data = {};
    function getPageData() {
        var key = getSavedQueryKey();
        var pageData = data[key];

        if (!pageData) {
            pageData = data[key] = {
                query: {
                    Fields: "PrimaryImageAspectRatio,SyncInfo",
                    EnableImageTypes: "Primary,Backdrop,Banner,Thumb",
                    StartIndex: 0,
                    Limit: LibraryBrowser.getDefaultPageSize()
                },
                view: LibraryBrowser.getSavedView(key) || LibraryBrowser.getDefaultItemsView('List', 'List')
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

        return getWindowUrl();
    }


    function reloadItems(page, item) {

        Dashboard.showLoadingMsg();

        var query = getQuery();

        query.UserId = Dashboard.getCurrentUserId();

        ApiClient.getJSON(ApiClient.getUrl('Playlists/' + item.Id + '/Items', query)).done(function (result) {

            // Scroll back up so they can see the results from the beginning
            window.scrollTo(0, 0);

            var html = '';

            html += LibraryBrowser.getQueryPagingHtml({
                startIndex: query.StartIndex,
                limit: query.Limit,
                totalRecordCount: result.TotalRecordCount,
                showLimit: false,
                updatePageSizeSetting: false

            });

            var view = getPageData().view;

            if (view == "List") {

                html = LibraryBrowser.getListViewHtml({
                    items: result.Items,
                    sortBy: query.SortBy,
                    showIndex: false,
                    showRemoveFromPlaylist: true,
                    playFromHere: true,
                    defaultAction: 'playallfromhere',
                    smallIcon: true

                });
            }

            var elem = page.querySelector('#childrenContent .itemsContainer');
            elem.innerHTML = html;
            $(elem).trigger('create');
            ImageLoader.lazyChildren(elem);

            $(elem).off('needsrefresh').on('needsrefresh', function () {

                reloadItems(page, item);

            }).off('removefromplaylist').on('removefromplaylist', function (e, playlistItemId) {

                removeFromPlaylist(page, item, [playlistItemId]);

            });


            $('.btnNextPage', elem).on('click', function () {
                query.StartIndex += query.Limit;
                reloadItems(page, item);
            });

            $('.btnPreviousPage', elem).on('click', function () {
                query.StartIndex -= query.Limit;
                reloadItems(page, item);
            });

            Dashboard.hideLoadingMsg();
        });
    }

    function removeFromPlaylist(page, item, ids) {

        ApiClient.ajax({

            url: ApiClient.getUrl('Playlists/' + item.Id + '/Items', {
                EntryIds: ids.join(',')
            }),

            type: 'DELETE'

        }).done(function () {

            reloadItems(page, item);
        });

    }

    window.PlaylistViewer = {
        render: function (page, item) {
            reloadItems(page, item);
        }
    };

})(jQuery, document);