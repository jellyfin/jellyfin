define(["layoutManager", "loading", "datetime", "libraryBrowser", "cardBuilder", "apphost", "imageLoader", "scrollStyles", "emby-itemscontainer"], function(layoutManager, loading, datetime, libraryBrowser, cardBuilder, appHost, imageLoader) {
    "use strict";

    function getUpcomingPromise(context, params) {
        loading.show();
        var query = {
            Limit: 48,
            Fields: "AirTime,UserData",
            UserId: ApiClient.getCurrentUserId(),
            ImageTypeLimit: 1,
            EnableImageTypes: "Primary,Backdrop,Banner,Thumb",
            EnableTotalRecordCount: !1
        };
        return query.ParentId = params.topParentId, ApiClient.getJSON(ApiClient.getUrl("Shows/Upcoming", query))
    }

    function loadUpcoming(context, params, promise) {
        promise.then(function(result) {
            var items = result.Items;
            items.length ? context.querySelector(".noItemsMessage").style.display = "none" : context.querySelector(".noItemsMessage").style.display = "block", renderUpcoming(context.querySelector("#upcomingItems"), items), loading.hide()
        })
    }

    function enableScrollX() {
        return !layoutManager.desktop
    }

    function getThumbShape() {
        return enableScrollX() ? "overflowBackdrop" : "backdrop"
    }

    function renderUpcoming(elem, items) {
        var i, length, groups = [],
            currentGroupName = "",
            currentGroup = [];
        for (i = 0, length = items.length; i < length; i++) {
            var item = items[i],
                dateText = "";
            if (item.PremiereDate) try {
                var premiereDate = datetime.parseISO8601Date(item.PremiereDate, !0);
                dateText = datetime.isRelativeDay(premiereDate, -1) ? Globalize.translate("Yesterday") : datetime.toLocaleDateString(premiereDate, {
                    weekday: "long",
                    month: "short",
                    day: "numeric"
                })
            } catch (err) {}
            dateText != currentGroupName ? (currentGroup.length && groups.push({
                name: currentGroupName,
                items: currentGroup
            }), currentGroupName = dateText, currentGroup = [item]) : currentGroup.push(item)
        }
        var html = "";
        for (i = 0, length = groups.length; i < length; i++) {
            var group = groups[i];
            html += '<div class="verticalSection">', html += '<h2 class="sectionTitle sectionTitle-cards padded-left">' + group.name + "</h2>";
            var allowBottomPadding = !0;
            if (enableScrollX()) {
                allowBottomPadding = !1;
                var scrollXClass = "scrollX hiddenScrollX";
                layoutManager.tv && (scrollXClass += " smoothScrollX"), html += '<div is="emby-itemscontainer" class="itemsContainer ' + scrollXClass + ' padded-left padded-right">'
            } else html += '<div is="emby-itemscontainer" class="itemsContainer vertical-wrap padded-left padded-right">';
            var supportsImageAnalysis = appHost.supports("imageanalysis");
            supportsImageAnalysis = !1, html += cardBuilder.getCardsHtml({
                items: group.items,
                showLocationTypeIndicator: !1,
                shape: getThumbShape(),
                showTitle: !0,
                preferThumb: !0,
                lazy: !0,
                showDetailsMenu: !0,
                centerText: !supportsImageAnalysis,
                showParentTitle: !0,
                overlayText: !1,
                allowBottomPadding: allowBottomPadding,
                cardLayout: supportsImageAnalysis,
                vibrant: supportsImageAnalysis,
                overlayMoreButton: !0,
                missingIndicator: !1
            }), html += "</div>", html += "</div>"
        }
        elem.innerHTML = html, imageLoader.lazyChildren(elem)
    }
    return function(view, params, tabContent) {
        var upcomingPromise, self = this;
        self.preRender = function() {
            upcomingPromise = getUpcomingPromise(view, params)
        }, self.renderTab = function() {
            loadUpcoming(tabContent, params, upcomingPromise)
        }
    }
});