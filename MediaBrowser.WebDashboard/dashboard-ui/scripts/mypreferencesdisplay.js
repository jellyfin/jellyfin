define(["displaySettings", "userSettingsBuilder", "userSettings"], function(DisplaySettings, userSettingsBuilder, currentUserSettings) {
    "use strict";
    return function(view, params) {
        function onBeforeUnload(e) {
            hasChanges && (e.returnValue = "You currently have unsaved changes. Are you sure you wish to leave?")
        }
        var settingsInstance, hasChanges, userId = params.userId || ApiClient.getCurrentUserId(),
            userSettings = userId === ApiClient.getCurrentUserId() ? currentUserSettings : new userSettingsBuilder;
        view.addEventListener("viewshow", function() {
            window.addEventListener("beforeunload", onBeforeUnload), settingsInstance ? settingsInstance.loadData() : settingsInstance = new DisplaySettings({
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
            window.removeEventListener("beforeunload", onBeforeUnload), hasChanges = !1, settingsInstance && settingsInstance.submit()
        }), view.addEventListener("viewdestroy", function() {
            settingsInstance && (settingsInstance.destroy(), settingsInstance = null)
        }), view.addEventListener("viewdestroy", function() {
            settingsInstance && (settingsInstance.destroy(), settingsInstance = null)
        })
    }
});