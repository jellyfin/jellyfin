define(["dom", "emby-linkbutton"], function(dom) {
    "use strict";

    function onSubmit(e) {
        return dom.parentWithClass(this, "page").querySelector(".chkAccept").checked ? Dashboard.navigate("wizardfinish.html") : Dashboard.alert({
            message: Globalize.translate("MessagePleaseAcceptTermsOfServiceBeforeContinuing"),
            title: ""
        }), e.preventDefault(), !1
    }
    return function(view, params) {
        view.querySelector(".wizardAgreementForm").addEventListener("submit", onSubmit), view.addEventListener("viewshow", function() {
            document.querySelector(".skinHeader").classList.add("noHomeButtonHeader")
        }), view.addEventListener("viewhide", function() {
            document.querySelector(".skinHeader").classList.remove("noHomeButtonHeader")
        })
    }
});