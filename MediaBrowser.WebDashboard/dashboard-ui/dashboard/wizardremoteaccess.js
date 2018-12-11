define(["loading", "emby-checkbox", "emby-button", "emby-select"], function(loading) {
    "use strict";

    function save(page) {
        loading.show();
        var apiClient = ApiClient,
            config = {};
        config.EnableRemoteAccess = page.querySelector("#chkRemoteAccess").checked, config.EnableAutomaticPortMapping = page.querySelector("#chkEnableUpnp").checked, apiClient.ajax({
            type: "POST",
            data: config,
            url: apiClient.getUrl("Startup/RemoteAccess")
        }).then(function() {
            loading.hide(), navigateToNextPage()
        })
    }

    function navigateToNextPage() {
        Dashboard.navigate("wizardfinish.html")
    }

    function onSubmit(e) {
        return save(this), e.preventDefault(), !1
    }
    return function(view, params) {
        view.querySelector(".wizardSettingsForm").addEventListener("submit", onSubmit), view.addEventListener("viewshow", function() {
            document.querySelector(".skinHeader").classList.add("noHomeButtonHeader")
        }), view.addEventListener("viewhide", function() {
            document.querySelector(".skinHeader").classList.remove("noHomeButtonHeader")
        })
    }
});
