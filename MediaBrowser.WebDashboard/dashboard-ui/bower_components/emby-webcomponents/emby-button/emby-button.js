define(["browser", "dom", "layoutManager", "shell", "appRouter", "apphost", "css!./emby-button", "registerElement"], function(browser, dom, layoutManager, shell, appRouter, appHost) {
    "use strict";

    function openPremiumInfo() {
        require(["registrationServices"], function(registrationServices) {
            registrationServices.showPremiereInfo()
        })
    }

    function onAnchorClick(e) {
        var href = this.getAttribute("href") || "";
        "#" !== href ? this.getAttribute("target") ? -1 === href.indexOf("emby.media/premiere") || appHost.supports("externalpremium") ? appHost.supports("targetblank") || (e.preventDefault(), shell.openUrl(href)) : (e.preventDefault(), openPremiumInfo()) : appRouter.handleAnchorClick(e) : e.preventDefault()
    }
    var EmbyButtonPrototype = Object.create(HTMLButtonElement.prototype),
        EmbyLinkButtonPrototype = Object.create(HTMLAnchorElement.prototype);
    return EmbyButtonPrototype.createdCallback = function() {
        this.classList.contains("emby-button") || (this.classList.add("emby-button"), browser.firefox && this.classList.add("button-link-inline"), layoutManager.tv && ("false" !== this.getAttribute("data-focusscale") && this.classList.add("emby-button-focusscale"), this.classList.add("emby-button-tv")))
    }, EmbyButtonPrototype.attachedCallback = function() {
        "A" === this.tagName && (dom.removeEventListener(this, "click", onAnchorClick, {}), dom.addEventListener(this, "click", onAnchorClick, {}), "true" === this.getAttribute("data-autohide") && (appHost.supports("externallinks") ? this.classList.remove("hide") : this.classList.add("hide")))
    }, EmbyButtonPrototype.detachedCallback = function() {
        dom.removeEventListener(this, "click", onAnchorClick, {})
    }, EmbyLinkButtonPrototype.createdCallback = EmbyButtonPrototype.createdCallback, EmbyLinkButtonPrototype.attachedCallback = EmbyButtonPrototype.attachedCallback, document.registerElement("emby-button", {
        prototype: EmbyButtonPrototype,
        extends: "button"
    }), document.registerElement("emby-linkbutton", {
        prototype: EmbyLinkButtonPrototype,
        extends: "a"
    }), EmbyButtonPrototype
});