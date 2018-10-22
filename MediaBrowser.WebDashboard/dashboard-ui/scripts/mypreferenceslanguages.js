define(["playbackSettings", "userSettingsBuilder", "dom", "globalize", "loading", "userSettings", "listViewStyle"], function(PlaybackSettings, userSettingsBuilder, dom, globalize, loading, currentUserSettings) {
    "use strict";
    return function(view, params) {
        function onBeforeUnload(e) {
            hasChanges && (e.returnValue = "You currently have unsaved changes. Are you sure you wish to leave?")
        }
        var settingsInstance, hasChanges, userId = params.userId || ApiClient.getCurrentUserId(),
            userSettings = userId === ApiClient.getCurrentUserId() ? currentUserSettings : new userSettingsBuilder;
        view.addEventListener("viewshow", function() {
            window.addEventListener("beforeunload", onBeforeUnload), settingsInstance ? settingsInstance.loadData() : settingsInstance = new PlaybackSettings({
                serverId: ApiClient.serverId(),
                userId: userId,
                element: view.querySelector(".settingsContainer"),
                userSettings: userSettings,
                enableSaveButton: !1,
                enableSaveConfirmation: !1
            })
        }), view.addEventListener("change", function() {
            hasChanges = !0
        }), view.addEventListener("viewbeforehide", function() {
            hasChanges = !1, settingsInstance && settingsInstance.submit()
        }), view.addEventListener("viewdestroy", function() {
            settingsInstance && (settingsInstance.destroy(), settingsInstance = null)
        })
    }
});