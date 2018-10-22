define(["backdrop", "userSettings", "libraryMenu"], function(backdrop, userSettings, libraryMenu) {
    "use strict";

    function enabled() {
        return userSettings.enableBackdrops()
    }

    function getBackdropItemIds(apiClient, userId, types, parentId) {
        var key = "backdrops2_" + userId + (types || "") + (parentId || ""),
            data = cache[key];
        if (data) return console.log("Found backdrop id list in cache. Key: " + key), data = JSON.parse(data), Promise.resolve(data);
        var options = {
            SortBy: "IsFavoriteOrLiked,Random",
            Limit: 20,
            Recursive: !0,
            IncludeItemTypes: types,
            ImageTypes: "Backdrop",
            ParentId: parentId,
            EnableTotalRecordCount: !1
        };
        return apiClient.getItems(apiClient.getCurrentUserId(), options).then(function(result) {
            var images = result.Items.map(function(i) {
                return {
                    Id: i.Id,
                    tag: i.BackdropImageTags[0],
                    ServerId: i.ServerId
                }
            });
            return cache[key] = JSON.stringify(images), images
        })
    }

    function showBackdrop(type, parentId) {
        var apiClient = window.ApiClient;
        apiClient && getBackdropItemIds(apiClient, apiClient.getCurrentUserId(), type, parentId).then(function(images) {
            images.length ? backdrop.setBackdrops(images.map(function(i) {
                return i.BackdropImageTags = [i.tag], i
            })) : backdrop.clear()
        })
    }
    var cache = {};
    pageClassOn("pagebeforeshow", "page", function() {
        var page = this;
        if (!page.classList.contains("selfBackdropPage"))
            if (page.classList.contains("backdropPage"))
                if (enabled()) {
                    var type = page.getAttribute("data-backdroptype"),
                        parentId = page.classList.contains("globalBackdropPage") ? "" : libraryMenu.getTopParentId();
                    showBackdrop(type, parentId)
                } else page.classList.remove("backdropPage"), backdrop.clear();
        else backdrop.clear()
    })
});