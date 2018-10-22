define(["appSettings", "events"], function(appsettings, events) {
    "use strict";

    function onSaveTimeout() {
        var self = this;
        self.saveTimeout = null, self.currentApiClient.updateDisplayPreferences("usersettings", self.displayPrefs, self.currentUserId, "emby")
    }

    function saveServerPreferences(instance) {
        instance.saveTimeout && clearTimeout(instance.saveTimeout), instance.saveTimeout = setTimeout(onSaveTimeout.bind(instance), 50)
    }

    function UserSettings() {}
    return UserSettings.prototype.setUserInfo = function(userId, apiClient) {
        if (this.saveTimeout && clearTimeout(this.saveTimeout), this.currentUserId = userId, this.currentApiClient = apiClient, !userId) return this.displayPrefs = null, Promise.resolve();
        var self = this;
        return apiClient.getDisplayPreferences("usersettings", userId, "emby").then(function(result) {
            result.CustomPrefs = result.CustomPrefs || {}, self.displayPrefs = result
        })
    }, UserSettings.prototype.getData = function() {
        return this.displayPrefs
    }, UserSettings.prototype.importFrom = function(instance) {
        this.displayPrefs = instance.getData()
    }, UserSettings.prototype.set = function(name, value, enableOnServer) {
        var userId = this.currentUserId;
        if (!userId) throw new Error("userId cannot be null");
        var currentValue = this.get(name, enableOnServer),
            result = appsettings.set(name, value, userId);
        return !1 !== enableOnServer && this.displayPrefs && (this.displayPrefs.CustomPrefs[name] = null == value ? value : value.toString(), saveServerPreferences(this)), currentValue !== value && events.trigger(this, "change", [name]), result
    }, UserSettings.prototype.get = function(name, enableOnServer) {
        var userId = this.currentUserId;
        return userId ? !1 !== enableOnServer && this.displayPrefs ? this.displayPrefs.CustomPrefs[name] : appsettings.get(name, userId) : null
    }, UserSettings.prototype.serverConfig = function(config) {
        var apiClient = this.currentApiClient;
        return config ? apiClient.updateUserConfiguration(this.currentUserId, config) : apiClient.getUser(this.currentUserId).then(function(user) {
            return user.Configuration
        })
    }, UserSettings.prototype.enableCinemaMode = function(val) {
        return null != val ? this.set("enableCinemaMode", val.toString(), !1) : !(val = this.get("enableCinemaMode", !1)) || "false" !== val
    }, UserSettings.prototype.enableNextVideoInfoOverlay = function(val) {
        return null != val ? this.set("enableNextVideoInfoOverlay", val.toString()) : "false" !== (val = this.get("enableNextVideoInfoOverlay"))
    }, UserSettings.prototype.enableThemeSongs = function(val) {
        return null != val ? this.set("enableThemeSongs", val.toString(), !1) : "false" !== (val = this.get("enableThemeSongs", !1))
    }, UserSettings.prototype.enableThemeVideos = function(val) {
        return null != val ? this.set("enableThemeVideos", val.toString(), !1) : (val = this.get("enableThemeVideos", !1), val ? "false" !== val : UserSettings.defaults.enableThemeVideos)
    }, UserSettings.prototype.enableBackdrops = function(val) {
        return null != val ? this.set("enableBackdrops", val.toString(), !1) : (val = this.get("enableBackdrops", !1), val ? "false" !== val : UserSettings.defaults.enableBackdrops)
    }, UserSettings.prototype.language = function(val) {
        return null != val ? this.set("language", val.toString(), !1) : this.get("language", !1)
    }, UserSettings.prototype.dateTimeLocale = function(val) {
        return null != val ? this.set("datetimelocale", val.toString(), !1) : this.get("datetimelocale", !1)
    }, UserSettings.prototype.skipBackLength = function(val) {
        return null != val ? this.set("skipBackLength", val.toString()) : parseInt(this.get("skipBackLength") || "10000")
    }, UserSettings.prototype.skipForwardLength = function(val) {
        return null != val ? this.set("skipForwardLength", val.toString()) : parseInt(this.get("skipForwardLength") || "30000")
    }, UserSettings.prototype.dashboardTheme = function(val) {
        return null != val ? this.set("dashboardTheme", val) : this.get("dashboardTheme")
    }, UserSettings.prototype.skin = function(val) {
        return null != val ? this.set("skin", val, !1) : this.get("skin", !1) || UserSettings.defaults.skin
    }, UserSettings.prototype.theme = function(val) {
        return null != val ? this.set("appTheme", val, !1) : this.get("appTheme", !1) || UserSettings.defaults.theme
    }, UserSettings.prototype.enableSeasonalThemes = function(val) {
        return null != val ? this.set("enableSeasonalThemes", val, !1) : "false" !== this.get("enableSeasonalThemes", !1)
    }, UserSettings.prototype.screensaver = function(val) {
        return null != val ? this.set("screensaver", val, !1) : this.get("screensaver", !1) || UserSettings.defaults.screensaver
    }, UserSettings.prototype.soundEffects = function(val) {
        return null != val ? this.set("soundeffects", val, !1) : this.get("soundeffects", !1) || UserSettings.defaults.soundEffects
    }, UserSettings.defaults = {
        theme: null,
        enableThemeVideos: !0
    }, UserSettings.prototype.loadQuerySettings = function(key, query) {
        var values = this.get(key);
        return values ? (values = JSON.parse(values), Object.assign(query, values)) : query
    }, UserSettings.prototype.saveQuerySettings = function(key, query) {
        var values = {};
        return query.SortBy && (values.SortBy = query.SortBy), query.SortOrder && (values.SortOrder = query.SortOrder), this.set(key, JSON.stringify(values))
    }, UserSettings.prototype.getSubtitleAppearanceSettings = function(key) {
        return key = key || "localplayersubtitleappearance3", JSON.parse(this.get(key, !1) || "{}")
    }, UserSettings.prototype.setSubtitleAppearanceSettings = function(value, key) {
        return key = key || "localplayersubtitleappearance3", this.set(key, JSON.stringify(value), !1)
    }, UserSettings.prototype.setFilter = function(key, value) {
        return this.set(key, value, !0)
    }, UserSettings.prototype.getFilter = function(key) {
        return this.get(key, !0)
    }, UserSettings
});