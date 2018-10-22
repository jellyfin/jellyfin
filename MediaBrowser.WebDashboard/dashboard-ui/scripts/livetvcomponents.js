define(["layoutManager", "datetime", "cardBuilder", "apphost"], function(layoutManager, datetime, cardBuilder, appHost) {
    "use strict";

    function enableScrollX() {
        return !layoutManager.desktop
    }

    function getBackdropShape() {
        return enableScrollX() ? "overflowBackdrop" : "backdrop"
    }

    function getTimersHtml(timers, options) {
        options = options || {};
        var i, length, items = timers.map(function(t) {
                return t.Type = "Timer", t
            }),
            groups = [],
            currentGroupName = "",
            currentGroup = [];
        for (i = 0, length = items.length; i < length; i++) {
            var item = items[i],
                dateText = "";
            if (!1 !== options.indexByDate && item.StartDate) try {
                var premiereDate = datetime.parseISO8601Date(item.StartDate, !0);
                dateText = datetime.toLocaleDateString(premiereDate, {
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
        currentGroup.length && groups.push({
            name: currentGroupName,
            items: currentGroup
        });
        var html = "";
        for (i = 0, length = groups.length; i < length; i++) {
            var group = groups[i],
                supportsImageAnalysis = appHost.supports("imageanalysis"),
                cardLayout = appHost.preferVisualCards || supportsImageAnalysis;
            if (cardLayout = !0, group.name && (html += '<div class="verticalSection">', html += '<h2 class="sectionTitle sectionTitle-cards padded-left">' + group.name + "</h2>"), enableScrollX()) {
                var scrollXClass = "scrollX hiddenScrollX";
                layoutManager.tv && (scrollXClass += " smoothScrollX"), html += '<div is="emby-itemscontainer" class="itemsContainer ' + scrollXClass + ' padded-left padded-right">'
            } else html += '<div is="emby-itemscontainer" class="itemsContainer vertical-wrap padded-left padded-right">';
            html += cardBuilder.getCardsHtml({
                items: group.items,
                shape: cardLayout ? getBackdropShape() : enableScrollX() ? "autoOverflow" : "autoVertical",
                showParentTitleOrTitle: !0,
                showAirTime: !0,
                showAirEndTime: !0,
                showChannelName: !cardLayout,
                cardLayout: cardLayout,
                centerText: !cardLayout,
                action: "edit",
                cardFooterAside: "none",
                preferThumb: !!cardLayout || "auto",
                defaultShape: cardLayout ? null : "portrait",
                coverImage: !0,
                allowBottomPadding: !1,
                overlayText: !1,
                showChannelLogo: cardLayout
            }), html += "</div>", group.name && (html += "</div>")
        }
        return Promise.resolve(html)
    }
    window.LiveTvHelpers = {
        getTimersHtml: getTimersHtml
    }
});