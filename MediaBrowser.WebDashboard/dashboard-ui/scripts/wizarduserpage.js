define(["loading", "globalize", "dashboardcss", "emby-input", "emby-button", "emby-linkbutton"], function(loading, globalize) {
    "use strict";

    function getApiClient() {
        return ApiClient
    }

    function nextWizardPage() {
        Dashboard.navigate("wizardlibrary.html")
    }

    function onUpdateUserComplete(result) {
        if (loading.hide(), result.UserLinkResult) {
            var msgKey = result.UserLinkResult.IsPending ? "MessagePendingEmbyAccountAdded" : "MessageEmbyAccountAdded";
            Dashboard.alert({
                message: globalize.translate(msgKey),
                title: globalize.translate("HeaderEmbyAccountAdded"),
                callback: nextWizardPage
            })
        } else nextWizardPage()
    }

    function submit(form) {
        loading.show();
        var apiClient = getApiClient();
        apiClient.ajax({
            type: "POST",
            data: {
                Name: form.querySelector("#txtUsername").value
            },
            url: apiClient.getUrl("Startup/User"),
            dataType: "json"
        }).then(onUpdateUserComplete, function(response) {
            response && response.status;
        })
    }

    function onSubmit(e) {
        return submit(this), e.preventDefault(), !1
    }

    function onViewShow() {
        loading.show();
        var page = this,
            apiClient = getApiClient();
        apiClient.getJSON(apiClient.getUrl("Startup/User")).then(function(user) {
            page.querySelector("#txtUsername").value = user.Name || "", loading.hide()
        })
    }
    return function(view, params) {
        view.querySelector(".wizardUserForm").addEventListener("submit", onSubmit), view.addEventListener("viewshow", function() {
            document.querySelector(".skinHeader").classList.add("noHomeButtonHeader")
        }), view.addEventListener("viewhide", function() {
            document.querySelector(".skinHeader").classList.remove("noHomeButtonHeader")
        }), view.addEventListener("viewshow", onViewShow)
    }
});
