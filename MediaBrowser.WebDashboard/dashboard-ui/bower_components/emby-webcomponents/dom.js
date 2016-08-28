define([], function () {

    function parentWithAttribute(elem, name, value) {

        while ((value ? elem.getAttribute(name) != value : !elem.getAttribute(name))) {
            elem = elem.parentNode;

            if (!elem || !elem.getAttribute) {
                return null;
            }
        }

        return elem;
    }

    function parentWithTag(elem, tagNames) {

        // accept both string and array passed in
        if (!Array.isArray(tagNames)) {
            tagNames = [tagNames];
        }

        while (tagNames.indexOf(elem.tagName || '') == -1) {
            elem = elem.parentNode;

            if (!elem) {
                return null;
            }
        }

        return elem;
    }

    function parentWithClass(elem, className) {

        while (!elem.classList || !elem.classList.contains(className)) {
            elem = elem.parentNode;

            if (!elem) {
                return null;
            }
        }

        return elem;
    }

    var supportsCaptureOption = false;
    try {
        var opts = Object.defineProperty({}, 'capture', {
            get: function () {
                supportsCaptureOption = true;
            }
        });
        window.addEventListener("test", null, opts);
    } catch (e) { }

    function addEventListenerWithOptions(target, type, handler, options) {
        var optionsOrCapture = options;
        if (!supportsCaptureOption) {
            optionsOrCapture = options.capture;
        }
        target.addEventListener(type, handler, optionsOrCapture);
    }

    function removeEventListenerWithOptions(target, type, handler, options) {
        var optionsOrCapture = options;
        if (!supportsCaptureOption) {
            optionsOrCapture = options.capture;
        }
        target.removeEventListener(type, handler, optionsOrCapture);
    }

    var windowSize;
    var windowSizeEventsBound;
    function clearWindowSize() {
        windowSize = null;
    }

    function getWindowSize() {
        if (!windowSize) {
            windowSize = {
                innerHeight: window.innerHeight,
                innerWidth: window.innerWidth
            };

            if (!windowSizeEventsBound) {
                windowSizeEventsBound = true;
                addEventListenerWithOptions(window, "orientationchange", clearWindowSize, { passive: true });
                addEventListenerWithOptions(window, 'resize', clearWindowSize, { passive: true });
            }
        }

        return windowSize;
    }

    return {
        parentWithAttribute: parentWithAttribute,
        parentWithClass: parentWithClass,
        parentWithTag: parentWithTag,
        addEventListener: addEventListenerWithOptions,
        removeEventListener: removeEventListenerWithOptions,
        getWindowSize: getWindowSize
    };
});