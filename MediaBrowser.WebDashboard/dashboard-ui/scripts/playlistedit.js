define(['jQuery', 'listView'], function ($, listView) {

    var data = {};
    function getPageData() {
        var key = getSavedQueryKey();
        var pageData = data[key];

        if (!pageData) {
            pageData = data[key] = {
                query: {
                    Fields: "PrimaryImageAspectRatio",
                    EnableImageTypes: "Primary,Backdrop,Banner,Thumb",
                    StartIndex: 0,
                    Limit: 200
                },
                view: LibraryBrowser.getSavedView(key) || 'List'
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

    function reloadItems(page, item) {

        Dashboard.showLoadingMsg();

        var query = getQuery();

        query.UserId = Dashboard.getCurrentUserId();

        ApiClient.getJSON(ApiClient.getUrl('Playlists/' + item.Id + '/Items', query)).then(function (result) {

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

            html += listView.getListViewHtml({
                items: result.Items,
                sortBy: query.SortBy,
                showIndex: false,
                showRemoveFromPlaylist: true,
                playFromHere: true,
                action: 'playallfromhere',
                smallIcon: true,
                dragHandle: true,
                playlistId: item.Id
            });

            var elem = page.querySelector('#childrenContent .itemsContainer');
            elem.classList.add('vertical-list');
            elem.classList.remove('vertical-wrap');
            elem.innerHTML = html;

            ImageLoader.lazyChildren(elem);

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

    function init(page, item) {

        var elem = page.querySelector('#childrenContent .itemsContainer');

        elem.enableDragReordering(true);

        elem.addEventListener('needsrefresh', function () {

            reloadItems(page, item);
        });
    }

    window.PlaylistViewer = {
        render: function (page, item) {

            if (!page.playlistInit) {
                page.playlistInit = true;
                init(page, item);
            }

            reloadItems(page, item);
        }
    };

});