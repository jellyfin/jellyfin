define(["events", "globalize", "dom", "datetime", "userSettings", "serverNotifications", "connectionManager", "emby-button", "listViewStyle"], function(events, globalize, dom, datetime, userSettings, serverNotifications, connectionManager) {
    "use strict";

    function getEntryHtml(entry, apiClient) {
        var html = "";
        html += '<div class="listItem listItem-border">';
        var color = "Error" == entry.Severity || "Fatal" == entry.Severity || "Warn" == entry.Severity ? "#cc0000" : "#00a4dc";
        if (entry.UserId && entry.UserPrimaryImageTag) {
            html += '<i class="listItemIcon md-icon" style="width:2em!important;height:2em!important;padding:0;color:transparent;background-color:' + color + ";background-image:url('" + apiClient.getUserImageUrl(entry.UserId, {
                type: "Primary",
                tag: entry.UserPrimaryImageTag,
                height: 40
            }) + "');background-repeat:no-repeat;background-position:center center;background-size: cover;\">dvr</i>"
        } else html += '<i class="listItemIcon md-icon" style="background-color:' + color + '">dvr</i>';
        html += '<div class="listItemBody three-line">', html += '<div class="listItemBodyText">', html += entry.Name, html += "</div>", html += '<div class="listItemBodyText secondary">';
        var date = datetime.parseISO8601Date(entry.Date, !0);
        return html += datetime.toLocaleString(date).toLowerCase(), html += "</div>", html += '<div class="listItemBodyText secondary listItemBodyText-nowrap">', html += entry.ShortOverview || "", html += "</div>", html += "</div>", entry.Overview && (html += '<button type="button" is="paper-icon-button-light" class="btnEntryInfo" data-id="' + entry.Id + '" title="' + globalize.translate("Info") + '"><i class="md-icon">info</i></button>'), html += "</div>"
    }

    function renderList(elem, apiClient, result, startIndex, limit) {
        elem.innerHTML = result.Items.map(function(i) {
            return getEntryHtml(i, apiClient)
        }).join("")
    }

    function reloadData(instance, elem, apiClient, startIndex, limit) {
        null == startIndex && (startIndex = parseInt(elem.getAttribute("data-activitystartindex") || "0")), limit = limit || parseInt(elem.getAttribute("data-activitylimit") || "7");
        var minDate = new Date,
            hasUserId = "false" !== elem.getAttribute("data-useractivity");
        hasUserId ? minDate.setTime(minDate.getTime() - 864e5) : minDate.setTime(minDate.getTime() - 6048e5), ApiClient.getJSON(ApiClient.getUrl("System/ActivityLog/Entries", {
            startIndex: startIndex,
            limit: limit,
            minDate: minDate.toISOString(),
            hasUserId: hasUserId
        })).then(function(result) {
            if (elem.setAttribute("data-activitystartindex", startIndex), elem.setAttribute("data-activitylimit", limit), !startIndex) {
                var activityContainer = dom.parentWithClass(elem, "activityContainer");
                activityContainer && (result.Items.length ? activityContainer.classList.remove("hide") : activityContainer.classList.add("hide"))
            }
            instance.items = result.Items, renderList(elem, apiClient, result, startIndex, limit)
        })
    }

    function onActivityLogUpdate(e, apiClient, data) {
        var options = this.options;
        options && options.serverId === apiClient.serverId() && reloadData(this, options.element, apiClient)
    }

    function onListClick(e) {
        var btnEntryInfo = dom.parentWithClass(e.target, "btnEntryInfo");
        if (btnEntryInfo) {
            var id = btnEntryInfo.getAttribute("data-id"),
                items = this.items;
            if (items) {
                var item = items.filter(function(i) {
                    return i.Id.toString() === id
                })[0];
                item && showItemOverview(item)
            }
        }
    }

    function showItemOverview(item) {
        require(["alert"], function(alert) {
            alert({
                text: item.Overview
            })
        })
    }

    function ActivityLog(options) {
        this.options = options;
        var element = options.element;
        element.classList.add("activityLogListWidget"), element.addEventListener("click", onListClick.bind(this));
        var apiClient = connectionManager.getApiClient(options.serverId);
        reloadData(this, element, apiClient);
        var onUpdate = onActivityLogUpdate.bind(this);
        this.updateFn = onUpdate, events.on(serverNotifications, "ActivityLogEntry", onUpdate), apiClient.sendMessage("ActivityLogEntryStart", "0,1500")
    }
    return ActivityLog.prototype.destroy = function() {
        var options = this.options;
        if (options) {
            options.element.classList.remove("activityLogListWidget");
            connectionManager.getApiClient(options.serverId).sendMessage("ActivityLogEntryStop", "0,1500")
        }
        var onUpdate = this.updateFn;
        onUpdate && events.off(serverNotifications, "ActivityLogEntry", onUpdate), this.items = null, this.options = null
    }, ActivityLog
});