define(["subtitleSettings", "userSettingsBuilder", "userSettings"], function(SubtitleSettings, userSettingsBuilder, currentUserSettings) {
    "use strict";
    return function(view, params) {
        function onBeforeUnload(e) {
            hasChanges && (e.returnValue = "You currently have unsaved changes. Are you sure you wish to leave?")
        }
        var subtitleSettingsInstance, hasChanges, userId = params.userId || ApiClient.getCurrentUserId(),
            userSettings = userId === ApiClient.getCurrentUserId() ? currentUserSettings : new userSettingsBuilder;
        view.addEventListener("viewshow", function() {
            window.addEventListener("beforeunload", onBeforeUnload), subtitleSettingsInstance ? subtitleSettingsInstance.loadData() : subtitleSettingsInstance = new SubtitleSettings({
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
            hasChanges = !1, subtitleSettingsInstance && subtitleSettingsInstance.submit()
        }), view.addEventListener("viewdestroy", function() {
            subtitleSettingsInstance && (subtitleSettingsInstance.destroy(), subtitleSettingsInstance = null)
        })
    }
});