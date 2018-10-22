define(["playbackManager", "userSettings", "connectionManager"], function(playbackManager, userSettings, connectionManager) {
    "use strict";

    function playThemeMedia(items, ownerId) {
        var currentThemeItems = items.filter(function(i) {
            return enabled(i.MediaType)
        });
        if (currentThemeItems.length) {
            if (!currentOwnerId && playbackManager.isPlaying()) return;
            currentThemeIds = currentThemeItems.map(function(i) {
                return i.Id
            }), playbackManager.play({
                items: currentThemeItems,
                fullscreen: !1,
                enableRemotePlayers: !1
            }).then(function() {
                currentOwnerId = ownerId
            })
        } else stopIfPlaying()
    }

    function stopIfPlaying() {
        currentOwnerId && playbackManager.stop(), currentOwnerId = null
    }

    function enabled(mediaType) {
        return "Video" === mediaType ? userSettings.enableThemeVideos() : userSettings.enableThemeSongs()
    }

    function loadThemeMedia(item) {
        if (item.CollectionType) return void stopIfPlaying();
        if (-1 !== excludeTypes.indexOf(item.Type)) return void stopIfPlaying();
        var apiClient = connectionManager.getApiClient(item.ServerId);
        apiClient.getThemeMedia(apiClient.getCurrentUserId(), item.Id, !0).then(function(themeMediaResult) {
            var ownerId = themeMediaResult.ThemeVideosResult.Items.length ? themeMediaResult.ThemeVideosResult.OwnerId : themeMediaResult.ThemeSongsResult.OwnerId;
            if (ownerId !== currentOwnerId) {
                playThemeMedia(themeMediaResult.ThemeVideosResult.Items.length ? themeMediaResult.ThemeVideosResult.Items : themeMediaResult.ThemeSongsResult.Items, ownerId)
            }
        })
    }
    var currentOwnerId, currentThemeIds = [],
        excludeTypes = ["CollectionFolder", "UserView", "Program", "SeriesTimer", "Person", "TvChannel", "Channel"];
    document.addEventListener("viewshow", function(e) {
        var state = e.detail.state || {},
            item = state.item;
        if (item && item.ServerId) return void loadThemeMedia(item);
        (e.detail.options || {}).supportsThemeMedia || playThemeMedia([], null)
    }, !0)
});