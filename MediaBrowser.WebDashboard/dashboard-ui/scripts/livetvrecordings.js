define(["layoutManager", "loading", "components/categorysyncbuttons", "cardBuilder", "apphost", "imageLoader", "scripts/livetvcomponents", "listViewStyle", "emby-itemscontainer"], function(layoutManager, loading, categorysyncbuttons, cardBuilder, appHost, imageLoader) {
    "use strict";

    function renderRecordings(elem, recordings, cardOptions, scrollX) {
        recordings.length ? elem.classList.remove("hide") : elem.classList.add("hide");
        var recordingItems = elem.querySelector(".recordingItems");
        scrollX ? (recordingItems.classList.add("scrollX"), recordingItems.classList.add("hiddenScrollX"), recordingItems.classList.remove("vertical-wrap")) : (recordingItems.classList.remove("scrollX"), recordingItems.classList.remove("hiddenScrollX"), recordingItems.classList.add("vertical-wrap"));
        appHost.supports("imageanalysis");
        recordingItems.innerHTML = cardBuilder.getCardsHtml(Object.assign({
            items: recordings,
            shape: scrollX ? "autooverflow" : "auto",
            defaultShape: scrollX ? "overflowBackdrop" : "backdrop",
            showTitle: !0,
            showParentTitle: !0,
            coverImage: !0,
            cardLayout: !1,
            centerText: !0,
            vibrant: !1,
            allowBottomPadding: !scrollX,
            preferThumb: "auto",
            overlayText: !1
        }, cardOptions || {})), imageLoader.lazyChildren(recordingItems)
    }

    function renderLatestRecordings(context, promise) {
        promise.then(function(result) {
            renderRecordings(context.querySelector("#latestRecordings"), result.Items, {
                showYear: !0,
                lines: 2
            }, !1), loading.hide()
        })
    }

    function renderRecordingFolders(context, promise) {
        promise.then(function(result) {
            renderRecordings(context.querySelector("#recordingFolders"), result.Items, {
                showYear: !1,
                showParentTitle: !1
            }, !1)
        })
    }

    function onMoreClick(e) {
        var type = this.getAttribute("data-type"),
            serverId = ApiClient.serverId();
        switch (type) {
            case "latest":
                Dashboard.navigate("list/list.html?type=Recordings&serverId=" + serverId)
        }
    }
    return function(view, params, tabContent) {
        function enableFullRender() {
            return (new Date).getTime() - lastFullRender > 3e5
        }
        var foldersPromise, latestPromise, self = this,
            lastFullRender = 0;
        categorysyncbuttons.init(tabContent);
        for (var moreButtons = tabContent.querySelectorAll(".more"), i = 0, length = moreButtons.length; i < length; i++) moreButtons[i].addEventListener("click", onMoreClick);
        self.preRender = function() {
            enableFullRender() && (latestPromise = ApiClient.getLiveTvRecordings({
                UserId: Dashboard.getCurrentUserId(),
                Limit: 12,
                Fields: "CanDelete,PrimaryImageAspectRatio,BasicSyncInfo",
                EnableTotalRecordCount: !1,
                EnableImageTypes: "Primary,Thumb,Backdrop"
            }), foldersPromise = ApiClient.getRecordingFolders(Dashboard.getCurrentUserId()))
        }, self.renderTab = function() {
            enableFullRender() && (loading.show(), renderLatestRecordings(tabContent, latestPromise), renderRecordingFolders(tabContent, foldersPromise), lastFullRender = (new Date).getTime())
        }
    }
});