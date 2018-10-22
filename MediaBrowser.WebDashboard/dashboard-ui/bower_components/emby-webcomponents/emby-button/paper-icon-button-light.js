define(["layoutManager", "css!./emby-button", "registerElement"], function(layoutManager) {
    "use strict";
    var EmbyButtonPrototype = Object.create(HTMLButtonElement.prototype);
    EmbyButtonPrototype.createdCallback = function() {
        this.classList.add("paper-icon-button-light"), layoutManager.tv && this.classList.add("icon-button-focusscale")
    }, document.registerElement("paper-icon-button-light", {
        prototype: EmbyButtonPrototype,
        extends: "button"
    })
});