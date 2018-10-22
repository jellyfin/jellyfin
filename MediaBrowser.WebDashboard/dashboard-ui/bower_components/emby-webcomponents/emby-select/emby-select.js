define(["layoutManager", "browser", "actionsheet", "css!./emby-select", "registerElement"], function(layoutManager, browser, actionsheet) {
    "use strict";

    function enableNativeMenu() {
        return !(!browser.edgeUwp && !browser.xboxOne) || !(browser.tizen || browser.orsay || browser.web0s) && (!!browser.tv || !layoutManager.tv)
    }

    function triggerChange(select) {
        var evt = document.createEvent("HTMLEvents");
        evt.initEvent("change", !1, !0), select.dispatchEvent(evt)
    }

    function setValue(select, value) {
        select.value = value
    }

    function showActionSheet(select) {
        var labelElem = getLabel(select),
            title = labelElem ? labelElem.textContent || labelElem.innerText : null;
        actionsheet.show({
            items: select.options,
            positionTo: select,
            title: title
        }).then(function(value) {
            setValue(select, value), triggerChange(select)
        })
    }

    function getLabel(select) {
        for (var elem = select.previousSibling; elem && "LABEL" !== elem.tagName;) elem = elem.previousSibling;
        return elem
    }

    function onFocus(e) {
        var label = getLabel(this);
        label && label.classList.add("selectLabelFocused")
    }

    function onBlur(e) {
        var label = getLabel(this);
        label && label.classList.remove("selectLabelFocused")
    }

    function onMouseDown(e) {
        e.button || enableNativeMenu() || (e.preventDefault(), showActionSheet(this))
    }

    function onKeyDown(e) {
        switch (e.keyCode) {
            case 13:
                return void(enableNativeMenu() || (e.preventDefault(), showActionSheet(this)));
            case 37:
            case 38:
            case 39:
            case 40:
                return void(layoutManager.tv && e.preventDefault())
        }
    }
    var EmbySelectPrototype = Object.create(HTMLSelectElement.prototype),
        inputId = 0;
    EmbySelectPrototype.createdCallback = function() {
        this.id || (this.id = "embyselect" + inputId, inputId++), browser.firefox || (this.classList.add("emby-select-withcolor"), layoutManager.tv && this.classList.add("emby-select-tv-withcolor")), layoutManager.tv && this.classList.add("emby-select-focusscale"), this.addEventListener("mousedown", onMouseDown), this.addEventListener("keydown", onKeyDown), this.addEventListener("focus", onFocus), this.addEventListener("blur", onBlur)
    }, EmbySelectPrototype.attachedCallback = function() {
        if (!this.classList.contains("emby-select")) {
            this.classList.add("emby-select");
            var label = this.ownerDocument.createElement("label");
            label.innerHTML = this.getAttribute("label") || "", label.classList.add("selectLabel"), label.htmlFor = this.id, this.parentNode.insertBefore(label, this), this.classList.contains("emby-select-withcolor") && this.parentNode.insertAdjacentHTML("beforeend", '<div class="selectArrowContainer"><div style="visibility:hidden;">0</div><i class="selectArrow md-icon">&#xE313;</i></div>')
        }
    }, EmbySelectPrototype.setLabel = function(text) {
        this.parentNode.querySelector("label").innerHTML = text
    }, document.registerElement("emby-select", {
        prototype: EmbySelectPrototype,
        extends: "select"
    })
});