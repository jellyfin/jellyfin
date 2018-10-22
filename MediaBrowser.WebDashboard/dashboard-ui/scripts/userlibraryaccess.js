define(["jQuery", "loading", "libraryMenu", "fnchecked"], function($, loading, libraryMenu) {
    "use strict";

    function triggerChange(select) {
        var evt = document.createEvent("HTMLEvents");
        evt.initEvent("change", !1, !0), select.dispatchEvent(evt)
    }

    function loadMediaFolders(page, user, mediaFolders) {
        var html = "";
        html += '<h3 class="checkboxListLabel">' + Globalize.translate("HeaderLibraries") + "</h3>", html += '<div class="checkboxList paperList checkboxList-paperList">';
        for (var i = 0, length = mediaFolders.length; i < length; i++) {
            var folder = mediaFolders[i],
                isChecked = user.Policy.EnableAllFolders || -1 != user.Policy.EnabledFolders.indexOf(folder.Id),
                checkedAttribute = isChecked ? ' checked="checked"' : "";
            html += '<label><input type="checkbox" is="emby-checkbox" class="chkFolder" data-id="' + folder.Id + '" ' + checkedAttribute + "><span>" + folder.Name + "</span></label>"
        }
        html += "</div>", page.querySelector(".folderAccess").innerHTML = html;
        var chkEnableAllFolders = page.querySelector("#chkEnableAllFolders");
        chkEnableAllFolders.checked = user.Policy.EnableAllFolders, triggerChange(chkEnableAllFolders)
    }

    function loadChannels(page, user, channels) {
        var html = "";
        html += '<h3 class="checkboxListLabel">' + Globalize.translate("HeaderChannels") + "</h3>", html += '<div class="checkboxList paperList checkboxList-paperList">';
        for (var i = 0, length = channels.length; i < length; i++) {
            var folder = channels[i],
                isChecked = user.Policy.EnableAllChannels || -1 != user.Policy.EnabledChannels.indexOf(folder.Id),
                checkedAttribute = isChecked ? ' checked="checked"' : "";
            html += '<label><input type="checkbox" is="emby-checkbox" class="chkChannel" data-id="' + folder.Id + '" ' + checkedAttribute + "><span>" + folder.Name + "</span></label>"
        }
        html += "</div>", $(".channelAccess", page).show().html(html), channels.length ? $(".channelAccessContainer", page).show() : $(".channelAccessContainer", page).hide(), $("#chkEnableAllChannels", page).checked(user.Policy.EnableAllChannels).trigger("change")
    }

    function loadDevices(page, user, devices) {
        var html = "";
        html += '<h3 class="checkboxListLabel">' + Globalize.translate("HeaderDevices") + "</h3>", html += '<div class="checkboxList paperList checkboxList-paperList">';
        for (var i = 0, length = devices.length; i < length; i++) {
            var device = devices[i],
                checkedAttribute = user.Policy.EnableAllDevices || -1 != user.Policy.EnabledDevices.indexOf(device.Id) ? ' checked="checked"' : "";
            html += '<label><input type="checkbox" is="emby-checkbox" class="chkDevice" data-id="' + device.Id + '" ' + checkedAttribute + "><span>" + device.Name + " - " + device.AppName + "</span></label>"
        }
        html += "</div>", $(".deviceAccess", page).show().html(html), $("#chkEnableAllDevices", page).checked(user.Policy.EnableAllDevices).trigger("change"), user.Policy.IsAdministrator ? page.querySelector(".deviceAccessContainer").classList.add("hide") : page.querySelector(".deviceAccessContainer").classList.remove("hide")
    }

    function loadUser(page, user, loggedInUser, mediaFolders, channels, devices) {
        page.querySelector(".username").innerHTML = user.Name, libraryMenu.setTitle(user.Name), loadChannels(page, user, channels), loadMediaFolders(page, user, mediaFolders), loadDevices(page, user, devices), loading.hide()
    }

    function onSaveComplete(page) {
        loading.hide(), require(["toast"], function(toast) {
            toast(Globalize.translate("SettingsSaved"))
        })
    }

    function saveUser(user, page) {
        user.Policy.EnableAllFolders = $("#chkEnableAllFolders", page).checked(), user.Policy.EnabledFolders = user.Policy.EnableAllFolders ? [] : $(".chkFolder", page).get().filter(function(c) {
            return c.checked
        }).map(function(c) {
            return c.getAttribute("data-id")
        }), user.Policy.EnableAllChannels = $("#chkEnableAllChannels", page).checked(), user.Policy.EnabledChannels = user.Policy.EnableAllChannels ? [] : $(".chkChannel", page).get().filter(function(c) {
            return c.checked
        }).map(function(c) {
            return c.getAttribute("data-id")
        }), user.Policy.EnableAllDevices = $("#chkEnableAllDevices", page).checked(), user.Policy.EnabledDevices = user.Policy.EnableAllDevices ? [] : $(".chkDevice", page).get().filter(function(c) {
            return c.checked
        }).map(function(c) {
            return c.getAttribute("data-id")
        }), user.Policy.BlockedChannels = null, user.Policy.BlockedMediaFolders = null, ApiClient.updateUserPolicy(user.Id, user.Policy).then(function() {
            onSaveComplete(page)
        })
    }

    function onSubmit() {
        var page = $(this).parents(".page");
        loading.show();
        var userId = getParameterByName("userId");
        return ApiClient.getUser(userId).then(function(result) {
            saveUser(result, page)
        }), !1
    }
    $(document).on("pageinit", "#userLibraryAccessPage", function() {
        var page = this;
        $("#chkEnableAllDevices", page).on("change", function() {
            this.checked ? $(".deviceAccessListContainer", page).hide() : $(".deviceAccessListContainer", page).show()
        }), $("#chkEnableAllChannels", page).on("change", function() {
            this.checked ? $(".channelAccessListContainer", page).hide() : $(".channelAccessListContainer", page).show()
        }), page.querySelector("#chkEnableAllFolders").addEventListener("change", function() {
            this.checked ? page.querySelector(".folderAccessListContainer").classList.add("hide") : page.querySelector(".folderAccessListContainer").classList.remove("hide")
        }), $(".userLibraryAccessForm").off("submit", onSubmit).on("submit", onSubmit)
    }).on("pageshow", "#userLibraryAccessPage", function() {
        var page = this;
        loading.show();
        var promise1, userId = getParameterByName("userId");
        if (userId) promise1 = ApiClient.getUser(userId);
        else {
            var deferred = $.Deferred();
            deferred.resolveWith(null, [{
                Configuration: {}
            }]), promise1 = deferred.promise()
        }
        var promise2 = Dashboard.getCurrentUser(),
            promise4 = ApiClient.getJSON(ApiClient.getUrl("Library/MediaFolders", {
                IsHidden: !1
            })),
            promise5 = ApiClient.getJSON(ApiClient.getUrl("Channels")),
            promise6 = ApiClient.getJSON(ApiClient.getUrl("Devices"));
        Promise.all([promise1, promise2, promise4, promise5, promise6]).then(function(responses) {
            loadUser(page, responses[0], responses[1], responses[2].Items, responses[3].Items, responses[4].Items)
        })
    })
});