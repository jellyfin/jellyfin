define(['appStorage', 'jQuery'], function (appStorage, $) {

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
                    Limit: 200
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

            var listItems = [];
            var elems = elem.querySelectorAll('.listItem');
            for (var i = 0, length = elems.length; i < length; i++) {
                listItems.push(elems[i]);
            }

            var listParent = elem.querySelector('.paperList');

            if (!AppInfo.isTouchPreferred) {
                require(['sortable'], function (Sortable) {

                    var sortable = new Sortable(listParent, {

                        draggable: ".listItem",

                        // dragging ended
                        onEnd: function (/**Event*/evt) {

                            onDrop(evt, page, item);
                        }
                    });
                });
            }

            ImageLoader.lazyChildren(elem);
            LibraryBrowser.createCardMenus(elem);

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

    function onDrop(evt, page, item) {

        var el = evt.item;

        var newIndex = evt.newIndex;

        var itemId = el.getAttribute('data-playlistitemid');

        Dashboard.showLoadingMsg();

        ApiClient.ajax({

            url: ApiClient.getUrl('Playlists/' + item.Id + '/Items/' + itemId + '/Move/' + newIndex),

            type: 'POST'

        }).then(function () {

            Dashboard.hideLoadingMsg();

        }, function () {

            Dashboard.hideLoadingMsg();
            reloadItems(page, item);
        });
    }

    function removeFromPlaylist(page, item, ids) {

        ApiClient.ajax({

            url: ApiClient.getUrl('Playlists/' + item.Id + '/Items', {
                EntryIds: ids.join(',')
            }),

            type: 'DELETE'

        }).then(function () {

            reloadItems(page, item);
        });

    }

    function showDragAndDropHelp() {

        if (AppInfo.isTouchPreferred) {
            // Not implemented for mobile yet
            return;
        }

        var expectedValue = "7";
        if (appStorage.getItem("playlistitemdragdrophelp") == expectedValue) {
            return;
        }

        appStorage.setItem("playlistitemdragdrophelp", expectedValue);

        Dashboard.alert({
            message: Globalize.translate('TryDragAndDropMessage'),
            title: Globalize.translate('HeaderTryDragAndDrop')
        });
    }

    function init(page, item) {

        var elem = page.querySelector('#childrenContent .itemsContainer');

        elem.addEventListener('removefromplaylist', function (e) {

            var playlistItemId = e.detail.playlistItemId;
            removeFromPlaylist(page, item, [playlistItemId]);
        });

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
            showDragAndDropHelp();
        }
    };

});