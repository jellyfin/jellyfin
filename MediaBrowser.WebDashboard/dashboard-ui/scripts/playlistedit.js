define(["listView"], function(listView) {
    "use strict";

    function getFetchPlaylistItemsFn(itemId) {
        return function() {
            var query = {
                Fields: "PrimaryImageAspectRatio,UserData",
                EnableImageTypes: "Primary,Backdrop,Banner,Thumb",
                UserId: ApiClient.getCurrentUserId()
            };
            return ApiClient.getJSON(ApiClient.getUrl("Playlists/" + itemId + "/Items", query))
        }
    }

    function getItemsHtmlFn(itemId) {
        return function(items) {
            return listView.getListViewHtml({
                items: items,
                showIndex: !1,
                showRemoveFromPlaylist: !0,
                playFromHere: !0,
                action: "playallfromhere",
                smallIcon: !0,
                dragHandle: !0,
                playlistId: itemId
            })
        }
    }

    function init(page, item) {
        var elem = page.querySelector("#childrenContent .itemsContainer");
        elem.classList.add("vertical-list"), elem.classList.remove("vertical-wrap"), elem.enableDragReordering(!0), elem.fetchData = getFetchPlaylistItemsFn(item.Id), elem.getItemsHtml = getItemsHtmlFn(item.Id)
    }
    window.PlaylistViewer = {
        render: function(page, item) {
            page.playlistInit || (page.playlistInit = !0, init(page, item)), page.querySelector("#childrenContent").classList.add("verticalSection-extrabottompadding"), page.querySelector("#childrenContent .itemsContainer").refreshItems()
        }
    }
});