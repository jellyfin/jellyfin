define(["layoutManager", "cardBuilder", "apphost", "imageLoader", "loading", "scripts/livetvcomponents", "emby-button", "emby-itemscontainer"], function(layoutManager, cardBuilder, appHost, imageLoader, loading) {
    "use strict";

    function enableScrollX() {
        return !layoutManager.desktop
    }

    function renderRecordings(elem, recordings, cardOptions) {
        recordings.length ? elem.classList.remove("hide") : elem.classList.add("hide");
        var recordingItems = elem.querySelector(".recordingItems");
        enableScrollX() ? (recordingItems.classList.add("scrollX"), layoutManager.tv && recordingItems.classList.add("smoothScrollX"), recordingItems.classList.add("hiddenScrollX"), recordingItems.classList.remove("vertical-wrap")) : (recordingItems.classList.remove("scrollX"), recordingItems.classList.remove("smoothScrollX"), recordingItems.classList.remove("hiddenScrollX"), recordingItems.classList.add("vertical-wrap"));
        var supportsImageAnalysis = appHost.supports("imageanalysis"),
            cardLayout = appHost.preferVisualCards || supportsImageAnalysis;
        cardLayout = !1, recordingItems.innerHTML = cardBuilder.getCardsHtml(Object.assign({
            items: recordings,
            shape: enableScrollX() ? "autooverflow" : "auto",
            showTitle: !0,
            showParentTitle: !0,
            coverImage: !0,
            cardLayout: cardLayout,
            centerText: !cardLayout,
            vibrant: cardLayout && supportsImageAnalysis,
            allowBottomPadding: !enableScrollX(),
            preferThumb: "auto"
        }, cardOptions || {})), imageLoader.lazyChildren(recordingItems)
    }

    function getBackdropShape() {
        return enableScrollX() ? "overflowBackdrop" : "backdrop"
    }

    function renderActiveRecordings(context, promise) {
        promise.then(function(result) {
            renderRecordings(context.querySelector("#activeRecordings"), result.Items, {
                shape: enableScrollX() ? "autooverflow" : "auto",
                defaultShape: getBackdropShape(),
                showParentTitle: !1,
                showParentTitleOrTitle: !0,
                showTitle: !1,
                showAirTime: !0,
                showAirEndTime: !0,
                showChannelName: !0,
                coverImage: !0,
                overlayText: !1,
                overlayMoreButton: !0
            })
        })
    }

    function renderTimers(context, timers, options) {
        LiveTvHelpers.getTimersHtml(timers, options).then(function(html) {
            var elem = context;
            html ? elem.classList.remove("hide") : elem.classList.add("hide"), elem.querySelector(".recordingItems").innerHTML = html, imageLoader.lazyChildren(elem)
        })
    }

    function renderUpcomingRecordings(context, promise) {
        promise.then(function(result) {
            renderTimers(context.querySelector("#upcomingRecordings"), result.Items), loading.hide()
        })
    }
    return function(view, params, tabContent) {
        var activeRecordingsPromise, upcomingRecordingsPromise, self = this;
        tabContent.querySelector("#upcomingRecordings .recordingItems").addEventListener("timercancelled", function() {
            self.preRender(), self.renderTab()
        }), self.preRender = function() {
            activeRecordingsPromise = ApiClient.getLiveTvRecordings({
                UserId: Dashboard.getCurrentUserId(),
                IsInProgress: !0,
                Fields: "CanDelete,PrimaryImageAspectRatio,BasicSyncInfo",
                EnableTotalRecordCount: !1,
                EnableImageTypes: "Primary,Thumb,Backdrop"
            }), upcomingRecordingsPromise = ApiClient.getLiveTvTimers({
                IsActive: !1,
                IsScheduled: !0
            })
        }, self.renderTab = function() {
            loading.show(), renderActiveRecordings(tabContent, activeRecordingsPromise), renderUpcomingRecordings(tabContent, upcomingRecordingsPromise)
        }
    }
});