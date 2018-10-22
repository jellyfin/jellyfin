define(["jQuery", "emby-checkbox", "fnchecked"], function($) {
    "use strict";

    function fillItems(elem, items, cssClass, idPrefix, currentList, isEnabledList) {
        var html = '<div class="checkboxList paperList" style="padding: .5em 1em;">';
        html += items.map(function(u) {
            var isChecked = isEnabledList ? -1 != currentList.indexOf(u.Id) : -1 == currentList.indexOf(u.Id),
                checkedHtml = isChecked ? ' checked="checked"' : "";
            return '<label><input is="emby-checkbox" class="' + cssClass + '" type="checkbox" data-itemid="' + u.Id + '"' + checkedHtml + "/><span>" + u.Name + "</span></label>"
        }).join(""), html += "</div>", elem.html(html).trigger("create")
    }

    function reload(page) {
        var type = getParameterByName("type"),
            promise1 = ApiClient.getUsers(),
            promise2 = ApiClient.getNamedConfiguration(notificationsConfigurationKey),
            promise3 = ApiClient.getJSON(ApiClient.getUrl("Notifications/Types")),
            promise4 = ApiClient.getJSON(ApiClient.getUrl("Notifications/Services"));
        Promise.all([promise1, promise2, promise3, promise4]).then(function(responses) {
            var users = responses[0],
                notificationOptions = responses[1],
                types = responses[2],
                services = responses[3],
                notificationConfig = notificationOptions.Options.filter(function(n) {
                    return n.Type == type
                })[0],
                typeInfo = types.filter(function(n) {
                    return n.Type == type
                })[0] || {};
            typeInfo.IsBasedOnUserEvent ? $(".monitorUsers", page).show() : $(".monitorUsers", page).hide(), $(".notificationType", page).html(typeInfo.Name || "Unknown Notification"), notificationConfig || (notificationConfig = {
                DisabledMonitorUsers: [],
                SendToUsers: [],
                DisabledServices: [],
                SendToUserMode: "Admins"
            }), fillItems($(".monitorUsersList", page), users, "chkMonitor", "chkMonitor", notificationConfig.DisabledMonitorUsers), fillItems($(".sendToUsersList", page), users, "chkSendTo", "chkSendTo", notificationConfig.SendToUsers, !0), fillItems($(".servicesList", page), services, "chkService", "chkService", notificationConfig.DisabledServices), $("#chkEnabled", page).checked(notificationConfig.Enabled || !1), $("#selectUsers", page).val(notificationConfig.SendToUserMode).trigger("change")
        })
    }

    function save(page) {
        var type = getParameterByName("type"),
            promise1 = ApiClient.getNamedConfiguration(notificationsConfigurationKey),
            promise2 = ApiClient.getJSON(ApiClient.getUrl("Notifications/Types"));
        Promise.all([promise1, promise2]).then(function(responses) {
            var notificationOptions = responses[0],
                types = responses[1],
                notificationConfig = notificationOptions.Options.filter(function(n) {
                    return n.Type == type
                })[0];
            notificationConfig || (notificationConfig = {
                Type: type
            }, notificationOptions.Options.push(notificationConfig));
            types.filter(function(n) {
                return n.Type == type
            })[0];
            notificationConfig.Enabled = $("#chkEnabled", page).checked(), notificationConfig.SendToUserMode = $("#selectUsers", page).val(), notificationConfig.DisabledMonitorUsers = $(".chkMonitor", page).get().filter(function(c) {
                return !c.checked
            }).map(function(c) {
                return c.getAttribute("data-itemid")
            }), notificationConfig.SendToUsers = $(".chkSendTo", page).get().filter(function(c) {
                return c.checked
            }).map(function(c) {
                return c.getAttribute("data-itemid")
            }), notificationConfig.DisabledServices = $(".chkService", page).get().filter(function(c) {
                return !c.checked
            }).map(function(c) {
                return c.getAttribute("data-itemid")
            }), ApiClient.updateNamedConfiguration(notificationsConfigurationKey, notificationOptions).then(function(r) {
                Dashboard.processServerConfigurationUpdateResult(), Dashboard.navigate("notificationsettings.html")
            })
        })
    }

    function onSubmit() {
        return save($(this).parents(".page")), !1
    }
    var notificationsConfigurationKey = "notifications";
    $(document).on("pageinit", "#notificationSettingPage", function() {
        var page = this;
        $("#selectUsers", page).on("change", function() {
            "Custom" == this.value ? $(".selectCustomUsers", page).show() : $(".selectCustomUsers", page).hide()
        }), $(".notificationSettingForm").off("submit", onSubmit).on("submit", onSubmit)
    }).on("pageshow", "#notificationSettingPage", function() {
        reload(this)
    })
});