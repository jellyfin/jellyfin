define([], function () {
    'use strict';

    function parentWithAttribute(elem, name, value) {

        while ((value ? elem.getAttribute(name) !== value : !elem.getAttribute(name))) {
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

        while (tagNames.indexOf(elem.tagName || '') === -1) {
            elem = elem.parentNode;

            if (!elem) {
                return null;
            }
        }

        return elem;
    }

    function containsAnyClass(classList, classNames) {

        for (var i = 0, length = classNames.length; i < length; i++) {
            if (classList.contains(classNames[i])) {
                return true;
            }
        }
        return false;
    }

    function parentWithClass(elem, classNames) {

        // accept both string and array passed in
        if (!Array.isArray(classNames)) {
            classNames = [classNames];
        }

        while (!elem.classList || !containsAnyClass(elem.classList, classNames)) {
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

    var _animationEvent;
    function whichAnimationEvent() {

        if (_animationEvent) {
            return _animationEvent;
        }

        var t,
            el = document.createElement("div");
        var animations = {
            "animation": "animationend",
            "OAnimation": "oAnimationEnd",
            "MozAnimation": "animationend",
            "WebkitAnimation": "webkitAnimationEnd"
        };
        for (t in animations) {
            if (el.style[t] !== undefined) {
                _animationEvent = animations[t];
                return animations[t];
            }
        }

        _animationEvent = 'animationend';
        return _animationEvent;
    }

    function whichAnimationCancelEvent() {

        return whichAnimationEvent().replace('animationend', 'animationcancel').replace('AnimationEnd', 'AnimationCancel');
    }

    var _transitionEvent;
    function whichTransitionEvent() {
        if (_transitionEvent) {
            return _transitionEvent;
        }

        var t,
            el = document.createElement("div");
        var transitions = {
            "transition": "transitionend",
            "OTransition": "oTransitionEnd",
            "MozTransition": "transitionend",
            "WebkitTransition": "webkitTransitionEnd"
        };
        for (t in transitions) {
            if (el.style[t] !== undefined) {
                _transitionEvent = transitions[t];
                return transitions[t];
            }
        }

        _transitionEvent = 'transitionend';
        return _transitionEvent;
    }

    return {
        parentWithAttribute: parentWithAttribute,
        parentWithClass: parentWithClass,
        parentWithTag: parentWithTag,
        addEventListener: addEventListenerWithOptions,
        removeEventListener: removeEventListenerWithOptions,
        getWindowSize: getWindowSize,
        whichTransitionEvent: whichTransitionEvent,
        whichAnimationEvent: whichAnimationEvent,
        whichAnimationCancelEvent: whichAnimationCancelEvent
    };
});