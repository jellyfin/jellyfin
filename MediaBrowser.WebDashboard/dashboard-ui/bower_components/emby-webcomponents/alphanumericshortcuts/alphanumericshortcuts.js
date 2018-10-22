define(["dom", "focusManager"], function(dom, focusManager) {
    "use strict";

    function onKeyDown(e) {
        if (!e.ctrlKey && !e.shiftKey && !e.altKey) {
            var key = e.key,
                chr = key ? alphanumeric(key) : null;
            chr && (chr = chr.toString().toUpperCase(), 1 === chr.length && (currentDisplayTextContainer = this.options.itemsContainer, onAlphanumericKeyPress(e, chr)))
        }
    }

    function alphanumeric(value) {
        var letterNumber = /^[0-9a-zA-Z]+$/;
        return value.match(letterNumber)
    }

    function ensureInputDisplayElement() {
        inputDisplayElement || (inputDisplayElement = document.createElement("div"), inputDisplayElement.classList.add("alphanumeric-shortcut"), inputDisplayElement.classList.add("hide"), document.body.appendChild(inputDisplayElement))
    }

    function clearAlphaNumericShortcutTimeout() {
        alpanumericShortcutTimeout && (clearTimeout(alpanumericShortcutTimeout), alpanumericShortcutTimeout = null)
    }

    function resetAlphaNumericShortcutTimeout() {
        clearAlphaNumericShortcutTimeout(), alpanumericShortcutTimeout = setTimeout(onAlphanumericShortcutTimeout, 2e3)
    }

    function onAlphanumericKeyPress(e, chr) {
        currentDisplayText.length >= 3 || (ensureInputDisplayElement(), currentDisplayText += chr, inputDisplayElement.innerHTML = currentDisplayText, inputDisplayElement.classList.remove("hide"), resetAlphaNumericShortcutTimeout())
    }

    function onAlphanumericShortcutTimeout() {
        var value = currentDisplayText,
            container = currentDisplayTextContainer;
        currentDisplayText = "", currentDisplayTextContainer = null, inputDisplayElement.innerHTML = "", inputDisplayElement.classList.add("hide"), clearAlphaNumericShortcutTimeout(), selectByShortcutValue(container, value)
    }

    function selectByShortcutValue(container, value) {
        value = value.toUpperCase();
        var focusElem;
        "#" === value && (focusElem = container.querySelector("*[data-prefix]")), focusElem || (focusElem = container.querySelector("*[data-prefix^='" + value + "']")), focusElem && focusManager.focus(focusElem)
    }

    function AlphaNumericShortcuts(options) {
        this.options = options;
        var keyDownHandler = onKeyDown.bind(this);
        dom.addEventListener(window, "keydown", keyDownHandler, {
            passive: !0
        }), this.keyDownHandler = keyDownHandler
    }
    var inputDisplayElement, currentDisplayTextContainer, alpanumericShortcutTimeout, currentDisplayText = "";
    return AlphaNumericShortcuts.prototype.destroy = function() {
        var keyDownHandler = this.keyDownHandler;
        keyDownHandler && (dom.removeEventListener(window, "keydown", keyDownHandler, {
            passive: !0
        }), this.keyDownHandler = null), this.options = null
    }, AlphaNumericShortcuts
});