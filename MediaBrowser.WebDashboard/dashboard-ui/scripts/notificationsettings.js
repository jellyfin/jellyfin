define(["loading", "libraryMenu", "globalize", "listViewStyle", "emby-linkbutton"], function(loading, libraryMenu, globalize) {
    "use strict";

    function reload(page) {
        loading.show(), ApiClient.getJSON(ApiClient.getUrl("Notifications/Types")).then(function(list) {
            var html = "",
                lastCategory = "",
                showHelp = !0;
            html += list.map(function(i) {
                var itemHtml = "";
                return i.Category != lastCategory && (lastCategory = i.Category, lastCategory && (itemHtml += "</div>", itemHtml += "</div>"), itemHtml += '<div class="verticalSection verticalSection-extrabottompadding">', itemHtml += '<div class="sectionTitleContainer" style="margin-bottom:1em;">', itemHtml += '<h2 class="sectionTitle">', itemHtml += i.Category, itemHtml += "</h2>", showHelp && (showHelp = !1, itemHtml += '<a is="emby-linkbutton" class="raised button-alt headerHelpButton" target="_blank" href="https://web.archive.org/web/20181216120305/https://github.com/MediaBrowser/Wiki/wiki/Notifications">' + globalize.translate("Help") + "</a>"), itemHtml += "</div>", itemHtml += '<div class="paperList">'), itemHtml += '<a class="listItem listItem-border" is="emby-linkbutton" data-ripple="false" href="notificationsetting.html?type=' + i.Type + '">', i.Enabled ? itemHtml += '<i class="listItemIcon md-icon">notifications_active</i>' : itemHtml += '<i class="listItemIcon md-icon" style="background-color:#999;">notifications_off</i>', itemHtml += '<div class="listItemBody">', itemHtml += '<div class="listItemBodyText">' + i.Name + "</div>", itemHtml += "</div>", itemHtml += '<button type="button" is="paper-icon-button-light"><i class="md-icon">mode_edit</i></button>', itemHtml += "</a>"
            }).join(""), list.length && (html += "</div>", html += "</div>"), page.querySelector(".notificationList").innerHTML = html, loading.hide()
        })
    }

    function getTabs() {
        return [{
            href: "notificationsettings.html",
            name: globalize.translate("TabNotifications")
        }, {
            href: "appservices.html?context=notifications",
            name: globalize.translate("TabServices")
        }]
    }
    return function(view, params) {
        view.addEventListener("viewshow", function() {
            libraryMenu.setTabs("notifications", 0, getTabs), reload(view)
        })
    }
});