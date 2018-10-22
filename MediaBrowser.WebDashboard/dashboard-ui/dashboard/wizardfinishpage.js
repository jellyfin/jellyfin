define(["loading"], function(loading) {
    "use strict";

    function onFinish() {
        loading.show(), ApiClient.ajax({
            url: ApiClient.getUrl("Startup/Complete"),
            type: "POST"
        }).then(function() {
            Dashboard.navigate("dashboard.html"), loading.hide()
        })
    }
    return function(view, params) {
        view.querySelector(".btnWizardNext").addEventListener("click", onFinish), view.addEventListener("viewshow", function() {
            document.querySelector(".skinHeader").classList.add("noHomeButtonHeader")
        }), view.addEventListener("viewhide", function() {
            document.querySelector(".skinHeader").classList.remove("noHomeButtonHeader")
        })
    }
});