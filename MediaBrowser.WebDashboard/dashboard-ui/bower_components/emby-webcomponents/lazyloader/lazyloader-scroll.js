define(['visibleinviewport', 'dom', 'browser'], function (visibleinviewport, dom, browser) {
    'use strict';

    var thresholdX;
    var thresholdY;

    var requestIdleCallback = window.requestIdleCallback || function (fn) {
        fn();
    };

    function resetThresholds() {

        var threshold = 0.3;

        thresholdX = screen.availWidth * threshold;
        thresholdY = screen.availHeight * threshold;
    }

    function resetThresholdsOnTimer() {

        setTimeout(resetThresholds, 500);
    }

    if (browser.iOS) {
        dom.addEventListener(window, "orientationchange", resetThresholdsOnTimer, { passive: true });
        dom.addEventListener(window, 'resize', resetThresholdsOnTimer, { passive: true });
    } else {
        dom.addEventListener(window, "orientationchange", resetThresholds, { passive: true });
        dom.addEventListener(window, 'resize', resetThresholds, { passive: true });
    }
    resetThresholds();

    function isVisible(elem) {
        return visibleinviewport(elem, true, thresholdX, thresholdY);
    }

    var wheelEvent = (document.implementation.hasFeature('Event.wheel', '3.0') ? 'wheel' : 'mousewheel');
    var self = {};

    function cancelAll(tokens) {
        for (var i = 0, length = tokens.length; i < length; i++) {

            tokens[i] = true;
        }
    }

    function unveilElementsInternal(instance, callback) {

        var unveiledElements = [];
        var cancellationTokens = [];
        var loadedCount = 0;

        function unveilInternal(tokenIndex) {

            var anyFound = false;
            var out = false;

            var elements = instance.elements;
            // TODO: This out construct assumes left to right, top to bottom

            for (var i = 0, length = elements.length; i < length; i++) {

                if (cancellationTokens[tokenIndex]) {
                    return;
                }
                if (unveiledElements[i]) {
                    continue;
                }
                var elem = elements[i];
                if (!out && isVisible(elem)) {
                    anyFound = true;
                    unveiledElements[i] = true;
                    callback(elem);
                    loadedCount++;
                } else {

                    if (anyFound) {
                        out = true;
                    }
                }
            }

            if (loadedCount >= elements.length) {
                dom.removeEventListener(document, 'focus', unveil, {
                    capture: true,
                    passive: true
                });
                dom.removeEventListener(document, 'scroll', unveil, {
                    capture: true,
                    passive: true
                });
                dom.removeEventListener(document, wheelEvent, unveil, {
                    capture: true,
                    passive: true
                });
                dom.removeEventListener(window, 'resize', unveil, {
                    capture: true,
                    passive: true
                });
            }
        }

        function unveil() {

            cancelAll(cancellationTokens);

            var index = cancellationTokens.length;
            cancellationTokens.length++;

            setTimeout(function () {
                unveilInternal(index);
            }, 1);
        }

        dom.addEventListener(document, 'focus', unveil, {
            capture: true,
            passive: true
        });
        dom.addEventListener(document, 'scroll', unveil, {
            capture: true,
            passive: true
        });
        dom.addEventListener(document, wheelEvent, unveil, {
            capture: true,
            passive: true
        });
        dom.addEventListener(window, 'resize', unveil, {
            capture: true,
            passive: true
        });

        unveil();
    }

    function LazyLoader(options) {

        this.options = options;
    }

    LazyLoader.prototype.createObserver = function () {

        unveilElementsInternal(this, this.options.callback);
        this.observer = 1;
    };

    LazyLoader.prototype.addElements = function (elements) {

        this.elements = this.elements || [];

        for (var i = 0, length = elements.length; i < length; i++) {
            this.elements.push(elements[i]);
        }

        var observer = this.observer;

        if (!observer) {
            this.createObserver();
        }

    };

    LazyLoader.prototype.destroyObserver = function (elements) {

    };

    LazyLoader.prototype.destroy = function (elements) {

        this.destroyObserver();
        this.options = null;
    };

    function unveilElements(elements, root, callback) {

        if (!elements.length) {
            return;
        }
        var lazyLoader = new LazyLoader({
            callback: callback
        });
        lazyLoader.addElements(elements);
    }

    LazyLoader.lazyChildren = function (elem, callback) {

        unveilElements(elem.getElementsByClassName('lazy'), elem, callback);
    };

    return LazyLoader;
});