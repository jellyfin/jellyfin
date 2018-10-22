define(["homescreenSettings", "userSettingsBuilder", "dom", "globalize", "loading", "userSettings", "listViewStyle"], function(HomescreenSettings, userSettingsBuilder, dom, globalize, loading, currentUserSettings) {
    "use strict";
    return function(view, params) {
        function onBeforeUnload(e) {
            hasChanges && (e.returnValue = "You currently have unsaved changes. Are you sure you wish to leave?")
        }
        var homescreenSettingsInstance, hasChanges, userId = params.userId || ApiClient.getCurrentUserId(),
            userSettings = userId === ApiClient.getCurrentUserId() ? currentUserSettings : new userSettingsBuilder;
        view.addEventListener("viewshow", function() {
            window.addEventListener("beforeunload", onBeforeUnload), homescreenSettingsInstance ? homescreenSettingsInstance.loadData() : homescreenSettingsInstance = new HomescreenSettings({
                serverId: ApiClient.serverId(),
                userId: userId,
                element: view.querySelector(".homeScreenSettingsContainer"),
                userSettings: userSettings,
                enableSaveButton: !1,
                enableSaveConfirmation: !1
            })
        }), view.addEventListener("change", function() {
            hasChanges = !0
        }), view.addEventListener("viewbeforehide", function() {
            hasChanges = !1, homescreenSettingsInstance && homescreenSettingsInstance.submit()
        }), view.addEventListener("viewdestroy", function() {
            homescreenSettingsInstance && (homescreenSettingsInstance.destroy(), homescreenSettingsInstance = null)
        })
    }
});