define(["connectionManager", "globalize", "userSettings", "apphost"], function(connectionManager, globalize, userSettings, appHost) {
    "use strict";

    function getWeek(date) {
        var d = new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate())),
            dayNum = d.getUTCDay() || 7;
        d.setUTCDate(d.getUTCDate() + 4 - dayNum);
        var yearStart = new Date(Date.UTC(d.getUTCFullYear(), 0, 1));
        return Math.ceil(((d - yearStart) / 864e5 + 1) / 7)
    }

    function showMessage(text, userSettingsKey, appHostFeature) {
        if (appHost.supports(appHostFeature)) return Promise.resolve();
        var now = new Date;
        return userSettingsKey += now.getFullYear() + "-w" + getWeek(now), "1" === userSettings.get(userSettingsKey, !1) ? Promise.resolve() : new Promise(function(resolve, reject) {
            userSettings.set(userSettingsKey, "1", !1), require(["alert"], function(alert) {
                return alert(text).then(resolve, resolve)
            })
        })
    }

    function showBlurayMessage() {
        return showMessage("Playback of Bluray folders in this app is experimental. Some titles may not work at all. For a better experience, consider converting to mkv video files, or use an Jellyfin app with native Bluray folder support.", "blurayexpirementalinfo", "nativeblurayplayback")
    }

    function showDvdMessage() {
        return showMessage("Playback of Dvd folders in this app is experimental. Some titles may not work at all. For a better experience, consider converting to mkv video files, or use an Jellyfin app with native Dvd folder support.", "dvdexpirementalinfo", "nativedvdplayback")
    }

    function showIsoMessage() {
        return showMessage("Playback of ISO files in this app is experimental. Some titles may not work at all. For a better experience, consider converting to mkv video files, or use an Jellyfin app with native ISO support.", "isoexpirementalinfo", "nativeisoplayback")
    }

    function ExpirementalPlaybackWarnings() {
        this.name = "Experimental playback warnings", this.type = "preplayintercept", this.id = "expirementalplaybackwarnings"
    }
    return ExpirementalPlaybackWarnings.prototype.intercept = function(options) {
        var item = options.item;
        return item ? "Iso" === item.VideoType ? showIsoMessage() : "BluRay" === item.VideoType ? showBlurayMessage() : "Dvd" === item.VideoType ? showDvdMessage() : Promise.resolve() : Promise.resolve()
    }, ExpirementalPlaybackWarnings
});
